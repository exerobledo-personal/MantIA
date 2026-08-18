using MudBlazor;

namespace MantIA.WEB.Demo;

public enum Vista
{
    Completa,
    Operacion,
    Empresa,
    Plataforma
}

/// <summary>
/// Vista activa del superadministrador. Se registra con alcance scoped, de modo que
/// cada circuito de Blazor Server (cada pestaña o usuario conectado) tiene su propia
/// vista sin pisar la de los demás.
/// </summary>
public class Sesion
{
    private readonly DatosDemo _datos;

    public Sesion(DatosDemo datos) => _datos = datos;

    private static readonly Vista[] TodasLasVistas =
        [Vista.Completa, Vista.Operacion, Vista.Empresa, Vista.Plataforma];

    public Vista Actual { get; private set; } = Vista.Completa;

    public event Action? Cambio;

    public bool EsCompleta => Actual == Vista.Completa;

    public void Ver(Vista vista)
    {
        if (vista == Actual) return;
        Actual = vista;
        Cambio?.Invoke();
    }

    public void Restablecer() => Ver(Vista.Completa);

    public IReadOnlyList<Vista> Disponibles => TodasLasVistas;

    public string Nombre(Vista vista) => vista switch
    {
        Vista.Operacion => "Operación",
        Vista.Empresa => "Administración de empresa",
        Vista.Plataforma => "Plataforma MantIA",
        _ => "Vista completa"
    };

    public string Rol(Vista vista) => vista switch
    {
        Vista.Operacion => Roles.Supervisor,
        Vista.Empresa => Roles.AdminEmpresa,
        _ => Roles.SuperAdmin
    };

    public string Descripcion(Vista vista) => vista switch
    {
        Vista.Operacion => "Lo que ve un supervisor de mantenimiento en la planta",
        Vista.Empresa => "Lo que ve el administrador de la empresa cliente",
        Vista.Plataforma => "Lo que ve el superadministrador como operador del servicio",
        _ => "Todos los módulos de la plataforma, sin filtrar"
    };

    public string RutaInicio(Vista vista) => vista switch
    {
        Vista.Empresa => "/empresa",
        Vista.Plataforma => "/plataforma",
        _ => "/dashboard"
    };

    public string NombreInicio(Vista vista) => vista switch
    {
        Vista.Empresa => "Panel de empresa",
        Vista.Plataforma => "Panel de plataforma",
        _ => "Panel operativo"
    };

    public string Icono(Vista vista) => vista switch
    {
        Vista.Operacion => Icons.Material.Filled.PrecisionManufacturing,
        Vista.Empresa => Icons.Material.Filled.Business,
        Vista.Plataforma => Icons.Material.Filled.Hub,
        _ => Icons.Material.Filled.GridView
    };

    public UsuarioVm UsuarioVisible
    {
        get
        {
            var rol = Rol(Actual);
            return rol == Roles.SuperAdmin
                ? _datos.UsuarioActual
                : _datos.Usuarios.FirstOrDefault(u => u.Rol == rol && u.Estado == EstadoGenerico.Activo)
                  ?? _datos.UsuarioActual;
        }
    }

    public bool PuedeVer(Modulo modulo) => Actual switch
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
