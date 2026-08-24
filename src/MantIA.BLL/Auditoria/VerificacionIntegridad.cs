using MantIA.BE.Auditoria;
using MantIA.DAL.Context;
using MantIA.DAL.Seguridad;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MantIA.BLL.Auditoria;

/// <summary>
/// Recorre periodicamente las tablas con digito verificador, empresa por empresa, y cierra una foto
/// vertical de cada una.
///
/// <para><b>Por que es un trabajo de fondo y no una comprobacion al leer.</b> Verificar al leer
/// duplicaria el costo de cada consulta y, sobre todo, solo miraria lo que alguien consulta: una
/// fila alterada en un movimiento de hace ocho meses no la abre nadie, y es exactamente la que
/// conviene alterar. El barrido las mira todas, le importe a alguien o no.</para>
///
/// <para><b>Deja constancia en la bitacora.</b> Un hallazgo que solo va al log del servidor se
/// pierde: el log rota, no esta encadenado y no lo lee el cliente. Un hallazgo de integridad tiene
/// que quedar en el mismo lugar donde se audita todo lo demas, y con severidad alta.</para>
/// </summary>
public class VerificacionIntegridad : BackgroundService
{
    private readonly IServiceProvider _servicios;
    private readonly OpcionesVerificacion _opciones;
    private readonly ILogger<VerificacionIntegridad> _log;

    public VerificacionIntegridad(
        IServiceProvider servicios,
        IOptions<OpcionesVerificacion> opciones,
        ILogger<VerificacionIntegridad> log)
    {
        _servicios = servicios;
        _opciones = opciones.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_opciones.Habilitado)
        {
            _log.LogWarning(
                "La verificacion periodica de digitos esta desactivada. Los digitos se siguen " +
                "calculando al escribir, pero nadie los esta comprobando.");
            return;
        }

        if (!await EsperarAsync(_opciones.Demora, ct)) return;

        using var reloj = new PeriodicTimer(_opciones.Intervalo);

        do
        {
            try
            {
                await UnaPasadaAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Igual que el mantenimiento de la bitacora: una pasada que falla se reintenta en la
                // siguiente. Lo unico que no se puede perder es el aviso de que fallo.
                _log.LogError(ex, "Fallo una pasada de verificacion de digitos.");
            }
        }
        while (await SiguienteAsync(reloj, ct));
    }

    private async Task UnaPasadaAsync(CancellationToken ct)
    {
        var empresas = await EmpresasAsync(ct);

        foreach (var empresaId in empresas)
        {
            if (ct.IsCancellationRequested) return;
            await VerificarEmpresaAsync(empresaId, ct);
        }
    }

    /// <summary>
    /// Un alcance nuevo por empresa. Ademas de acotar la memoria del contexto, es lo que permite
    /// posicionar el tenant: el recorrido de una empresa corre con el mismo aislamiento que tendria
    /// una sesion de esa empresa, en lugar de esquivar los filtros.
    /// </summary>
    private async Task VerificarEmpresaAsync(Guid empresaId, CancellationToken ct)
    {
        using var alcance = _servicios.CreateScope();

        // El trabajo de fondo no tiene sesion, asi que se posiciona el tenant a mano. Es el unico
        // lugar del sistema donde esto se hace: en cualquier otro, el tenant sale de la
        // autenticacion y escribirlo seria una forma de saltar el aislamiento.
        if (alcance.ServiceProvider.GetRequiredService<ICurrentTenant>() is not CurrentTenant tenant)
        {
            _log.LogError(
                "ICurrentTenant no es CurrentTenant, asi que la verificacion no puede posicionarse " +
                "en una empresa. Revisar el registro de servicios.");
            return;
        }

        tenant.EmpresaId = empresaId;

        var verificador = alcance.ServiceProvider.GetRequiredService<IVerificadorDigitos>();
        var bitacora = alcance.ServiceProvider.GetRequiredService<IBitacora>();

        foreach (var tabla in verificador.Tablas())
        {
            if (ct.IsCancellationRequested) return;

            var foto = await verificador.TomarFotoAsync(empresaId, tabla, ct);

            if (foto.Integra && foto.FilasPerdidas == 0)
            {
                _log.LogDebug(
                    "Integridad {Tabla} de {Empresa}: foto {Secuencia}, {Filas} filas, sin hallazgos.",
                    tabla, empresaId, foto.Secuencia, foto.Filas);
                continue;
            }

            await AvisarAsync(bitacora, foto, ct);
        }
    }

    /// <summary>
    /// Registra el hallazgo. Va como accion <b>fallida</b> y no como una consulta con resultado: lo
    /// que fallo no es la verificacion sino la integridad, y la escala de severidad ya sabe que un
    /// evento fallido pesa mas que uno exitoso.
    /// </summary>
    private async Task AvisarAsync(IBitacora bitacora, ResultadoFoto foto, CancellationToken ct)
    {
        // Un puñado de ejemplos alcanza: el detalle completo esta en las tablas de digitos, y una
        // descripcion con diez mil identificadores no la lee nadie y ademas infla la bitacora.
        var ejemplos = foto.Fallas
            .Take(10)
            .Select(f => $"{f.Tipo}: {f.FilaId:N}");

        var detalle =
            $"Foto {foto.Secuencia} de {foto.Tabla}: {foto.Filas} filas, " +
            $"{foto.Fallas.Count} hallazgos, {foto.FilasPerdidas} filas menos que la foto anterior. " +
            string.Join(" | ", ejemplos);

        _log.LogError("Integridad comprometida. {Detalle}", detalle);

        await bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Integridad",
                Accion: "Verificar",
                Descripcion: detalle,
                Exitoso: false,
                MotivoFallo: "Los datos no coinciden con sus digitos verificadores.",
                EmpresaAfectadaId: foto.EmpresaId),
            ct);
    }

    private async Task<List<Guid>> EmpresasAsync(CancellationToken ct)
    {
        using var alcance = _servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<MantIADbContext>();

        // Tambien las suspendidas y las dadas de baja: dejar de mirar una empresa el dia que se la
        // suspende es abrirle la ventana justo cuando mas motivos hay para vigilarla.
        return await db.Empresas
            .IgnoreQueryFilters()
            .Select(e => e.Id)
            .ToListAsync(ct);
    }

    private static async Task<bool> EsperarAsync(TimeSpan cuanto, CancellationToken ct)
    {
        try
        {
            await Task.Delay(cuanto, ct);
            return true;
        }
        catch (OperationCanceledException) { return false; }
    }

    private static async Task<bool> SiguienteAsync(PeriodicTimer reloj, CancellationToken ct)
    {
        try { return await reloj.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
