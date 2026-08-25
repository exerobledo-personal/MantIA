using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Un dominio de correo habilitado dentro de una empresa.
///
/// <para><b>Qué hace y qué NO hace.</b> Acota <b>a quién se puede invitar</b>: el administrador de
/// la empresa no puede emitir una invitación a un correo de un dominio que no esté acá. No da acceso
/// por sí solo — tener el dominio correcto y ninguna invitación deja a la persona afuera igual.
/// Es un cerrojo adicional, no la puerta.</para>
///
/// <para><b>Por qué es una tabla y no un campo.</b> Una fábrica que se fusionó arrastra dos dominios
/// durante años, y el modelo de un solo campo la deja afuera. Además, el campo único que había antes
/// impedía que dos empresas usaran el mismo dominio, lo que hace imposible el caso de dos clientes
/// que trabajan con correo personal.</para>
///
/// <para><b>Por qué puede repetirse entre empresas.</b> Justamente por ese caso: dos clientes chicos
/// pueden operar los dos con <c>gmail.com</c>. Eso no rompe el aislamiento porque el dominio no
/// resuelve el tenant — lo resuelve la invitación, que es nominal y de una sola empresa.</para>
/// </summary>
public class DominioEmpresa : TenantEntity
{
    /// <summary>Dominio en minúsculas, sin arroba: <c>acerosdellitoral.com.ar</c>.</summary>
    public string Dominio { get; set; } = string.Empty;

    /// <summary>
    /// El que se muestra como dominio corporativo de la empresa. Sale del correo del Usuario 0 al
    /// dar de alta el cliente. Hay exactamente uno por empresa.
    /// </summary>
    public bool EsPrincipal { get; set; }

    /// <summary>Normaliza lo que se escriba en pantalla: recorta, baja a minúsculas y saca la arroba.</summary>
    public static string Normalizar(string valor)
    {
        var limpio = valor.Trim().ToLowerInvariant();
        var arroba = limpio.LastIndexOf('@');
        return arroba >= 0 && arroba < limpio.Length - 1 ? limpio[(arroba + 1)..] : limpio;
    }

    /// <summary>Saca el dominio de una dirección de correo. Nulo si no parece una.</summary>
    public static string? De(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var arroba = email.LastIndexOf('@');
        return arroba > 0 && arroba < email.Length - 1
            ? email[(arroba + 1)..].Trim().ToLowerInvariant()
            : null;
    }
}
