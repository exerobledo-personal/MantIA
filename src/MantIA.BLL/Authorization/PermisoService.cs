using MantIA.BE.Common;
using MantIA.BE.Seguridad;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MantIA.BLL.Authorization;

public class PermisoService : IPermisoService
{
    private static readonly TimeSpan Expiracion = TimeSpan.FromMinutes(10);

    private readonly MantIADbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenant _tenant;

    public PermisoService(MantIADbContext db, IMemoryCache cache, ICurrentTenant tenant)
    {
        _db = db;
        _cache = cache;
        _tenant = tenant;
    }

    /// <summary>
    /// Resuelve un permiso en seis pasos. <b>El orden es la parte que importa</b>: cada paso solo
    /// puede recortar lo que dejaron pasar los anteriores, nunca ampliarlo.
    ///
    /// <list type="number">
    /// <item><b>Superadministrador de MantIA.</b> Pasa por encima de todo. Es una excepcion
    /// explicita y por eso cada uso se audita aparte, con severidad critica.</item>
    ///
    /// <item><b>Estado de la empresa.</b> Una empresa suspendida entra en modo de solo lectura:
    /// consultar y exportar siguen, todo lo demas se deniega. Una empresa dada de baja no llega
    /// hasta aca, porque el login ya la rechazo.</item>
    ///
    /// <item><b>Frontera estructural.</b> Si la combinacion no es valida para el ROL, se deniega
    /// sin mirar nada configurable. Este paso es el que hace seguras las excepciones nominales del
    /// paso 5: como se evalua contra el rol y no contra la excepcion, ninguna fila nominal puede
    /// sacar a un usuario del ambito de su rol.</item>
    ///
    /// <item><b>Piso irrevocable.</b> Lo que un rol no puede perder se concede aunque la matriz o
    /// una excepcion nominal digan lo contrario.</item>
    ///
    /// <item><b>Excepcion nominal vigente.</b> Lo mas especifico gana: una fila para esta persona
    /// pesa mas que cualquier regla de su rol.</item>
    ///
    /// <item><b>Matriz de la empresa.</b> Primero la celda del nivel exacto, despues la generica
    /// del rol.</item>
    /// </list>
    /// </summary>
    public async Task<bool> PuedeAsync(ContextoPermiso contexto, string recurso, string accion)
    {
        if (contexto.Rol == RolSistema.SuperAdminMantIA)
            return true;

        if (_tenant.EmpresaId is null)
            return false;   // sin empresa resuelta no se evalua nada: fail-closed

        var empresaId = _tenant.EmpresaId.Value;

        if (await EnSoloLecturaAsync(empresaId) && !EsLectura(accion))
            return false;

        if (!CatalogoPermisos.EsCombinacionValida(contexto.Rol, recurso, accion))
            return false;

        if (PermisosMinimos.EsMinimo(contexto.Rol, recurso, accion))
            return true;

        if (contexto.UsuarioId is { } usuarioId)
        {
            var nominal = await ExcepcionAsync(usuarioId, recurso, accion);
            if (nominal is not null) return nominal.Value;
        }

        var matriz = await ObtenerMatrizAsync(empresaId);

        // La celda del nivel exacto gana sobre la generica del rol. Sin esta precedencia,
        // "Supervisor puede consultar" y "Supervisor Jr no puede consultar" darian un resultado que
        // depende del orden en que la base devuelva las filas.
        var celda =
            matriz.FirstOrDefault(p => Coincide(p.Recurso, p.Accion, recurso, accion)
                                    && p.Rol == contexto.Rol
                                    && p.NivelPermisoId == contexto.NivelPermisoId)
            ?? matriz.FirstOrDefault(p => Coincide(p.Recurso, p.Accion, recurso, accion)
                                    && p.Rol == contexto.Rol
                                    && p.NivelPermisoId is null);

        return celda?.Concedido ?? false;
    }

    public void Invalidar(Guid empresaId) => _cache.Remove(ClaveMatriz(empresaId));

