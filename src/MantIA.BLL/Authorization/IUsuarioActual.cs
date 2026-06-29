namespace MantIA.BLL.Authorization;

public interface IUsuarioActual
{
    Task<bool> PuedeAsync(string authUserId, string recurso, string accion);
}