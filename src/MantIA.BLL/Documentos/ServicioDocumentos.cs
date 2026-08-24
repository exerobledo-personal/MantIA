using MantIA.BE.Documentos;
using MantIA.BE.Entities;
using MantIA.BLL.Auditoria;
using MantIA.DAL.Context;
using MantIA.DAL.Documentos;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Documentos;

/// <summary>Datos que escribe la persona al adjuntar. El resto lo deduce el servicio del archivo.</summary>
public record AltaDocumento(
    Guid MaquinaId,
    TipoDocumentoMaquina Tipo,
    string Titulo,
    string NombreArchivo,
    long TamanioBytes,
    string? Descripcion = null,
    string? Emisor = null,
    string? NumeroDocumento = null,
    DateTimeOffset? FechaDocumento = null,
    DateTimeOffset? FechaVencimiento = null,
    Guid? OrdenTrabajoId = null);

public record ResultadoAlta(DocumentoMaquina? Documento, RechazoArchivo Rechazo, string Detalle)
{
    public bool Exitoso => Documento is not null;

    public static ResultadoAlta Ok(DocumentoMaquina d) => new(d, RechazoArchivo.Ninguno, string.Empty);
}

public interface IServicioDocumentos
{
    Task<ResultadoAlta> AdjuntarAsync(AltaDocumento alta, Stream contenido, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentoMaquina>> DeMaquinaAsync(Guid maquinaId, CancellationToken ct = default);

    /// <summary>Documentos que vencen dentro de los proximos <paramref name="dias"/> dias, y los ya vencidos.</summary>
    Task<IReadOnlyList<DocumentoMaquina>> PorVencerAsync(int dias = 30, CancellationToken ct = default);

    /// <summary>Abre el contenido comprobando antes que sea el mismo que se guardo.</summary>
    Task<Stream> DescargarAsync(Guid documentoId, CancellationToken ct = default);

    Task<bool> DarDeBajaAsync(Guid documentoId, string motivo, CancellationToken ct = default);
}

/// <summary>
/// Alta, consulta y baja de los archivos adjuntos a una maquina.
///
/// <para><b>El orden es: validar, guardar el archivo, escribir la ficha.</b> Si se invirtiera, una
/// falla al escribir en disco dejaria una ficha apuntando a un archivo que no existe, y eso no se
/// descubre hasta que alguien intenta descargarlo, que puede ser meses despues. Al reves, el peor
/// caso es un contenido en el almacen sin ficha: invisible, inofensivo, y lo levanta cualquier
/// limpieza posterior.</para>
///
/// <para><b>La validacion mira el contenido, no lo que dice el navegador.</b> Se leen los primeros
/// bytes antes de guardar nada. Un archivo cuya firma no corresponde a su extension se rechaza sin
/// tocar el disco.</para>
/// </summary>
public class ServicioDocumentos : IServicioDocumentos
{
    private readonly MantIADbContext _db;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IBitacora _bitacora;

    public ServicioDocumentos(MantIADbContext db, IAlmacenDocumentos almacen, IBitacora bitacora)
    {
        _db = db;
        _almacen = almacen;
        _bitacora = bitacora;
    }

    public async Task<ResultadoAlta> AdjuntarAsync(
        AltaDocumento alta, Stream contenido, CancellationToken ct = default)
    {
        var maquina = await _db.Maquinas.FirstOrDefaultAsync(m => m.Id == alta.MaquinaId, ct);

        // El filtro de tenant ya hace que una maquina de otra empresa no aparezca. Que no aparezca
        // y que no exista son indistinguibles desde afuera, y eso es lo correcto: responder "existe
        // pero no es tuya" confirmaria la existencia del dato a quien no deberia saberlo.
        if (maquina is null)
            return new ResultadoAlta(null, RechazoArchivo.NombreInvalido, "La maquina no existe.");

        if (string.IsNullOrWhiteSpace(alta.Titulo))
            return new ResultadoAlta(null, RechazoArchivo.NombreInvalido, "Falta el titulo del documento.");

        var (validacion, contenidoCompleto) = await ValidarAsync(alta, contenido, ct);

        if (!validacion.Valido)
        {
            // Un rechazo se registra igual, y con motivo. Un archivo cuya firma no coincide con su
            // extension no es un usuario distraido: es la clase de intento que solo se ve si quedo
            // anotado, porque el que lo hace no lo va a mencionar.
            await _bitacora.RegistrarAsync(
                new AccionAuditada(
                    Recurso: "Maquinas",
                    Accion: "Modificacion",
                    RecursoId: alta.MaquinaId,
                    Descripcion: $"Se rechazo el archivo '{alta.NombreArchivo}'.",
                    Exitoso: false,
                    MotivoFallo: $"{validacion.Motivo}: {validacion.Detalle}"),
                ct);

            return new ResultadoAlta(null, validacion.Motivo, validacion.Detalle);
        }

        var guardado = await _almacen.GuardarAsync(maquina.EmpresaId, contenidoCompleto, ct);

        var documento = new DocumentoMaquina
        {
            MaquinaId = alta.MaquinaId,
            OrdenTrabajoId = alta.OrdenTrabajoId,
            Tipo = alta.Tipo,
            Titulo = alta.Titulo.Trim(),
            Descripcion = alta.Descripcion?.Trim(),
            Emisor = alta.Emisor?.Trim(),
            NumeroDocumento = alta.NumeroDocumento?.Trim(),
            FechaDocumento = alta.FechaDocumento,
            FechaVencimiento = alta.FechaVencimiento,
            NombreArchivo = Path.GetFileName(alta.NombreArchivo),
            TipoContenido = validacion.TipoContenido,
            TamanioBytes = guardado.TamanioBytes,
            Hash = guardado.Hash,
            Ubicacion = guardado.Ubicacion,
        };

        _db.Set<DocumentoMaquina>().Add(documento);
        await _db.SaveChangesAsync(ct);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Maquinas",
                Accion: "Modificacion",
                RecursoId: alta.MaquinaId,
                Descripcion:
                    $"Se adjunto {documento.Tipo} '{documento.Titulo}' " +
                    $"({documento.NombreArchivo}, {documento.TamanioBytes} bytes, " +
                    $"sha256 {documento.Hash[..12]}).",
                EstadoPosterior: documento.Id.ToString("N")),
            ct);