    public void InvalidarUsuario(Guid usuarioId) => _cache.Remove(ClaveUsuario(usuarioId));

    // ---------------------------------------------------------------- estado de la empresa

    /// <summary>
    /// Modo degradado por falta de pago o suspension comercial: el cliente entra, consulta sus
    /// maquinas y sus ordenes, ve los graficos y exporta reportes, pero no puede cargar ni
    /// modificar nada.
    /// <para>
    /// Se implementa aca y no en cada pantalla por la misma razon que los filtros de tenant: si
    /// dependiera de que cada modulo se acuerde de preguntarlo, alcanza con que uno se olvide para
    /// que la suspension no signifique nada.
    /// </para>
    /// </summary>
    private async Task<bool> EnSoloLecturaAsync(Guid empresaId) =>
        await _cache.GetOrCreateAsync($"estado_empresa_{empresaId}", async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = Expiracion;
            var estado = await _db.Empresas
                .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
                .Where(e => e.Id == empresaId)
                .Select(e => e.Estado)
                .FirstOrDefaultAsync();
            return estado == EstadoEmpresa.Suspendida;
        });

    private static bool EsLectura(string accion) =>
        string.Equals(accion, Acciones.Consultar, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(accion, Acciones.Exportar, StringComparison.OrdinalIgnoreCase);

    // ---------------------------------------------------------------- excepciones nominales

    /// <summary>Nulo si no hay excepcion vigente para ese par.</summary>
    private async Task<bool?> ExcepcionAsync(Guid usuarioId, string recurso, string accion)
    {
        var excepciones = await _cache.GetOrCreateAsync(ClaveUsuario(usuarioId), async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = Expiracion;

            // El filtro global de empresa ya acota al tenant: una excepcion de otra empresa no
            // puede llegar hasta aca aunque el identificador de usuario coincidiera.
            return await _db.PermisosPorUsuario
                .AsNoTracking()
                .Where(p => p.UsuarioId == usuarioId)
                .Select(p => new Excepcion(p.Recurso, p.Accion, p.Concedido, p.VigenteHasta))
                .ToListAsync();
        }) ?? [];

        var ahora = DateTimeOffset.UtcNow;

        // El vencimiento se compara al evaluar y no al leer de la base: una excepcion que vence a
        // las 15:00 tiene que dejar de aplicar a las 15:00, no cuando expire la entrada de cache.
        return excepciones
            .FirstOrDefault(e => Coincide(e.Recurso, e.Accion, recurso, accion)
                              && (e.VigenteHasta is null || e.VigenteHasta > ahora))
            ?.Concedido;
    }

    // ---------------------------------------------------------------- matriz

    private async Task<List<Celda>> ObtenerMatrizAsync(Guid empresaId) =>
        await _cache.GetOrCreateAsync(ClaveMatriz(empresaId), async entrada =>
        {
            // La expiracion es una red de contencion por si alguna escritura no invalidara la
            // entrada, no el mecanismo por el que se propagan los cambios.
            entrada.AbsoluteExpirationRelativeToNow = Expiracion;

            return await _db.PermisosPorRolYNivel
                .AsNoTracking()
                .Select(p => new Celda(p.Rol, p.NivelPermisoId, p.Recurso, p.Accion, p.Concedido))
                .ToListAsync();
        }) ?? [];

    // ---------------------------------------------------------------- auxiliares

    private static bool Coincide(string recursoFila, string accionFila, string recurso, string accion) =>
        string.Equals(recursoFila, recurso, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(accionFila, accion, StringComparison.OrdinalIgnoreCase);

    private static string ClaveMatriz(Guid empresaId) => $"matriz_permisos_{empresaId}";
    private static string ClaveUsuario(Guid usuarioId) => $"permisos_usuario_{usuarioId}";

    private record Celda(RolSistema Rol, Guid? NivelPermisoId, string Recurso, string Accion, bool Concedido);
    private record Excepcion(string Recurso, string Accion, bool Concedido, DateTimeOffset? VigenteHasta);
}
