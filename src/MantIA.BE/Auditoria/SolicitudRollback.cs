using MantIA.BE.Common;

namespace MantIA.BE.Auditoria;

public enum EstadoRollback { Solicitado, Aprobado, Aplicado, Rechazado, AplicadoParcial }

/// <summary>
/// Pedido de reversion de una o varias acciones de un usuario.
///
/// <para>Existe para un escenario concreto y real: alguien que se va de la empresa y borra
/// o altera registros antes de irse. Poder deshacer eso en bloque, y no registro por
/// registro a mano, es la diferencia entre un incidente y un desastre.</para>
///
/// <para><b>La reversion nunca borra el historial.</b> Cada accion revertida genera su propio
/// evento de bitacora: queda registrado que se hizo, quien lo pidio, quien lo aprobo y sobre
/// que eventos se aplico. Un rollback es tan auditable como lo que revierte.</para>
///
/// <para>Requiere aprobacion de un rol distinto al que lo solicita. Si el mismo que puede
/// romper puede deshacer sin control, el mecanismo se vuelve el problema.</para>
/// </summary>
public class SolicitudRollback : TenantEntity
{
    /// <summary>Usuario cuyas acciones se quieren revertir.</summary>
    public Guid UsuarioObjetivoId { get; set; }

    /// <summary>Ventana de tiempo a revertir.</summary>
    public DateTimeOffset Desde { get; set; }
    public DateTimeOffset Hasta { get; set; }

    /// <summary>Acota a un recurso puntual. Nulo revierte todo lo de la ventana.</summary>
    public string? RecursoFiltro { get; set; }

    public string Motivo { get; set; } = string.Empty;
    public EstadoRollback Estado { get; set; } = EstadoRollback.Solicitado;

    public Guid SolicitadaPorUsuarioId { get; set; }
    public DateTimeOffset FechaSolicitud { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Debe ser distinto de quien solicita.</summary>
    public Guid? AprobadaPorUsuarioId { get; set; }
    public DateTimeOffset? FechaAprobacion { get; set; }
    public string? MotivoRechazo { get; set; }

    public int EventosAlcanzados { get; set; }
    public int EventosRevertidos { get; set; }
    /// <summary>Eventos que no se pudieron revertir y por que. Un rollback parcial se informa, no se oculta.</summary>
    public string? EventosNoRevertidos { get; set; }

    public DateTimeOffset? FechaAplicacion { get; set; }
}
