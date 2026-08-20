using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Intervencion de mantenimiento sobre una maquina. Es la unidad basica del historial de
/// fallas: al cerrarse descuenta stock, enriquece el historial del activo y alimenta al
/// motor de recomendaciones con datos operativos reales.
/// </summary>
public class OrdenTrabajo : TenantEntity, IConcurrencia
{
    /// <summary>Numero legible por humanos (OT-2026-0001). Lo asigna una secuencia, no un conteo.</summary>
    public string Numero { get; set; } = string.Empty;

    public Guid MaquinaId { get; set; }
    public Maquina? Maquina { get; set; }

    public TipoMantenimiento Tipo { get; set; } = TipoMantenimiento.Correctivo;
    public Prioridad Prioridad { get; set; } = Prioridad.Media;
    public EstadoOrden Estado { get; set; } = EstadoOrden.Abierta;

    public string DescripcionProblema { get; set; } = string.Empty;
    /// <summary>Que se hizo. Se vectoriza para agrupar modos de falla equivalentes.</summary>
    public string? DescripcionResolucion { get; set; }

    public Guid? ResponsableUsuarioId { get; set; }
    public Usuario? Responsable { get; set; }

    public DateTimeOffset FechaApertura { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaCierre { get; set; }
    public decimal? HorasResolucion { get; set; }

    public uint Version { get; set; }

    public ICollection<OrdenTrabajoRepuesto> Repuestos { get; set; } = [];
}
