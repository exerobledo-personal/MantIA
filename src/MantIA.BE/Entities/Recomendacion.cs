using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Sugerencia de reposicion generada por el motor.
/// <para>
/// <see cref="Origen"/> distingue los dos mecanismos y es lo que hace auditable al sistema:
/// una <see cref="OrigenRecomendacion.Regla"/> es deterministica y siempre se puede explicar
/// con una condicion verificable; el <see cref="OrigenRecomendacion.Modelo"/> proyecta y por
/// eso lleva <see cref="Confianza"/>.
/// </para>
/// <para>
/// La aceptacion y el rechazo se guardan con su motivo porque son la senal de
/// realimentacion con la que se recalibra el modelo.
/// </para>
/// </summary>
public class Recomendacion : TenantEntity
{
    public Guid RepuestoId { get; set; }
    public Repuesto? Repuesto { get; set; }

    public Guid? MaquinaId { get; set; }
    public Maquina? Maquina { get; set; }

    public OrigenRecomendacion Origen { get; set; }
    public Prioridad Prioridad { get; set; } = Prioridad.Media;
    public EstadoRecomendacion Estado { get; set; } = EstadoRecomendacion.Activa;

    public int CantidadSugerida { get; set; }
    public int StockAlGenerar { get; set; }

    /// <summary>Explicacion en lenguaje natural, visible en la tarjeta.</summary>
    public string Justificacion { get; set; } = string.Empty;

    /// <summary>Regla aplicada, cuando el origen es una regla de negocio.</summary>
    public string? ReglaAplicada { get; set; }

    /// <summary>Confianza del modelo entre 0 y 1. Nula cuando el origen es una regla.</summary>
    public decimal? Confianza { get; set; }

    public DateTimeOffset FechaGeneracion { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FechaDecision { get; set; }
    public Guid? DecididaPorUsuarioId { get; set; }
    public int? CantidadConfirmada { get; set; }
    public string? MotivoRechazo { get; set; }
}
