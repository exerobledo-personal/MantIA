using MantIA.BE.Common;

namespace MantIA.BE.Entities;

public class PermisoPorRolYNivel : TenantEntity
{
    public string Rol { get; set; } = string.Empty;        // Empleado | Supervisor | Gerente | AdminEmpresa | SuperAdminMantIA

    public Guid? NivelPermisoId { get; set; }
    public NivelPermiso? NivelPermiso { get; set; }

    public string Recurso { get; set; } = string.Empty;        
    public string AccionPermitida { get; set; } = string.Empty; 
}