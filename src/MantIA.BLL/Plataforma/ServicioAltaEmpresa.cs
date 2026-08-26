using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.BLL.Acceso;
using MantIA.BLL.Auditoria;
using MantIA.BLL.Authorization;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Plataforma;

/// <summary>Lo que hace falta para dar de alta un cliente. El Usuario 0 no es opcional.</summary>
public record AltaEmpresa(
    string RazonSocial,
    string TenantId,
    Guid PlanId,
    string EmailUsuarioCero,
    string NombreUsuarioCero,
    string ApellidoUsuarioCero,

    // Los cuatro topes y la vigencia salen del plan si vienen en nulo. Estan para poder acordar una
    // excepcion con un cliente al momento del alta —o acortar una prueba— sin inventar un plan.
    int? MaxMaquinas = null,
    int? MaxUsuarios = null,
    int? MaxPlantas = null,
    int? MaxOrdenesTrabajo = null,
    DateTimeOffset? FinVigencia = null);

/// <summary>Cambio de plan de un cliente que ya existe. Es el upgrade de una prueba.</summary>
public record CambioPlan(
    Guid EmpresaId,
    Guid PlanId,
    int? MaxMaquinas = null,
    int? MaxUsuarios = null,
    int? MaxPlantas = null,
    int? MaxOrdenesTrabajo = null,
    DateTimeOffset? FinVigencia = null);

public record ResultadoAltaEmpresa(
    Empresa? Empresa, InvitacionUsuario? Invitacion, string Detalle)
{
    public bool Exitoso => Empresa is not null;
}

public interface IServicioAltaEmpresa
{
    Task<ResultadoAltaEmpresa> AltaAsync(AltaEmpresa alta, CancellationToken ct = default);

    /// <summary>
    /// Cambia el plan de un cliente y con el sus topes y su vigencia. Es el upgrade de una prueba a
    /// cliente pago, y no mueve un solo dato: la empresa, sus maquinas y sus ordenes son las mismas.
    /// </summary>
    Task<bool> CambiarPlanAsync(CambioPlan cambio, CancellationToken ct = default);
}

/// <summary>
/// Da de alta un cliente completo: la empresa, su dominio, sus niveles y la invitación del Usuario 0.
///
/// <para><b>Es la única forma de que exista un tenant.</b> No hay registro público. Detrás de cada
/// cliente hay un contrato, así que el alta es una operación de MantIA y no un formulario en
/// internet. Eso también significa que la verificación del dominio es comercial y no técnica: quien
/// da de alta ya sabe con qué empresa está tratando.</para>
///
/// <para><b>Las cuatro cosas van juntas y no por separado.</b> Una empresa sin dominio no puede
/// invitar a nadie; una sin niveles no puede asignar permisos; una sin Usuario 0 no tiene quién la
/// administre y hay que volver a MantIA por cada empleado que quieran sumar. Dejar cualquiera de las
/// tres para después produce un cliente a medio crear que nadie recuerda terminar.</para>
///
/// <para><b>El dominio sale del correo del Usuario 0.</b> Es lo que fija a quién va a poder invitar
/// esa empresa. Si después necesita más de uno —el caso típico es una fábrica que se fusionó— los
/// suma su administrador.</para>
/// </summary>
public class ServicioAltaEmpresa : IServicioAltaEmpresa
{
    private readonly MantIADbContext _db;
    private readonly CurrentTenant _tenant;
    private readonly IBitacora _bitacora;
    private readonly IPermisoService _permisos;

    public ServicioAltaEmpresa(
        MantIADbContext db, ICurrentTenant tenant, IBitacora bitacora, IPermisoService permisos)
    {
        _db = db;
        _tenant = (CurrentTenant)tenant;
        _bitacora = bitacora;
        _permisos = permisos;
    }

