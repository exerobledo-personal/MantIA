using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Qué se le hizo a la orden.</summary>
public enum AccionHistorial
{
    Apertura,
    CambioEstado,
    CambioDatos,
    AsignacionResponsable,
    AltaRepuesto,
    BajaRepuesto,
    Cierre,
    Reapertura,
    Anulacion
}

/// <summary>
/// Línea de historial de una orden de trabajo. **Append-only:** cada cambio agrega una fila y
/// ninguna se borra ni se edita.
///
/// <para><b>Por qué existe si ya está la bitácora.</b> Son dos cosas distintas y las dos hacen
/// falta. La bitácora es transversal, vive en MongoDB y responde "qué hizo esta persona en el
/// sistema"; el historial es de la orden, vive al lado de ella y responde "qué le pasó a esta
/// orden". El supervisor que abre la OT-2026-00047 quiere ver su línea de tiempo ahí mismo, no
/// filtrar un log de auditoría. Y la consulta es un <c>JOIN</c> barato en lugar de un viaje a otro
/// motor.</para>
///
/// <para><b>El update sigue permitido.</b> No se bloquea la edición: se registra. Cada modificación
/// deja qué campo cambió, de qué a qué, quién y cuándo. Es la diferencia entre un sistema que impide
/// trabajar y uno que deja trabajar y después puede explicar lo que pasó.</para>
/// </summary>
public class HistorialOrdenTrabajo : TenantEntity
{
    public Guid OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }

    public AccionHistorial Accion { get; set; }

    /// <summary>Campo afectado, cuando la acción es un cambio de datos. Nulo si cambió varias cosas.</summary>
    public string? Campo { get; set; }

    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }

    /// <summary>Texto para mostrar en la línea de tiempo: "Cerrada por Diego Ferrero".</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Motivo escrito, cuando la acción lo exige: anular una orden abierta, reabrir una cerrada,
    /// sacar un repuesto ya cargado.
    /// </summary>
    public string? Motivo { get; set; }

    public Guid UsuarioId { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Enlaza esta línea con su evento de bitácora. Permite saltar de la línea de tiempo de la
    /// orden al registro completo —con su sello de integridad— sin duplicar la información acá.
    /// </summary>
    public Guid? EventoBitacoraId { get; set; }
}
