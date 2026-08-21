using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MantIA.DAL.Bitacora;

/// <summary>
/// Registro de la bitácora en el contenedor. Se agrupa acá para que MantIA.WEB y MantIA.API no
/// repitan la configuración y no puedan quedar desalineados.
/// </summary>
public static class RegistroBitacora
{
    /// <param name="conRespaldo">
    /// Con respaldo, una caida de MongoDB no frena la operacion: el evento queda en la base del
    /// cliente y se refleja despues. Sin respaldo, la bitacora es bloqueante y una caida de Mongo
    /// hace fallar la accion que se estaba auditando. Lo primero es el modo normal; lo segundo
    /// tiene sentido en un entorno donde no auditar sea inaceptable.
    /// </param>
    public static IServiceCollection AgregarBitacora(
        this IServiceCollection servicios, IConfiguration configuracion, bool conRespaldo = true)
    {
        servicios.Configure<OpcionesMongo>(configuracion.GetSection(OpcionesMongo.Seccion));

        // El cliente es singleton por diseno del driver: mantiene el pool de conexiones. Crear uno
        // por pedido agota los sockets del servidor bastante antes de lo que uno espera.
        servicios.AddSingleton<IMongoClient>(sp =>
        {
            var opciones = sp.GetRequiredService<IOptions<OpcionesMongo>>().Value;
            MapeoBitacora.Registrar();
            return new MongoClient(opciones.Conexion);
        });

        // El repositorio de Mongo se registra tambien como tipo concreto: lo necesitan el
        // envoltorio de respaldo y el trabajo de mantenimiento, que sellan y drenan sin pasar por
        // la interfaz.
        servicios.AddScoped<RepositorioBitacoraMongo>();

        if (conRespaldo)
            servicios.AddScoped<IRepositorioBitacora, RepositorioBitacoraConRespaldo>();
        else
            servicios.AddScoped<IRepositorioBitacora>(sp => sp.GetRequiredService<RepositorioBitacoraMongo>());

        return servicios;
    }

    /// <summary>
    /// Crea los indices al arrancar.
    /// <para>
    /// Si Mongo no esta disponible la aplicacion <b>no</b> se cae: se registra el problema y sigue.
    /// La bitacora es critica, pero dejar la aplicacion sin arrancar por un indice que se puede
    /// crear despues cambia una falla parcial por una total.
    /// </para>
    /// </summary>
    public static async Task PrepararBitacoraAsync(this IServiceProvider servicios)
    {
        var cliente = servicios.GetRequiredService<IMongoClient>();
        var opciones = servicios.GetRequiredService<IOptions<OpcionesMongo>>().Value;

        using var origen = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await MapeoBitacora.PrepararAsync(cliente, opciones, origen.Token);
    }
}
