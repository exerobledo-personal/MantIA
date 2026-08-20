using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Reporte operativo generado bajo demanda. La eliminacion es SIEMPRE logica: el
/// administrador de empresa tiene que poder ver quien lo genero, quien lo modifico y
/// quien lo elimino, incluso despues de eliminado.
/// </summary>
public class Reporte : TenantEntity
{
    public string Nombre { get; set; } = string.Empty;
    public TipoReporte Tipo { get; set; }
    public EstadoReporte Estado { get; set; } = EstadoReporte.Activo;

    /// <summary>Filtros con los que se genero, serializados. Permiten reproducirlo igual.</summary>
    public string? FiltrosJson { get; set; }

    public DateTimeOffset? PeriodoDesde { get; set; }
    public DateTimeOffset? PeriodoHasta { get; set; }

    public ICollection<ReporteHistorial> Historial { get; set; } = [];
}
