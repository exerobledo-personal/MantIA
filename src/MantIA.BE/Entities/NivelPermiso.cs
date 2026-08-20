using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Eje "Nivel" del modelo de seguridad. Cada empresa define los suyos; el sistema siembra
/// Jr y Sr al dar de alta el tenant. Es independiente del rol: existe un Supervisor Jr y un
/// Supervisor Sr, pero un "Gerente Jr" no significa nada.
/// </summary>
public class NivelPermiso : TenantEntity, IBajaLogica
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    /// <summary>Orden relativo. Un nivel superior no deberia tener menos permisos que uno inferior.</summary>
    public int Jerarquia { get; set; }

    public DateTimeOffset? FechaBaja { get; set; }
}
