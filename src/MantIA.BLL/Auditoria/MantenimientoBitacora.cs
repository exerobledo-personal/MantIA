using System.Text.Json;
using MantIA.BE.Auditoria;
using MantIA.DAL.Bitacora;
using MantIA.DAL.Seguridad;
using MantIA.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MantIA.BLL.Auditoria;

/// <summary>
/// Trabajo de fondo de la bitácora. Hace dos cosas, en este orden y por una razón:
///
/// <list type="number">
/// <item><b>Drena el respaldo.</b> Los eventos que quedaron en la base del cliente porque Mongo no
/// estaba disponible se reflejan ahora. Va primero porque un evento sin reflejar es un hecho que
/// todavía no está en la bitácora, y eso es peor que un eslabón sin sellar.</item>
/// <item><b>Sella lo pendiente.</b> Cierra los eslabones que quedaron abiertos porque el pedido que
/// los escribió se topó con un hueco.</item>
/// </list>
///
/// <para>Es un ciclo, no una cola con estado: cada vuelta mira la realidad y hace lo que falta. Si
/// se cae en el medio, la siguiente vuelta retoma sin necesidad de recordar nada.</para>
/// </summary>
public class MantenimientoBitacora : BackgroundService
{
    private readonly IServiceProvider _servicios;
    private readonly OpcionesMongo _opciones;
    private readonly ILogger<MantenimientoBitacora> _log;

    public MantenimientoBitacora(
        IServiceProvider servicios,
        IOptions<OpcionesMongo> opciones,
        ILogger<MantenimientoBitacora> log)
    {
        _servicios = servicios;
        _opciones = opciones.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var reloj = new PeriodicTimer(_opciones.IntervaloMantenimiento);

        while (await EsperarAsync(reloj, ct))
        {
            try
            {
                using var alcance = _servicios.CreateScope();
                await DrenarRespaldoAsync(alcance.ServiceProvider, ct);
                await SellarPendientesAsync(alcance.ServiceProvider, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // El mantenimiento no puede tumbar la aplicacion: si falla, se reintenta en la
                // proxima vuelta. Lo unico que no se puede perder es el aviso.
                _log.LogError(ex, "Fallo una pasada de mantenimiento de la bitacora.");
            }
        }
    }

    private static async Task<bool> EsperarAsync(PeriodicTimer reloj, CancellationToken ct)
    {
        try { return await reloj.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>
    /// Pasa a Mongo lo que quedo en la base del cliente. Se ordena por fecha del hecho para que la
    /// cadena reconstruya el orden real en la medida de lo posible.
    /// </summary>
    private async Task DrenarRespaldoAsync(IServiceProvider alcance, CancellationToken ct)
    {
        var db = alcance.GetRequiredService<MantIADbContext>();
        var mongo = alcance.GetRequiredService<RepositorioBitacoraMongo>();
        var protector = alcance.GetRequiredService<IProtectorDatos>();

        // Sin filtro de empresa: el drenaje corre fuera de un pedido de usuario y tiene que ver
        // los pendientes de todos los tenants.
        var pendientes = await db.EventosPendientes
            .IgnoreQueryFilters()
            .OrderBy(p => p.FechaEvento)
            .Take(_opciones.MaximoDrenajePorPasada)
            .ToListAsync(ct);

        if (pendientes.Count == 0) return;

        var reflejados = 0;

        foreach (var pendiente in pendientes)
        {
            var evento = JsonSerializer.Deserialize<EventoBitacora>(pendiente.Contenido);
            if (evento is null)
            {
                pendiente.UltimoError = "El contenido guardado no se pudo deserializar.";
                pendiente.Intentos++;
                pendiente.UltimoIntento = DateTimeOffset.UtcNow;
                continue;
            }

            try
            {
                await mongo.AgregarAsync(
                    evento,
                    (e, hashAnterior) => protector.Sellar(
                        CanonicalizacionEvento.Canonizar(e, hashAnterior), e.VersionLlave),
                    ct);

                // Recien se borra el respaldo cuando el evento ya esta en Mongo. Si el proceso
                // muere entre una cosa y la otra, el peor caso es un evento duplicado, que se ve
                // en la bitacora; el caso contrario seria un evento perdido, que no se ve.
                db.EventosPendientes.Remove(pendiente);
                reflejados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                pendiente.Intentos++;
                pendiente.UltimoIntento = DateTimeOffset.UtcNow;
                pendiente.UltimoError = $"{ex.GetType().Name}: {ex.Message}";

                // Mongo sigue sin responder. No tiene sentido insistir con el resto en esta vuelta.
                break;
            }
        }

        await db.SaveChangesAsync(ct);

        if (reflejados > 0)
            _log.LogInformation("Se reflejaron {Cantidad} eventos del respaldo local en la bitacora.", reflejados);
    }

    private async Task SellarPendientesAsync(IServiceProvider alcance, CancellationToken ct)
    {
        var mongo = alcance.GetRequiredService<RepositorioBitacoraMongo>();
        var protector = alcance.GetRequiredService<IProtectorDatos>();

        foreach (var cadena in await mongo.CadenasConPendientesAsync(ct))
        {
            var sellados = await mongo.SellarPendientesAsync(
                cadena,
                (e, hashAnterior) => protector.Sellar(
                    CanonicalizacionEvento.Canonizar(e, hashAnterior), e.VersionLlave),
                ct);

            if (sellados > 0)
                _log.LogDebug("Cadena {Cadena}: {Cantidad} eslabones sellados.", cadena, sellados);
        }
    }
}
