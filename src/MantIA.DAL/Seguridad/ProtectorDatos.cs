using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace MantIA.DAL.Seguridad;

public interface IProtectorDatos
{
    /// <summary>Version de llave de sellado vigente. Se guarda junto al evento para poder rotar.</summary>
    string VersionSello { get; }

    /// <summary>Sella un texto con HMAC-SHA256. Devuelve el sello en base64.</summary>
    string Sellar(string contenido, string version);

    /// <summary>
    /// Cifra un texto, atandolo a su contexto.
    /// <para>
    /// El <paramref name="contexto"/> no se guarda: entra en el calculo de la etiqueta de
    /// autenticacion. Un valor cifrado para un contexto no descifra en otro, de modo que copiar el
    /// texto cifrado de una columna a otra deja de funcionar en lugar de pasar desapercibido.
    /// </para>
    /// </summary>
    string Cifrar(string texto, string contexto);

    /// <summary>
    /// Cifra de forma que el mismo texto produzca siempre el mismo resultado, para poder buscar por
    /// igualdad e indexar.
    /// <para>
    /// El nonce se deriva del propio texto con HMAC en lugar de sortearse. Eso es exactamente lo que
    /// lo hace repetible, y tambien lo que revela la igualdad entre filas: dos usuarios con el mismo
    /// correo se ven iguales en la base, aunque nadie sepa cual es. Se usa solo donde hay que buscar.
    /// </para>
    /// </summary>
    string CifrarDeterminista(string texto, string contexto);

    /// <summary>
    /// Descifra probando la llave vigente y despues las anteriores. Falla si el
    /// <paramref name="contexto"/> no es el mismo con el que se cifro.
    /// </summary>
    string Descifrar(string cifrado, string contexto);

    /// <summary>Verdadero si el texto tiene el prefijo que pone <see cref="Cifrar"/>.</summary>
    bool EstaCifrado(string? texto);
}

/// <summary>
/// Las primitivas criptograficas del sistema. Deliberadamente cortas y sin opciones: cada parametro
/// que se pueda elegir mal es una forma de romper la seguridad sin que nada falle.
///
/// <list type="bullet">
/// <item><b>Dos juegos de llaves.</b> Sellar y cifrar usan llaves distintas. Con una sola, quien la
/// obtenga puede a la vez leer los datos cifrados y falsificar la cadena de auditoria que deberia
/// delatarlo; con dos, comprometer una no da la otra.</item>
/// <item><b>HMAC-SHA256 para sellar.</b> Un SHA-256 pelado lo recalcula cualquiera que pueda escribir
/// en la base: altera el evento, recalcula la cadena entera y no queda rastro. El HMAC necesita
/// ademas la llave, que vive en la configuracion de la aplicacion.</item>
/// <item><b>AES-256-GCM para cifrar.</b> GCM es cifrado autenticado: ademas de ocultar el contenido,
/// detecta si alguien lo modifico. Con un modo sin autenticar —CBC, por ejemplo— se puede alterar el
/// texto cifrado y el descifrado devuelve basura silenciosamente.</item>
/// <item><b>Un nonce nuevo por operacion.</b> En GCM, repetir el nonce con la misma llave no degrada
/// la seguridad: la destruye. Por eso se genera con el generador criptografico del sistema y nunca
/// se reutiliza, salvo en el modo determinista, donde se deriva del texto a proposito.</item>
/// </list>
///
/// <para><b>Lo que esto NO resuelve, y conviene tener presente:</b> con la llave en la mano, ningun
/// mecanismo con llave detiene a nadie — puede recalcular todo y quedar consistente. La defensa real
/// contra ese caso es publicar periodicamente el hash de la punta de cada cadena fuera del sistema:
/// se puede reescribir la base entera, pero no un hash que ya se publico ayer en otro lado.</para>
/// </summary>
public class ProtectorDatos : IProtectorDatos
{
    private const string Prefijo = "enc:";
    private const int BytesNonce = 12;    // 96 bits, el tamano que recomienda GCM
    private const int BytesEtiqueta = 16; // 128 bits
    private const int BytesLlave = 32;    // 256 bits

    private readonly Juego _sello;
    private readonly Juego _cifrado;

    public ProtectorDatos(IOptions<OpcionesAuditoria> opciones)
    {
        _sello = new Juego("Auditoria:Sello", opciones.Value.Sello);
        _cifrado = new Juego("Auditoria:Cifrado", opciones.Value.Cifrado);

        if (_sello.MismaLlaveQue(_cifrado))
            throw new InvalidOperationException(
                "Las llaves de sellado y de cifrado son iguales. Tienen que ser distintas: con una " +
                "sola, quien la obtenga puede leer los datos y ademas falsificar la bitacora que " +
                "deberia delatarlo. Generar dos con: openssl rand -base64 32");
    }

    public string VersionSello => _sello.VersionActual;

    // ------------------------------------------------------------------ integridad

