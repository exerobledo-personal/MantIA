namespace MantIA.DAL.Seguridad;

/// <summary>
/// Llaves y politicas de la bitacora. Se enlaza desde la seccion <c>Auditoria</c> de la
/// configuracion.
///
/// <para><b>Las llaves no van en el repositorio ni en appsettings.json.</b> En desarrollo se cargan
/// con <c>dotnet user-secrets</c>; en produccion, con variables de entorno o el almacen de secretos
/// del proveedor. Una llave de sellado versionada en git no protege de nada: cualquiera que clone
/// el repositorio puede reescribir la cadena entera.</para>
/// </summary>
public class OpcionesAuditoria
{
    public const string Seccion = "Auditoria";

    /// <summary>
    /// Version de llave con la que se firma y se cifra <b>de ahora en adelante</b>. Los eventos
    /// viejos conservan la suya y se siguen verificando con ella.
    /// </summary>
    public string VersionActual { get; set; } = "v1";

    /// <summary>
    /// Llaves por version, en base64. Cada una debe tener 32 bytes: es lo que piden HMAC-SHA256
    /// y AES-256.
    /// <para>
    /// Una llave <b>nunca</b> se elimina de este diccionario aunque se haya rotado: sin ella, los
    /// eventos firmados con esa version pasan a ser inverificables, que a efectos practicos es lo
    /// mismo que haberlos perdido.
    /// </para>
    /// </summary>
    public Dictionary<string, string> Llaves { get; set; } = [];

    /// <summary>
    /// Cifra el estado anterior y posterior antes de guardarlos.
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
