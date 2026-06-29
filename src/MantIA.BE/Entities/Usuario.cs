using MantIA.BE.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MantIA.BE.Entities;

public class Usuario : TenantEntity
{
    [Column("auth0_user_id")]
    public string Auth0UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Rol { get; set; } = "Empleado";
    public string Estado { get; set; } = "activo";
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaBaja { get; set; }
}