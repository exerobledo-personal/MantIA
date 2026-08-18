namespace MantIA.WEB.Demo;

/// <summary>
/// Idioma activo de la interfaz. Se registra con alcance scoped: cada circuito de
/// Blazor Server elige su idioma sin afectar al resto de las sesiones conectadas.
/// </summary>
public class Idioma
{
    public const string Espanol = "es";
    public const string Ingles = "en";

    public string Actual { get; private set; } = Espanol;

    public event Action? Cambio;

    private static readonly (string Codigo, string Nombre)[] TodosLosIdiomas =
    [
        (Espanol, "Español"),
        (Ingles, "English")
    ];

    public IReadOnlyList<(string Codigo, string Nombre)> Disponibles => TodosLosIdiomas;

    public string NombreActual =>
        Disponibles.First(i => i.Codigo == Actual).Nombre;

    public void Cambiar(string codigo)
    {
        if (codigo == Actual) return;
        Actual = codigo;
        Cambio?.Invoke();
    }

    public string T(string clave) =>
        Actual == Ingles && Traducciones.TryGetValue(clave, out var texto) ? texto : Textos[clave];

    private static readonly Dictionary<string, string> Textos = new()
    {
        ["nav.panel"] = "Panel principal",
        ["nav.operacion"] = "Operación",
        ["nav.maquinas"] = "Máquinas",
        ["nav.repuestos"] = "Repuestos críticos",
        ["nav.alertas"] = "Alertas de stock",
        ["nav.ordenes"] = "Órdenes de trabajo",
        ["nav.recomendaciones"] = "Recomendaciones IA",
        ["nav.catalogo"] = "Catálogo técnico",
        ["nav.reportes"] = "Reportes",
        ["nav.empresa"] = "Administración de empresa",
        ["nav.panelEmpresa"] = "Panel de empresa",
        ["nav.usuarios"] = "Usuarios y niveles",
        ["nav.plantas"] = "Plantas",
        ["nav.mapa"] = "Mapa de plantas",
        ["nav.permisos"] = "Matriz de permisos",
        ["nav.bitacora"] = "Bitácora de empresa",
        ["nav.plataforma"] = "Plataforma MantIA",
        ["nav.panelPlataforma"] = "Panel de plataforma",
        ["nav.empresas"] = "Empresas cliente",
        ["nav.planes"] = "Planes de suscripción",
        ["nav.sistema"] = "Panel de sistema",
        ["nav.perfil"] = "Mi perfil",
        ["marca.bajada"] = "Gestión de repuestos críticos",

        ["barra.empresa"] = "Empresa en contexto",
        ["barra.empresaAyuda"] = "Como superadministrador podés operar sobre cualquier empresa cliente de la plataforma.",
        ["barra.alertas"] = "Alertas de stock activas",
        ["barra.verTodas"] = "Ver todas las alertas",
        ["barra.sinAlertas"] = "No hay alertas de stock activas",
        ["barra.modoClaro"] = "Cambiar a modo claro",
        ["barra.modoOscuro"] = "Cambiar a modo oscuro",
        ["barra.idioma"] = "Idioma",
        ["barra.menu"] = "Mostrar u ocultar el menú",
        ["barra.verComo"] = "Ver la aplicación como",
        ["barra.miPerfil"] = "Mi perfil",
        ["barra.sistema"] = "Panel de sistema",
        ["barra.salir"] = "Cerrar sesión",

        ["rol.aviso"] = "Estás viendo MantIA con los permisos de",
        ["rol.volver"] = "Volver a superadministrador",
        ["rol.explicacion"] = "El menú y las acciones disponibles se recortan según la matriz de permisos de este perfil.",

        ["acceso.titulo"] = "Ingresar a la plataforma",
        ["acceso.bajada"] = "Usá las credenciales corporativas que te asignó el administrador de tu empresa.",
        ["acceso.correo"] = "Correo corporativo",
        ["acceso.clave"] = "Contraseña",
        ["acceso.recordar"] = "Mantener la sesión iniciada",
        ["acceso.olvide"] = "¿Olvidaste tu contraseña?",
        ["acceso.ingresar"] = "Ingresar",
        ["acceso.verificando"] = "Verificando credenciales…",
        ["acceso.google"] = "Continuar con Google corporativo",
        ["acceso.dominio"] = "El acceso con Google está restringido al dominio corporativo registrado por cada empresa cliente.",
        ["acceso.titular"] = "El repuesto que falta no debería frenar la planta.",
        ["acceso.relato"] = "MantIA cruza el historial de fallas de cada máquina con el stock disponible y el catálogo técnico del fabricante para anticipar qué repuesto crítico va a faltar, antes de que falte.",
        ["acceso.punto1"] = "Alertas tempranas por umbral de stock configurable",
        ["acceso.punto2"] = "Recomendaciones de reposición con la justificación a la vista",
        ["acceso.punto3"] = "Catálogo técnico que se enriquece solo al registrar una máquina"
    };

