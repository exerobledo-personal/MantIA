using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Linea de tiempo de operaciones sobre un reporte. Es append-only: cada generacion,
/// modificacion, exportacion o eliminacion agrega una fila y ninguna se borra.
/// </summary>
public class ReporteHistorial : TenantEntity
{
    public Guid ReporteId { get; set; }
    public Reporte? Reporte { get; set; }

    /// <summary>Generacion, Modificacion, Exportacion, Eliminacion.</summary>
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }

    public Guid UsuarioId { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;
}
