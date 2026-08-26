using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Plataforma;

/// <summary>Qué se cuenta contra el cupo.</summary>
public enum RecursoConCupo
{
    Maquinas,
    Usuarios,
    Plantas,
    OrdenesTrabajo
}

/// <summary>Cuánto hay, cuánto entra y si se puede crear uno más.</summary>
public record EstadoCupo(RecursoConCupo Recurso, int Usados, int? Tope)
{
    public bool SinLimite => Tope is null;

    public bool HayLugar => Tope is null || Usados < Tope;

    /// <summary>Cuántos faltan para el tope. Nulo si no hay tope.</summary>
    public int? Disponibles => Tope is { } t ? Math.Max(0, t - Usados) : null;

    /// <summary>
    /// Por encima del tope, no solo lleno. Pasa cuando se baja el plan o se acorta la prueba: no es
    /// un error, y no se corrige borrando nada.
    /// </summary>
    public bool Excedido => Tope is { } t && Usados > t;
}

public interface IControlCupos
{
    /// <summary>Puede crearse uno más de ese recurso en la empresa del contexto actual.</summary>
    Task<bool> HayLugarAsync(RecursoConCupo recurso, CancellationToken ct = default);

    /// <summary>Estado de un cupo, para mostrarlo en pantalla antes de que alguien choque contra él.</summary>
    Task<EstadoCupo> EstadoAsync(RecursoConCupo recurso, CancellationToken ct = default);

    /// <summary>Todos los cupos de la empresa. Es lo que muestra el panel de la cuenta.</summary>
    Task<IReadOnlyList<EstadoCupo>> TodosAsync(CancellationToken ct = default);

    /// <summary>
    /// Lanza si no hay lugar. Se usa en los servicios de alta, donde seguir adelante sin cupo sería
    /// una violación del contrato comercial y no un detalle de interfaz.
    /// </summary>
    Task ExigirLugarAsync(RecursoConCupo recurso, CancellationToken ct = default);
}

/// <summary>
/// Hace valer los topes contratados.
///
/// <para><b>Sin esto, los planes son decoración.</b> Los campos de cupo existían y se mostraban en
/// pantalla —el "12 / 200" del panel— pero nada impedía cargar quinientas máquinas con un plan de
/// doscientas. Una cuenta de prueba "de hasta 5 máquinas" no significaba nada.</para>
///
/// <para><b>El cupo bloquea el alta y nunca borra.</b> Una empresa que quede por encima de su techo
/// conserva todo lo que tiene y solo deja de poder crear más. Es la única política defendible: bajar
/// un plan no puede destruir el trabajo de nadie, y el caso se da todo el tiempo —se acorta una
/// prueba, se renegocia un contrato, alguien se equivoca al cargar el número—.</para>
///
/// <para><b>Manda el número de la empresa, no el del plan.</b> El del plan es el valor por defecto
/// que se copia al dar de alta; después se puede ajustar por acuerdo comercial sin inventar un plan
/// nuevo para cada cliente. Así hay un solo número que mirar cuando algo se bloquea.</para>
///
/// <para><b>Lo dado de baja no ocupa lugar.</b> Se cuenta lo vivo. Dar de baja una máquina libera su
/// cupo aunque la fila siga existiendo, que es lo que espera cualquiera: el cliente paga por lo que
/// opera, no por su historial.</para>
/// </summary>
public class ControlCupos : IControlCupos
{
    private readonly MantIADbContext _db;
    private readonly ICurrentTenant _tenant;

