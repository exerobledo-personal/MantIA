using MantIA.BE.Common;

namespace MantIA.BE.Entities;

// Nivel Jr/Sr configurable por empresa. Lleva tenant.
public class NivelPermiso : TenantEntity
{
    public string Nombre { get; set; } = string.Empty;   // "Jr" | "Sr"
    public string? Descripcion { get; set; }
}