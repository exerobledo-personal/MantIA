namespace MantIA.DAL.Seguridad;

/// <summary>
/// Un conjunto de llaves versionadas para un proposito.
/// <para>
/// Una llave <b>nunca</b> se elimina del diccionario aunque se haya rotado: sin ella, lo firmado o
/// cifrado con esa version queda inverificable o indescifrable, que a efectos practicos es lo mismo
/// que haberlo perdido.
/// </para>
/// </summary>
public class JuegoLlaves
{
    /// <summary>Version con la que se firma o cifra <b>de ahora en adelante</b>.</summary>
    public string VersionActual { get; set; } = "v1";

    /// <summary>Llaves por version, en base64. Cada una debe tener 32 bytes.</summary>
    public Dictionary<string, string> Llaves { get; set; } = [];
}

/// <summary>
/// Llaves y politicas de proteccion de datos. Se enlaza desde la seccion <c>Auditoria</c>.
///
/// <para><b>Dos juegos de llaves separados, y esa separacion es el punto.</b> Sellar la bitacora y
/// cifrar campos son dos propositos distintos, con dos superficies de exposicion distintas. Con una
/// sola llave, quien la obtenga por cualquier via —un volcado de configuracion, un descuido en un
/// entorno de prueba— puede a la vez leer los datos cifrados y falsificar la cadena de auditoria que
/// deberia delatarlo. Con dos, comprometer una no da la otra: puede leer pero no borrar sus huellas,
/// o al reves.</para>
///
/// <para>Idealmente cada juego vive en un almacen distinto y con distinto responsable. Aunque hoy
/// esten los dos en la misma configuracion, tenerlos separados desde el modelo es lo que permite
/// moverlos despues sin tocar una linea de codigo.</para>
///
/// <para><b>Las llaves no van en el repositorio ni en appsettings.json.</b> En desarrollo se cargan
/// con <c>dotnet user-secrets</c>; en produccion, con variables de entorno o el almacen de secretos
/// del proveedor. Una llave de sellado versionada en git no protege de nada: cualquiera que clone el
/// repositorio puede reescribir la cadena entera.</para>
/// </summary>
public class OpcionesAuditoria
{
    public const string Seccion = "Auditoria";

    /// <summary>
    /// Llaves de sellado: la cadena de la bitacora. Protegen la <b>integridad</b> — que nadie
    /// altere lo que quedo registrado.
    /// </summary>
    public JuegoLlaves Sello { get; set; } = new();

    /// <summary>
    /// Llaves de cifrado de campos. Protegen la <b>confidencialidad</b> — que quien lea la base sin
    /// pasar por la aplicacion no vea ciertos valores.
    /// </summary>
    public JuegoLlaves Cifrado { get; set; } = new();

    /// <summary>
    /// Llaves de los digitos verificadores de fila y de tabla. Protegen la <b>integridad de los
    /// datos operativos</b> — que nadie cambie una cantidad o un costo por fuera de la aplicacion.
    ///
    /// <para><b>Por que un tercer juego y no el de sellado.</b> La bitacora y los datos son dos
    /// superficies distintas: la bitacora vive en Mongo y los digitos en PostgreSQL, y no siempre
    /// los administra la misma persona. Con una sola llave para las dos cosas, quien pueda tocar el
    /// motor operativo puede ademas rehacer los sellos de la bitacora que deberian delatarlo, que es
    /// justo lo que este mecanismo intenta impedir.</para>
    /// </summary>
    public JuegoLlaves Verificacion { get; set; } = new();

    /// <summary>
    /// Cifra el estado anterior y posterior de los eventos antes de guardarlos.
    /// <para>
    /// El enmascarado y el cifrado resuelven cosas distintas y por eso conviven: el enmascarado
    /// protege de la exposicion cotidiana —una bitacora se exporta y se comparte—, y el cifrado
    /// protege de quien tenga acceso de lectura a la base pero no a la llave de la aplicacion.
    /// </para>
    /// </summary>
    public bool CifrarEstados { get; set; } = true;

    /// <summary>
    /// Valor de un movimiento de stock a partir del cual el evento sube un escalon de severidad.
    /// Cero desactiva la regla.
    /// </summary>
    public decimal UmbralMovimientoSensible { get; set; } = 0m;
}