        return ResultadoAlta.Ok(documento);
    }

    /// <summary>
    /// Lee el archivo a memoria una sola vez y valida sobre esa copia.
    /// <para>
    /// El flujo que llega de una carga web no siempre se puede rebobinar, y hacen falta dos
    /// pasadas: una para mirar la firma y otra para guardarlo. El tope de tamano ya esta puesto
    /// antes, asi que lo que se sostiene en memoria esta acotado por la politica y no por lo que
    /// mande quien sube.
    /// </para>
    /// </summary>
    private static async Task<(ResultadoValidacion, Stream)> ValidarAsync(
        AltaDocumento alta, Stream contenido, CancellationToken ct)
    {
        var previa = PoliticaArchivos.Validar(
            alta.NombreArchivo, alta.TamanioBytes, ReadOnlySpan<byte>.Empty);

        // Nombre, extension y tamano se resuelven sin leer un solo byte. Solo si eso pasa vale la
        // pena traer el archivo.
        if (!previa.Valido && previa.Motivo != RechazoArchivo.ContenidoNoCoincide)
            return (previa, Stream.Null);

        var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, ct);
        memoria.Position = 0;

        var firma = memoria.GetBuffer()
            .AsSpan(0, Math.Min(PoliticaArchivos.BytesDeFirma, (int)memoria.Length));

        // El tamano real, no el declarado: el que llega del cliente es una sugerencia.
        var validacion = PoliticaArchivos.Validar(alta.NombreArchivo, memoria.Length, firma);

        return (validacion, memoria);
    }

    public async Task<IReadOnlyList<DocumentoMaquina>> DeMaquinaAsync(
        Guid maquinaId, CancellationToken ct = default) =>
        await _db.Set<DocumentoMaquina>()
            .Where(d => d.MaquinaId == maquinaId)
            .OrderByDescending(d => d.FechaDocumento ?? d.FechaCreacion)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DocumentoMaquina>> PorVencerAsync(
        int dias = 30, CancellationToken ct = default)
    {
        var limite = DateTimeOffset.UtcNow.AddDays(dias);

        return await _db.Set<DocumentoMaquina>()
            .Include(d => d.Maquina)
            .Where(d => d.FechaVencimiento != null && d.FechaVencimiento <= limite)
            .OrderBy(d => d.FechaVencimiento)
            .ToListAsync(ct);
    }

    public async Task<Stream> DescargarAsync(Guid documentoId, CancellationToken ct = default)
    {
        var documento = await _db.Set<DocumentoMaquina>()
            .FirstOrDefaultAsync(d => d.Id == documentoId, ct)
            ?? throw new InvalidOperationException("El documento no existe.");

        // Se comprueba el hash antes de entregarlo. Un archivo reemplazado en el almacen es
        // exactamente lo que este control detecta, y entregarlo sin mirar convertiria al sistema en
        // el que distribuye el certificado falso.
        if (!await _almacen.VerificarAsync(documento.Ubicacion, documento.Hash, ct))
        {
            await _bitacora.RegistrarAsync(
                new AccionAuditada(
                    Recurso: "Integridad",
                    Accion: "Verificar",
                    RecursoId: documento.Id,
                    Descripcion: $"El archivo de '{documento.Titulo}' no coincide con su hash.",
                    Exitoso: false,
                    MotivoFallo: "El contenido almacenado fue reemplazado o esta danado."),
                ct);

            throw new InvalidOperationException(
                "El archivo almacenado no coincide con el que se subio. Quedo registrado.");
        }

        return await _almacen.AbrirAsync(documento.Ubicacion, ct);
    }

    public async Task<bool> DarDeBajaAsync(
        Guid documentoId, string motivo, CancellationToken ct = default)
    {
        var documento = await _db.Set<DocumentoMaquina>()
            .FirstOrDefaultAsync(d => d.Id == documentoId, ct);

        if (documento is null) return false;

        // Baja logica, como todo lo demas. El contenido queda en el almacen: puede estar
        // referenciado por otra ficha, y ademas un certificado retirado sigue siendo parte de la
        // historia del equipo.
        documento.FechaBaja = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Maquinas",
                Accion: "Modificacion",
                RecursoId: documento.MaquinaId,
                Descripcion: $"Se dio de baja el documento '{documento.Titulo}'.",
                Motivo: motivo,
                EstadoAnterior: documento.Id.ToString("N")),
            ct);

        return true;
    }
}
