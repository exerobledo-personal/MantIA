using MantIA.BE.Common;

namespace MantIA.BE.Auditoria;

/// <summary>A que bitacora pertenece un evento. Son dos flujos con lectores distintos.</summary>
public enum AlcanceBitacora
{
    /// <summary>Visible para el administrador de la empresa. Solo eventos de su tenant.</summary>
    Empresa,
    /// <summary>Visible unicamente para MantIA. Altas de empresas, cambios sobre cuentas
    /// administradoras, y uso del bypass de superadministrador.</summary>
    Plataforma
}

public enum TipoEvento
{
    /// <summary>Cambio de estado o de datos sobre una entidad del negocio.</summary>
    Transaccion,
    /// <summary>Inicio de sesion, alta de usuario, cambio de permisos.</summary>
    Auditoria,
    /// <summary>Error del sistema.</summary>
    Excepcion
}

public enum NivelLog { Debug, Info, Warning, Error }

/// <summary>
/// Entrada de bitacora. Se persiste en MongoDB, no en PostgreSQL: volumen alto, escritura
/// secuencial y esquema variable.
///
/// <para><b>Es inmutable y encadenada.</b> Cada entrada incluye el hash de la anterior, de
/// modo que la bitacora forma una cadena: alterar o borrar un evento del medio invalida
/// todos los hashes posteriores y la manipulacion queda en evidencia.</para>
///
/// <para>Para una bitacora de auditoria, la integridad importa mas que la confidencialidad:
/// de nada sirve cifrar un registro que alguien puede reemplazar. Se hace lo primero con la
/// cadena de hashes, y lo segundo con el cifrado en reposo del motor.</para>
/// </summary>
public class EventoBitacora
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public AlcanceBitacora Alcance { get; set; }
    public TipoEvento Tipo { get; set; }

    /// <summary>Cuanto le importa a un tecnico. Para excepciones, sobre todo.</summary>
    public NivelLog Nivel { get; set; } = NivelLog.Info;

    /// <summary>
    /// Cuanto le importa al negocio. La deriva <see cref="CatalogoEventos"/> del par
    /// recurso/accion y del contexto; no la elige quien escribe el evento.
    /// </summary>
    public Severidad Severidad { get; set; } = Severidad.Operativa;

    /// <summary>
    /// Agrupa todos los eventos de una misma operacion de negocio. Cerrar una orden genera el
    /// cierre, un asiento de stock por cada repuesto y las alertas que se disparen: sin esto,
    /// revertir la operacion completa obliga a adivinar cuales iban juntos.
    /// </summary>
    public Guid? CorrelacionId { get; set; }

    /// <summary>
    /// Motivo escrito por quien ejecuto la accion. Obligatorio para las acciones que lo exigen
    /// (ver <see cref="CatalogoEventos.ExigeMotivo"/>).
    /// </summary>
    public string? Motivo { get; set; }

    /// <summary>Empresa afectada. Nulo en eventos de plataforma que no aplican a un tenant.</summary>
    public Guid? EmpresaId { get; set; }

    public Guid? UsuarioId { get; set; }
    public string? UsuarioEmail { get; set; }
    public string? RolAlMomento { get; set; }

    /// <summary>Clave del recurso del catalogo de permisos y accion ejecutada.</summary>
    public string Recurso { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public Guid? RecursoId { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>
    /// Estado de la entidad antes y despues, serializado. Es lo que hace posible el rollback:
    /// sin el estado anterior no hay a donde volver.
    /// <para>Los campos clasificados como sensibles se guardan enmascarados
    /// (ver <see cref="DatosSensibles"/>).</para>
    /// </summary>
    public string? EstadoAnterior { get; set; }
    public string? EstadoPosterior { get; set; }

    public bool Exitoso { get; set; } = true;
    public string? MotivoFallo { get; set; }

    /// <summary>Marca cuando la accion se ejecuto usando el bypass de superadministrador.</summary>
    public bool UsoBypass { get; set; }

    public string? DireccionIp { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    // NO hay campo de vencimiento, y es una decision explicita: la bitacora no expira.
    // Un registro de auditoria vale justamente el dia que pasa algo raro, y ese dia nadie sabe de
    // antemano cuando es. El costo de guardar texto es despreciable frente al de no poder
    // reconstruir un incidente. Si alguna vez el volumen molesta, se archiva a mano y se decide
    // en ese momento; no se programa el olvido por adelantado.

    // ---- Cadena de integridad ----
    /// <summary>Hash del evento inmediatamente anterior de la misma cadena.</summary>
    public string? HashAnterior { get; set; }

    /// <summary>
    /// Sello de este evento: HMAC-SHA256 sobre su contenido canonico mas
    /// <see cref="HashAnterior"/>. Es HMAC y no un hash simple porque un hash simple lo puede
    /// recalcular cualquiera que tenga acceso de escritura a la base; el HMAC necesita ademas la
    /// llave, que vive en la configuracion de la aplicacion y no en el motor de datos.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Falso mientras el evento existe pero todavia no se encadeno.
    /// <para>
    /// La escritura es en dos tiempos: primero se guarda el hecho con su numero de orden, despues
    /// se sella contra el eslabon anterior. Es lo que permite que el numero lo asigne la base de
    /// forma atomica en lugar de calcularlo la aplicacion. Un evento sin sellar ya es evidencia de
    /// que la accion ocurrio; lo que le falta es la prueba de que nadie lo movio de lugar.
    /// </para>
    /// </summary>
    public bool Sellado { get; set; }

    /// <summary>
    /// Version de la llave con la que se sello. Permite rotar la llave sin invalidar la cadena
    /// vieja: cada evento se verifica con la llave que le corresponde.
    /// </summary>
    public string VersionLlave { get; set; } = string.Empty;

    /// <summary>
    /// Posicion en la cadena. Un salto de numero delata una eliminacion.
    /// <para>
    /// <b>Cero significa "guardado pero todavia sin numerar".</b> El numero se asigna despues de
    /// insertar, nunca antes: reservarlo por adelantado dejaria un hueco permanente cada vez que
    /// algo se cancela o el proceso muere en el medio. Un evento sin numerar ya es evidencia de que
    /// la accion ocurrio; lo que le falta es su lugar en la cadena.
    /// </para>
    /// </summary>
    public long Secuencia { get; set; }

    /// <summary>
    /// Cadena a la que pertenece el evento. Hay una por empresa mas una de plataforma: si fuera una
    /// sola global, el volumen de un cliente frenaria la escritura de todos los demas, porque cada
    /// evento necesita el hash del anterior.
    /// <para>
    /// Se guarda como dato y no se calcula al vuelo, aunque se derive de <see cref="Alcance"/> y
    /// <see cref="EmpresaId"/>: es la clave por la que se ordena e indexa la cadena, y un registro
    /// de auditoria no puede cambiar de cadena porque alguien edite otro campo.
    /// </para>
    /// </summary>
    public string Cadena { get; set; } = string.Empty;

    /// <summary>Nombre de la cadena que le corresponde a un evento. Lo asigna quien lo escribe.</summary>
    public static string CadenaDe(AlcanceBitacora alcance, Guid? empresaId) =>
        alcance == AlcanceBitacora.Plataforma ? "plataforma" : $"empresa:{empresaId}";
}
