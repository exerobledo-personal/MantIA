using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Alerta por stock bajo el umbral configurado.
/// <para>
/// Se PERSISTE en lugar de derivarse en cada consulta, y la razon es la trazabilidad:
/// hay que poder responder "cuantas veces estuvimos en quiebre el mes pasado" aunque hoy
/// el stock este cubierto. La alerta no se borra al resolverse: se marca resuelta.
/// </para>
/// </summary>
public class AlertaStock : TenantEntity
{
    public Guid RepuestoId { get; set; }
    public Repuesto? Repuesto { get; set; }

    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Activa;
    public Criticidad Criticidad { get; set; } = Criticidad.Media;

    /// <summary>Valores al momento del disparo. Congelados: explican por que se genero.</summary>
    public int StockAlDisparar { get; set; }
    public int UmbralAlDisparar { get; set; }

    public DateTimeOffset FechaDisparo { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaResolucion { get; set; }
    public Guid? ResueltaPorUsuarioId { get; set; }
}
