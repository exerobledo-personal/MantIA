using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Plan de suscripcion. Compartido: lo define MantIA, no el cliente.</summary>
public class Plan : CatalogEntity, IBajaLogica
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int MaxMaquinas { get; set; }
    public int MaxUsuarios { get; set; }
    public int MaxPlantas { get; set; }

    public decimal PrecioMensual { get; set; }
    public string Moneda { get; set; } = "ARS";

    public DateTimeOffset? FechaBaja { get; set; }
}
