using MantIA.BE.Entities;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.DAL.Numeracion;

public interface INumeradorDocumentos
{
    /// <summary>Entrega el proximo numero legible: <c>OT-2026-00001</c>.</summary>
    Task<string> SiguienteAsync(TipoDocumento tipo, int? anio = null, CancellationToken ct = default);
}

/// <summary>
/// Asigna los números legibles de documentos. El número lo entrega PostgreSQL en una sola
/// operación atómica; la aplicación no lo calcula.
///
/// <para><b>Por qué SQL directo y no EF.</b> Con EF habría que leer el contador, sumarle uno y
/// guardar: entre la lectura y la escritura entra otra transacción y las dos entregan el mismo
/// número. El <c>UPDATE ... RETURNING</c> lo resuelve la base en una sola operación, tomando el
/// bloqueo de fila el tiempo que dura, que es del orden de microsegundos.</para>
///
/// <para><b>Se llama DENTRO de la transacción que crea el documento, nunca antes.</b> Esa es la
/// regla que hace que la serie no tenga huecos: el contador es una tabla, no una secuencia, así que
/// su incremento participa de la transacción. Si la carga se cancela o algo falla después de pedir
/// el número, el <c>ROLLBACK</c> deshace también el incremento y el número vuelve a estar
/// disponible. Nunca se reserva un número para un registro que todavía no existe.</para>
///
/// <para>Una secuencia de PostgreSQL <c>no</c> serviría para esto: las secuencias quedan fuera de la
/// transacción a propósito, precisamente para no serializar a quien las usa, y por eso dejan huecos.
/// La tabla es más lenta —toma el bloqueo de fila mientras dura la transacción— y a cambio la serie
/// es continua. Para un comprobante que se muestra al cliente, esa es la propiedad que importa.</para>
/// </summary>
public class NumeradorDocumentos : INumeradorDocumentos
{
    private readonly MantIADbContext _db;
    private readonly ICurrentTenant _tenant;

    public NumeradorDocumentos(MantIADbContext db, ICurrentTenant tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> SiguienteAsync(
        TipoDocumento tipo, int? anio = null, CancellationToken ct = default)
    {
        if (_tenant.EmpresaId is not { } empresaId)
            throw new InvalidOperationException(
                "No se puede numerar un documento sin contexto de empresa.");

        var ejercicio = anio ?? DateTimeOffset.UtcNow.Year;
        var tipoTexto = tipo.ToString();

        // Un solo viaje: inserta el contador si no existe y lo incrementa si ya estaba, devolviendo
        // el valor nuevo. ON CONFLICT usa el indice unico (empresa, tipo, anio).
        var numero = await _db.Database
            .SqlQuery<long>($"""
                INSERT INTO contador_documento
                    (id, empresa_id, tipo, anio, ultimo, fecha_creacion)
                VALUES
                    (gen_random_uuid(), {empresaId}, {tipoTexto}, {ejercicio}, 1, now())
                ON CONFLICT (empresa_id, tipo, anio)
                DO UPDATE SET ultimo = contador_documento.ultimo + 1
                RETURNING ultimo
                """)
            .SingleAsync(ct);

        return ContadorDocumento.Formatear(tipo, ejercicio, numero);
    }
}
