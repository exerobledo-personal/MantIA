using MantIA.BE.Entities;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MantIA.IntegrationTests;

public class MultiTenantTests
{
    private const string Conn =
        "Host=localhost;Port=5432;Database=mantia;Username=mantia;Password=dev_local_pwd";

    private static MantIADbContext NuevoContexto(Guid? empresaId)
    {
        var options = new DbContextOptionsBuilder<MantIADbContext>()
            .UseNpgsql(Conn)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new MantIADbContext(options, new CurrentTenant { EmpresaId = empresaId });
    }

    [Fact]
    public void Tenant_aisla_operativo_y_comparte_catalogo()
    {
        var empresaA = Guid.NewGuid();
        var empresaB = Guid.NewGuid();

        // 1. Modelo en el catalogo COMPARTIDO (no lleva tenant)
        Guid catalogoId;
        using (var db = NuevoContexto(null))
        {
            var modelo = new CatalogoMaquina { Marca = "Siemens", Modelo = "S7-1200" };
            db.CatalogosMaquina.Add(modelo);
            db.SaveChanges();
            catalogoId = modelo.Id;
        }

        // 2. Empresa A registra una maquina apuntando a ese modelo compartido
        using (var db = NuevoContexto(empresaA))
        {
            db.Maquinas.Add(new Maquina
            {
                Nombre = "Linea 1 - PLC",
                CatalogoMaquinaId = catalogoId
            });
            db.SaveChanges();
        }

        // 3. Empresa B registra otra, usando EL MISMO modelo del catalogo
        using (var db = NuevoContexto(empresaB))
        {
            db.Maquinas.Add(new Maquina
            {
                Nombre = "Planta Norte - PLC",
                CatalogoMaquinaId = catalogoId
            });
            db.SaveChanges();
        }

        // 4. Empresa A ve solo SUS maquinas
        using (var db = NuevoContexto(empresaA))
        {
            var maquinas = db.Maquinas.ToList();
            Assert.All(maquinas, m => Assert.Equal(empresaA, m.EmpresaId));
            Assert.DoesNotContain(maquinas, m => m.EmpresaId == empresaB);
        }

        // 5. El catalogo es visible para AMBAS empresas (compartido)
        using (var dbA = NuevoContexto(empresaA))
        using (var dbB = NuevoContexto(empresaB))
        {
            Assert.Contains(dbA.CatalogosMaquina.ToList(), c => c.Id == catalogoId);
            Assert.Contains(dbB.CatalogosMaquina.ToList(), c => c.Id == catalogoId);
        }

        // 6. Sin tenant: fail-closed en operativo, pero el catalogo sigue visible
        using (var db = NuevoContexto(null))
        {
            Assert.Empty(db.Maquinas.ToList());
            Assert.Contains(db.CatalogosMaquina.ToList(), c => c.Id == catalogoId);
        }
    }
}