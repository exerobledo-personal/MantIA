using MantIA.BE.Common;

namespace MantIA.BE.Seguridad;

/// <summary>Por que un permiso no se puede quitar. Se guarda para poder explicarlo en pantalla.</summary>
public enum MotivoMinimo
{
    /// <summary>
    /// Quitarlo deja el sistema sin salida: nadie podria volver a concederlo.
    /// El caso tipico es el administrador que se quita a si mismo el acceso a la matriz.
    /// </summary>
    Bloqueo,

    /// <summary>
    /// Quitarlo permite que alguien actue sin que nadie pueda ver lo que hizo. La
    /// trazabilidad no es una funcionalidad opcional del producto: es lo que sostiene el
    /// rollback, la bitacora y cualquier reclamo posterior.
    /// </summary>
    Rendicion,

    /// <summary>
    /// Quitarlo vacia el rol: el usuario sigue existiendo pero ya no puede hacer aquello
    /// para lo que el rol fue creado. Es preferible darle de baja que dejarlo entrar a una
    /// aplicacion en la que no puede hacer nada.
    /// </summary>
    RazonDeSer
}

/// <summary>Un permiso que la matriz nunca puede revocar, con su justificacion.</summary>
public sealed record PermisoMinimo(
    RolSistema Rol,
    string Recurso,
    string Accion,
    MotivoMinimo Motivo,
    string Justificacion);

/// <summary>
/// Plantilla base de permisos irrevocables por rol.
/// <para>
/// <b>Esto no es una matriz por defecto.</b> No dice que permisos tiene cada rol al crear una
/// empresa: eso lo decide cada cliente y va a variar mucho entre una planta de veinte
/// personas y una de trescientas. Dice cual es el <b>piso</b>: el conjunto de celdas que la
/// pantalla de permisos muestra bloqueadas y que el servicio rechaza si alguien intenta
/// desactivarlas por API o por SQL.
/// </para>
/// <para>
/// El criterio para entrar en esta lista es estrecho a proposito. Un permiso es minimo solo
/// si quitarlo produce uno de los tres efectos de <see cref="MotivoMinimo"/>. Todo lo demas
/// —incluso cosas que en la practica casi siempre se van a conceder, como que un supervisor
/// cierre ordenes— queda afuera, porque son decisiones de organizacion del cliente y no
/// nuestras. Una lista de minimos larga es una matriz por defecto disfrazada.
/// </para>
/// </summary>
public static class PermisosMinimos
{
    private const string C = Acciones.Consultar;

