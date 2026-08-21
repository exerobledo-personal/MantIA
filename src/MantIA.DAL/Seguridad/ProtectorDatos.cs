using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace MantIA.DAL.Seguridad;

public interface IProtectorDatos
{
    /// <summary>Version de llave vigente. Se guarda junto al dato para poder rotar.</summary>
    string VersionActual { get; }

    /// <summary>Sella un texto con HMAC-SHA256. Devuelve el sello en base64.</summary>
    string Sellar(string contenido, string version);

    /// <summary>Cifra un texto. El resultado incluye el nonce y la etiqueta de autenticacion.</summary>
    string Cifrar(string texto, string version);

    /// <summary>
    /// Cifra de forma que el mismo texto produzca siempre el mismo resultado, para poder buscar por
    /// igualdad e indexar.
    /// <para>
    /// El nonce se deriva del propio texto con HMAC en lugar de sortearse. Eso es exactamente lo que
    /// lo hace repetible, y tambien lo que revela la igualdad entre filas: dos usuarios con el mismo
    /// correo se ven iguales en la base, aunque nadie sepa cual es. Se usa solo donde hay que buscar.
    /// </para>
    /// </summary>
    string CifrarDeterminista(string texto, string version);

    /// <summary>Descifra un texto producido por <see cref="Cifrar"/>.</summary>
    string Descifrar(string cifrado, string version);

    /// <summary>Verdadero si el texto tiene el prefijo que pone <see cref="Cifrar"/>.</summary>
    bool EstaCifrado(string? texto);

    /// <summary>Descifra probando la llave vigente y despues las anteriores.</summary>
    string DescifrarConCualquierLlave(string cifrado);
}

/// <summary>
/// Las primitivas criptograficas de la auditoria. Deliberadamente cortas y sin opciones: cada
/// parametro que se pueda elegir mal es una forma de romper la seguridad sin que nada falle.
///
/// <list type="bullet">
/// <item><b>HMAC-SHA256 para sellar.</b> Un SHA-256 pelado lo recalcula cualquiera que pueda
/// escribir en la base: altera el evento, recalcula la cadena entera y no queda rastro. El HMAC
/// necesita ademas la llave, que vive en la configuracion de la aplicacion.</item>
/// <item><b>AES-256-GCM para cifrar.</b> GCM es cifrado autenticado: ademas de ocultar el
/// contenido, detecta si alguien lo modifico. Con un modo sin autenticar —CBC, por ejemplo— se
/// puede alterar el texto cifrado y el descifrado devuelve basura silenciosamente.</item>
/// <item><b>Un nonce nuevo por operacion.</b> En GCM, repetir el nonce con la misma llave no
/// degrada la seguridad: la destruye. Por eso se genera con el generador criptografico del sistema
/// y nunca se reutiliza.</item>
/// </list>
/// </summary>
public class ProtectorDatos : IProtectorDatos
{
    private const string Prefijo = "enc:";
    private const int BytesNonce = 12;   // 96 bits, el tamano que recomienda GCM
    private const int BytesEtiqueta = 16; // 128 bits
    private const int BytesLlave = 32;   // 256 bits

    private readonly OpcionesAuditoria _opciones;
    private readonly Dictionary<string, byte[]> _llaves;

    public ProtectorDatos(IOptions<OpcionesAuditoria> opciones)
    {
        _opciones = opciones.Value;
        _llaves = _opciones.Llaves.ToDictionary(
            par => par.Key,
            par => DecodificarLlave(par.Key, par.Value),
            StringComparer.OrdinalIgnoreCase);

        if (!_llaves.ContainsKey(_opciones.VersionActual))
            throw new InvalidOperationException(
                $"No hay llave configurada para la version vigente '{_opciones.VersionActual}'. " +
                "Revisar la seccion Auditoria de la configuracion.");
    }

    public string VersionActual => _opciones.VersionActual;

