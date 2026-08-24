namespace MantIA.DAL.Seguridad;

/// <summary>
/// Cada cuanto se comprueban los digitos verificadores y se cierra una foto vertical.
/// Se enlaza desde la seccion <c>Verificacion</c>.
/// </summary>
public class OpcionesVerificacion
{
    public const string Seccion = "Verificacion";

    /// <summary>
    /// Permite apagar el recorrido sin sacar el servicio. Los digitos de fila se siguen calculando
    /// al escribir: lo unico que se suspende es el barrido y la foto.
    /// </summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>
    /// Cada cuanto se toma una foto de cada tabla de cada empresa.
    /// <para>
    /// El intervalo es el margen de incertidumbre del mecanismo: entre dos fotos, un cambio hecho
    /// por fuera y uno hecho por la aplicacion se ven igual, y lo que los separa es la bitacora de
    /// ese rato. Achicarlo mejora la precision del "cuando" y cuesta un recorrido completo de las
    /// tres tablas por empresa; seis horas es un punto razonable para arrancar y hay que revisarlo
    /// con volumen real.
    /// </para>
    /// </summary>
    public TimeSpan Intervalo { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Espera antes del primer recorrido. Existe para no competir con el arranque de la aplicacion,
    /// que es cuando peor sienta un recorrido completo de tablas grandes.
    /// </summary>
    public TimeSpan Demora { get; set; } = TimeSpan.FromMinutes(5);
}
