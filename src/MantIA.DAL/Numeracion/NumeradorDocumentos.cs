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
/// <para><b>Sobre el hueco en la numeración.</b> Si la transacción que pidió el número después
/// falla, ese número queda sin usar y la serie tiene un salto. Es el comportamiento correcto: la
/// alternativa —devolver el número al contador— reintroduce exactamente la carrera que se estaba
/// evitando. Una numeración con huecos es normal en cualquier sistema con comprobantes; una
/// numeración con duplicados no lo es.</para>
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
