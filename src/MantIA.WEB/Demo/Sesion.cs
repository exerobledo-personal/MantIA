using MudBlazor;

namespace MantIA.WEB.Demo;

public enum Vista
{
    Completa,
    Operacion,
    Empresa,
    Plataforma
}

public static class Sesion
{
    public static Vista Actual { get; private set; } = Vista.Completa;

    public static event Action? Cambio;

    public static bool EsCompleta => Actual == Vista.Completa;

    public static void Ver(Vista vista)
    {
        if (vista == Actual) return;
        Actual = vista;
        Cambio?.Invoke();
    }

    public static void Restablecer() => Ver(Vista.Completa);

    public static IReadOnlyList<Vista> Disponibles { get; } =
        [Vista.Completa, Vista.Operacion, Vista.Empresa, Vista.Plataforma];

    public static string Nombre(Vista vista) => vista switch
    {
        Vista.Operacion => "Operación",
        Vista.Empresa => "Administración de empresa",
        Vista.Plataforma => "Plataforma MantIA",
        _ => "Vista completa"
    };

    public static string Rol(Vista vista) => vista switch
    {
        Vista.Operacion => Roles.Supervisor,
        Vista.Empresa => Roles.AdminEmpresa,
        _ => Roles.SuperAdmin
    };

    public static string Descripcion(Vista vista) => vista switch
    {
        Vista.Operacion => "Lo que ve un supervisor de mantenimiento en la planta",
        Vista.Empresa => "Lo que ve el administrador de la empresa cliente",
        Vista.Plataforma => "Lo que ve el superadministrador como operador del servicio",
        _ => "Todos los módulos de la plataforma, sin filtrar"
    };

    public static string RutaInicio(Vista vista) => vista switch
    {
        Vista.Empresa => "/empresa",
        Vista.Plataforma => "/plataforma",
        _ => "/dashboard"
    };

    public static string NombreInicio(Vista vista) => vista switch
    {
        Vista.Empresa => "Panel de empresa",
        Vista.Plataforma => "Panel de plataforma",
        _ => "Panel operativo"
    };

    public static string Icono(Vista vista) => vista switch
    {
        Vista.Operacion => Icons.Material.Filled.PrecisionManufacturing,
        Vista.Empresa => Icons.Material.Filled.Business,
        Vista.Plataforma => Icons.Material.Filled.Hub,
        _ => Icons.Material.Filled.GridView
    };

    public static UsuarioVm UsuarioVisible
    {
        get
        {
            var rol = Rol(Actual);
            return rol == Roles.SuperAdmin
                ? DatosDemo.UsuarioActual
                : DatosDemo.Usuarios.FirstOrDefault(u => u.Rol == rol && u.Estado == EstadoGenerico.Activo)
                  ?? DatosDemo.UsuarioActual;
        }
    }

    public static bool PuedeVer(Modulo modulo) => Actual switch
    {
        Vista.Completa => true,
        Vista.Operacion => modulo is Modulo.Operacion or Modulo.Recomendaciones or Modulo.Reportes,
        Vista.Empresa => modulo is Modulo.AdministracionEmpresa or Modulo.Reportes,
        Vista.Plataforma => modulo == Modulo.Plataforma,
        _ => false
    };
}

public enum Modulo
{
    Operacion,
    Recomendaciones,
    Reportes,
    AdministracionEmpresa,
    Plataforma
}
