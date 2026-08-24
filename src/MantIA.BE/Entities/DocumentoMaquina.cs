using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Que es el archivo. No es decorativo: de esto depende si el documento vence y si su ausencia
/// es un hallazgo.
/// </summary>
public enum TipoDocumentoMaquina
{
    /// <summary>Constancia de un proveedor de que hizo el mantenimiento. Vence.</summary>
    CertificadoMantenimiento,

    /// <summary>Habilitacion o certificacion del equipo ante un organismo. Vence.</summary>
    Habilitacion,

    /// <summary>Manual del fabricante, hoja de datos, despiece.</summary>
    Manual,

    /// <summary>Garantia del equipo o de una reparacion. Vence.</summary>
    Garantia,

    /// <summary>Remito, factura o presupuesto asociado a la maquina.</summary>
    Comprobante,

    /// <summary>Informe tecnico, medicion, analisis de aceite, termografia.</summary>
    Informe,

    /// <summary>Foto del equipo, de la placa o de una falla.</summary>
    Fotografia,

    Otro
}

/// <summary>
/// Archivo adjunto a una maquina.
///
/// <para><b>De donde sale.</b> El caso que lo motiva es el certificado de mantenimiento del
/// proveedor: cuando un tercero interviene un equipo deja un papel, y ese papel hoy termina en el
/// correo de alguien. Adjuntado a la maquina, pasa a ser parte de su historia y sobrevive a la
/// persona que lo recibio.</para>
///
/// <para><b>Por que cuelga de la maquina y no de la orden.</b> Muchos de estos documentos no tienen
/// orden que los origine —una habilitacion, un manual, la foto de la placa— y los que si la tienen
/// igual se buscan por equipo. La orden queda como referencia opcional, que es lo que permite
/// responder las dos preguntas: "que papeles tiene esta maquina" y "que quedo de esta
/// intervencion".</para>
///
/// <para><b>El archivo no vive en esta tabla.</b> Aca queda su ficha; el contenido va al almacen de
/// documentos, referenciado por su hash. Guardar binarios en la base infla las copias de respaldo,
/// castiga cada consulta que traiga la fila entera y complica cualquier mudanza posterior a
/// almacenamiento de objetos.</para>
///
/// <para><b>El hash es su digito verificador.</b> Las tres tablas del regimen de digitos protegen
/// valores que se editan; un archivo no se edita, se reemplaza. Recalcular el SHA-256 del contenido
/// y compararlo con el guardado detecta cualquier cambio en el almacen, y ademas hace que dos altas
/// del mismo archivo compartan un solo contenido.</para>
/// </summary>
public class DocumentoMaquina : TenantEntity, IBajaLogica
{
    public Guid MaquinaId { get; set; }
    public Maquina? Maquina { get; set; }

    /// <summary>Orden que lo origino, cuando la hay. Opcional a proposito.</summary>
    public Guid? OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }

    public TipoDocumentoMaquina Tipo { get; set; } = TipoDocumentoMaquina.CertificadoMantenimiento;

    /// <summary>Titulo que escribe quien lo sube. Es por lo que se busca.</summary>
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    /// <summary>Quien emitio el documento. En un certificado, el proveedor que hizo el trabajo.</summary>
    public string? Emisor { get; set; }

    /// <summary>Numero del certificado, remito o informe, tal como lo trae el papel.</summary>
    public string? NumeroDocumento { get; set; }

    /// <summary>Fecha del documento, que rara vez es la de carga.</summary>
    public DateTimeOffset? FechaDocumento { get; set; }

    /// <summary>
    /// Cuando deja de valer. Nulo en lo que no vence, como un manual. Es lo que permite avisar
    /// antes de que una habilitacion se caiga, en lugar de descubrirlo en una inspeccion.
    /// </summary>
    public DateTimeOffset? FechaVencimiento { get; set; }

    // --- El archivo ---

    /// <summary>Nombre con el que llego. Se conserva para devolverlo igual al descargar.</summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>Tipo de contenido <b>verificado</b>, no el que declaro el navegador.</summary>
    public string TipoContenido { get; set; } = string.Empty;

    public long TamanioBytes { get; set; }

    /// <summary>SHA-256 del contenido, en hexadecimal. Es la clave en el almacen.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>Ubicacion dentro del almacen. Depende de la implementacion del almacen.</summary>
    public string Ubicacion { get; set; } = string.Empty;

    public DateTimeOffset? FechaBaja { get; set; }

    /// <summary>Verdadero si el documento vence y ya vencio.</summary>
    public bool Vencido(DateTimeOffset ahora) =>
        FechaVencimiento is { } vence && vence <= ahora;

    /// <summary>Dias que faltan para que venza. Nulo si no vence.</summary>
    public int? DiasParaVencer(DateTimeOffset ahora) =>
        FechaVencimiento is { } vence ? (int)Math.Ceiling((vence - ahora).TotalDays) : null;
}
