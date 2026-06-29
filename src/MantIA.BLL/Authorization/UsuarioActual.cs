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
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Auth0UserId == authUserId);

        if (usuario is null)
            return false;

        return await _permisos.PuedeAsync(usuario.Rol, usuario.NivelPermisoId, recurso, accion);
    }
}