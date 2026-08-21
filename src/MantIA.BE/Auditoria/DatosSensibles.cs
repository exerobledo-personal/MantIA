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
/// <para><b>La clasificacion depende de a que bitacora va el evento</b>, y esa es la correccion
/// importante respecto de la primera version. El costo de un repuesto estaba marcado como omitido
/// "porque revela margenes", pero la pregunta que faltaba hacer era: revelarselo <i>a quien</i>.
/// Un evento de ambito empresa lo lee el administrador de esa misma empresa, que ya conoce sus
/// propios costos; ocultarselo no protege nada y en cambio vuelve inauditable exactamente el
/// escenario que motivo todo esto — quien cambio el costo de un repuesto y cuando. En la bitacora
/// de plataforma, que lee MantIA, el mismo dato si se omite.</para>
///
/// <para>El criterio general sigue en pie: una bitacora se consulta, se exporta y se comparte mucho
/// mas que la base operativa. Es el lugar donde un dato sensible termina filtrandose sin que nadie
/// lo note.</para>
/// </summary>
public static class DatosSensibles
{
    /// <summary>Tratamiento de un campo segun el alcance de la bitacora en la que se escribe.</summary>
    private record Politica(Sensibilidad EnEmpresa, Sensibilidad EnPlataforma);

    private static readonly IReadOnlyDictionary<string, Politica> Clasificacion =
        new Dictionary<string, Politica>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Identificacion de personas ---
            // El correo se enmascara siempre: identifica a una persona fisica y no aporta nada que
            // no aporte ya el identificador de usuario.
            ["Usuario.Email"]       = new(Sensibilidad.Enmascarado, Sensibilidad.Enmascarado),
            ["Usuario.Nombre"]      = new(Sensibilidad.Publico,     Sensibilidad.Enmascarado),
            ["Usuario.Apellido"]    = new(Sensibilidad.Publico,     Sensibilidad.Enmascarado),
            // Credencial de acceso: no se registra nunca. Conocerla no ayuda a auditar y tenerla en
            // un registro exportable es un riesgo sin contrapartida.
            ["Usuario.Auth0UserId"] = new(Sensibilidad.Omitido,     Sensibilidad.Omitido),

            // --- Datos de la empresa cliente ---
            ["Empresa.Dominio"]     = new(Sensibilidad.Publico,     Sensibilidad.Publico),
            ["Empresa.TenantId"]    = new(Sensibilidad.Omitido,     Sensibilidad.Omitido),

            // --- Informacion economica ---
            // Publica hacia adentro de la empresa: es la unica forma de auditar la manipulacion de
            // presupuestos. Omitida hacia la plataforma: MantIA no necesita los costos de sus
            // clientes para operar, y tenerlos en un registro propio es responsabilidad de mas.
            ["Repuesto.CostoUnitario"]                      = new(Sensibilidad.Publico, Sensibilidad.Omitido),
            ["OrdenTrabajoRepuesto.CostoUnitarioAlConsumo"] = new(Sensibilidad.Publico, Sensibilidad.Omitido),
            ["Repuesto.Proveedor"]                          = new(Sensibilidad.Publico, Sensibilidad.Omitido),

            // El precio del plan es dato comercial de MantIA, no de la empresa.
            ["Plan.PrecioMensual"]  = new(Sensibilidad.Omitido,     Sensibilidad.Publico),
        };

    public static Sensibilidad De(string entidad, string campo, AlcanceBitacora alcance)
    {
        if (!Clasificacion.TryGetValue($"{entidad}.{campo}", out var politica))
            return Sensibilidad.Publico;

        return alcance == AlcanceBitacora.Plataforma
            ? politica.EnPlataforma
            : politica.EnEmpresa;
    }

    /// <summary>
    /// Aplica la politica a un valor y devuelve lo que corresponde guardar.
    /// <para>
    /// Un campo omitido no se borra: se reemplaza por un marcador. La diferencia importa, porque
    /// "no se registro el valor" y "el valor estaba vacio" son dos hechos distintos y el segundo
    /// puede ser justamente lo que se esta auditando.
    /// </para>
    /// </summary>
    public static string? Aplicar(string entidad, string campo, string? valor, AlcanceBitacora alcance) =>
        De(entidad, campo, alcance) switch
        {
            Sensibilidad.Omitido => "[omitido]",
            Sensibilidad.Enmascarado => Enmascarar(valor),
            _ => valor
        };

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
