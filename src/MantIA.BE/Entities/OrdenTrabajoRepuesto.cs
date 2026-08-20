using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Repuesto consumido en una orden. Al cerrar la orden, cada linea genera su
/// <see cref="MovimientoStock"/> correspondiente dentro de la misma transaccion.
/// </summary>
public class OrdenTrabajoRepuesto : TenantEntity
{
    public Guid OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }

    public Guid RepuestoId { get; set; }
    public Repuesto? Repuesto { get; set; }

    public int Cantidad { get; set; }

    /// <summary>Costo al momento del consumo. Se congela: el precio de hoy no reescribe la historia.</summary>
    public decimal CostoUnitarioAlConsumo { get; set; }
}
