using MantIA.BE.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MantIA.DAL.Configurations;

// Los nombres de tablas y columnas NO se declaran aca: los resuelve UseSnakeCaseNamingConvention,
// registrado junto a UseNpgsql. Escribirlos ademas a mano abre la puerta a que la tabla tenga
// un nombre explicito y las columnas otro, que es el peor de los dos mundos.

// Entidades del ambito Plataforma: no llevan filtro de empresa porque son el activo
// compartido entre todos los clientes. El catalogo tecnico es exactamente eso.

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Nombre).HasMaxLength(60).IsRequired();
        b.Property(p => p.Descripcion).HasMaxLength(400);
        b.Property(p => p.Moneda).HasMaxLength(3).IsRequired();
        b.Property(p => p.PrecioMensual).HasPrecision(14, 2);

        b.HasIndex(p => p.Nombre).IsUnique();
    }
}

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.RazonSocial).HasMaxLength(200).IsRequired();
        b.Property(e => e.TenantId).HasMaxLength(120).IsRequired();

        b.HasIndex(e => e.TenantId).IsUnique();

        b.HasMany(e => e.Dominios)
            .WithOne()
            .HasForeignKey(d => d.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CatalogoMaquinaConfiguration : IEntityTypeConfiguration<CatalogoMaquina>
{
    public void Configure(EntityTypeBuilder<CatalogoMaquina> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Marca).HasMaxLength(120).IsRequired();
        b.Property(c => c.Modelo).HasMaxLength(120).IsRequired();
        b.Property(c => c.Categoria).HasMaxLength(120);
        b.Property(c => c.IntervalosMantenimiento).HasMaxLength(1000);
        b.Property(c => c.VersionIngesta).HasMaxLength(40);
        b.Property(c => c.UltimoError).HasMaxLength(2000);

        // Una ficha por modelo. Es la restriccion que sostiene el efecto de red del catalogo:
        // si se duplica, el conocimiento acumulado se parte en dos y ninguna mitad alcanza
        // el umbral de promocion.
        b.HasIndex(c => new { c.Marca, c.Modelo }).IsUnique();
        b.HasIndex(c => c.Estado);

        // Las fichas hijas se reescriben completas en cada reingesta, por eso cascada.
        b.HasMany(c => c.FallasComunes)
            .WithOne(f => f.CatalogoMaquina)
            .HasForeignKey(f => f.CatalogoMaquinaId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(c => c.RepuestosSugeridos)
            .WithOne(r => r.CatalogoMaquina)
            .HasForeignKey(r => r.CatalogoMaquinaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CatalogoFallaComunConfiguration : IEntityTypeConfiguration<CatalogoFallaComun>
{
    public void Configure(EntityTypeBuilder<CatalogoFallaComun> b)
    {
        b.HasKey(f => f.Id);

        b.Property(f => f.Descripcion).HasMaxLength(500).IsRequired();
        b.HasIndex(f => f.CatalogoMaquinaId);
    }
}

public class CatalogoRepuestoSugeridoConfiguration : IEntityTypeConfiguration<CatalogoRepuestoSugerido>
{
    public void Configure(EntityTypeBuilder<CatalogoRepuestoSugerido> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Nombre).HasMaxLength(200).IsRequired();
        b.Property(r => r.NumeroParteReferencia).HasMaxLength(80);
        b.HasIndex(r => r.CatalogoMaquinaId);
    }
}

public class EvidenciaModeloConfiguration : IEntityTypeConfiguration<EvidenciaModelo>
{
    /// <summary>
    /// Dimension del vector. La fija el modelo de embeddings: 768 es la salida de
    /// <c>text-embedding-004</c> de Gemini. Cambiarla obliga a una migracion y a recalcular
    /// todos los vectores existentes, asi que esta en un solo lugar a proposito.
    /// </summary>
    public const int Dimension = 768;

    public void Configure(EntityTypeBuilder<EvidenciaModelo> b)
    {
        b.HasKey(e => e.Id);

        b.Property(e => e.TextoOriginal).HasMaxLength(4000).IsRequired();
        b.Property(e => e.ModoFallaNormalizado).HasMaxLength(300);

        // float[] en la entidad, columna vector en la base. La conversion la hace el proveedor
        // de Npgsql sin necesidad de que MantIA.BE conozca la libreria de pgvector.
        b.Property(e => e.Embedding)
            .HasConversion<VectorConverter>()
            .HasColumnType($"vector({Dimension})");

        // Indice aproximado para la busqueda por similitud. HNSW da mejor recall que IVFFlat
        // sin necesidad de reentrenar el indice cuando crece la tabla.
        b.HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // La evidencia privada de una empresa se consulta siempre acotada al modelo y al
        // estado de promocion: es el camino de lectura del motor de recomendaciones.
        b.HasIndex(e => new { e.CatalogoMaquinaId, e.Promovida });
        b.HasIndex(e => e.EmpresaId);

        b.HasOne(e => e.CatalogoMaquina)
            .WithMany()
            .HasForeignKey(e => e.CatalogoMaquinaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