    public string Sellar(string contenido, string version)
    {
        using var hmac = new HMACSHA256(Llave(version));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(contenido)));
    }

    public string Cifrar(string texto, string version) =>
        CifrarCon(texto, version, RandomNumberGenerator.GetBytes(BytesNonce));

    public string CifrarDeterminista(string texto, string version)
    {
        // El nonce sale de un HMAC del propio texto con la misma llave: repetible sin necesidad de
        // guardarlo aparte, y sin que el nonce revele nada de lo que protege.
        using var hmac = new HMACSHA256(Llave(version));
        var derivado = hmac.ComputeHash(Encoding.UTF8.GetBytes("nonce:" + texto));
        return CifrarCon(texto, version, derivado[..BytesNonce]);
    }

    private string CifrarCon(string texto, string version, byte[] nonce)
    {
        var llave = Llave(version);
        var claro = Encoding.UTF8.GetBytes(texto);

        var cifrado = new byte[claro.Length];
        var etiqueta = new byte[BytesEtiqueta];

        using (var aes = new AesGcm(llave, BytesEtiqueta))
            aes.Encrypt(nonce, claro, cifrado, etiqueta);

        // Se concatena nonce + cifrado + etiqueta en un solo valor para que el almacenamiento sea
        // una sola columna y no haya forma de guardar las partes desapareadas.
        var salida = new byte[BytesNonce + cifrado.Length + BytesEtiqueta];
        nonce.CopyTo(salida, 0);
        cifrado.CopyTo(salida, BytesNonce);
        etiqueta.CopyTo(salida, BytesNonce + cifrado.Length);

        return Prefijo + Convert.ToBase64String(salida);
    }

    public string Descifrar(string cifrado, string version)
    {
        if (!EstaCifrado(cifrado)) return cifrado;

        var datos = Convert.FromBase64String(cifrado[Prefijo.Length..]);
        if (datos.Length < BytesNonce + BytesEtiqueta)
            throw new CryptographicException("El valor cifrado esta truncado.");

        var nonce = datos.AsSpan(0, BytesNonce);
        var etiqueta = datos.AsSpan(datos.Length - BytesEtiqueta, BytesEtiqueta);
        var cuerpo = datos.AsSpan(BytesNonce, datos.Length - BytesNonce - BytesEtiqueta);

        var claro = new byte[cuerpo.Length];

        // Si alguien modifico el texto cifrado, Decrypt lanza en lugar de devolver basura.
        using (var aes = new AesGcm(Llave(version), BytesEtiqueta))
            aes.Decrypt(nonce, cuerpo, etiqueta, claro);

        return Encoding.UTF8.GetString(claro);
    }

    public bool EstaCifrado(string? texto) =>
        texto is not null && texto.StartsWith(Prefijo, StringComparison.Ordinal);

    /// <summary>
    /// Descifra probando la llave vigente y despues las anteriores.
    /// <para>
    /// Los campos de las tablas no guardan con que version se cifraron —seria una columna extra por
    /// cada campo cifrado—, asi que se prueba. GCM lo hace seguro: con la llave equivocada la
    /// verificacion de la etiqueta falla y lanza, no devuelve basura. Como las llaves son dos o tres,
    /// el costo es despreciable.
    /// </para>
    /// </summary>
    public string DescifrarConCualquierLlave(string cifrado)
    {
        if (!EstaCifrado(cifrado)) return cifrado;

        var versiones = new[] { _opciones.VersionActual }
            .Concat(_llaves.Keys.Where(v => v != _opciones.VersionActual));

        foreach (var version in versiones)
        {
            try { return Descifrar(cifrado, version); }
            catch (CryptographicException) { }
        }

        throw new CryptographicException(
            "Ninguna de las llaves configuradas descifra el valor. O falta una llave rotada, o el " +
            "dato fue alterado.");
    }

    private byte[] Llave(string version) =>
        _llaves.TryGetValue(version, out var llave)
            ? llave
            : throw new InvalidOperationException(
                $"No hay llave para la version '{version}'. Una llave rotada no se elimina nunca: " +
                "sin ella los eventos firmados con esa version quedan inverificables.");

    private static byte[] DecodificarLlave(string version, string base64)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64); }
        catch (FormatException) { throw new InvalidOperationException($"La llave '{version}' no es base64 valido."); }

        if (bytes.Length != BytesLlave)
            throw new InvalidOperationException(
                $"La llave '{version}' tiene {bytes.Length} bytes y debe tener {BytesLlave}. " +
                "Generar con: openssl rand -base64 32");

        return bytes;
    }
}