    public async Task<ResultadoAltaEmpresa> AltaAsync(AltaEmpresa alta, CancellationToken ct = default)
    {
        var correo = alta.EmailUsuarioCero.Trim().ToLowerInvariant();
        var dominio = DominioEmpresa.De(correo);

        if (dominio is null)
            return new ResultadoAltaEmpresa(
                null, null, $"'{alta.EmailUsuarioCero}' no es una dirección de correo.");

        // Una identidad pertenece a una sola empresa, asi que el Usuario 0 no puede ser alguien que
        // ya esté en otra. Se comprueba acá y no al final para no dejar una empresa creada a medias.
        if (await _db.Usuarios
                .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
                .AnyAsync(u => u.Email == correo, ct))
            return new ResultadoAltaEmpresa(
                null, null,
                $"{correo} ya es usuario de otra empresa. Una identidad pertenece a una sola.");

        if (await _db.Invitaciones
                .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
                .AnyAsync(i => i.Email == correo && i.Estado == EstadoInvitacion.Pendiente, ct))
            return new ResultadoAltaEmpresa(
                null, null, $"Ya hay una invitación pendiente para {correo}.");

        if (await _db.Empresas.IgnoreQueryFilters().AnyAsync(e => e.TenantId == alta.TenantId, ct))
            return new ResultadoAltaEmpresa(
                null, null, $"Ya existe una empresa con el identificador '{alta.TenantId}'.");

        var plan = await _db.Planes.FirstOrDefaultAsync(p => p.Id == alta.PlanId, ct);
        if (plan is null)
            return new ResultadoAltaEmpresa(null, null, "El plan no existe.");

        var empresaPrevia = _tenant.EmpresaId;
        var usuarioPrevio = _tenant.UsuarioId;

        try
        {
            var ahora = DateTimeOffset.UtcNow;

            var empresa = new Empresa
            {
                RazonSocial = alta.RazonSocial.Trim(),
                TenantId = alta.TenantId.Trim(),
                PlanId = alta.PlanId,
                Estado = EstadoEmpresa.Activa,

                // Los topes se COPIAN del plan y desde ahi mandan los de la empresa. Copiarlos en
                // lugar de leerlos del plan en cada consulta es lo que permite ajustar uno sin
                // afectar al resto de los clientes del mismo plan, y lo que hace que cambiarle el
                // plan a alguien no le pise en silencio una excepcion acordada.
                MaxMaquinasHabilitadas = alta.MaxMaquinas ?? plan.MaxMaquinas,
                MaxUsuariosHabilitados = alta.MaxUsuarios ?? plan.MaxUsuarios,
                MaxPlantasHabilitadas = alta.MaxPlantas ?? plan.MaxPlantas,
                MaxOrdenesTrabajo = alta.MaxOrdenesTrabajo ?? plan.MaxOrdenesTrabajo,

                InicioVigencia = ahora,
                FinVigencia = alta.FinVigencia ?? ahora.AddDays(plan.DiasVigencia),
            };

            // La empresa no es entidad de tenant, así que se puede crear con el contexto todavía
            // posicionado en MantIA.
            _db.Empresas.Add(empresa);
            await _db.SaveChangesAsync(ct);

            // A partir de acá todo lo que se crea pertenece al cliente nuevo, y el guardado completa
            // la empresa de cada alta con el tenant activo. Es el único lugar, junto con la siembra
            // y el barrido de integridad, donde el tenant se escribe a mano en vez de salir de la
            // autenticación.
            _tenant.EmpresaId = empresa.Id;

            _db.DominiosEmpresa.Add(new DominioEmpresa
            {
                Dominio = dominio,
                EsPrincipal = true,
            });

            var nivelSr = new NivelPermiso
            {
                Nombre = "Sr",
                Descripcion = "Nivel pleno del rol dentro de la empresa.",
                Jerarquia = 2,
            };

            _db.NivelesPermiso.AddRange(
                new NivelPermiso
                {
                    Nombre = "Jr",
                    Descripcion = "Nivel de entrada. Ejecuta, no decide.",
                    Jerarquia = 1,
                },
                nivelSr);

            var invitacion = new InvitacionUsuario
            {
                Email = correo,
                Nombre = alta.NombreUsuarioCero.Trim(),
                Apellido = alta.ApellidoUsuarioCero.Trim(),
                Rol = RolSistema.AdminEmpresa,
                NivelPermisoId = nivelSr.Id,
                Estado = EstadoInvitacion.Pendiente,
                FechaVencimiento = DateTimeOffset.UtcNow.Add(ServicioAcceso.VigenciaInvitacion),

                // Nula a propósito: la emitió MantIA, no alguien de la empresa. Esa distinción se
                // ve después en la bitácora y explica por qué el Usuario 0 existe sin que nadie de
                // adentro lo haya invitado.
                InvitadaPorUsuarioId = null,
            };

            _db.Invitaciones.Add(invitacion);
            await _db.SaveChangesAsync(ct);

            await _bitacora.RegistrarAsync(
                new AccionAuditada(
                    Recurso: "Empresas",
                    Accion: "Alta",
                    RecursoId: empresa.Id,
                    Descripcion:
                        $"Alta de {empresa.RazonSocial} con plan {plan.Nombre}, vigencia hasta " +
                        $"{empresa.FinVigencia:dd/MM/yyyy}. Dominio principal '{dominio}'. " +
                        $"Se invito al Usuario 0 {correo} como AdminEmpresa.",
                    EmpresaAfectadaId: empresa.Id),
                ct);

            return new ResultadoAltaEmpresa(empresa, invitacion, string.Empty);
        }
        finally
        {
            // El contexto vuelve a donde estaba, pase lo que pase. Quien llamó es personal de MantIA
            // y tiene que seguir siéndolo después de esta operación.
            _tenant.EmpresaId = empresaPrevia;
            _tenant.UsuarioId = usuarioPrevio;
        }
    }