    public string Sellar(string contenido, string version)
    {
        using var hmac = new HMACSHA256(_sello.Llave(version));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(contenido)));
    }

    // ------------------------------------------------------------------ confidencialidad

    public string Cifrar(string texto, string contexto) =>
        CifrarCon(texto, contexto, RandomNumberGenerator.GetBytes(BytesNonce));

    public string CifrarDeterminista(string texto, string contexto)
    {
        // El nonce sale de un HMAC del propio texto y su contexto: repetible sin necesidad de
        // guardarlo aparte, y sin que el nonce revele nada de lo que protege. Incluir el contexto
        // hace que el mismo correo en dos columnas distintas no produzca el mismo texto cifrado.
        using var hmac = new HMACSHA256(_cifrado.Llave(_cifrado.VersionActual));
        var derivado = hmac.ComputeHash(Encoding.UTF8.GetBytes($"nonce:{contexto}:{texto}"));
        return CifrarCon(texto, contexto, derivado[..BytesNonce]);
    }

    /// <summary>
    /// Descifra probando la llave vigente y despues las anteriores.
    /// <para>
    /// Los campos no guardan con que version se cifraron —seria una columna extra por cada campo
    /// cifrado—, asi que se prueba. GCM lo hace seguro: con la llave equivocada la verificacion de
    /// la etiqueta falla y lanza, no devuelve basura. Como las llaves son dos o tres, el costo es
    /// despreciable.
    /// </para>
    /// </summary>
    public string Descifrar(string cifrado, string contexto)
    {
        if (!EstaCifrado(cifrado)) return cifrado;

        foreach (var version in _cifrado.VersionesPorPreferencia())
        {
            try { return DescifrarCon(cifrado, _cifrado.Llave(version), contexto); }
            catch (CryptographicException) { }
        }

        throw new CryptographicException(
            $"No se pudo descifrar el valor en el contexto '{contexto}'. Puede ser una llave " +
            "rotada que falta, un dato alterado, o un valor cifrado que fue movido desde otra " +
            "columna o tabla.");
    }

    public bool EstaCifrado(string? texto) =>
        texto is not null && texto.StartsWith(Prefijo, StringComparison.Ordinal);

    // ------------------------------------------------------------------ mecanica

    private string CifrarCon(string texto, string contexto, byte[] nonce)
    {
        var claro = Encoding.UTF8.GetBytes(texto);
        var cifrado = new byte[claro.Length];
        var etiqueta = new byte[BytesEtiqueta];
        var atadura = Encoding.UTF8.GetBytes(contexto);

        // El contexto va como "dato autenticado adicional": no se cifra ni se guarda, pero sin el
        // mismo valor la etiqueta no verifica y el descifrado falla.
        using (var aes = new AesGcm(_cifrado.Llave(_cifrado.VersionActual), BytesEtiqueta))
            aes.Encrypt(nonce, claro, cifrado, etiqueta, atadura);

        // Se concatena nonce + cifrado + etiqueta en un solo valor para que el almacenamiento sea
        // una sola columna y no haya forma de guardar las partes desapareadas.
        var salida = new byte[BytesNonce + cifrado.Length + BytesEtiqueta];
        nonce.CopyTo(salida, 0);
        cifrado.CopyTo(salida, BytesNonce);
        etiqueta.CopyTo(salida, BytesNonce + cifrado.Length);

        return Prefijo + Convert.ToBase64String(salida);
    }

    private static string DescifrarCon(string cifrado, byte[] llave, string contexto)
    {
        var datos = Convert.FromBase64String(cifrado[Prefijo.Length..]);
        if (datos.Length < BytesNonce + BytesEtiqueta)
            throw new CryptographicException("El valor cifrado esta truncado.");

        var nonce = datos.AsSpan(0, BytesNonce);
        var etiqueta = datos.AsSpan(datos.Length - BytesEtiqueta, BytesEtiqueta);
        var cuerpo = datos.AsSpan(BytesNonce, datos.Length - BytesNonce - BytesEtiqueta);
        var claro = new byte[cuerpo.Length];

        // Si alguien modifico el texto cifrado —o lo movio a otra columna, con lo cual el contexto
        // ya no coincide— Decrypt lanza en lugar de devolver basura.
        using (var aes = new AesGcm(llave, BytesEtiqueta))
            aes.Decrypt(nonce, cuerpo, etiqueta, claro, Encoding.UTF8.GetBytes(contexto));

        return Encoding.UTF8.GetString(claro);
    }

    /// <summary>Un juego de llaves ya decodificado y validado.</summary>
    private sealed class Juego
    {
        private readonly Dictionary<string, byte[]> _llaves;
        private readonly string _nombre;

        public Juego(string nombre, JuegoLlaves configuracion)
        {
            _nombre = nombre;
            VersionActual = configuracion.VersionActual;

            _llaves = configuracion.Llaves.ToDictionary(
                par => par.Key,
                par => Decodificar(nombre, par.Key, par.Value),
                StringComparer.OrdinalIgnoreCase);

            if (!_llaves.ContainsKey(VersionActual))
                throw new InvalidOperationException(
                    $"No hay llave configurada para la version vigente '{VersionActual}' de " +
                    $"{nombre}. Revisar la configuracion.");
        }

        public string VersionActual { get; }

        public byte[] Llave(string version) =>
            _llaves.TryGetValue(version, out var llave)
                ? llave
                : throw new InvalidOperationException(
                    $"No hay llave '{version}' en {_nombre}. Una llave rotada no se elimina nunca: " +
                    "sin ella, lo protegido con esa version queda irrecuperable.");

        /// <summary>La vigente primero: es la que va a servir en la enorme mayoria de los casos.</summary>
        public IEnumerable<string> VersionesPorPreferencia() =>
            new[] { VersionActual }.Concat(_llaves.Keys.Where(v => v != VersionActual));

        public bool MismaLlaveQue(Juego otro) =>
            _llaves.Values.Any(a => otro._llaves.Values.Any(b => a.SequenceEqual(b)));

        private static byte[] Decodificar(string juego, string version, string base64)
        {
            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException)
            {
                throw new InvalidOperationException($"La llave '{version}' de {juego} no es base64 valido.");
            }

            if (bytes.Length != BytesLlave)
                throw new InvalidOperationException(
                    $"La llave '{version}' de {juego} tiene {bytes.Length} bytes y debe tener " +
                    $"{BytesLlave}. Generar con: openssl rand -base64 32");

            return bytes;
        }
    }
}
