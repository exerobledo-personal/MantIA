using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Asiento del libro de stock. <b>Es inmutable:</b> nunca se edita ni se borra. Un error
/// se corrige agregando un movimiento de <see cref="TipoMovimientoStock.Ajuste"/>.
/// <para>
/// Esta es la respuesta al problema de concurrencia. Dos operaciones simultaneas sobre el
/// mismo repuesto insertan dos filas distintas y no compiten entre si; lo unico que se
/// coordina es el contador denormalizado de <see cref="Repuesto.StockActual"/>.
/// El diseno sigue siendo valido si en el futuro se agrega compra automatica, porque un
/// proceso desatendido tambien deja su asiento.
/// </para>
/// </summary>
public class MovimientoStock : TenantEntity
{
    public Guid RepuestoId { get; set; }
    public Repuesto? Repuesto { get; set; }

    public TipoMovimientoStock Tipo { get; set; }

    /// <summary>
    /// Cantidad con signo: positiva suma al stock, negativa lo resta. El signo debe ser
    /// coherente con <see cref="Tipo"/> y esa validacion vive en la capa de negocio.
    /// </summary>
    public int Cantidad { get; set; }

    /// <summary>Saldo resultante despues de aplicar este movimiento. Permite auditar sin recalcular.</summary>
    public int SaldoResultante { get; set; }

    /// <summary>Orden que origino el movimiento, cuando corresponde.</summary>
    public Guid? OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }

    public string? Motivo { get; set; }
    public DateTimeOffset FechaMovimiento { get; set; } = DateTimeOffset.UtcNow;
}
