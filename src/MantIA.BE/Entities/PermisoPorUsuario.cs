using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Excepcion nominal a la matriz: un permiso concedido o quitado <b>a una persona concreta</b>,
/// por encima de lo que le corresponde por rol y nivel.
///
/// <para>Resuelve el caso que la matriz no cubre: el gerente nuevo que necesita una capacidad
/// puntual, el supervisor que cubre a otro durante una licencia, el operario de confianza al que
/// se le habilita algo sin cambiarle el rol a toda su categoria.</para>
///
/// <para><b>Por que esto no abre un agujero.</b> Un permiso nominal se evalua DESPUES de la
/// frontera estructural, nunca antes. Concretamente: la validacion se hace contra el
/// <i>rol</i> del usuario, no contra su fila nominal, de modo que ninguna excepcion puede
/// concederle a un operario un recurso del ambito Empresa ni a un administrador la capacidad de
/// cerrar una orden. Lo que si puede hacer es mover una casilla dentro de lo que su rol ya podia
/// alcanzar. Es una excepcion en grado, nunca en ambito.</para>
///
/// <para>Tres reglas mas, que la capa de servicio hace cumplir:</para>
/// <list type="bullet">
/// <item>Nadie puede editar sus propios permisos nominales. Sin esto, quien administra permisos se
/// concede lo que quiera y la separacion de funciones deja de existir.</item>
/// <item>No puede revocar un minimo del rol (ver <c>PermisosMinimos</c>).</item>
/// <item>Exige motivo escrito y genera un evento de severidad critica.</item>
/// </list>
/// </summary>
public class PermisoPorUsuario : TenantEntity
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Clave del recurso. Debe existir en el catalogo de codigo.</summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>Clave de la accion. Debe existir en el catalogo de codigo.</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>
    /// Concede o quita. Se modela como dato y no como ausencia de fila para poder registrar una
    /// revocacion nominal: "a esta persona, ademas, se le saco esto".
    /// </summary>
    public bool Concedido { get; set; } = true;

    /// <summary>
    /// Fecha a partir de la cual la excepcion deja de aplicar. Nulo es permanente.
    /// <para>
    /// Existe porque la mayoria de estas excepciones nacen temporales —una licencia, un cierre de
    /// mes, una auditoria externa— y nadie se acuerda de quitarlas despues. Un permiso que se
    /// apaga solo es la unica defensa practica contra la acumulacion silenciosa de privilegios.
    /// </para>
    /// </summary>
    public DateTimeOffset? VigenteHasta { get; set; }

    /// <summary>Por que se otorgo. Obligatorio: una excepcion sin explicacion es un privilegio.</summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>Quien la otorgo. Debe ser distinto de <see cref="UsuarioId"/>.</summary>
    public Guid OtorgadoPorUsuarioId { get; set; }
}
