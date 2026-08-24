using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Digito verificador del conjunto de una tabla en un momento dado: el "DV vertical".
///
/// <para><b>Que agrega sobre el digito de fila.</b> El de fila detecta que una fila cambio; no
/// detecta que una fila <i>desaparecio</i>. Quien borre el movimiento y su digito juntos deja las dos
/// tablas perfectamente consistentes entre si. El digito vertical resume <b>todas</b> las filas de la
/// tabla —cuantas son y cuales son— asi que la ausencia de una cambia el resultado.</para>
///
/// <para><b>Por que es una foto y no un valor unico que se mantiene.</b> Recalcular el resumen de una
/// tabla entera en cada escritura es carisimo y ademas serializa todas las escrituras contra una
/// misma fila. Aca se toma una foto cada tanto, en el trabajo de fondo. Entre dos fotos, un cambio
/// legitimo y uno ilegitimo se ven igual; lo que los distingue es la bitacora, que dice que se hizo
/// en ese intervalo. El vertical acota <b>cuando</b> paso y prueba que <b>algo</b> paso; la bitacora
/// dice si eso fue una operacion real.</para>
///
/// <para><b>Las fotos van encadenadas.</b> Cada una incluye el digito de la anterior, igual que la
/// bitacora. Sin eso, se podria borrar una fila y reescribir la ultima foto para que cierre; con la
/// cadena hay que reescribir todas las fotos hacia atras, y ninguna de esas escrituras es
/// silenciosa.</para>
///
/// <para><b>Es append-only.</b> Ni se edita ni se borra: cada pasada agrega una foto nueva.</para>
/// </summary>
public class SelloTabla : TenantEntity
{
    /// <summary>Nombre de la entidad, tal como figura en <c>CamposSellados</c>.</summary>
    public string Tabla { get; set; } = string.Empty;

    /// <summary>Numero de foto dentro de la serie de esta empresa y tabla. Arranca en 1.</summary>
    public long Secuencia { get; set; }

    /// <summary>Cuantas filas se resumieron. Un salto sin operaciones que lo expliquen es la senal.</summary>
    public long Filas { get; set; }

    /// <summary>
    /// Cuantas de esas filas no verificaron contra su propio digito al momento de la foto. Cero es
    /// lo normal; cualquier otro numero es un hallazgo que ya quedo registrado.
    /// </summary>
    public long FilasConDigitoInvalido { get; set; }

    /// <summary>HMAC-SHA256 en base64 del conjunto.</summary>
    public string Digito { get; set; } = string.Empty;

    /// <summary>Digito de la foto anterior de la misma serie. Nulo en la primera.</summary>
    public string? DigitoAnterior { get; set; }

    public string VersionLlave { get; set; } = string.Empty;
    public string VersionFormato { get; set; } = string.Empty;

    public DateTimeOffset CalculadoEn { get; set; } = DateTimeOffset.UtcNow;
}
