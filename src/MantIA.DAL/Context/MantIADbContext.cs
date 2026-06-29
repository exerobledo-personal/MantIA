using Microsoft.EntityFrameworkCore;
using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.DAL.Tenancy;

namespace MantIA.DAL.Context;

public class MantIADbContext : DbContext
{
    private readonly ICurrentTenant _tenant;

    public MantIADbContext(DbContextOptions<MantIADbContext> options, ICurrentTenant tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<CatalogoMaquina> CatalogosMaquina => Set<CatalogoMaquina>();
    public DbSet<Maquina> Maquinas => Set<Maquina>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtro multi-tenant SOLO sobre datos operativos privados.
        // Fail-closed: si _tenant.EmpresaId es null, ninguna fila matchea.
        modelBuilder.Entity<Maquina>()
            .HasQueryFilter(m => m.EmpresaId == _tenant.EmpresaId);

        // CatalogoMaquina queda SIN filtro: es el catálogo compartido global.
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTenant()
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (_tenant.EmpresaId is null)
                    throw new InvalidOperationException(
                        "Intento de escritura sin contexto de tenant (fail-closed).");
                entry.Entity.EmpresaId = _tenant.EmpresaId.Value;
            }
        }
    }
}