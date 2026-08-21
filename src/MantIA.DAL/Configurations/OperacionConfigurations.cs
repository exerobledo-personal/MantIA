using MantIA.BE.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MantIA.DAL.Configurations;

// Ambito Operacion. Aca estan los caminos de lectura mas calientes del producto, asi que
// los indices no son decorativos: cada uno responde a una consulta concreta de pantalla.

public class MaquinaConfiguration : IEntityTypeConfiguration<Maquina>
{
    public void Configure(EntityTypeBuilder<Maquina> b)
    {
        b.HasKey(m => m.Id);

        b.Property(m => m.Codigo).HasMaxLength(40).IsRequired();
        b.Property(m => m.Nombre).HasMaxLength(150).IsRequired();
        b.Property(m => m.NumeroSerie).HasMaxLength(80);
        b.Property(m => m.LineaSector).HasMaxLength(120);

        b.HasIndex(m => new { m.EmpresaId, m.Codigo }).IsUnique();

        // Listado de maquinas de una planta: es la pantalla que mas se abre.
        b.HasIndex(m => new { m.EmpresaId, m.PlantaId, m.Estado });

        // Cuantas empresas usan una ficha del catalogo. Alimenta el "23 empresas la utilizan"
        // que se muestra al dar de alta una maquina.
        b.HasIndex(m => m.CatalogoMaquinaId);

        b.HasOne(m => m.CatalogoMaquina)
            .WithMany()
            .HasForeignKey(m => m.CatalogoMaquinaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(m => m.Ordenes)
            .WithOne(o => o.Maquina)
            .HasForeignKey(o => o.MaquinaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(m => m.Repuestos)
            .WithOne(r => r.Maquina)
            .HasForeignKey(r => r.MaquinaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RepuestoConfiguration : IEntityTypeConfiguration<Repuesto>
{
    public void Configure(EntityTypeBuilder<Repuesto> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Nombre).HasMaxLength(200).IsRequired();
        b.Property(r => r.NumeroParte).HasMaxLength(80).IsRequired();
        b.Property(r => r.Descripcion).HasMaxLength(1000);
        b.Property(r => r.UnidadMedida).HasMaxLength(30).IsRequired();
        b.Property(r => r.Proveedor).HasMaxLength(200);
        b.Property(r => r.CostoUnitario).HasPrecision(14, 2);

        b.HasIndex(r => new { r.EmpresaId, r.NumeroParte }).IsUnique();

        // Barrido de repuestos en riesgo. Es la consulta del motor de alertas y corre seguido.
        b.HasIndex(r => new { r.EmpresaId, r.Estado, r.Criticidad });

        b.HasMany(r => r.Movimientos)
            .WithOne(m => m.Repuesto)
            .HasForeignKey(m => m.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(r => r.Maquinas)
            .WithOne(m => m.Repuesto)
            .HasForeignKey(m => m.RepuestoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaquinaRepuestoConfiguration : IEntityTypeConfiguration<MaquinaRepuesto>
{
    public void Configure(EntityTypeBuilder<MaquinaRepuesto> b)
    {
        b.HasKey(mr => mr.Id);
        b.HasIndex(mr => new { mr.MaquinaId, mr.RepuestoId }).IsUnique();
        b.HasIndex(mr => mr.RepuestoId);
    }
}

public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> b)
    {
        b.HasKey(m => m.Id);

        b.Property(m => m.Motivo).HasMaxLength(500);

        // Recorrido del libro mayor de un repuesto en orden cronologico: es como se audita
        // que la suma de movimientos coincide con el stock denormalizado.
        b.HasIndex(m => new { m.EmpresaId, m.RepuestoId, m.FechaMovimiento });

        b.HasOne(m => m.OrdenTrabajo)
            .WithMany()
            .HasForeignKey(m => m.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrdenTrabajoConfiguration : IEntityTypeConfiguration<OrdenTrabajo>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajo> b)
    {
        b.HasKey(o => o.Id);

        b.Property(o => o.Numero).HasMaxLength(30).IsRequired();
        b.Property(o => o.DescripcionProblema).HasMaxLength(4000).IsRequired();
        b.Property(o => o.DescripcionResolucion).HasMaxLength(4000);
        b.Property(o => o.HorasResolucion).HasPrecision(8, 2);

        // El numero legible es unico dentro de la empresa. Lo asigna una secuencia de base,
        // no un conteo: con dos altas simultaneas, contar filas devuelve el mismo numero dos veces.
        b.HasIndex(o => new { o.EmpresaId, o.Numero }).IsUnique();

        // Tablero de ordenes abiertas y historial por maquina.
        b.HasIndex(o => new { o.EmpresaId, o.Estado, o.FechaApertura });
        b.HasIndex(o => new { o.EmpresaId, o.MaquinaId, o.FechaApertura });

        b.HasOne(o => o.Responsable)
            .WithMany()
            .HasForeignKey(o => o.ResponsableUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Repuestos)
            .WithOne(r => r.OrdenTrabajo)
            .HasForeignKey(r => r.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict y no Cascade: el historial sobrevive a la orden. Si alguna vez se borrara una
        // orden fisicamente, perder su linea de tiempo seria justo lo contrario de lo que se busca.
        b.HasMany(o => o.Historial)
            .WithOne(h => h.OrdenTrabajo)
            .HasForeignKey(h => h.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HistorialOrdenTrabajoConfiguration : IEntityTypeConfiguration<HistorialOrdenTrabajo>
{
    public void Configure(EntityTypeBuilder<HistorialOrdenTrabajo> b)
    {
        b.HasKey(h => h.Id);

        b.Property(h => h.Campo).HasMaxLength(60);
        b.Property(h => h.ValorAnterior).HasMaxLength(4000);
        b.Property(h => h.ValorNuevo).HasMaxLength(4000);
        b.Property(h => h.Descripcion).HasMaxLength(500).IsRequired();
        b.Property(h => h.Motivo).HasMaxLength(1000);

        // La consulta es siempre la misma: la linea de tiempo de una orden, en orden cronologico.
        b.HasIndex(h => new { h.OrdenTrabajoId, h.Fecha });

        // "Que toco esta persona en las ordenes", para el rollback.
        b.HasIndex(h => new { h.EmpresaId, h.UsuarioId, h.Fecha });
    }
}

public class OrdenTrabajoRepuestoConfiguration : IEntityTypeConfiguration<OrdenTrabajoRepuesto>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajoRepuesto> b)
    {
        b.HasKey(otr => otr.Id);

        b.Property(otr => otr.CostoUnitarioAlConsumo).HasPrecision(14, 2);

        b.HasIndex(otr => new { otr.OrdenTrabajoId, otr.RepuestoId }).IsUnique();

        b.HasOne(otr => otr.Repuesto)
            .WithMany()
            .HasForeignKey(otr => otr.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AlertaStockConfiguration : IEntityTypeConfiguration<AlertaStock>
{
    public void Configure(EntityTypeBuilder<AlertaStock> b)
    {
        b.HasKey(a => a.Id);

        // Bandeja de alertas activas por criticidad, y conteo historico de quiebres por periodo.
        b.HasIndex(a => new { a.EmpresaId, a.Estado, a.Criticidad });
        b.HasIndex(a => new { a.EmpresaId, a.RepuestoId, a.FechaDisparo });

        b.HasOne(a => a.Repuesto)
            .WithMany()
            .HasForeignKey(a => a.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecomendacionConfiguration : IEntityTypeConfiguration<Recomendacion>
{
    public void Configure(EntityTypeBuilder<Recomendacion> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Justificacion).HasMaxLength(2000).IsRequired();
        b.Property(r => r.ReglaAplicada).HasMaxLength(120);

        // Confianza entre 0 y 1 con tres decimales. Nula cuando el origen es una regla:
        // una regla determinista no tiene confianza, se cumple o no se cumple.
        b.Property(r => r.Confianza).HasPrecision(4, 3);

        b.HasIndex(r => new { r.EmpresaId, r.Estado, r.Prioridad });
        b.HasIndex(r => new { r.EmpresaId, r.RepuestoId, r.FechaGeneracion });

        b.HasOne(r => r.Repuesto)
            .WithMany()
            .HasForeignKey(r => r.RepuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(r => r.Maquina)
            .WithMany()
            .HasForeignKey(r => r.MaquinaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReporteConfiguration : IEntityTypeConfiguration<Reporte>
{
    public void Configure(EntityTypeBuilder<Reporte> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Nombre).HasMaxLength(200).IsRequired();

        // jsonb y no texto: los filtros hay que poder consultarlos para responder "cuantos
        // reportes se generaron sobre esta planta", no solo reproducirlos.
        b.Property(r => r.FiltrosJson).HasColumnType("jsonb");

        b.HasIndex(r => new { r.EmpresaId, r.Estado, r.Tipo });

        b.HasMany(r => r.Historial)
            .WithOne(h => h.Reporte)
            .HasForeignKey(h => h.ReporteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReporteHistorialConfiguration : IEntityTypeConfiguration<ReporteHistorial>
{
    public void Configure(EntityTypeBuilder<ReporteHistorial> b)
    {
        b.HasKey(h => h.Id);

        b.Property(h => h.Accion).HasMaxLength(40).IsRequired();
        b.Property(h => h.Detalle).HasMaxLength(2000);

        b.HasIndex(h => new { h.ReporteId, h.Fecha });
    }
}
