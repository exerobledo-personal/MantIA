using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Empresa cliente. Cada una es un tenant con su espacio de datos aislado.</summary>
public class Empresa : BaseEntity, IBajaLogica
{
    public string RazonSocial { get; set; } = string.Empty;
    /// <summary>Dominio corporativo. Restringe el acceso con Google al personal de la empresa.</summary>
    public string Dominio { get; set; } = string.Empty;

    /// <summary>Identificador de organizacion en Auth0. Distinto de la clave primaria interna.</summary>
    public string TenantId { get; set; } = string.Empty;

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    /// <summary>Cupo contratado. Puede diferir del tope del plan por acuerdo comercial.</summary>
    public int MaxMaquinasHabilitadas { get; set; }

    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTimeOffset? FechaBaja { get; set; }

    public ICollection<Planta> Plantas { get; set; } = [];
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
