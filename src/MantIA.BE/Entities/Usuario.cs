using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Usuario de una empresa cliente. La identidad la gestiona Auth0; aca vive el perfil
/// con el que el sistema decide que puede hacer.
/// </summary>
public class Usuario : TenantEntity, IBajaLogica
{
    /// <summary>Identificador del sujeto en Auth0. Es el vinculo con la identidad externa.</summary>
    public string Auth0UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Eje "Rol": que acciones puede ejecutar.</summary>
    public RolSistema Rol { get; set; } = RolSistema.Empleado;

    /// <summary>Eje "Nivel": cuanto se recorta ese rol. Lo configura cada empresa.</summary>
    public Guid? NivelPermisoId { get; set; }
    public NivelPermiso? NivelPermiso { get; set; }

    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTimeOffset? UltimoAcceso { get; set; }
    public DateTimeOffset? FechaBaja { get; set; }

    /// <summary>Plantas asignadas. Vacio significa todas las de su empresa.</summary>
    public ICollection<UsuarioAlcance> Alcance { get; set; } = [];
}
