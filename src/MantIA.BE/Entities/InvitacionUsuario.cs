using MantIA.BE.Common;

namespace MantIA.BE.Entities;

public enum EstadoInvitacion
{
    /// <summary>Emitida y esperando el primer ingreso.</summary>
    Pendiente,

    /// <summary>La persona entró y quedó creada su fila en <c>usuarios</c>.</summary>
    Aceptada,

    /// <summary>Anulada antes de usarse. No se borra: quién invitó a quién es parte del historial.</summary>
    Revocada
}

/// <summary>
/// Habilitación nominal para que una persona entre a una empresa.
///
/// <para><b>Por qué existe, que no es obvio.</b> El acceso se controla contra el identificador que
/// asigna el proveedor de identidad, y ese identificador <b>no se conoce hasta el primer ingreso</b>.
/// Un administrador que quiere dar de alta a un empleado sabe su correo, no su <c>sub</c> de Google.
/// Sin este paso intermedio, aprovisionar a alguien sería adivinar un dato que todavía no existe.</para>
///
/// <para>Entonces: se invita por correo, y en el primer ingreso el correo se cruza con la invitación
/// y recién ahí nace la fila de <c>usuarios</c> con el identificador real ya atado. Es lo que
/// mantiene la separación entre <c>invitaciones</c> —quién está habilitado a entrar— y
/// <c>usuarios</c> —quién efectivamente entró—, que son dos cosas distintas.</para>
///
/// <para><b>Nadie entra sin una.</b> Incluido el Usuario 0 de cada empresa, cuya invitación la emite
/// MantIA al dar de alta el cliente en lugar del administrador. No hay registro público ni alta
/// automática por dominio: una identidad sin invitación se rechaza y el rechazo queda auditado.</para>
///
/// <para><b>Vence.</b> Una invitación abierta para siempre es una llave que quedó puesta: la persona
/// que se fue antes de entrar, el correo que se escribió mal, el alta que nunca se completó. Al
/// vencer deja de servir sola, sin que nadie tenga que acordarse de revocarla.</para>
/// </summary>
public class InvitacionUsuario : TenantEntity
{
    /// <summary>
    /// Correo al que se emitió. Es la clave de búsqueda del primer ingreso, así que va cifrado de
    /// forma determinista: sin eso habría que traer y descifrar todas las invitaciones en cada
    /// intento de login.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Rol con el que nacerá la fila de usuario.</summary>
    public RolSistema Rol { get; set; } = RolSistema.Empleado;

    /// <summary>Nivel con el que nacerá. Nulo solo para roles que no se recortan por nivel.</summary>
    public Guid? NivelPermisoId { get; set; }
    public NivelPermiso? NivelPermiso { get; set; }

    public EstadoInvitacion Estado { get; set; } = EstadoInvitacion.Pendiente;

    public DateTimeOffset FechaVencimiento { get; set; }

    /// <summary>Quién la emitió. Nulo cuando la emitió MantIA al dar de alta la empresa.</summary>
    public Guid? InvitadaPorUsuarioId { get; set; }

    /// <summary>Usuario que nació de esta invitación. Se llena al aceptarla.</summary>
    public Guid? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTimeOffset? FechaAceptacion { get; set; }

    /// <summary>Por qué se revocó. Texto libre, así que va cifrado.</summary>
    public string? MotivoRevocacion { get; set; }

    /// <summary>Sirve para entrar: está pendiente y no venció.</summary>
    public bool Vigente(DateTimeOffset ahora) =>
        Estado == EstadoInvitacion.Pendiente && FechaVencimiento > ahora;
}
