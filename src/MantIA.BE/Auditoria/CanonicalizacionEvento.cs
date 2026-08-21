using System.Globalization;
using System.Text;

namespace MantIA.BE.Auditoria;

/// <summary>
/// Convierte un evento en la cadena exacta que se sella.
///
/// <para><b>Por que no se serializa a JSON y listo:</b> el sello tiene que dar el mismo resultado
/// hoy y dentro de tres anos, corriendo en otra maquina y con otra version de la libreria. Un
/// serializador puede cambiar el orden de las propiedades, la forma de escribir un decimal o el
/// formato de una fecha entre versiones, y cualquiera de esas tres cosas romperia la verificacion
/// de toda la cadena historica sin que nadie haya tocado un dato.</para>
///
/// <para><b>Por que va con prefijo de longitud:</b> si los campos se pegaran con un separador,
/// dos eventos distintos podrian producir la misma cadena. Un motivo que contenga el separador
/// alcanzaria para fabricar una colision a mano y sellar un evento como si fuera otro. Con
/// <c>longitud:valor</c> eso no es posible: la frontera entre campos no depende del contenido.</para>
///
/// <para>Los campos que se incluyen son los que describen <i>que paso</i>. Se sellan tambien
/// <see cref="EventoBitacora.EstadoAnterior"/> y <see cref="EventoBitacora.EstadoPosterior"/> tal
/// como quedan almacenados —enmascarados y, si corresponde, cifrados—, de modo que verificar la
/// cadena nunca obliga a descifrar nada.</para>
/// </summary>
public static class CanonicalizacionEvento
{
    /// <summary>Version del formato. Si algun dia cambia el conjunto de campos sellados, sube.</summary>
    public const string Version = "v1";

    public static string Canonizar(EventoBitacora e, string? hashAnterior)
    {
        var sb = new StringBuilder(512);

        Campo(sb, Version);
        Campo(sb, e.Id.ToString("N"));
        Campo(sb, hashAnterior);

        // El numero de secuencia NO entra en el sello, y es deliberado. Son dos mecanismos
        // independientes: el numero lo asigna el contador de la base para ordenar, y el sello
        // protege el contenido. Mezclarlos ataria el sello al lugar que le toco al evento en la
        // fila, cuando lo que hay que proteger es lo que dice. El orden ya lo garantiza el
        // encadenado por hash, que es mas fuerte: reordenar rompe la cadena entera.

        Campo(sb, e.Alcance.ToString());
        Campo(sb, e.Tipo.ToString());
        Campo(sb, e.Nivel.ToString());
        Campo(sb, e.Severidad.ToString());

        Campo(sb, e.EmpresaId?.ToString("N"));
        Campo(sb, e.UsuarioId?.ToString("N"));
        Campo(sb, e.UsuarioEmail);
        Campo(sb, e.RolAlMomento);

        Campo(sb, e.Recurso);
        Campo(sb, e.Accion);
        Campo(sb, e.RecursoId?.ToString("N"));
        Campo(sb, e.CorrelacionId?.ToString("N"));
        Campo(sb, e.Descripcion);
        Campo(sb, e.Motivo);

        Campo(sb, e.EstadoAnterior);
        Campo(sb, e.EstadoPosterior);

        Campo(sb, e.Exitoso ? "1" : "0");
        Campo(sb, e.MotivoFallo);
        Campo(sb, e.UsoBypass ? "1" : "0");
        Campo(sb, e.DireccionIp);

        // "O" es el formato de ida y vuelta: precision de 100 ns y desplazamiento explicito.
        // Con cultura invariante, la misma fecha produce la misma cadena en cualquier maquina.
        Campo(sb, e.Fecha.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    private static void Campo(StringBuilder sb, string? valor)
    {
        // Nulo y cadena vacia tienen que ser distinguibles, o dos eventos diferentes colisionan.
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
