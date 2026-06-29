using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MantIA.BLL.Authorization;

public class PermisoService : IPermisoService
{
    private readonly MantIADbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenant _tenant;

    public PermisoService(MantIADbContext db, IMemoryCache cache, ICurrentTenant tenant)
    {
        _db = db;
        _cache = cache;
        _tenant = tenant;
    }

    public async Task<bool> PuedeAsync(string rol, Guid? nivelPermisoId, string recurso, string accion)
    {
        // SuperAdmin siempre puede: bypass explicito (se audita aparte).
        if (string.Equals(rol, "SuperAdminMantIA", StringComparison.OrdinalIgnoreCase))
            return true;

        // Sin tenant resuelto: fail-closed, deniega.
        if (_tenant.EmpresaId is null)
            return false;

        var permisos = await ObtenerMatrizAsync(_tenant.EmpresaId.Value);

        return permisos.Any(p =>
            string.Equals(p.Rol, rol, StringComparison.OrdinalIgnoreCase) &&
            p.NivelPermisoId == nivelPermisoId &&
            string.Equals(p.Recurso, recurso, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Accion, accion, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<PermisoCacheado>> ObtenerMatrizAsync(Guid empresaId)
    {
        // Clave por tenant: cada empresa tiene su propia matriz cacheada. Aislamiento correcto.
        var clave = $"matriz_permisos_{empresaId}";

        return await _cache.GetOrCreateAsync(clave, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // TTL configurable

            // El query filter ya filtra por el tenant actual, asi que esto trae solo los de la empresa.
            return await _db.PermisosPorRolYNivel
                .Select(p => new PermisoCacheado(
                    p.Rol, p.NivelPermisoId, p.Recurso, p.AccionPermitida))
                .ToListAsync();
        }) ?? new List<PermisoCacheado>();
    }

    private record PermisoCacheado(string Rol, Guid? NivelPermisoId, string Recurso, string Accion);
}