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

    // ------------------------------------------------------------------ control de la solicitud

    /// <summary>
    /// Quien la reporto. Puede ser cualquier empleado de cualquier area, no solo mantenimiento, y
    /// es el eje por el que ve sus propios pedidos alguien que solo tiene permiso de generar.
    /// </summary>
    public Guid? SolicitanteUsuarioId { get; set; }

    /// <summary>Quien la reviso y decidio si correspondia. Nulo mientras nadie la miro.</summary>
    public Guid? ControladaPorUsuarioId { get; set; }

    public DateTimeOffset? FechaControl { get; set; }

    /// <summary>
    /// Por que se rechazo. Es obligatorio al rechazar: quien reporto algo merece saber por que no se
    /// hizo, y sin motivo la proxima vez no reporta.
    /// </summary>
    public string? MotivoRechazo { get; set; }

    /// <summary>Nacio como pedido y todavia nadie la miro.</summary>
    public bool EsperandoControl => Estado == EstadoOrden.Solicitada;

    public uint Version { get; set; }

    public ICollection<OrdenTrabajoRepuesto> Repuestos { get; set; } = [];

    /// <summary>
    /// Linea de tiempo de la orden. Cada modificacion agrega una fila: el update esta permitido,
    /// pero queda registrado que cambio, de que a que y quien lo hizo.
    /// <para>
    /// La fecha de creacion y la de ultima modificacion vienen de <c>BaseEntity</c> y las sella el
    /// contexto en cada guardado; el historial cuenta lo que paso en el medio.
    /// </para>
    /// </summary>
    public ICollection<HistorialOrdenTrabajo> Historial { get; set; } = [];
}
