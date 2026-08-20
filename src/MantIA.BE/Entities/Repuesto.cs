using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Repuesto critico del inventario de una empresa.
/// <para>
/// <b>Sobre el stock:</b> la fuente de verdad es el libro de <see cref="MovimientoStock"/>.
/// <see cref="StockActual"/> es una denormalizacion que se actualiza dentro de la misma
/// transaccion que inserta el movimiento, para no tener que sumar el historico en cada
/// lectura. La invariante es verificable: la suma de los movimientos debe dar exactamente
/// <see cref="StockActual"/>.
/// </para>
/// <para>
/// Por eso lleva <see cref="Version"/>: el campo denormalizado es el unico punto de
/// contencion, y ante un conflicto la operacion se reintenta. Como la operacion de negocio
/// es "sumar N", no "escribir N", el reintento siempre converge al valor correcto.
/// </para>
/// </summary>
public class Repuesto : TenantEntity, IBajaLogica, IConcurrencia
{
    public string Nombre { get; set; } = string.Empty;
    public string NumeroParte { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = "Unidad";

    public int StockActual { get; set; }
    public int StockMinimo { get; set; }

    public Criticidad Criticidad { get; set; } = Criticidad.Media;

    public decimal CostoUnitario { get; set; }
    public string? Proveedor { get; set; }
    /// <summary>Dias que tarda el proveedor en reponer. Alimenta la cobertura estimada.</summary>
    public int PlazoReposicionDias { get; set; }

    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTimeOffset? FechaBaja { get; set; }

    public uint Version { get; set; }

    public ICollection<MaquinaRepuesto> Maquinas { get; set; } = [];
    public ICollection<MovimientoStock> Movimientos { get; set; } = [];
}
