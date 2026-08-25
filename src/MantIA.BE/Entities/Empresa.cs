using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Empresa cliente. Cada una es un tenant con su espacio de datos aislado.</summary>
public class Empresa : BaseEntity, IBajaLogica
{
    public string RazonSocial { get; set; } = string.Empty;
    /// <summary>Identificador de organizacion en Auth0. Distinto de la clave primaria interna.</summary>
    public string TenantId { get; set; } = string.Empty;

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    /// <summary>Cupo contratado. Puede diferir del tope del plan por acuerdo comercial.</summary>
    public int MaxMaquinasHabilitadas { get; set; }

    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTimeOffset? FechaBaja { get; set; }

    /// <summary>
    /// Dominios de correo habilitados. Acotan a quien se puede invitar; no dan acceso por si solos.
    /// El principal sale del correo del Usuario 0 al dar de alta el cliente.
    /// </summary>
    public ICollection<DominioEmpresa> Dominios { get; set; } = [];

    public ICollection<Planta> Plantas { get; set; } = [];
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
