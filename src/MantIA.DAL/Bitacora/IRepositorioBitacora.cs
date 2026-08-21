using MantIA.BE.Auditoria;

namespace MantIA.DAL.Bitacora;

/// <summary>
/// Acceso al almacen de la bitacora.
///
/// <para><b>No hay metodo de modificacion ni de borrado, y es a proposito.</b> La bitacora es
/// append-only: si el repositorio ofreciera <c>Actualizar</c>, tarde o temprano alguien lo usaria
/// "para corregir un typo" y la cadena de sellos dejaria de verificar sin que nadie entienda por
/// que. La unica baja posible es por vencimiento de retencion, y la hace el motor solo.</para>
/// </summary>
public interface IRepositorioBitacora
{
    /// <summary>Ultimo eslabon de una cadena. Nulo si la cadena todavia no tiene eventos.</summary>
    Task<EventoBitacora?> UltimoAsync(string cadena, CancellationToken ct = default);

    /// <summary>
    /// Agrega el evento resolviendo la secuencia y el encadenado. Devuelve el evento tal como
    /// quedo almacenado.
    /// <para>
    /// Recibe una funcion de sellado en lugar de un evento ya sellado porque el numero de secuencia
    /// solo se conoce al momento de insertar: si dos operaciones del mismo tenant escriben a la vez,
    /// una de las dos tiene que recalcular su sello con el eslabon correcto.
    /// </para>
    /// </summary>
    Task<EventoBitacora> AgregarAsync(
        EventoBitacora evento,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default);

    /// <summary>
    /// Eventos de una cadena en orden de secuencia. El rango es inclusivo en ambos extremos.
    /// </summary>
    IAsyncEnumerable<EventoBitacora> RecorrerAsync(
        string cadena, long desde, long hasta, CancellationToken ct = default);
}