    public static readonly IReadOnlyList<PermisoMinimo> Todos =
    [
        // ---------- Empleado ----------
        new(RolSistema.Empleado, "Ordenes", C, MotivoMinimo.RazonDeSer,
            "Un operario que no ve sus ordenes de trabajo no tiene nada que hacer en el sistema."),
        new(RolSistema.Empleado, "Maquinas", C, MotivoMinimo.RazonDeSer,
            "Una orden apunta a una maquina. Sin poder abrir la ficha, la orden es un numero suelto."),

        // ---------- Supervisor ----------
        new(RolSistema.Supervisor, "Ordenes", C, MotivoMinimo.RazonDeSer,
            "Supervisar es, como minimo, ver el trabajo asignado al equipo."),
        new(RolSistema.Supervisor, "Maquinas", C, MotivoMinimo.RazonDeSer,
            "Misma razon que en Empleado: la orden se lee contra la ficha de la maquina."),
        new(RolSistema.Supervisor, "Alertas", C, MotivoMinimo.RazonDeSer,
            "La alerta de faltante de repuesto critico es el aviso que el producto existe para dar. "
          + "Un supervisor que no la recibe convierte a MantIA en una lista de tareas."),

        // ---------- Gerente ----------
        new(RolSistema.Gerente, "Ordenes", C, MotivoMinimo.RazonDeSer,
            "El rol es de control de la operacion; sin lectura de ordenes no controla nada."),
        new(RolSistema.Gerente, "Reportes", C, MotivoMinimo.RazonDeSer,
            "Es la via por la que el rol rinde cuentas hacia arriba."),
        new(RolSistema.Gerente, "Recomendaciones", C, MotivoMinimo.RazonDeSer,
            "La decision de compra anticipada de un repuesto critico se toma en este nivel. "
          + "Ocultarle las recomendaciones deja al motor sin destinatario."),

        // ---------- AdminEmpresa ----------
        new(RolSistema.AdminEmpresa, "Permisos", C, MotivoMinimo.Bloqueo,
            "Sin lectura de la matriz no puede ni diagnosticar por que alguien no entra."),
        new(RolSistema.AdminEmpresa, "Permisos", Acciones.Configurar, MotivoMinimo.Bloqueo,
            "Es el caso de bloqueo puro: si el administrador se quita este permiso, nadie dentro "
          + "de la empresa puede devolverselo y el tenant queda congelado hasta que intervenga soporte."),
        new(RolSistema.AdminEmpresa, "Usuarios", C, MotivoMinimo.Bloqueo,
            "No se puede asignar un permiso a un usuario que no se puede listar."),
        new(RolSistema.AdminEmpresa, "BitacoraEmpresa", C, MotivoMinimo.Rendicion,
            "El administrador es el responsable de lo que pasa en su empresa. Poder apagarse a si "
          + "mismo la bitacora es exactamente el movimiento que un uso malicioso haria primero."),

        // ---------- SuperAdminMantIA ----------
        new(RolSistema.SuperAdminMantIA, "BitacoraPlataforma", C, MotivoMinimo.Rendicion,
            "Es el registro de lo que MantIA hace sobre las cuentas de sus clientes. Tiene que "
          + "seguir siendo legible aunque la configuracion de la plataforma cambie."),
        new(RolSistema.SuperAdminMantIA, "Empresas", C, MotivoMinimo.RazonDeSer,
            "Sin el padron de empresas no hay soporte posible."),
    ];

    /// <summary>
    /// Verdadero si la celda es un minimo del rol. El evaluador de permisos la consulta
    /// <b>antes</b> de mirar la matriz: si una fila con <c>Concedido = false</c> llegara a la
    /// base por una migracion vieja o por SQL directo, el permiso se concede igual.
    /// </summary>
    public static bool EsMinimo(RolSistema rol, string recurso, string accion) =>
        Todos.Any(p =>
            p.Rol == rol &&
            string.Equals(p.Recurso, recurso, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Accion, accion, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Guarda que el servicio de edicion de la matriz llama antes de persistir una
    /// revocacion. Devuelve falso cuando la celda no se puede quitar.
    /// </summary>
    public static bool EsRevocable(RolSistema rol, string recurso, string accion) =>
        !EsMinimo(rol, recurso, accion);

    /// <summary>Texto para mostrar en la pantalla de permisos junto a la casilla bloqueada.</summary>
    public static string? MotivoDe(RolSistema rol, string recurso, string accion) =>
        Todos.FirstOrDefault(p =>
            p.Rol == rol &&
            string.Equals(p.Recurso, recurso, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Accion, accion, StringComparison.OrdinalIgnoreCase))?.Justificacion;

    /// <summary>Minimos de un rol, para pintar la fila bloqueada al abrir la matriz.</summary>
    public static IEnumerable<PermisoMinimo> De(RolSistema rol) =>
        Todos.Where(p => p.Rol == rol);

    /// <summary>
    /// Chequeo de coherencia entre esta plantilla y el catalogo: ningun minimo puede apuntar
    /// a una combinacion que <see cref="CatalogoPermisos.EsCombinacionValida"/> rechace.
    /// Se usa en un test de arranque; si falla, el error esta en el codigo, no en el dato.
    /// </summary>
    public static IEnumerable<PermisoMinimo> Incoherentes() =>
        Todos.Where(p => !CatalogoPermisos.EsCombinacionValida(p.Rol, p.Recurso, p.Accion));
}
