using MantIA.BE.Auditoria;
using MantIA.BE.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MantIA.DAL.Configurations;

// Ambito Empresa. Todas derivan de TenantEntity, asi que el filtro de aislamiento se lo
// aplica el contexto por convencion: aca solo van claves, indices y longitudes.

public class PlantaConfiguration : IEntityTypeConfiguration<Planta>
{
    public void Configure(EntityTypeBuilder<Planta> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
        b.Property(p => p.Direccion).HasMaxLength(250).IsRequired();
        b.Property(p => p.Localidad).HasMaxLength(120).IsRequired();

        // Seis decimales dan una precision de unos 11 cm, mas que suficiente para ubicar
        // una planta en el mapa y bastante mas barato que un tipo geografico.
        b.Property(p => p.Latitud).HasPrecision(9, 6);
        b.Property(p => p.Longitud).HasPrecision(9, 6);

        b.HasIndex(p => new { p.EmpresaId, p.Nombre }).IsUnique();

        b.HasMany(p => p.Maquinas)
            .WithOne(m => m.Planta)
            .HasForeignKey(m => m.PlantaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.HasKey(u => u.Id);

        // Unica columna con nombre explicito en todo el modelo: la convencion snake_case corta
        // "Auth0UserId" como "auth0user_id" porque el 0 rompe la deteccion de mayusculas.
        b.Property(u => u.Auth0UserId)
            .HasColumnName("auth0_user_id")
            .HasMaxLength(120)
            .IsRequired();
        b.Property(u => u.Email).HasMaxLength(200).IsRequired();
        b.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
        b.Property(u => u.Apellido).HasMaxLength(100).IsRequired();

        // Indices UNICOS PARCIALES: la restriccion aplica solo entre usuarios activos.
        //
        // Es lo que sostiene el modelo de baja de usuarios. Al dar de baja se marca FechaBaja y se
        // revocan permisos y alcance; la fila queda, porque de ella cuelga todo el historial —quien
        // cerro cada orden, quien resolvio cada alerta, quien creo cada registro—. Si esa persona
        // vuelve a la empresa, se crea una fila NUEVA con identificador nuevo y sin ningun permiso,
        // que es justamente lo que se busca: la reincorporacion no hereda nada.
        //
        // Sin el filtro, la segunda alta chocaria contra la fila vieja y habria que elegir entre
        // borrar historial o inventar un correo distinto.
        b.HasIndex(u => u.Auth0UserId)
            .IsUnique()
            .HasFilter("fecha_baja IS NULL");

        b.HasIndex(u => new { u.EmpresaId, u.Email })
            .IsUnique()
            .HasFilter("fecha_baja IS NULL");

        b.HasOne(u => u.NivelPermiso)
            .WithMany()
            .HasForeignKey(u => u.NivelPermisoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(u => u.Alcance)
            .WithOne(a => a.Usuario)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UsuarioAlcanceConfiguration : IEntityTypeConfiguration<UsuarioAlcance>
{
    public void Configure(EntityTypeBuilder<UsuarioAlcance> b)
    {
        b.HasKey(a => a.Id);

        // Sin esta restriccion, asignar dos veces la misma planta duplicaria filas y el
        // "vacio significa todas" dejaria de ser distinguible de un alcance mal cargado.
        b.HasIndex(a => new { a.UsuarioId, a.PlantaId }).IsUnique();

        b.HasOne(a => a.Planta)
            .WithMany()
            .HasForeignKey(a => a.PlantaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NivelPermisoConfiguration : IEntityTypeConfiguration<NivelPermiso>
{
    public void Configure(EntityTypeBuilder<NivelPermiso> b)
    {
        b.HasKey(n => n.Id);

        b.Property(n => n.Nombre).HasMaxLength(60).IsRequired();
        b.Property(n => n.Descripcion).HasMaxLength(300);

        b.HasIndex(n => new { n.EmpresaId, n.Nombre }).IsUnique();
    }
}

public class PermisoPorRolYNivelConfiguration : IEntityTypeConfiguration<PermisoPorRolYNivel>
{
    public void Configure(EntityTypeBuilder<PermisoPorRolYNivel> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Recurso).HasMaxLength(40).IsRequired();
        b.Property(p => p.Accion).HasMaxLength(40).IsRequired();

        // Una sola celda por combinacion. Sin esto, dos filas contradictorias para el mismo
        // rol harian que el resultado dependa del orden de lectura, que es la peor forma de
        // fallar que puede tener un sistema de permisos.
        b.HasIndex(p => new { p.EmpresaId, p.Rol, p.NivelPermisoId, p.Recurso, p.Accion })
            .IsUnique();

        b.HasOne(p => p.NivelPermiso)
            .WithMany()
            .HasForeignKey(p => p.NivelPermisoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermisoPorUsuarioConfiguration : IEntityTypeConfiguration<PermisoPorUsuario>
{
    public void Configure(EntityTypeBuilder<PermisoPorUsuario> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Recurso).HasMaxLength(40).IsRequired();
        b.Property(p => p.Accion).HasMaxLength(40).IsRequired();
        b.Property(p => p.Motivo).HasMaxLength(500).IsRequired();

        // Una sola excepcion vigente por persona y par recurso/accion. Dos filas contradictorias
        // para el mismo usuario harian que el permiso dependa del orden de lectura.
        b.HasIndex(p => new { p.UsuarioId, p.Recurso, p.Accion }).IsUnique();

        // Barrido de excepciones vencidas para limpiarlas.
        b.HasIndex(p => new { p.EmpresaId, p.VigenteHasta });

        b.HasOne(p => p.Usuario)
            .WithMany()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContadorDocumentoConfiguration : IEntityTypeConfiguration<ContadorDocumento>
{
    public void Configure(EntityTypeBuilder<ContadorDocumento> b)
    {
        b.HasKey(c => c.Id);

        // Este indice no es para leer rapido: es el que hace posible el ON CONFLICT del numerador.
        // Sin el, dos altas simultaneas crearian dos contadores para la misma serie.
        b.HasIndex(c => new { c.EmpresaId, c.Tipo, c.Anio }).IsUnique();
    }
}

public class EventoPendienteConfiguration : IEntityTypeConfiguration<EventoPendiente>
{
    public void Configure(EntityTypeBuilder<EventoPendiente> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.Cadena).HasMaxLength(80).IsRequired();
        b.Property(p => p.Severidad).HasMaxLength(20).IsRequired();
        b.Property(p => p.UltimoError).HasMaxLength(2000);
        b.Property(p => p.Contenido).HasColumnType("jsonb");

        // El drenaje recorre por fecha del hecho para reconstruir el orden real.
        b.HasIndex(p => p.FechaEvento);
    }
}

public class SolicitudRollbackConfiguration : IEntityTypeConfiguration<SolicitudRollback>
{
    public void Configure(EntityTypeBuilder<SolicitudRollback> b)
    {
        b.HasKey(s => s.Id);

        b.Property(s => s.Motivo).HasMaxLength(1000).IsRequired();
        b.Property(s => s.MotivoRechazo).HasMaxLength(1000);
        b.Property(s => s.RecursoFiltro).HasMaxLength(40);
        b.Property(s => s.EventosNoRevertidos).HasColumnType("jsonb");

        b.HasIndex(s => new { s.EmpresaId, s.Estado });
        b.HasIndex(s => s.UsuarioObjetivoId);

        // La solicitud vive en PostgreSQL y no en Mongo junto a la bitacora: es una entidad
        // transaccional con aprobacion de dos personas y estados, no un evento append-only.
    }
}
