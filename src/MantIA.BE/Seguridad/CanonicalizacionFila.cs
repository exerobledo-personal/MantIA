using System.Globalization;
using System.Text;

namespace MantIA.BE.Seguridad;

/// <summary>
/// Convierte los campos de una fila en la cadena exacta sobre la que se calcula su digito
/// verificador.
///
/// <para><b>Es el mismo problema que el de la bitacora y se resuelve igual.</b> El digito tiene que
/// dar lo mismo hoy y dentro de tres anos, en otra maquina y con otra version del framework. Por eso
/// no se serializa a JSON: el orden de las propiedades, el formato de un decimal o el de una fecha
/// pueden cambiar entre versiones de la libreria, y cualquiera de esas tres cosas invalidaria de
/// golpe el digito de todas las filas historicas sin que nadie haya tocado un dato.</para>
///
/// <para><b>Prefijo de longitud, por la misma razon.</b> Con un separador, un valor de texto que lo
/// contenga alcanza para fabricar una colision a mano y hacer pasar una fila por otra. Con
/// <c>longitud:valor</c> la frontera entre campos no depende del contenido.</para>
///
/// <para><b>La identidad de la fila entra en el calculo.</b> La tabla y el identificador van
/// primero, de modo que el digito de una fila no vale para otra: copiar un digito valido de una fila
/// barata a una cara deja de servir, que es la forma mas comoda de falsificar si el digito dependiera
/// solo de los valores.</para>
/// </summary>
public static class CanonicalizacionFila
{
    /// <summary>Version del formato. Si cambia como se escribe un valor, sube, y hay que recalcular.</summary>
    public const string Version = "v1";

    /// <summary>
    /// Arma la cadena canonica de una fila. <paramref name="valorDe"/> devuelve el valor
    /// <b>del dominio</b> —en claro, sin cifrar— de cada campo del catalogo.
    /// </summary>
    public static string Canonizar(string entidad, Guid filaId, Func<string, object?> valorDe)
    {
        var campos = CamposSellados.CamposDe(entidad);

        var sb = new StringBuilder(256);

        Campo(sb, Version);
        Campo(sb, entidad);
        Campo(sb, filaId.ToString("N"));

        foreach (var campo in campos)
        {
            // El nombre del campo entra junto al valor: si algun dia se reordena el catalogo o se
            // agrega un campo en el medio, el digito cambia y la fila se marca para recalcular, en
            // lugar de seguir verificando contra una definicion que ya no es la misma.
            Campo(sb, campo);
            Campo(sb, Escribir(valorDe(campo)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escribe un valor de forma estable. Cada tipo tiene una unica representacion posible y no
    /// depende de la cultura de la maquina: en una maquina con cultura espanola, un decimal escrito
    /// con <c>ToString()</c> sale con coma, y el digito calculado en un servidor no verificaria en
    /// otro.
    /// </summary>
    private static string? Escribir(object? valor) => valor switch
    {
        null => null,
        string s => s,
        Guid g => g.ToString("N"),
        bool b => b ? "1" : "0",

        // "O" es el formato de ida y vuelta: precision de 100 ns y desplazamiento explicito.
        // Siempre en UTC, porque la misma fecha guardada con otro desplazamiento es la misma fecha.
        DateTimeOffset f => f.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        DateTime f => f.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),

        // Cantidad fija de decimales. En decimal, 1.5 y 1.50 son iguales como numero pero distintos
        // como texto, y ninguna de las dos formas se conserva al ir y volver de la base. Seis
        // decimales exceden la precision de cualquier importe del sistema.
        decimal d => d.ToString("F6", CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float d => d.ToString("R", CultureInfo.InvariantCulture),

        Enum e => e.ToString(),
        IFormattable n => n.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString()
    };

    private static void Campo(StringBuilder sb, string? valor)
    {
        // Nulo y cadena vacia tienen que ser distinguibles: un motivo borrado y un motivo vacio son
        // hechos distintos, y si colisionan se puede cambiar uno por el otro sin romper el digito.
        if (valor is null)
        {
            sb.Append("-|");
            return;
        }

        sb.Append(valor.Length.ToString(CultureInfo.InvariantCulture))
          .Append(':')
          .Append(valor)
          .Append('|');
    }
}
