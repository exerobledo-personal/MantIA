using System.Text.Json;
using MantIA.BE.Auditoria;
using MantIA.BE.Entities;
using MantIA.DAL.Context;
using Microsoft.Extensions.Logging;

namespace MantIA.DAL.Bitacora;

/// <summary>
/// Envoltorio que hace que una caída de MongoDB no frene la operación.
///
/// <para>El flujo es el que pediste: se intenta escribir en Mongo; si falla, el evento se guarda en
/// la base del propio cliente y la acción de negocio continúa. Un trabajo de fondo reintenta
/// después y lo refleja en Mongo. <b>Si la base del cliente también está caída, no se hace nada
/// más</b>: el sistema entero está caído en ese punto y no hay servicio que preservar.</para>
///
/// <para>Va como envoltorio y no dentro del repositorio de Mongo para que el respaldo sea una
/// decisión de composición: en un entorno donde se prefiera que la bitácora sea bloqueante, se
/// registra el repositorio de Mongo pelado y listo.</para>
/// </summary>
public class RepositorioBitacoraConRespaldo : IRepositorioBitacora
{
    private readonly RepositorioBitacoraMongo _mongo;
    private readonly MantIADbContext _db;
    private readonly ILogger<RepositorioBitacoraConRespaldo> _log;

    public RepositorioBitacoraConRespaldo(
        RepositorioBitacoraMongo mongo,
        MantIADbContext db,
        ILogger<RepositorioBitacoraConRespaldo> log)
    {
        _mongo = mongo;
        _db = db;
        _log = log;
    }

    public Task<EventoBitacora?> UltimoAsync(string cadena, CancellationToken ct = default) =>
        _mongo.UltimoAsync(cadena, ct);

    public IAsyncEnumerable<EventoBitacora> RecorrerAsync(
        string cadena, long desde, long hasta, CancellationToken ct = default) =>
        _mongo.RecorrerAsync(cadena, desde, hasta, ct);

    public async Task<EventoBitacora> AgregarAsync(
        EventoBitacora evento,
        Func<EventoBitacora, string?, string> sellar,
        CancellationToken ct = default)
    {
        try
        {
            return await _mongo.AgregarAsync(evento, sellar, ct);
        }
        catch (Exception ex)
        {
            // No se atrapa la cancelacion: si el usuario aborto el pedido, no hay nada que respaldar.
            if (ex is OperationCanceledException) throw;

            _log.LogWarning(ex,
                "No se pudo escribir en la bitacora de MongoDB. El evento queda en respaldo local " +
                "y se reintenta. Cadena {Cadena}, recurso {Recurso}.{Accion}",
                evento.Cadena, evento.Recurso, evento.Accion);

            await GuardarEnRespaldoAsync(evento, ct);
            return evento;
        }
    }

    private async Task GuardarEnRespaldoAsync(EventoBitacora evento, CancellationToken ct)
    {
        _db.EventosPendientes.Add(new EventoPendiente
        {
            EmpresaId = evento.EmpresaId ?? Guid.Empty,
            Cadena = evento.Cadena,
            Contenido = JsonSerializer.Serialize(evento),
            Severidad = evento.Severidad.ToString(),
            FechaEvento = evento.Fecha,
        });

        // Si esto tambien falla, se deja propagar: significa que PostgreSQL esta caido, y con la
        // base principal caida el sistema no tiene como seguir operando de todas formas.
        await _db.SaveChangesAsync(ct);
    }
}
