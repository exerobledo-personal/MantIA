using MantIA.BE.Common;
using MantIA.BE.Seguridad;

namespace MantIA.BLL.Authorization;

/// <summary>Resultado de validar un otorgamiento. Si no se permite, dice exactamente por que.</summary>
public record ResultadoOtorgamiento(bool Permitido, string? Motivo = null)
{
    public static readonly ResultadoOtorgamiento Ok = new(true);
    public static ResultadoOtorgamiento No(string motivo) => new(false, motivo);
}

/// <summary>Quien otorga, con lo que hace falta saber de el.</summary>
public record Otorgante(Guid UsuarioId, RolSistema Rol, Guid? NivelPermisoId);

/// <summary>Que se quiere otorgar o revocar.</summary>
public record Otorgamiento(
    RolSistema RolDestino,
    string Recurso,
    string Accion,
    bool Concedido,
    string? Motivo,
    Guid? UsuarioDestinoId = null);

public interface IGestorPermisos
{
    Task<ResultadoOtorgamiento> ValidarAsync(Otorgante otorgante, Otorgamiento cambio);
}

/// <summary>
/// Las reglas que decide si un cambio de permisos puede guardarse.
///
/// <para>Vive separado de <see cref="IPermisoService"/> a proposito: uno responde "¿esta persona
/// puede hacer esto?" y el otro "¿esta persona puede <b>habilitar</b> a otra a hacer esto?". Son
/// preguntas distintas y mezclarlas es como se cuelan las escaladas de privilegio.</para>
/// </summary>
public class GestorPermisos : IGestorPermisos
{
    private readonly IPermisoService _permisos;

    public GestorPermisos(IPermisoService permisos) => _permisos = permisos;

    /// <summary>
    /// Seis controles, en orden. El tercero es la regla nueva: <b>nadie otorga lo que no tiene</b>.
    /// </summary>
    public async Task<ResultadoOtorgamiento> ValidarAsync(Otorgante otorgante, Otorgamiento cambio)
    {
        // 1. Quien otorga tiene que poder administrar permisos.
        var puedeAdministrar = await _permisos.PuedeAsync(
            new ContextoPermiso(otorgante.Rol, otorgante.NivelPermisoId, otorgante.UsuarioId),
            RecursoAdministracion(cambio.Recurso), Acciones.Configurar);

        if (!puedeAdministrar)
            return ResultadoOtorgamiento.No("No tenes permiso para administrar permisos.");

        // 2. Nadie edita sus propios permisos. Sin esto, quien administra permisos se concede lo
        //    que quiera y la separacion de funciones deja de existir.
        if (cambio.UsuarioDestinoId == otorgante.UsuarioId)
            return ResultadoOtorgamiento.No(
                "No podes modificar tus propios permisos. Tiene que hacerlo otra persona.");

        // 3. Nadie otorga lo que no tiene. Sin excepciones.
        //
        //    Se evalua CON LOS PERMISOS REALES del otorgante, no con su rol nominal: si el propio
        //    otorgante recibio la capacidad por una excepcion, puede transmitirla; si nunca la tuvo,
        //    no puede fabricarla para otro. Es lo que impide que administrar permisos sea, de hecho,
        //    tener todos los permisos.
        //
        //    La regla puede ser estricta porque la administracion esta repartida por ambito (ver
        //    RecursoAdministracion): quien reparte permisos operativos es alguien de operaciones,
        //    que si puede tener las capacidades que reparte.
        if (cambio.Concedido)
        {
            var loTiene = await _permisos.PuedeAsync(
                new ContextoPermiso(otorgante.Rol, otorgante.NivelPermisoId, otorgante.UsuarioId),
                cambio.Recurso, cambio.Accion);

            if (!loTiene)
                return ResultadoOtorgamiento.No(
                    $"No podes conceder '{cambio.Recurso}.{cambio.Accion}' porque vos no lo tenes.");
        }

        // 4. La frontera estructural del rol de destino. Una excepcion nominal es una excepcion en
        //    grado, nunca en ambito: no puede sacar a nadie de lo que su rol alcanza.
        if (!CatalogoPermisos.EsCombinacionValida(cambio.RolDestino, cambio.Recurso, cambio.Accion))
            return ResultadoOtorgamiento.No(
                $"El rol {cambio.RolDestino} no puede alcanzar '{cambio.Recurso}.{cambio.Accion}'. " +
                "Es una frontera estructural, no una configuracion.");

        // 5. El piso irrevocable.
        if (!cambio.Concedido &&
            !PermisosMinimos.EsRevocable(cambio.RolDestino, cambio.Recurso, cambio.Accion))
            return ResultadoOtorgamiento.No(
                PermisosMinimos.MotivoDe(cambio.RolDestino, cambio.Recurso, cambio.Accion)
                ?? "Ese permiso no se puede quitar.");

        // 6. Motivo escrito. Una excepcion sin explicacion es un privilegio.
        if (string.IsNullOrWhiteSpace(cambio.Motivo))
            return ResultadoOtorgamiento.No("Hay que escribir el motivo del cambio.");

        return ResultadoOtorgamiento.Ok;
    }

    /// <summary>
    /// Que recurso hay que poder configurar para tocar los permisos de otro recurso.
    /// <b>La administracion de permisos esta repartida por ambito</b>, y eso es lo que permite que
    /// la regla "nadie otorga lo que no tiene" sea estricta.
    ///
    /// <para>Con una unica administracion centralizada en el administrador de empresa, la regla se
    /// trababa sola: su ambito es Empresa, nunca tendria <c>Ordenes.Cerrar</c> —no puede tenerlo,
    /// por separacion de funciones— y por lo tanto nadie podria concederselo jamas a un supervisor.
    /// La operacion entera quedaba sin forma de configurarse.</para>
    ///
    /// <para>Repartida, cada jefe reparte lo que el mismo puede hacer: el gerente de mantenimiento
    /// habilita a cerrar ordenes porque el cierra ordenes, y el administrador de empresa da de alta
    /// usuarios porque el da de alta usuarios. Ninguno alcanza el terreno del otro. Es la misma
    /// separacion de funciones que ya sostenia el modelo, aplicada tambien a quien reparte.</para>
    ///
    /// <para><b>Arranque de un tenant nuevo:</b> las dos capacidades de administracion son minimos
    /// irrevocables —<c>Permisos.Configurar</c> para el administrador, <c>PermisosOperacion.Configurar</c>
    /// para el gerente—, asi que ninguna empresa nace sin quien reparta. La matriz operativa inicial
    /// la carga MantIA durante la alta, con el bypass de superadministrador, que se audita.</para>
    /// </summary>
    private static string RecursoAdministracion(string recurso)
    {
        var r = CatalogoPermisos.BuscarRecurso(recurso);
        return r?.Ambito == Ambito.Operacion ? "PermisosOperacion" : "Permisos";
    }
}
