using MantIA.BE.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MantIA.DAL.Configurations;

// Digitos verificadores. Son tablas de control, no de negocio: nada las referencia y ellas no
// referencian a nada mas que a la empresa. Esa falta de relaciones es a proposito —permite moverlas
// a otro esquema, con otro rol de base, sin tocar el resto del modelo—.

public class SelloFilaConfiguration : IEntityTypeConfiguration<SelloFila>
{
    public void Configure(EntityTypeBuilder<SelloFila> b)
    {
        b.HasKey(s => s.Id);

        b.Property(s => s.Tabla).HasMaxLength(60).IsRequired();
        b.Property(s => s.Digito).HasMaxLength(64).IsRequired();
        b.Property(s => s.VersionLlave).HasMaxLength(20).IsRequired();
        b.Property(s => s.VersionFormato).HasMaxLength(10).IsRequired();

        // Un solo digito por fila. Sin esta restriccion, dos digitos distintos para la misma fila
        // harian que verificar dependa de cual se lea primero, y siempre existiria uno que valida.
        b.HasIndex(s => new { s.Tabla, s.FilaId }).IsUnique();

        // El recorrido de la foto vertical: todas las filas de una tabla de una empresa, en orden
        // estable. El orden del indice ES el orden en que se calcula el digito vertical.
        b.HasIndex(s => new { s.EmpresaId, s.Tabla, s.FilaId });
    }
}

public class DocumentoMaquinaConfiguration : IEntityTypeConfiguration<DocumentoMaquina>
{
    public void Configure(EntityTypeBuilder<DocumentoMaquina> b)
    {
        b.HasKey(d => d.Id);

        b.Property(d => d.Titulo).HasMaxLength(200).IsRequired();
        b.Property(d => d.Descripcion).HasMaxLength(1000);
        b.Property(d => d.Emisor).HasMaxLength(200);
        b.Property(d => d.NumeroDocumento).HasMaxLength(80);
        b.Property(d => d.NombreArchivo).HasMaxLength(260).IsRequired();
        b.Property(d => d.TipoContenido).HasMaxLength(120).IsRequired();

        // SHA-256 en hexadecimal: 64 caracteres, siempre.
        b.Property(d => d.Hash).HasMaxLength(64).IsRequired();
        b.Property(d => d.Ubicacion).HasMaxLength(200).IsRequired();

        // La ficha de una maquina abre su lista de documentos: es la consulta de todos los dias.
        b.HasIndex(d => new { d.MaquinaId, d.Tipo });

        // El aviso de vencimientos recorre por fecha. Sin filtro parcial a proposito: los ya
        // vencidos son justamente los que hay que mostrar primero.
        b.HasIndex(d => new { d.EmpresaId, d.FechaVencimiento });

        // Cuantas fichas apuntan al mismo contenido. Es lo que hay que mirar antes de borrar un
        // archivo del almacen, el dia que exista una purga.
        b.HasIndex(d => new { d.EmpresaId, d.Hash });

        b.HasOne(d => d.Maquina)
            .WithMany()
            .HasForeignKey(d => d.MaquinaId)
            .OnDelete(DeleteBehavior.Restrict);

        // La orden es opcional y no arrastra al documento: si algun dia se purga una orden, el
        // certificado del proveedor tiene que seguir colgando de la maquina.
        b.HasOne(d => d.OrdenTrabajo)
            .WithMany()
            .HasForeignKey(d => d.OrdenTrabajoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SelloTablaConfiguration : IEntityTypeConfiguration<SelloTabla>
{
    public void Configure(EntityTypeBuilder<SelloTabla> b)
    {
        b.HasKey(s => s.Id);

        b.Property(s => s.Tabla).HasMaxLength(60).IsRequired();
        b.Property(s => s.Digito).HasMaxLength(64).IsRequired();
        b.Property(s => s.DigitoAnterior).HasMaxLength(64);
        b.Property(s => s.VersionLlave).HasMaxLength(20).IsRequired();
        b.Property(s => s.VersionFormato).HasMaxLength(10).IsRequired();

        // Una foto por numero de serie. Es lo que impide reescribir una foto pasada haciendola
        // pasar por nueva: la posicion en la cadena ya esta tomada.
        b.HasIndex(s => new { s.EmpresaId, s.Tabla, s.Secuencia }).IsUnique();

        // Para traer la ultima foto de una serie, que es la operacion de cada pasada.
        b.HasIndex(s => new { s.EmpresaId, s.Tabla, s.CalculadoEn });
    }
}
