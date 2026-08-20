using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Activo industrial de una empresa. Marca y modelo NO se duplican aca: se obtienen de la
/// ficha compartida del catalogo, que es lo que permite que el conocimiento se acumule
/// entre clientes.
/// </summary>
public class Maquina : TenantEntity, IBajaLogica
{
    /// <summary>Codigo interno legible (MAQ-001).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? NumeroSerie { get; set; }

    public Guid PlantaId { get; set; }
    public Planta? Planta { get; set; }

    /// <summary>Ubicacion dentro de la planta: linea, sector o area.</summary>
    public string? LineaSector { get; set; }

    public Guid CatalogoMaquinaId { get; set; }
    public CatalogoMaquina? CatalogoMaquina { get; set; }

    /// <summary>Impacto de su parada. Prioriza las alertas y las recomendaciones.</summary>
    public Criticidad Criticidad { get; set; } = Criticidad.Media;

    public EstadoMaquina Estado { get; set; } = EstadoMaquina.Operativa;

    /// <summary>Horas acumuladas. Base de calculo de los intervalos de mantenimiento.</summary>
    public int HorasOperacion { get; set; }

    public DateTimeOffset? FechaBaja { get; set; }

    public ICollection<MaquinaRepuesto> Repuestos { get; set; } = [];
    public ICollection<OrdenTrabajo> Ordenes { get; set; } = [];
}
