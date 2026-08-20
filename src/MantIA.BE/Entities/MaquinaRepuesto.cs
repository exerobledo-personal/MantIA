using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Relacion entre una maquina y un repuesto critico. Un repuesto puede servir a varias
/// maquinas, y una maquina depende de varios repuestos.
/// </summary>
public class MaquinaRepuesto : TenantEntity
{
    public Guid MaquinaId { get; set; }
    public Maquina? Maquina { get; set; }

    public Guid RepuestoId { get; set; }
    public Repuesto? Repuesto { get; set; }

    /// <summary>
    /// Cuantas unidades lleva esta maquina. Un equipo con tres valvulas iguales necesita
    /// tres, y eso cambia el calculo de cobertura.
    /// </summary>
    public int CantidadPorEquipo { get; set; } = 1;
}
