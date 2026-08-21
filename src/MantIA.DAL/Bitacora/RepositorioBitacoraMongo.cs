using System.Runtime.CompilerServices;
using MantIA.BE.Auditoria;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MantIA.DAL.Bitacora;

/// <summary>Contador atómico por cadena. Es la base que asigna los números, no la aplicación.</summary>
public class ContadorCadena
{
    public string Id { get; set; } = string.Empty;   // la cadena
    public long Secuencia { get; set; }
}

/// <summary>
/// Bitácora sobre MongoDB.
///
/// <para><b>El número de orden lo asigna la base, no nosotros.</b> Un contador con
/// <c>$inc</c> atómico entrega el siguiente número en una sola operación: no hay leer-y-después-
/// escribir, no hay carrera y no hay reintentos por colisión. Escala con la concurrencia en lugar
/// de degradarse con ella.</para>
///
/// <para><b>La tensión que eso crea, y cómo se resuelve.</b> Una cadena de hashes es sequencial por
/// naturaleza: cada eslabón contiene el hash del anterior. Si el número se entrega en paralelo, el
/// evento número 7 puede llegar antes que el 6, y no puede sellarse hasta que el 6 exista. Se
/// resuelve escribiendo en dos tiempos:</para>
/// <list type="number">
/// <item><b>Se guarda el hecho</b> con su número, sin sellar. La acción ya quedó registrada.</item>
/// <item><b>Se sella</b> recorriendo desde el último eslabón cerrado hacia adelante, mientras los
/// números estén completos. Lo hace el mismo pedido que escribió, así que en operación normal el
/// evento queda sellado en el mismo instante.</item>
/// </list>
///
/// <para>El sellado es idempotente y con carrera segura: la actualización solo aplica si el evento
/// sigue sin sellar, de modo que dos pedidos que intenten sellar el mismo eslabón no se pisan.</para>
/// </summary>
public class RepositorioBitacoraMongo : IRepositorioBitacora
{
    private readonly IMongoCollection<EventoBitacora> _eventos;
    private readonly IMongoCollection<ContadorCadena> _contadores;
    private readonly OpcionesMongo _opciones;

    public RepositorioBitacoraMongo(IMongoClient cliente, IOptions<OpcionesMongo> opciones)
    {
        _opciones = opciones.Value;
        var baseDatos = cliente.GetDatabase(_opciones.BaseDeDatos);
        _eventos = baseDatos.GetCollection<EventoBitacora>(_opciones.Coleccion);
        _contadores = baseDatos.GetCollection<ContadorCadena>(_opciones.ColeccionContadores);
    }

    public async Task<EventoBitacora?> UltimoAsync(string cadena, CancellationToken ct = default) =>
        await _eventos
            .Find(e => e.Cadena == cadena)
            .SortByDescending(e => e.Secuencia)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task<EventoBitacora> AgregarAsync(
        EventoBitacora evento,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default)
    {
        evento.Secuencia = await SiguienteNumeroAsync(evento.Cadena, ct);
        evento.Sellado = false;
        evento.Hash = null;
        evento.HashAnterior = null;

        await _eventos.InsertOneAsync(evento, options: null, ct);

        // Sellar es "mejor esfuerzo dentro del mismo pedido": si otro evento anterior todavia no
        // llego, este queda pendiente y lo sella el proximo que pase o el trabajo de fondo. El
        // hecho ya esta registrado; lo que falta es la prueba de que nadie lo movio.
        await SellarPendientesAsync(evento.Cadena, sellar, ct);

        return await _eventos.Find(e => e.Id == evento.Id).FirstOrDefaultAsync(ct) ?? evento;
    }

    /// <summary>
    /// Un solo viaje a la base, atomico. <c>IsUpsert</c> crea el contador la primera vez, asi que
    /// no hace falta sembrar nada al dar de alta una empresa.
    /// </summary>
    private async Task<long> SiguienteNumeroAsync(string cadena, CancellationToken ct)
    {
        var contador = await _contadores.FindOneAndUpdateAsync<ContadorCadena>(
            Builders<ContadorCadena>.Filter.Eq(c => c.Id, cadena),
            Builders<ContadorCadena>.Update.Inc(c => c.Secuencia, 1),
            new FindOneAndUpdateOptions<ContadorCadena>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            ct);

        return contador.Secuencia;
    }

