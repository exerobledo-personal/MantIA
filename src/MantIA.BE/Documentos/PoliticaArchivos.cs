namespace MantIA.BE.Documentos;

/// <summary>Por que se rechazo un archivo. Cada motivo tiene un mensaje distinto en pantalla.</summary>
public enum RechazoArchivo
{
    Ninguno,
    ExtensionNoPermitida,
    ContenidoNoCoincide,
    DemasiadoGrande,
    Vacio,
    NombreInvalido
}

public record ResultadoValidacion(RechazoArchivo Motivo, string TipoContenido, string Detalle)
{
    public bool Valido => Motivo == RechazoArchivo.Ninguno;

    public static ResultadoValidacion Ok(string tipoContenido) =>
        new(RechazoArchivo.Ninguno, tipoContenido, string.Empty);
}

/// <summary>
/// Que archivos se aceptan y como se comprueba que lo son.
///
/// <para><b>La lista es blanca, no negra.</b> Enumerar lo prohibido es una carrera que se pierde
/// siempre: cada formato ejecutable que aparezca despues entra solo. Enumerar lo permitido falla del
/// lado correcto —un formato legitimo que falta se agrega en una linea, y mientras tanto nadie
/// subio nada raro—.</para>
///
/// <para><b>La extension y el tipo declarado no se creen.</b> Los dos los elige quien sube: renombrar
/// un ejecutable a <c>.pdf</c> es cuestion de un segundo. Lo que se comprueba son los primeros bytes
/// del contenido, que los pone el programa que genero el archivo. Un archivo cuya firma no coincide
/// con su extension se rechaza aunque las dos cosas por separado esten permitidas: eso no es un
/// error de tipeo, es alguien probando.</para>
///
/// <para><b>Que NO se acepta y por que.</b> Nada de Office con macros (<c>.docm</c>, <c>.xlsm</c>),
/// nada comprimido y nada ejecutable. Un ZIP hace que el control de tipos deje de significar algo,
/// porque lo que importa es lo que hay adentro y eso no se ve desde afuera.</para>
/// </summary>
public static class PoliticaArchivos
{
    /// <summary>
    /// Tope por archivo. Un certificado escaneado pesa uno o dos megabytes; veinte deja lugar para
    /// un manual completo sin habilitar que alguien use el sistema como disco.
    /// </summary>
    public const long TamanioMaximoBytes = 20L * 1024 * 1024;

    /// <summary>Cuantos bytes hay que leer para reconocer una firma.</summary>
    public const int BytesDeFirma = 16;

    private record Formato(string TipoContenido, byte[][] Firmas);

    private static readonly IReadOnlyDictionary<string, Formato> Permitidos =
        new Dictionary<string, Formato>(StringComparer.OrdinalIgnoreCase)
        {
            // El caso central: certificados, manuales, informes.
            [".pdf"] = new("application/pdf", [[0x25, 0x50, 0x44, 0x46]]), // %PDF

            // Fotos de la placa, del equipo o de la falla.
            [".jpg"] = new("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
            [".jpeg"] = new("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
            [".png"] = new("image/png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            [".webp"] = new("image/webp", [[0x52, 0x49, 0x46, 0x46]]), // RIFF; el WEBP va en el byte 8

            // Office moderno. Son ZIP por dentro, asi que la firma es la de ZIP: distingue un
            // documento real de un ejecutable renombrado, no de otro ZIP. Es el limite del metodo y
            // por eso las variantes con macros no estan en la lista.
            [".docx"] = new(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06]]),
            [".xlsx"] = new(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06]]),
        };

    /// <summary>Extensiones aceptadas, para el atributo <c>accept</c> del control de carga.</summary>
    public static IEnumerable<string> Extensiones() => Permitidos.Keys;

    public static string Aceptadas() => string.Join(", ", Permitidos.Keys);

    /// <summary>
    /// Valida nombre, tamano y contenido. <paramref name="primerosBytes"/> tiene que traer al menos
    /// <see cref="BytesDeFirma"/> bytes del principio del archivo, o los que haya si es mas corto.
    /// </summary>
    public static ResultadoValidacion Validar(
        string nombreArchivo, long tamanioBytes, ReadOnlySpan<byte> primerosBytes)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
            return new ResultadoValidacion(
                RechazoArchivo.NombreInvalido, string.Empty, "El archivo no tiene nombre.");

        if (tamanioBytes <= 0)
            return new ResultadoValidacion(
                RechazoArchivo.Vacio, string.Empty, "El archivo esta vacio.");

        if (tamanioBytes > TamanioMaximoBytes)
            return new ResultadoValidacion(
                RechazoArchivo.DemasiadoGrande, string.Empty,
                $"El archivo pesa {tamanioBytes / 1024 / 1024} MB y el maximo es " +
                $"{TamanioMaximoBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(nombreArchivo);

        if (!Permitidos.TryGetValue(extension, out var formato))
            return new ResultadoValidacion(
                RechazoArchivo.ExtensionNoPermitida, string.Empty,
                $"No se aceptan archivos {extension}. Formatos permitidos: {Aceptadas()}.");

        // Bucle y no LINQ: un Span no puede capturarse dentro de una lambda, y copiarlo a un arreglo
        // solo para poder usar Any() seria una asignacion por archivo para no escribir tres lineas.
        var reconocido = false;
        foreach (var firma in formato.Firmas)
            if (Empieza(primerosBytes, firma)) { reconocido = true; break; }

        if (!reconocido)
            return new ResultadoValidacion(
                RechazoArchivo.ContenidoNoCoincide, string.Empty,
                $"El contenido del archivo no corresponde a un {extension}. " +
                "Puede estar danado, o tener la extension cambiada.");

        // El tipo de contenido que se guarda sale de la tabla y no de lo que declaro el navegador.
        // Es lo que despues se devuelve al descargar, y devolver un tipo que dijo el que subio es
        // como no haber validado nada.
        return ResultadoValidacion.Ok(formato.TipoContenido);
    }

    private static bool Empieza(ReadOnlySpan<byte> contenido, ReadOnlySpan<byte> firma)
    {
        if (contenido.Length < firma.Length) return false;

        for (var i = 0; i < firma.Length; i++)
            if (contenido[i] != firma[i]) return false;

        return true;
    }
}
