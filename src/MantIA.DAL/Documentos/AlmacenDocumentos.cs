using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace MantIA.DAL.Documentos;

public class OpcionesDocumentos
{
    public const string Seccion = "Documentos";

    /// <summary>
    /// Carpeta raiz del almacen. En desarrollo, una carpeta local; en un servidor, un volumen
    /// montado. Tiene que estar FUERA de la carpeta publicada del sitio: si cae adentro, cualquiera
    /// con la URL descarga documentos de cualquier empresa sin pasar por el control de permisos.
    /// </summary>
    public string Raiz { get; set; } = "almacen";
}

/// <summary>Lo que quedo guardado despues de escribir un archivo.</summary>
public record ArchivoGuardado(string Hash, string Ubicacion, long TamanioBytes, bool YaExistia);

public interface IAlmacenDocumentos
{
    /// <summary>Guarda el contenido y devuelve donde quedo. Calcula el hash mientras escribe.</summary>
    Task<ArchivoGuardado> GuardarAsync(Guid empresaId, Stream contenido, CancellationToken ct = default);

    /// <summary>Abre el contenido para leerlo. Lanza si no esta.</summary>
    Task<Stream> AbrirAsync(string ubicacion, CancellationToken ct = default);

    /// <summary>Recalcula el hash de lo almacenado y lo compara con el esperado.</summary>
    Task<bool> VerificarAsync(string ubicacion, string hashEsperado, CancellationToken ct = default);

    Task<bool> ExisteAsync(string ubicacion, CancellationToken ct = default);
}

/// <summary>
/// Almacen de archivos sobre el sistema de archivos, direccionado por contenido.
///
/// <para><b>La ruta sale del hash del contenido, no del nombre.</b> Dos personas que suben el mismo
/// certificado escriben un solo archivo, y el nombre original —que puede traer acentos, barras o el
/// nombre de otro cliente— nunca toca el disco. Ademas hace imposible el caso clasico: un nombre
/// armado a mano con <c>../</c> que termina escribiendo fuera de la carpeta.</para>
///
/// <para><b>Se separa por empresa igual.</b> Aunque el hash ya seria unico, tener las carpetas
/// separadas permite copiar, migrar o entregar los documentos de un cliente sin tocar los de otro, y
/// es lo que hace posible cumplir un pedido de portabilidad sin un filtrado a mano.</para>
///
/// <para><b>Nada se borra.</b> Dar de baja un documento marca su ficha; el contenido queda. Un mismo
/// contenido puede estar referenciado por varias fichas —esa es la contracara del direccionamiento
/// por contenido— y borrarlo al dar de baja una romperia las otras.</para>
///
/// <para>Es la implementacion para desarrollo y para un despliegue en un solo servidor. Con varias
/// instancias hace falta almacenamiento de objetos; la interfaz esta pensada para que ese cambio sea
/// una clase nueva y una linea de registro.</para>
/// </summary>
public class AlmacenDocumentosLocal : IAlmacenDocumentos
{
    private readonly string _raiz;

    public AlmacenDocumentosLocal(IOptions<OpcionesDocumentos> opciones)
    {
        _raiz = Path.GetFullPath(opciones.Value.Raiz);
        Directory.CreateDirectory(_raiz);
    }

    public async Task<ArchivoGuardado> GuardarAsync(
        Guid empresaId, Stream contenido, CancellationToken ct = default)
    {
        // Se escribe primero a un temporal porque el nombre definitivo depende del hash, y el hash
        // no se conoce hasta haber leido todo. Escribir a un temporal y despues mover deja ademas
        // una propiedad util: en el almacen nunca hay un archivo a medio escribir.
        var temporal = Path.Combine(_raiz, $"tmp-{Guid.NewGuid():N}");
        string hash;
        long tamanio;

        try
        {
            await using (var salida = File.Create(temporal))
            using (var sha = SHA256.Create())
            await using (var espejo = new CryptoStream(salida, sha, CryptoStreamMode.Write))
            {
                await contenido.CopyToAsync(espejo, ct);
                await espejo.FlushFinalBlockAsync(ct);
                hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            }

            tamanio = new FileInfo(temporal).Length;

            var ubicacion = UbicacionDe(empresaId, hash);
            var destino = Ruta(ubicacion);

            Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

            if (File.Exists(destino))
            {
                File.Delete(temporal);
                return new ArchivoGuardado(hash, ubicacion, tamanio, YaExistia: true);
            }

            File.Move(temporal, destino);
            return new ArchivoGuardado(hash, ubicacion, tamanio, YaExistia: false);
        }
        catch
        {
            if (File.Exists(temporal)) File.Delete(temporal);
            throw;
        }
    }

    public Task<Stream> AbrirAsync(string ubicacion, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(Ruta(ubicacion)));

    public async Task<bool> VerificarAsync(
        string ubicacion, string hashEsperado, CancellationToken ct = default)
    {
        var ruta = Ruta(ubicacion);
        if (!File.Exists(ruta)) return false;

        await using var entrada = File.OpenRead(ruta);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(entrada, ct)).ToLowerInvariant();

        return string.Equals(hash, hashEsperado, StringComparison.OrdinalIgnoreCase);
    }

    public Task<bool> ExisteAsync(string ubicacion, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(Ruta(ubicacion)));

    /// <summary>
    /// Dos niveles de subcarpeta a partir del hash. Un directorio con cien mil archivos plano es
    /// lento en casi todo sistema de archivos; asi quedan repartidos parejo por construccion.
    /// </summary>
    private static string UbicacionDe(Guid empresaId, string hash) =>
        $"{empresaId:N}/{hash[..2]}/{hash[2..4]}/{hash}";

    /// <summary>
    /// Convierte una ubicacion en ruta real y comprueba que no se escape de la raiz. La ubicacion
    /// sale siempre de la base, pero una fila alterada seria justamente la forma de convertir una
    /// lectura de documento en una lectura de cualquier archivo del servidor.
    /// </summary>
    private string Ruta(string ubicacion)
    {
        var completa = Path.GetFullPath(Path.Combine(_raiz, ubicacion));

        if (!completa.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"La ubicacion '{ubicacion}' apunta fuera del almacen de documentos.");

        return completa;
    }
}
