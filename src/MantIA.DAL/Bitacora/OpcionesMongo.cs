namespace MantIA.DAL.Bitacora;

/// <summary>Conexion y politica del almacen de bitacoras. Se enlaza desde la seccion <c>Bitacora</c>.</summary>
public class OpcionesMongo
{
    public const string Seccion = "Bitacora";

    public string Conexion { get; set; } = "mongodb://localhost:27017";
    public string BaseDeDatos { get; set; } = "mantia_bitacora";
    public string Coleccion { get; set; } = "eventos";

    /// <summary>
    /// Cuantos eventos numera y sella un mismo pedido. Evita que a un usuario cualquiera le toque
    /// pagar el atraso acumulado; lo que quede afuera lo toma el trabajo de fondo.
    /// </summary>
    public int MaximoNumeradoPorPasada { get; set; } = 200;

    /// <summary>
    /// Reintentos ante colision de numeracion. Dos pedidos que numeran a la vez compiten por el
    /// mismo numero; el que pierde relee el estado y vuelve a empezar.
    /// </summary>
    public int ReintentosNumeracion { get; set; } = 5;

    /// <summary>Cada cuanto corre el trabajo que sella pendientes y drena el respaldo.</summary>
    public TimeSpan IntervaloMantenimiento { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cuantos eventos del respaldo se pasan a Mongo por pasada. Acotado para no convertir la
    /// recuperacion en una tormenta de escrituras justo cuando el servicio vuelve.
    /// </summary>
    public int MaximoDrenajePorPasada { get; set; } = 500;
}
