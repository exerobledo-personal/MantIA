using MantIA.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Authorization;

public class UsuarioActual : IUsuarioActual
{
    private readonly MantIADbContext _db;
    private readonly IPermisoService _permisos;

    public UsuarioActual(MantIADbContext db, IPermisoService permisos)
    {
        _db = db;
        _permisos = permisos;
    }

    public async Task<bool> PuedeAsync(string authUserId, string recurso, string accion)
    {
        // Sin ignorar filtros a proposito: si el usuario esta dado de baja la consulta no lo
        // encuentra y el permiso se deniega. Sale gratis y es el comportamiento correcto.
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Auth0UserId == authUserId)
            .Select(u => new { u.Id, u.Rol, u.NivelPermisoId })
            .FirstOrDefaultAsync();

        if (usuario is null)
            return false;

        return await _permisos.PuedeAsync(
            new ContextoPermiso(usuario.Rol, usuario.NivelPermisoId, usuario.Id),
            recurso, accion);
    }
}
