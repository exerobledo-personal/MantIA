namespace MantIA.DAL.Bitacora;

/// <summary>Conexion y politica del almacen de bitacoras. Se enlaza desde la seccion <c>Bitacora</c>.</summary>
public class OpcionesMongo
{
    public const string Seccion = "Bitacora";

    public string Conexion { get; set; } = "mongodb://localhost:27017";
    public string BaseDeDatos { get; set; } = "mantia_bitacora";
    public string Coleccion { get; set; } = "eventos";

    /// <summary>Contadores atomicos por cadena. Es la base la que asigna los numeros de orden.</summary>
    public string ColeccionContadores { get; set; } = "contadores";

    /// <summary>
    /// Cuantos eslabones puede cerrar un mismo pedido. Evita que a un usuario cualquiera le toque
    /// pagar el sellado de un atraso largo; lo que quede afuera lo toma el trabajo de fondo.
    /// </summary>
    public int MaximoSelladoPorPasada { get; set; } = 200;

    /// <summary>Cada cuanto corre el trabajo que sella pendientes y drena el respaldo.</summary>
    public TimeSpan IntervaloMantenimiento { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cuantos eventos del respaldo se pasan a Mongo por pasada. Acotado para no convertir la
    /// recuperacion en una tormenta de escrituras justo cuando el servicio vuelve.
    /// </summary>
    public int MaximoDrenajePorPasada { get; set; } = 500;
}
