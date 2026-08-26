using MantIA.BE.Common;

namespace MantIA.BLL.Authorization;

/// <summary>Todo lo que hace falta saber del usuario para resolver un permiso.</summary>
/// <param name="UsuarioId">Nulo solo en procesos desatendidos.</param>
public record ContextoPermiso(RolSistema Rol, Guid? NivelPermisoId, Guid? UsuarioId = null);

/// <summary>
/// Evaluacion de permisos. Se consulta <b>en el momento de la accion</b>, no al iniciar sesion:
/// el token de Auth0 lleva identidad, rol y empresa, y nunca los permisos finos. Por eso quitarle
/// un permiso a alguien tiene efecto en su proxima accion sin que haya que invalidar su token ni
/// pedirle que vuelva a entrar.
/// </summary>
public interface IPermisoService
{
    Task<bool> PuedeAsync(ContextoPermiso contexto, string recurso, string accion);

    /// <summary>
    /// Descarta la matriz cacheada de una empresa. La llama el servicio que edita permisos, dentro
    /// de la misma operacion que guarda: la cache acelera la lectura, no decide cuando aplica un
    /// cambio.
    /// </summary>
    void Invalidar(Guid empresaId);

    /// <summary>
    /// Descarta las excepciones nominales cacheadas de un usuario. Se llama al conceder o revocar
    /// un <c>PermisoPorUsuario</c>.
    /// </summary>
    void InvalidarUsuario(Guid usuarioId);

    /// <summary>
    /// Olvida el estado comercial de una empresa: plan, vigencia y suspension. Se llama al cambiar
    /// cualquiera de los tres.
    /// </summary>
    void InvalidarEstadoEmpresa(Guid empresaId);
}
