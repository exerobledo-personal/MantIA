namespace MantIA.BLL.Authorization;

public interface IPermisoService
{
    Task<bool> PuedeAsync(string rol, Guid? nivelPermisoId, string recurso, string accion);
}