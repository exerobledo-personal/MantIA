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

    #region DBSets
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<CatalogoMaquina> CatalogosMaquina => Set<CatalogoMaquina>();
    public DbSet<Maquina> Maquinas => Set<Maquina>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<NivelPermiso> NivelesPermiso => Set<NivelPermiso>();
    public DbSet<PermisoPorRolYNivel> PermisosPorRolYNivel => Set<PermisoPorRolYNivel>();
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Maquina>()
            .HasQueryFilter(m => m.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<Usuario>()
            .HasQueryFilter(u => u.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<NivelPermiso>()
            .HasQueryFilter(n => n.EmpresaId == _tenant.EmpresaId);
        modelBuilder.Entity<PermisoPorRolYNivel>()
            .HasQueryFilter(p => p.EmpresaId == _tenant.EmpresaId);
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