    public ControlCupos(MantIADbContext db, ICurrentTenant tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<bool> HayLugarAsync(RecursoConCupo recurso, CancellationToken ct = default) =>
        (await EstadoAsync(recurso, ct)).HayLugar;

    public async Task ExigirLugarAsync(RecursoConCupo recurso, CancellationToken ct = default)
    {
        var estado = await EstadoAsync(recurso, ct);
        if (estado.HayLugar) return;

        throw new CupoAgotadoException(estado);
    }

    public async Task<EstadoCupo> EstadoAsync(
        RecursoConCupo recurso, CancellationToken ct = default)
    {
        var empresa = await EmpresaAsync(ct);
        return new EstadoCupo(recurso, await ContarAsync(recurso, ct), TopeDe(empresa, recurso));
    }

    public async Task<IReadOnlyList<EstadoCupo>> TodosAsync(CancellationToken ct = default)
    {
        var empresa = await EmpresaAsync(ct);
        var estados = new List<EstadoCupo>();

        foreach (var recurso in Enum.GetValues<RecursoConCupo>())
            estados.Add(new EstadoCupo(recurso, await ContarAsync(recurso, ct), TopeDe(empresa, recurso)));

        return estados;
    }

    private static int? TopeDe(Empresa empresa, RecursoConCupo recurso) => recurso switch
    {
        RecursoConCupo.Maquinas => empresa.MaxMaquinasHabilitadas,
        RecursoConCupo.Usuarios => empresa.MaxUsuariosHabilitados,
        RecursoConCupo.Plantas => empresa.MaxPlantasHabilitadas,
        RecursoConCupo.OrdenesTrabajo => empresa.MaxOrdenesTrabajo,
        _ => null
    };

    /// <summary>
    /// Cuenta lo que ocupa lugar. Los filtros globales ya excluyen lo dado de baja y lo de otras
    /// empresas, así que estas cuentas son deliberadamente simples: si algún día hay que ignorar un
    /// filtro acá, es señal de que la cuenta dejó de significar lo que dice.
    /// </summary>
    private async Task<int> ContarAsync(RecursoConCupo recurso, CancellationToken ct) => recurso switch
    {
        RecursoConCupo.Maquinas => await _db.Maquinas.CountAsync(ct),
        RecursoConCupo.Plantas => await _db.Plantas.CountAsync(ct),

        // Las invitaciones pendientes cuentan como usuario. Si no, una empresa con el cupo lleno
        // podría invitar a veinte personas y el rechazo aparecería recién en el primer ingreso de
        // cada una, que es el peor momento y el más difícil de explicar.
        RecursoConCupo.Usuarios =>
            await _db.Usuarios.CountAsync(ct) +
            await _db.Invitaciones.CountAsync(i => i.Estado == EstadoInvitacion.Pendiente, ct),

        // Solo las que están vivas. Una prueba con tope de diez órdenes no debería agotarse por
        // diez órdenes ya cerradas hace un mes: lo que se acota es cuánto se puede tener en curso.
        RecursoConCupo.OrdenesTrabajo =>
            await _db.OrdenesTrabajo.CountAsync(
                o => o.Estado == EstadoOrden.Abierta || o.Estado == EstadoOrden.EnCurso, ct),

        _ => 0
    };

    private async Task<Empresa> EmpresaAsync(CancellationToken ct)
    {
        if (_tenant.EmpresaId is not { } empresaId)
            throw new InvalidOperationException(
                "No hay empresa en el contexto: no se puede evaluar el cupo.");

        return await _db.Empresas
                   .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
                   .FirstOrDefaultAsync(e => e.Id == empresaId, ct)
               ?? throw new InvalidOperationException($"La empresa {empresaId} no existe.");
    }
}

/// <summary>
/// Se intentó crear algo sin cupo. Es una excepción y no un booleano porque ocurre en el medio de
/// una operación de negocio, donde seguir adelante no es una opción: la interfaz debería haberlo
/// impedido antes, y si llegó hasta acá conviene que se note.
/// </summary>
public class CupoAgotadoException : Exception
{
    public CupoAgotadoException(EstadoCupo estado)
        : base(Mensaje(estado)) => Estado = estado;

    public EstadoCupo Estado { get; }

    private static string Mensaje(EstadoCupo estado) =>
        estado.Excedido
            ? $"La empresa tiene {estado.Usados} de {estado.Tope} en {estado.Recurso} y ya supera su " +
              "cupo. No se pierde nada de lo cargado, pero no se puede crear mas hasta ampliar el plan."
            : $"Se alcanzo el cupo de {estado.Recurso}: {estado.Usados} de {estado.Tope}.";
}
