namespace MantIA.BE.Auditoria;

/// <summary>Como se trata cada campo al escribirlo en una bitacora.</summary>
public enum Sensibilidad
{
    /// <summary>Se registra tal cual.</summary>
    Publico,
    /// <summary>Se registra parcialmente: j***@empresa.com.</summary>
    Enmascarado,
    /// <summary>No se registra el valor, solo que cambio.</summary>
    Omitido
}

/// <summary>
/// Clasificacion de los campos que no pueden viajar en claro a la bitacora.
///
/// <para>El criterio es que una bitacora se consulta, se exporta y se comparte mucho mas
/// que la base operativa: es el lugar donde un dato sensible termina filtrandose sin que
/// nadie lo note.</para>
/// </summary>
public static class DatosSensibles
{
    /// <summary>Campo a nivel de entidad y como debe tratarse.</summary>
    public static readonly IReadOnlyDictionary<string, Sensibilidad> Clasificacion =
        new Dictionary<string, Sensibilidad>(StringComparer.OrdinalIgnoreCase)
        {
            // Identificacion de personas
            ["Usuario.Email"]         = Sensibilidad.Enmascarado,
            ["Usuario.Auth0UserId"]   = Sensibilidad.Omitido,
            ["Usuario.Nombre"]        = Sensibilidad.Publico,
            ["Usuario.Apellido"]      = Sensibilidad.Publico,

            // Datos comerciales de la empresa cliente
            ["Empresa.Dominio"]       = Sensibilidad.Publico,
            ["Empresa.TenantId"]      = Sensibilidad.Omitido,

            // Informacion economica: revela margenes y poder de compra
            ["Repuesto.CostoUnitario"]                 = Sensibilidad.Omitido,
            ["Repuesto.Proveedor"]                     = Sensibilidad.Enmascarado,
            ["OrdenTrabajoRepuesto.CostoUnitarioAlConsumo"] = Sensibilidad.Omitido,
            ["Plan.PrecioMensual"]                     = Sensibilidad.Omitido,
        };

    public static Sensibilidad De(string entidad, string campo) =>
        Clasificacion.TryGetValue($"{entidad}.{campo}", out var s) ? s : Sensibilidad.Publico;

    /// <summary>Enmascara un valor conservando lo justo para reconocerlo sin exponerlo.</summary>
    public static string Enmascarar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var arroba = valor.IndexOf('@');
        if (arroba > 0)
            return $"{valor[0]}***{valor[arroba..]}";

        return valor.Length <= 2 ? "***" : $"{valor[0]}***{valor[^1]}";
    }
}
