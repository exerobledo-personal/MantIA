using MantIA.BLL.Authorization;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace MantIA.IntegrationTests;

public class PermisosTests
{
    private const string Conn =
        "Host=localhost;Port=5432;Database=mantia;Username=mantia;Password=dev_local_pwd";

    // Arma el PermisoService apuntando a una empresa concreta (tenant resuelto).
    private static IPermisoService NuevoServicio(Guid empresaId)
    {
        var options = new DbContextOptionsBuilder<MantIADbContext>()
            .UseNpgsql(Conn)
            .UseSnakeCaseNamingConvention()
            .Options;

        var tenant = new CurrentTenant { EmpresaId = empresaId };
        var db = new MantIADbContext(options, tenant);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new PermisoService(db, cache, tenant);
    }

    [Fact]
    public async Task Filtrado_de_permisos_respeta_rol_y_nivel()
    {
        // Tomamos la empresa y el nivel Sr reales de la base
        var options = new DbContextOptionsBuilder<MantIADbContext>()
            .UseNpgsql(Conn).UseSnakeCaseNamingConvention().Options;

        Guid empresaId;
        Guid nivelSr;
        using (var db = new MantIADbContext(options, new CurrentTenant { EmpresaId = null }))
        {
            var empresa = await db.Empresas.IgnoreQueryFilters()
                .FirstAsync(e => e.RazonSocial == "Empresa Demo");
            empresaId = empresa.Id;

            var sr = await db.NivelesPermiso.IgnoreQueryFilters()
                .FirstAsync(n => n.EmpresaId == empresaId && n.Nombre == "Sr");
            nivelSr = sr.Id;
        }

        var permisos = NuevoServicio(empresaId);

        // Supervisor Sr SI puede cerrar (Modificacion) una OrdenTrabajo
        Assert.True(await permisos.PuedeAsync("Supervisor", nivelSr, "OrdenTrabajo", "Modificacion"));

        // Empleado NO puede cerrar (no esta en la matriz)
        Assert.False(await permisos.PuedeAsync("Empleado", null, "OrdenTrabajo", "Modificacion"));

        // Empleado SI puede consultar
        Assert.True(await permisos.PuedeAsync("Empleado", null, "OrdenTrabajo", "Consulta"));

        // SuperAdmin SIEMPRE puede, sin importar la matriz
        Assert.True(await permisos.PuedeAsync("SuperAdminMantIA", null, "CualquierCosa", "Baja"));

        // Sin tenant resuelto: fail-closed, deniega aunque el rol exista
        var sinTenant = NuevoServicio(Guid.NewGuid()); // empresa que no tiene matriz cargada
        Assert.False(await sinTenant.PuedeAsync("Supervisor", nivelSr, "OrdenTrabajo", "Modificacion"));
    }
}