    private static readonly Dictionary<string, string> Traducciones = new()
    {
        ["nav.panel"] = "Dashboard",
        ["nav.operacion"] = "Operations",
        ["nav.maquinas"] = "Machines",
        ["nav.repuestos"] = "Critical spare parts",
        ["nav.alertas"] = "Stock alerts",
        ["nav.ordenes"] = "Work orders",
        ["nav.recomendaciones"] = "AI recommendations",
        ["nav.catalogo"] = "Technical catalogue",
        ["nav.reportes"] = "Reports",
        ["nav.empresa"] = "Company administration",
        ["nav.panelEmpresa"] = "Company dashboard",
        ["nav.usuarios"] = "Users and levels",
        ["nav.plantas"] = "Plants",
        ["nav.mapa"] = "Plant map",
        ["nav.permisos"] = "Permission matrix",
        ["nav.bitacora"] = "Company audit log",
        ["nav.plataforma"] = "MantIA platform",
        ["nav.panelPlataforma"] = "Platform dashboard",
        ["nav.empresas"] = "Client companies",
        ["nav.planes"] = "Subscription plans",
        ["nav.sistema"] = "System panel",
        ["nav.perfil"] = "My profile",
        ["marca.bajada"] = "Critical spare parts management",

        ["barra.empresa"] = "Active company",
        ["barra.empresaAyuda"] = "As a superadministrator you can operate on any client company of the platform.",
        ["barra.alertas"] = "Active stock alerts",
        ["barra.verTodas"] = "View all alerts",
        ["barra.sinAlertas"] = "There are no active stock alerts",
        ["barra.modoClaro"] = "Switch to light mode",
        ["barra.modoOscuro"] = "Switch to dark mode",
        ["barra.idioma"] = "Language",
        ["barra.menu"] = "Show or hide the menu",
        ["barra.verComo"] = "View the application as",
        ["barra.miPerfil"] = "My profile",
        ["barra.sistema"] = "System panel",
        ["barra.salir"] = "Sign out",

        ["rol.aviso"] = "You are viewing MantIA with the permissions of",
        ["rol.volver"] = "Back to superadministrator",
        ["rol.explicacion"] = "The menu and the available actions are restricted according to this profile's permission matrix.",

        ["acceso.titulo"] = "Sign in to the platform",
        ["acceso.bajada"] = "Use the corporate credentials assigned by your company administrator.",
        ["acceso.correo"] = "Corporate email",
        ["acceso.clave"] = "Password",
        ["acceso.recordar"] = "Keep me signed in",
        ["acceso.olvide"] = "Forgot your password?",
        ["acceso.ingresar"] = "Sign in",
        ["acceso.verificando"] = "Verifying credentials…",
        ["acceso.google"] = "Continue with corporate Google",
        ["acceso.dominio"] = "Google sign-in is restricted to the corporate domain registered by each client company.",
        ["acceso.titular"] = "A missing spare part should never stop the plant.",
        ["acceso.relato"] = "MantIA cross-references each machine's failure history with available stock and the manufacturer's technical catalogue to anticipate which critical spare part will run out, before it does.",
        ["acceso.punto1"] = "Early alerts based on a configurable stock threshold",
        ["acceso.punto2"] = "Replenishment recommendations with the reasoning in plain sight",
        ["acceso.punto3"] = "A technical catalogue that enriches itself when a machine is registered"
    };
}
