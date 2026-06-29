using MantIA.BE.Common;

namespace MantIA.BE.Entities;

public class Empresa : BaseEntity
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Dominio { get; set; } = string.Empty;

    // Identificador de organización provisto por Auth0 (distinto de la PK interna)
    public string TenantId { get; set; } = string.Empty;

    public Guid? PlanId { get; set; }              // FK a Plan (entidad aún no creada; queda nullable por ahora)
    public int MaxMaquinasHabilitadas { get; set; }
    public string Estado { get; set; } = "activa";
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaBaja { get; set; }
}