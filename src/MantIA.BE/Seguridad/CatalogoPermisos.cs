using MantIA.BE.Common;

namespace MantIA.BE.Seguridad;

/// <summary>Acciones que se pueden conceder sobre un recurso.</summary>
public static class Acciones
{
    public const string Consultar   = "Consultar";
    public const string Alta        = "Alta";
    public const string Modificacion= "Modificacion";
    public const string Baja        = "Baja";
    /// <summary>
    /// Revisar una solicitud y decidir si corresponde: la convierte en orden o la rechaza.
    /// <para>
    /// Es la accion que separa "alguien reporto algo" de "mantenimiento acepto que hay que hacerlo".
    /// Sin ella, cualquier empleado que carga un pedido esta creando trabajo comprometido para el
    /// equipo de mantenimiento, y el historial se llena de cosas que nunca fueron.
    /// </para>
    /// </summary>
    public const string Controlar   = "Controlar";

    /// <summary>Poner responsable a una orden. Es aparte de Modificacion porque reparte trabajo.</summary>
    public const string Asignar     = "Asignar";

    /// <summary>Ejecutar la orden: registrar avance, consumo de repuestos y resolucion.</summary>
    public const string Realizar    = "Realizar";

    /// <summary>Cerrar una orden. Es aparte de Modificacion porque mueve stock.</summary>
    public const string Cerrar      = "Cerrar";
    /// <summary>Aceptar o rechazar una recomendacion.</summary>
    public const string Decidir     = "Decidir";
    /// <summary>Cambiar parametros: umbrales, nivel de log, periodicidad de un proceso.</summary>
    public const string Configurar  = "Configurar";
    public const string Exportar    = "Exportar";
    /// <summary>Disparar un proceso: reingesta del catalogo, corrida del modelo.</summary>
    public const string Ejecutar    = "Ejecutar";
}

/// <summary>Un recurso protegible, con el ambito al que pertenece y las acciones que admite.</summary>
public sealed record Recurso(
    string Clave,
    string Nombre,
    Ambito Ambito,
    IReadOnlyList<string> AccionesValidas);

/// <summary>
/// Catalogo de recursos y acciones del sistema. <b>Vive en codigo, no en base.</b>
/// <para>
/// La distincion es deliberada. Los recursos existen porque hay codigo que los implementa:
/// un administrador no puede inventar el recurso "Facturacion" desde una pantalla y esperar
/// que aparezca funcionalidad. Lo que si es dato editable en vivo es la MATRIZ, es decir
/// que rol y que nivel tienen concedida cada accion (ver <c>PermisoPorRolYNivel</c>).
/// </para>
/// <para>
/// El <see cref="Recurso.Ambito"/> es la frontera estructural: administracion no ejecuta
/// tareas operativas, y operacion no administra la plataforma. Esa linea no se configura.
/// </para>
/// </summary>
public static class CatalogoPermisos
{
    private const string C = Acciones.Consultar;
    private const string A = Acciones.Alta;
    private const string M = Acciones.Modificacion;
    private const string B = Acciones.Baja;

    public static readonly IReadOnlyList<Recurso> Recursos =
    [
        // ---------- Ambito Operacion ----------
        new("Maquinas",        "Maquinas",                Ambito.Operacion, [C, A, M, B]),
        new("Repuestos",       "Repuestos criticos",      Ambito.Operacion, [C, A, M, B]),
        new("Stock",           "Movimientos de stock",    Ambito.Operacion, [C, A]),
        new("Alertas",         "Alertas de stock",        Ambito.Operacion, [C, Acciones.Configurar]),
        // La escalera de mantenimiento se arma con estas acciones y no con roles nuevos: generar,
        // generar y controlar, generar controlar y asignar, o todo eso mas realizar. Cada empresa
        // marca las celdas que quiere. Por eso la matriz no viene con valores por defecto.
        new("Ordenes",         "Ordenes de trabajo",      Ambito.Operacion,
            [C, A, M, B, Acciones.Controlar, Acciones.Asignar, Acciones.Realizar, Acciones.Cerrar]),
        new("Catalogo",        "Catalogo tecnico",        Ambito.Operacion, [C]),
        new("Recomendaciones", "Recomendaciones",         Ambito.Operacion, [C, Acciones.Decidir]),
        new("Reportes",        "Reportes operativos",     Ambito.Operacion, [C, A, M, B, Acciones.Exportar]),
        // Administrar los permisos DE OPERACION es una tarea de operacion, no de administracion.
        // Quien reparte permisos de mantenimiento es el jefe de mantenimiento, no el administrativo:
        // es el unico que sabe quien esta capacitado para cerrar una orden. Ver "Permisos", que es
        // su equivalente para el ambito Empresa.
        new("PermisosOperacion","Permisos de operacion",  Ambito.Operacion, [C, Acciones.Configurar]),

        // ---------- Ambito Empresa ----------
        new("Usuarios",        "Usuarios",                Ambito.Empresa,   [C, A, M, B]),
        new("Niveles",         "Niveles de permiso",      Ambito.Empresa,   [C, A, M, B]),
        new("Plantas",         "Plantas",                 Ambito.Empresa,   [C, A, M, B]),
        new("Permisos",        "Permisos de empresa",     Ambito.Empresa,   [C, Acciones.Configurar]),
        new("BitacoraEmpresa", "Bitacora de la empresa",  Ambito.Empresa,   [C, Acciones.Exportar]),

        // ---------- Ambito Plataforma ----------
        new("Empresas",        "Empresas cliente",        Ambito.Plataforma,[C, A, M, B]),
        new("Planes",          "Planes de suscripcion",   Ambito.Plataforma,[C, A, M, B]),
        // La INGESTA del catalogo es de plataforma aunque el catalogo se CONSUMA en operacion:
        // reejecutar el enriquecimiento modifica una ficha compartida por todos los clientes.
        new("CatalogoIngesta", "Ingesta del catalogo",    Ambito.Plataforma,[C, Acciones.Ejecutar, Acciones.Configurar]),
        new("BitacoraPlataforma","Bitacora de plataforma",Ambito.Plataforma,[C, Acciones.Exportar]),
        new("SaludServicios",  "Salud de servicios",      Ambito.Plataforma,[C, Acciones.Configurar]),
    ];

