using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Evento de bitácora que no pudo escribirse en MongoDB y quedó guardado en la base del cliente
/// hasta poder reflejarse.
///
/// <para><b>Es la red de contención del servicio.</b> Si Mongo se cae, la alternativa sería frenar
/// la fábrica o perder el registro; ninguna de las dos sirve. Mientras la base principal siga
/// funcionando, la operación continúa y el evento espera acá. Si la base principal también cae, el
/// sistema está caído de todos modos y no hay nada que preservar.</para>
///
/// <para>El evento se guarda serializado, ya enmascarado y cifrado, <b>pero sin sellar</b>: el
/// número de orden y el encadenado se asignan recién al reflejarlo en Mongo. Sellarlo acá
/// obligaría a adivinar qué lugar de la cadena le va a tocar, y ese lugar depende de lo que hayan
/// escrito los demás mientras tanto.</para>
///
/// <para>Consecuencia a tener presente: un evento que pasó por el respaldo conserva su
/// <c>Fecha</c> real, pero su posición en la cadena refleja el momento en que se drenó. La cadena
/// garantiza que nadie alteró el registro, no que el orden coincida con el reloj.</para>
/// </summary>
public class EventoPendiente : TenantEntity
{
    /// <summary>Cadena a la que pertenece: <c>empresa:{id}</c> o <c>plataforma</c>.</summary>
    public string Cadena { get; set; } = string.Empty;

    /// <summary>Evento completo serializado en JSON, tal como debe guardarse.</summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Severidad copiada afuera del JSON para poder priorizar el drenaje sin deserializar.</summary>
    public string Severidad { get; set; } = string.Empty;

    /// <summary>Fecha original del hecho, no la del guardado.</summary>
    public DateTimeOffset FechaEvento { get; set; }

    public int Intentos { get; set; }
    public DateTimeOffset? UltimoIntento { get; set; }
    public string? UltimoError { get; set; }
}
