using MantIA.BE.Seguridad;

namespace MantIA.BE.Auditoria;

/// <summary>
/// Qué se registra, con qué severidad y en cuál de las dos bitácoras.
///
/// <para><b>Regla de base: se registra todo.</b> Cualquier accion que pase por el evaluador de
/// permisos genera un evento. Lo que decide este catalogo no es <i>si</i> se anota, sino cuanto
/// pesa: no es lo mismo consultar el listado de ordenes que sacar cuatro repuestos del stock o
/// borrar una orden abierta.</para>
///
/// <para>Por eso la severidad se deriva del par recurso/accion y no la elige quien escribe el
/// evento. Si cada modulo decidiera su propia severidad, la escala dejaria de significar algo a la
/// tercera pantalla.</para>
/// </summary>
public static class CatalogoEventos
{
    /// <summary>
    /// Severidad base de cada par recurso/accion. Lo que no figura cae en la regla por defecto:
    /// consultar y exportar son <see cref="Severidad.Rutina"/>, cualquier escritura es
    /// <see cref="Severidad.Operativa"/>.
    /// </summary>
    private static readonly Dictionary<string, Severidad> Base = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Movimiento de valor: stock y ordenes ---
        ["Stock.Alta"]              = Severidad.Sensible,   // todo asiento del libro mayor
        ["Ordenes.Cerrar"]          = Severidad.Sensible,   // descuenta stock y congela costos
        ["Ordenes.Baja"]            = Severidad.Critica,    // ver EsDestructivo: siempre destaca
        ["Repuestos.Baja"]          = Severidad.Sensible,
        ["Maquinas.Baja"]           = Severidad.Sensible,

        // --- Capacidades: quien puede hacer que ---
        ["Permisos.Configurar"]     = Severidad.Critica,
        ["Usuarios.Alta"]           = Severidad.Sensible,
        ["Usuarios.Modificacion"]   = Severidad.Sensible,
        ["Usuarios.Baja"]           = Severidad.Critica,
        ["Niveles.Alta"]            = Severidad.Sensible,
        ["Niveles.Modificacion"]    = Severidad.Sensible,
        ["Niveles.Baja"]            = Severidad.Sensible,

        // --- Decisiones con consecuencia economica ---
        ["Recomendaciones.Decidir"] = Severidad.Sensible,   // aceptar dispara una compra
        ["Alertas.Configurar"]      = Severidad.Sensible,   // subir un umbral apaga avisos

        // --- Plataforma ---
        ["Empresas.Alta"]           = Severidad.Sensible,
        ["Empresas.Modificacion"]   = Severidad.Sensible,
        ["Empresas.Baja"]           = Severidad.Critica,
        ["Planes.Modificacion"]     = Severidad.Sensible,
        ["CatalogoIngesta.Ejecutar"]= Severidad.Sensible,   // reescribe fichas de todos los clientes
        ["SaludServicios.Configurar"] = Severidad.Sensible,

        // --- Sesion ---
        ["Sesion.Ingreso"]          = Severidad.Rutina,
        ["Sesion.Salida"]           = Severidad.Rutina,
        ["Sesion.Rechazo"]          = Severidad.Sensible,   // un rechazo dice mucho mas que un ingreso

        // --- Integridad de los datos ---
        // El barrido exitoso es rutina y se anota igual: la serie de verificaciones sin hallazgos es
        // parte de la evidencia. Cuando encuentra algo, el evento va como fallido y la agravante lo
        // sube a critico, que es donde tiene que estar.
        ["Integridad.Verificar"]    = Severidad.Sensible,