    /// <summary>
    /// Ambitos donde un rol puede EJERCER acciones. Es estructural y no configurable.
    /// <para>
    /// <c>AdminEmpresa</c> alcanza <b>solo</b> Empresa. Administrar la empresa (usuarios,
    /// plantas, permisos, planes) y operar sobre ella (cerrar o modificar una orden de
    /// trabajo) son dos funciones distintas, y juntarlas rompe la separacion de funciones:
    /// quien puede cerrar una orden decide cuanto stock se consumio y con que costo, y si
    /// ademas administra la matriz de permisos puede concederse esa capacidad a si mismo y
    /// borrarla despues. Es el camino clasico a la manipulacion de presupuestos.
    /// </para>
    /// <para>
    /// La supervision que el administrador si necesita se resuelve por
    /// <see cref="ConsultaFueraDeAmbitoDe"/>, que da lectura sin dar capacidad de accion.
    /// </para>
    /// </summary>
    public static IReadOnlyList<Ambito> AmbitosDe(RolSistema rol) => rol switch
    {
        RolSistema.Empleado         => [Ambito.Operacion],
        RolSistema.Supervisor       => [Ambito.Operacion],
        RolSistema.Gerente          => [Ambito.Operacion],
        RolSistema.AdminEmpresa     => [Ambito.Empresa],
        RolSistema.SuperAdminMantIA => [Ambito.Plataforma, Ambito.Empresa, Ambito.Operacion],
        _ => []
    };

    /// <summary>
    /// Recursos que un rol puede llegar a <b>consultar</b> aunque queden fuera de su ambito.
    /// Nunca habilita otra accion que <see cref="Acciones.Consultar"/>, ni siquiera
    /// <see cref="Acciones.Exportar"/>: exportar es sacar datos del sistema, no supervisar.
    /// <para>
    /// Esto es lo que permite que el administrador de la empresa vea el estado de la
    /// operacion sin poder intervenir en ella. Es lectura estructuralmente acotada: como la
    /// unica accion admitida es Consultar, ninguna edicion de la matriz puede escalarla.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ConsultaFueraDeAmbitoDe(RolSistema rol) => rol switch
    {
        RolSistema.AdminEmpresa =>
            ["Maquinas", "Repuestos", "Stock", "Alertas", "Ordenes", "Recomendaciones", "Reportes"],
        _ => []
    };

    public static Recurso? BuscarRecurso(string clave) =>
        Recursos.FirstOrDefault(r => string.Equals(r.Clave, clave, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Valida que una celda de la matriz tenga sentido antes de guardarla: que el recurso
    /// exista, que la accion aplique a ese recurso, y que el rol pueda alcanzarlo, sea por
    /// ambito o por consulta fuera de ambito. Impide configurar un permiso que el sistema
    /// nunca podria respetar, y sobre todo impide concederle a un rol una accion que la
    /// separacion de funciones le niega.
    /// </summary>
    public static bool EsCombinacionValida(RolSistema rol, string recurso, string accion)
    {
        var r = BuscarRecurso(recurso);
        if (r is null) return false;
        if (!r.AccionesValidas.Contains(accion, StringComparer.OrdinalIgnoreCase)) return false;

        if (AmbitosDe(rol).Contains(r.Ambito)) return true;

        return string.Equals(accion, Acciones.Consultar, StringComparison.OrdinalIgnoreCase)
            && ConsultaFueraDeAmbitoDe(rol).Contains(r.Clave, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Recursos que un rol puede llegar a tener concedidos, para armar la pantalla de matriz.</summary>
    public static IEnumerable<Recurso> RecursosDe(RolSistema rol)
    {
        var ambitos = AmbitosDe(rol);
        var lectura = ConsultaFueraDeAmbitoDe(rol);
        return Recursos.Where(r =>
            ambitos.Contains(r.Ambito) ||
            lectura.Contains(r.Clave, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Acciones que la matriz puede conceder a un rol sobre un recurso. Es lo que la
    /// pantalla de permisos necesita para dibujar cada fila: fuera del ambito propio la
    /// fila queda con una sola casilla.
    /// </summary>
    public static IEnumerable<string> AccionesConcedibles(RolSistema rol, string recurso)
    {
        var r = BuscarRecurso(recurso);
        if (r is null) return [];
        return r.AccionesValidas.Where(a => EsCombinacionValida(rol, r.Clave, a));
    }
}
