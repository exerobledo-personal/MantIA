using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Digito verificador de una fila: el "DV horizontal".
///
/// <para><b>Que problema resuelve.</b> El cifrado impide leer; no impide escribir. Alguien con acceso
/// al motor puede hacer <c>UPDATE movimientos_stock SET cantidad = 400 WHERE ...</c> y la aplicacion
/// no tiene forma de notarlo: el dato es valido, coherente y esta donde tiene que estar. El digito es
/// lo que convierte esa edicion en algo detectable, porque para que la fila siga verificando hay que
/// recalcular el digito, y para recalcularlo hace falta una llave que no esta en la base.</para>
///
/// <para><b>Por que en una tabla aparte y no en una columna de la fila.</b> Tres razones concretas:
/// una edicion a mano tiene que tocar dos tablas en lugar de una, y quien lo intente rara vez sabe
/// que la segunda existe; esta tabla puede tener permisos distintos —hasta vivir en otro esquema con
/// otro rol de base— mientras las tablas operativas siguen abiertas al que las necesita; y el
/// esquema operativo no se contamina con una columna tecnica en cada tabla protegida, con lo cual
/// sumar o sacar una tabla del regimen no es una migracion.</para>
///
/// <para><b>Lo que no resuelve.</b> Borrar la fila y su digito juntos no deja rastro aca: para eso
/// esta el <see cref="SelloTabla"/>, que cuenta y resume el conjunto.</para>
/// </summary>
public class SelloFila : TenantEntity
{
    /// <summary>Nombre de la entidad, tal como figura en <c>CamposSellados</c>.</summary>
    public string Tabla { get; set; } = string.Empty;

    /// <summary>Identificador de la fila protegida. No lleva clave foranea: apunta a varias tablas.</summary>
    public Guid FilaId { get; set; }

    /// <summary>HMAC-SHA256 en base64 de la forma canonica de la fila.</summary>
    public string Digito { get; set; } = string.Empty;

    /// <summary>
    /// Version de la llave con la que se calculo. Sin esto, rotar la llave invalidaria de golpe
    /// todas las filas ya selladas.
    /// </summary>
    public string VersionLlave { get; set; } = string.Empty;

    /// <summary>Version del formato canonico usado. Permite recalcular selectivamente si cambia.</summary>
    public string VersionFormato { get; set; } = string.Empty;

    /// <summary>Cuando se calculo por ultima vez. Es la fecha del ultimo cambio legitimo de la fila.</summary>
    public DateTimeOffset CalculadoEn { get; set; } = DateTimeOffset.UtcNow;
}