        // --- Reversion ---
        ["Rollback.Alta"]           = Severidad.Critica,
        ["Rollback.Decidir"]        = Severidad.Critica,
        ["Rollback.Ejecutar"]       = Severidad.Critica,
    };

    /// <summary>
    /// Acciones que no se pueden ejecutar sin un motivo escrito.
    /// <para>
    /// Es una regla de negocio, no de auditoria: la capa de servicio rechaza la operacion si el
    /// motivo viene vacio. Sirve para el caso que da nombre a todo esto — "elimino una orden
    /// abierta sin justificacion" deja de ser posible: o escribe por que, o no la borra.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> ExigenMotivo = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ordenes.Baja",
        "Maquinas.Baja",
        "Repuestos.Baja",
        "Usuarios.Baja",
        "Empresas.Baja",
        "Permisos.Configurar",
        "Rollback.Alta",
        "Rollback.Decidir",
    };

    /// <summary>
    /// Acciones que destruyen o revierten. Sube la severidad a critica cuando ademas el objeto
    /// estaba vivo: dar de baja una orden ya cerrada es administracion, darla de baja abierta o
    /// en curso es otra cosa.
    /// </summary>
    private static readonly HashSet<string> Destructivas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ordenes.Baja", "Maquinas.Baja", "Repuestos.Baja",
        "Usuarios.Baja", "Empresas.Baja", "Reportes.Baja",
    };

    public static string Clave(string recurso, string accion) => $"{recurso}.{accion}";

    public static bool ExigeMotivo(string recurso, string accion) =>
        ExigenMotivo.Contains(Clave(recurso, accion));

    public static bool EsDestructiva(string recurso, string accion) =>
        Destructivas.Contains(Clave(recurso, accion));

    /// <summary>Severidad antes de aplicar las agravantes del contexto.</summary>
    public static Severidad SeveridadBase(string recurso, string accion)
    {
        if (Base.TryGetValue(Clave(recurso, accion), out var s)) return s;

        return accion is Acciones.Consultar or Acciones.Exportar
            ? Severidad.Rutina
            : Severidad.Operativa;
    }

    /// <summary>
    /// Severidad final. Las agravantes son lo que hace util a la escala: el mismo par
    /// recurso/accion pesa distinto segun como haya terminado y con que se haya hecho.
    /// </summary>
    /// <param name="exitoso">Un intento fallido dice mas que uno exitoso: alguien intento algo que no podia.</param>
    /// <param name="usoBypass">El superadministrador pasando por encima de la matriz. Siempre critico.</param>
    /// <param name="objetoEstabaVivo">La entidad destruida estaba abierta, en curso o activa.</param>
    /// <param name="sinMotivo">La accion exigia motivo y llego vacio. No deberia ocurrir: si ocurre, es sintoma.</param>
    public static Severidad SeveridadDe(
        string recurso,
        string accion,
        bool exitoso = true,
        bool usoBypass = false,
        bool objetoEstabaVivo = false,
        bool sinMotivo = false)
    {
        if (usoBypass) return Severidad.Critica;

        var nivel = SeveridadBase(recurso, accion);

        if (EsDestructiva(recurso, accion) && objetoEstabaVivo)
            return Severidad.Critica;

        if (!exitoso) nivel = Subir(nivel);
        if (sinMotivo && ExigeMotivo(recurso, accion)) nivel = Subir(nivel);

        return nivel;
    }

    private static Severidad Subir(Severidad s) =>
        s == Severidad.Critica ? s : s + 1;

    /// <summary>
    /// A que bitacora va el evento. Se decide por el ambito del recurso: lo que pasa dentro de una
    /// empresa lo lee su administrador, lo que pasa sobre las empresas lo lee MantIA.
    /// <para>
    /// Excepcion deliberada: los cambios sobre usuarios administradores y el uso del bypass van
    /// <b>a las dos</b>. El administrador tiene que ver que le tocaron la cuenta, y MantIA tiene que
    /// poder auditar lo que hace su propio personal.
    /// </para>
    /// </summary>
    public static AlcanceBitacora AlcanceDe(string recurso)
    {
        var r = CatalogoPermisos.BuscarRecurso(recurso);
        return r?.Ambito == Common.Ambito.Plataforma
            ? AlcanceBitacora.Plataforma
            : AlcanceBitacora.Empresa;
    }

    // La bitacora NO tiene politica de vencimiento: nada se borra por antiguedad.
    // La severidad sirve para filtrar, destacar y priorizar avisos, no para decidir que se olvida.
    // El ruido de las consultas rutinarias se resuelve con filtros en la pantalla, que es barato y
    // reversible, y no borrando registros, que no lo es.
}
