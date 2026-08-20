using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Una celda de la matriz de permisos de una empresa: para una combinacion de rol y nivel,
/// si se concede o no una accion sobre un recurso.
/// <para>
/// <b>Es dato, no codigo.</b> El administrador de la empresa la edita en vivo y el cambio
/// impacta sin necesidad de recompilar ni de que el usuario vuelva a iniciar sesion: al
/// guardar se invalida la entrada de cache de ese tenant.
/// </para>
/// <para>
/// Lo que NO es editable es el <b>ambito</b>: que un rol de administracion no pueda ejecutar
/// tareas operativas es una frontera estructural definida en
/// <c>MantIA.BE.Seguridad.CatalogoPermisos</c>. La matriz solo puede conceder permisos sobre
/// recursos que pertenezcan al ambito del rol.
/// </para>
/// </summary>
public class PermisoPorRolYNivel : TenantEntity
{
    public RolSistema Rol { get; set; }

    /// <summary>Nulo significa "cualquier nivel dentro de ese rol".</summary>
    public Guid? NivelPermisoId { get; set; }
    public NivelPermiso? NivelPermiso { get; set; }

    /// <summary>Clave del recurso. Debe existir en el catalogo de codigo.</summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>Clave de la accion. Debe existir en el catalogo de codigo.</summary>
    public string Accion { get; set; } = string.Empty;

    /// <summary>
    /// Se guarda el permiso denegado en lugar de borrar la fila: asi queda registrado
    /// quien lo quito y cuando, a traves de los campos de auditoria heredados.
    /// </summary>
    public bool Concedido { get; set; } = true;
}
