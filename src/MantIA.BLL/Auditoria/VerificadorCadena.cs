using MantIA.BE.Auditoria;
using MantIA.DAL.Bitacora;
using MantIA.DAL.Seguridad;

namespace MantIA.BLL.Auditoria;

/// <summary>Qué se encontró al verificar un tramo de la cadena.</summary>
public record ResultadoVerificacion(
    string Cadena,
    long Desde,
    long Hasta,
    int EventosRevisados,
    IReadOnlyList<FallaIntegridad> Fallas,
    int EventosSinSellar = 0)
{
    public bool Integra => Fallas.Count == 0;

    /// <summary>
    /// Verdadero si ademas de integra esta completa: no hay eslabones esperando cerrarse. Un
    /// puñado de eventos sin sellar en la cola es normal; muchos, o los mismos vuelta tras vuelta,
    /// indican que el trabajo de mantenimiento no esta corriendo o que hay un hueco permanente.
    /// </summary>
    public bool Cerrada => Integra && EventosSinSellar == 0;
}

public enum TipoFalla
{
    /// <summary>El sello no coincide con el contenido: el evento fue modificado.</summary>
    SelloInvalido,
    /// <summary>El eslabon no apunta al hash del anterior: se inserto o se reemplazo un evento.</summary>
    CadenaRota,
    /// <summary>Falta un numero de secuencia: se elimino un evento del medio.</summary>
    SecuenciaFaltante,
    /// <summary>El evento dice estar firmado con una llave que no esta configurada.</summary>
    LlaveDesconocida
}

public record FallaIntegridad(long Secuencia, Guid EventoId, TipoFalla Tipo, string Detalle);

public interface IVerificadorCadena
{
    Task<ResultadoVerificacion> VerificarAsync(
        string cadena, long desde = 1, long hasta = long.MaxValue, CancellationToken ct = default);
}

/// <summary>
/// Recorre una cadena y comprueba que nadie la haya tocado.
///
/// <para>Detecta tres cosas distintas, y la distincion importa para saber que paso:</para>
/// <list type="bullet">
/// <item><b>Sello invalido</b>: el contenido del evento cambio despues de escrito.</item>
/// <item><b>Cadena rota</b>: el evento es integro pero no engancha con el anterior. Alguien
/// reemplazo un eslabon entero o inserto uno nuevo.</item>
/// <item><b>Secuencia faltante</b>: un numero que no esta. Alguien borro del medio y ni siquiera
/// intento disimular renumerando.</item>
/// </list>
///
/// <para><b>La verificacion no descifra nada.</b> El sello se calcula sobre lo que se almacena, asi
/// que auditar la integridad de un tenant no expone ni un solo dato de negocio. Es lo que permite
/// que la verificacion pueda correr como tarea periodica sin ser en si misma un riesgo.</para>
/// </summary>
public class VerificadorCadena : IVerificadorCadena
{
    private readonly IRepositorioBitacora _repositorio;
    private readonly IProtectorDatos _protector;

    public VerificadorCadena(IRepositorioBitacora repositorio, IProtectorDatos protector)
    {
        _repositorio = repositorio;
        _protector = protector;
    }

    public async Task<ResultadoVerificacion> VerificarAsync(
        string cadena, long desde = 1, long hasta = long.MaxValue, CancellationToken ct = default)
    {
        var fallas = new List<FallaIntegridad>();
        var revisados = 0;

        string? hashEsperado = null;
        long? secuenciaAnterior = null;
        var primero = true;

        var sinSellar = 0;

        await foreach (var evento in _repositorio.RecorrerAsync(cadena, desde, hasta, ct))
        {
            revisados++;

            // Un evento sin sellar no es una falla: es la cola de la cadena, esperando que el
            // trabajo de mantenimiento la cierre. Reportarlo como manipulacion seria gritar cada
            // vez que alguien escribe algo.
            if (!evento.Sellado)
            {
                sinSellar++;
                secuenciaAnterior = evento.Secuencia;
                continue;
            }

            if (secuenciaAnterior is { } previa && evento.Secuencia != previa + 1)
                fallas.Add(new FallaIntegridad(
                    evento.Secuencia, evento.Id, TipoFalla.SecuenciaFaltante,
                    $"Se esperaba la secuencia {previa + 1} y llego {evento.Secuencia}. " +
                    $"Faltan {evento.Secuencia - previa - 1} eventos."));

            // El primer evento del tramo no se puede encadenar si no arrancamos desde el principio:
            // su HashAnterior apunta a un evento que quedo fuera del rango pedido.
            if (!primero && evento.HashAnterior != hashEsperado)
                fallas.Add(new FallaIntegridad(
                    evento.Secuencia, evento.Id, TipoFalla.CadenaRota,
                    "El evento no engancha con el hash del anterior."));

            try
            {
                var esperado = _protector.Sellar(
                    CanonicalizacionEvento.Canonizar(evento, evento.HashAnterior),
                    evento.VersionLlave);

                if (!CryptographicEquals(esperado, evento.Hash ?? string.Empty))
                    fallas.Add(new FallaIntegridad(
                        evento.Secuencia, evento.Id, TipoFalla.SelloInvalido,
                        "El contenido del evento no coincide con su sello."));
            }
            catch (InvalidOperationException ex)
            {
                fallas.Add(new FallaIntegridad(
                    evento.Secuencia, evento.Id, TipoFalla.LlaveDesconocida, ex.Message));
            }

            hashEsperado = evento.Hash;
            secuenciaAnterior = evento.Secuencia;
            primero = false;
        }

        return new ResultadoVerificacion(cadena, desde, hasta, revisados, fallas, sinSellar);
    }

    /// <summary>
    /// Comparacion en tiempo constante. Comparar sellos con == filtra informacion por el tiempo
    /// que tarda en encontrar la primera diferencia, y con suficientes intentos eso alcanza para
    /// reconstruir un sello valido byte a byte.
    /// </summary>
    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diferencia = 0;
        for (var i = 0; i < a.Length; i++) diferencia |= a[i] ^ b[i];
        return diferencia == 0;
    }
}