    /// <summary>
    /// Cambia el plan de un cliente. Es el upgrade de una prueba, y es a propósito lo más aburrido
    /// posible: corre una fecha y sube unos números.
    ///
    /// <para><b>No se mueve un solo dato.</b> Una cuenta de prueba y un cliente pago son el mismo
    /// tenant con distintos topes, así que lo que el prospecto cargó durante la prueba —sus máquinas,
    /// sus órdenes, su stock— sigue exactamente donde estaba. Con una demo separada, todo eso se
    /// perdería justo en el momento en que decide pagar, que es el peor momento posible para pedirle
    /// que vuelva a cargar nada.</para>
    ///
    /// <para><b>Los topes que no se especifican se toman del plan nuevo.</b> Un downgrade puede dejar
    /// a la empresa por encima de su techo; eso está previsto y no borra nada: deja de poder crear
    /// hasta que baje sola o le amplíen el cupo.</para>
    /// </summary>
    public async Task<bool> CambiarPlanAsync(CambioPlan cambio, CancellationToken ct = default)
    {
        var empresa = await _db.Empresas
            .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
            .FirstOrDefaultAsync(e => e.Id == cambio.EmpresaId, ct);

        if (empresa is null || empresa.FechaBaja is not null) return false;

        var plan = await _db.Planes.FirstOrDefaultAsync(p => p.Id == cambio.PlanId, ct);
        if (plan is null) return false;

        var planAnterior = await _db.Planes
            .Where(p => p.Id == empresa.PlanId)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync(ct) ?? "sin plan";

        var vigenciaAnterior = empresa.FinVigencia;
        var ahora = DateTimeOffset.UtcNow;

        empresa.PlanId = plan.Id;
        empresa.MaxMaquinasHabilitadas = cambio.MaxMaquinas ?? plan.MaxMaquinas;
        empresa.MaxUsuariosHabilitados = cambio.MaxUsuarios ?? plan.MaxUsuarios;
        empresa.MaxPlantasHabilitadas = cambio.MaxPlantas ?? plan.MaxPlantas;
        empresa.MaxOrdenesTrabajo = cambio.MaxOrdenesTrabajo ?? plan.MaxOrdenesTrabajo;
        empresa.FinVigencia = cambio.FinVigencia ?? ahora.AddDays(plan.DiasVigencia);

        // Comprar saca de la suspension. Es lo que espera cualquiera que acaba de pagar, y no
        // hacerlo produce el reclamo mas evitable que existe.
        if (empresa.Estado == EstadoEmpresa.Suspendida && !empresa.VigenciaVencida(ahora))
            empresa.Estado = EstadoEmpresa.Activa;

        await _db.SaveChangesAsync(ct);

        // Sin esto, el cliente que acaba de pagar sigue en solo lectura hasta que caduque la cache.
        _permisos.InvalidarEstadoEmpresa(empresa.Id);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Empresas",
                Accion: "Modificacion",
                RecursoId: empresa.Id,
                Descripcion:
                    $"Cambio de plan: {planAnterior} a {plan.Nombre}. " +
                    $"Topes: {empresa.MaxMaquinasHabilitadas} maquinas, " +
                    $"{empresa.MaxUsuariosHabilitados} usuarios, " +
                    $"{empresa.MaxPlantasHabilitadas} plantas, " +
                    $"{empresa.MaxOrdenesTrabajo?.ToString() ?? "sin tope"} ordenes abiertas.",
                EstadoAnterior: $"{planAnterior} hasta {vigenciaAnterior:dd/MM/yyyy}",
                EstadoPosterior: $"{plan.Nombre} hasta {empresa.FinVigencia:dd/MM/yyyy}",
                EmpresaAfectadaId: empresa.Id),
            ct);

        return true;
    }
}