    /// <summary>
    /// Cierra los eslabones que se puedan cerrar, desde el ultimo sellado hacia adelante.
    /// Se corta al primer hueco: sellar salteando un numero produciria una cadena que no verifica.
    /// </summary>
    public async Task<int> SellarPendientesAsync(
        string cadena,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default)
    {
        var ultimoSellado = await _eventos
            .Find(e => e.Cadena == cadena && e.Sellado)
            .SortByDescending(e => e.Secuencia)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        var hashAnterior = ultimoSellado?.Hash;
        var siguiente = (ultimoSellado?.Secuencia ?? 0) + 1;
        var sellados = 0;

        // El tope evita que un pedido cualquiera termine sellando meses de atraso. Lo que quede
        // afuera lo toma el proximo, o el trabajo de fondo.
        for (var i = 0; i < _opciones.MaximoSelladoPorPasada; i++, siguiente++)
        {
            var evento = await _eventos
                .Find(e => e.Cadena == cadena && e.Secuencia == siguiente)
                .FirstOrDefaultAsync(ct);

            if (evento is null) break;          // hueco: alguien tomo el numero y todavia no inserto

            if (evento.Sellado)                  // otro pedido gano la carrera; se sigue desde su hash
            {
                hashAnterior = evento.Hash;
                continue;
            }

            evento.HashAnterior = hashAnterior;
            var hash = sellar(evento, hashAnterior);

            var resultado = await _eventos.UpdateOneAsync(
                Builders<EventoBitacora>.Filter.And(
                    Builders<EventoBitacora>.Filter.Eq(e => e.Id, evento.Id),
                    Builders<EventoBitacora>.Filter.Eq(e => e.Sellado, false)),
                Builders<EventoBitacora>.Update
                    .Set(e => e.HashAnterior, hashAnterior)
                    .Set(e => e.Hash, hash)
                    .Set(e => e.Sellado, true),
                cancellationToken: ct);

            if (resultado.ModifiedCount == 0)
            {
                // Lo sello otro en el intermedio. Se relee para continuar con SU hash, no con el
                // que acabamos de calcular: los dos son validos pero solo uno esta guardado.
                var recargado = await _eventos.Find(e => e.Id == evento.Id).FirstOrDefaultAsync(ct);
                hashAnterior = recargado?.Hash ?? hash;
                continue;
            }

            hashAnterior = hash;
            sellados++;
        }

        return sellados;
    }

    /// <summary>Cadenas con eslabones sin sellar. Lo consulta el trabajo de fondo.</summary>
    public async Task<IReadOnlyList<string>> CadenasConPendientesAsync(CancellationToken ct = default) =>
        await _eventos.Distinct(
            new StringFieldDefinition<EventoBitacora, string>(nameof(EventoBitacora.Cadena)),
            Builders<EventoBitacora>.Filter.Eq(e => e.Sellado, false),
            cancellationToken: ct)
            .ToListAsync(ct);

    public async IAsyncEnumerable<EventoBitacora> RecorrerAsync(
        string cadena, long desde, long hasta,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var filtro = Builders<EventoBitacora>.Filter.And(
            Builders<EventoBitacora>.Filter.Eq(e => e.Cadena, cadena),
            Builders<EventoBitacora>.Filter.Gte(e => e.Secuencia, desde),
            Builders<EventoBitacora>.Filter.Lte(e => e.Secuencia, hasta));

        // Cursor y no lista: verificar la cadena de un tenant con anios de historia no puede
        // depender de que entre entera en memoria.
        using var cursor = await _eventos
            .Find(filtro)
            .Sort(Builders<EventoBitacora>.Sort.Ascending(e => e.Secuencia))
            .ToCursorAsync(ct);

        while (await cursor.MoveNextAsync(ct))
            foreach (var evento in cursor.Current)
                yield return evento;
    }
}
