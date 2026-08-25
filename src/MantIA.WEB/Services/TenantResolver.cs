using System.Security.Claims;
using MantIA.BLL.Acceso;

namespace MantIA.WEB.Services;

/// <summary>
/// Traduce el principal de la sesión web a una llamada al servicio de acceso.
///
/// <para><b>Acá ya no se decide nada.</b> Antes esta clase tenía su propia copia de las reglas
/// —buscar el usuario, comprobar la empresa, validar el dominio— y el arranque de la aplicación
/// tenía otra copia de lo mismo. Dos copias de una regla de acceso siempre terminan divergiendo, y
/// la que se olvide de actualizarse es la que deja entrar a quien no debe. Ahora las dos llaman al
/// mismo <see cref="IServicioAcceso"/>, que además registra el ingreso o el rechazo en la
/// bitácora.</para>
/// </summary>
public class TenantResolver
{
    private readonly IServicioAcceso _acceso;

    public TenantResolver(IServicioAcceso acceso) => _acceso = acceso;

    public Task<AccesoResuelto> ResolverAsync(ClaimsPrincipal user, CancellationToken ct = default) =>
        _acceso.ResolverAsync(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            user.FindFirst(ClaimTypes.Email)?.Value,
            ct);
}
