using System.Runtime.CompilerServices;
using MantIA.BE.Auditoria;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MantIA.DAL.Bitacora;

/// <summary>
/// Bitácora sobre MongoDB.
///
/// <para><b>Por que Mongo y no PostgreSQL:</b> el volumen es de otro orden que el del dominio
/// —cada accion de cada usuario deja un evento—, la escritura es secuencial y de solo agregado, y
/// el esquema varia entre tipos de evento. Meterla en la base transaccional haria competir el log
/// con las operaciones que esta registrando.</para>
///
/// <para><b>El numero se asigna despues de insertar, nunca antes.</b> Reservar un numero y despues
/// escribir deja un hueco permanente cada vez que algo se cancela o el proceso muere en el medio: el
/// numero quedo consumido por un registro que no existe. Aca el orden es al reves —primero el hecho,
/// despues el numero— y por construccion un numero solo se escribe sobre un documento que ya esta
/// guardado. La numeracion no puede tener huecos porque nunca se entrega por adelantado.</para>
///
/// <para>La contrapartida es que numerar es un paso corto y serializado por cadena, en lugar de una
/// operacion paralela. No importa: esta fuera del camino critico. Lo que tiene que escalar es la
/// escritura del evento, y esa sigue siendo un insert sin coordinacion con nadie.</para>
/// </summary>
public class RepositorioBitacoraMongo : IRepositorioBitacora
{
    /// <summary>Un evento con secuencia cero existe pero todavia no fue numerado.</summary>
    private const long SinNumerar = 0;

    private readonly IMongoCollection<EventoBitacora> _eventos;
    private readonly OpcionesMongo _opciones;

    public RepositorioBitacoraMongo(IMongoClient cliente, IOptions<OpcionesMongo> opciones)
    {
        _opciones = opciones.Value;
        _eventos = cliente
            .GetDatabase(_opciones.BaseDeDatos)
            .GetCollection<EventoBitacora>(_opciones.Coleccion);
    }

    public async Task<EventoBitacora?> UltimoAsync(string cadena, CancellationToken ct = default) =>
        await _eventos
            .Find(e => e.Cadena == cadena && e.Secuencia > SinNumerar)
            .SortByDescending(e => e.Secuencia)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task<EventoBitacora> AgregarAsync(
        EventoBitacora evento,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default)
    {
        // 1. El hecho, primero y sin nada mas. Es la unica parte que esta en el camino critico de
        //    la operacion de negocio, y no coordina con nadie.
        evento.Secuencia = SinNumerar;
        evento.Sellado = false;
        evento.Hash = null;
        evento.HashAnterior = null;

        await _eventos.InsertOneAsync(evento, options: null, ct);

        // 2. Numerar y sellar lo que ya esta guardado. Si falla, el evento igual quedo registrado
        //    y lo toma el trabajo de mantenimiento en la proxima vuelta.
        await NumerarYSellarAsync(evento.Cadena, sellar, ct);

        return await _eventos.Find(e => e.Id == evento.Id).FirstOrDefaultAsync(ct) ?? evento;
    }

    /// <summary>
    /// Asigna numeros consecutivos a los eventos ya guardados de una cadena y los encadena por hash.
    ///
    /// <para>Los toma en orden de ocurrencia. La condicion <c>Secuencia == 0</c> en la actualizacion
    /// es lo que hace segura la carrera: si dos pedidos numeran a la vez, el segundo no encuentra el
    /// documento en ese estado y vuelve a empezar desde el ultimo numerado real.</para>
    /// </summary>
    public async Task<int> NumerarYSellarAsync(
        string cadena,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default)
    {
        var numerados = 0;

        for (var reintento = 0; reintento < _opciones.ReintentosNumeracion; reintento++)
        {
            var ultimo = await UltimoAsync(cadena, ct);
            var siguiente = (ultimo?.Secuencia ?? 0) + 1;
            var hashAnterior = ultimo?.Hash;

            var pendientes = await _eventos
                .Find(e => e.Cadena == cadena && e.Secuencia == SinNumerar)
                .Sort(Builders<EventoBitacora>.Sort
                    .Ascending(e => e.Fecha)
                    .Ascending(e => e.Id))     // desempate estable si dos caen en el mismo instante
                .Limit(_opciones.MaximoNumeradoPorPasada)
                .ToListAsync(ct);

            if (pendientes.Count == 0) return numerados;

            var colision = false;

            foreach (var pendiente in pendientes)
            {
                pendiente.Secuencia = siguiente;
                pendiente.HashAnterior = hashAnterior;
                var hash = sellar(pendiente, hashAnterior);

                try
                {
                    var resultado = await _eventos.UpdateOneAsync(
                        Builders<EventoBitacora>.Filter.And(
                            Builders<EventoBitacora>.Filter.Eq(e => e.Id, pendiente.Id),
                            Builders<EventoBitacora>.Filter.Eq(e => e.Secuencia, SinNumerar)),
                        Builders<EventoBitacora>.Update
                            .Set(e => e.Secuencia, siguiente)
                            .Set(e => e.HashAnterior, hashAnterior)
                            .Set(e => e.Hash, hash)
                            .Set(e => e.Sellado, true),
                        cancellationToken: ct);

                    if (resultado.ModifiedCount == 0)
                    {
                        // Otro pedido lo numero en el intermedio. Se recalcula todo desde el estado
                        // real en lugar de seguir con numeros que ya no valen.
                        colision = true;
                        break;
                    }

                    siguiente++;
                    hashAnterior = hash;
                    numerados++;
                }
                catch (MongoWriteException ex)
                    when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    // El indice unico (cadena, secuencia) rechazo el numero: otro se lo llevo.
                    // Misma respuesta que arriba, releer y reintentar.
                    colision = true;
                    break;
                }
            }

            if (!colision) return numerados;
        }

        // Contencion anormal. No se pierde nada: los eventos estan guardados y sin numerar, y la
        // proxima pasada del mantenimiento los toma.
        return numerados;
    }

    /// <summary>Cadenas con eventos guardados y todavia sin numerar. Lo consulta el mantenimiento.</summary>
    public async Task<IReadOnlyList<string>> CadenasConPendientesAsync(CancellationToken ct = default) =>
        await _eventos.Distinct(
            new StringFieldDefinition<EventoBitacora, string>(nameof(EventoBitacora.Cadena)),
            Builders<EventoBitacora>.Filter.Eq(e => e.Secuencia, SinNumerar),
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
