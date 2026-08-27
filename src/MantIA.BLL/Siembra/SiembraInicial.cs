using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MantIA.BLL.Siembra;

/// <summary>
/// Deja la base en el mínimo estado desde el que el sistema se puede usar.
///
/// <para><b>Por qué hace falta.</b> Con la base vacía no se puede hacer nada, y no por comodidad:
/// cada escritura registra quién la hizo, el evaluador de permisos necesita una fila en
/// <c>usuarios</c> con su rol para decidir, y el resolvedor de tenant traduce la identidad externa a
/// esa fila para saber de qué empresa es. Sin una persona cargada no hay ni login ni permisos.</para>
///
/// <para><b>Solo en desarrollo, y con dos cerrojos.</b> El registro en <c>Program.cs</c> está dentro
/// de un <c>if (Environment.IsDevelopment())</c> y además esta clase lo vuelve a comprobar. Duplicado
/// a propósito: sembrar usuarios administradores en producción es la clase de accidente que no se
/// descubre hasta que alguien entra con ellos.</para>
///
/// <para><b>Es idempotente.</b> Cada bloque comprueba si su fila ya está antes de crearla, así que
/// correrla veinte veces deja el mismo resultado que correrla una. No actualiza lo que encuentra: si
/// cambiaste algo a mano, se respeta.</para>
///
/// <para><b>Identificadores fijos.</b> Los <c>Guid</c> están escritos en el código en lugar de
/// sortearse. Es lo que hace que un enlace directo a <c>/maquinas/{id}</c> siga funcionando después
/// de borrar y volver a sembrar, y lo que permite reconocer de un vistazo en la base qué filas son
/// de siembra y cuáles se cargaron probando.</para>
///
/// <para><b>Lo que deliberadamente NO siembra: la matriz de permisos.</b> Cada empresa define la
/// suya; en código solo vive el piso irrevocable. Un administrador recién sembrado puede lo que
/// <c>PermisosMinimos</c> le garantiza y nada más, que es exactamente el comportamiento buscado. Si
/// la siembra cargara una matriz "razonable", esa matriz se convertiría en el default de hecho y
/// nadie volvería a mirarla.</para>
/// </summary>
public class SiembraInicial : BackgroundService
{
    // --- Identificadores fijos ---
    private static readonly Guid PlanInterno = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid PlanProfesional = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid PlanPrueba = new("11111111-0000-0000-0000-000000000003");

    private static readonly Guid EmpresaMantIA = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid EmpresaCliente = new("22222222-0000-0000-0000-000000000002");

    private static readonly Guid NivelJr = new("33333333-0000-0000-0000-000000000001");
    private static readonly Guid NivelSr = new("33333333-0000-0000-0000-000000000002");

    private static readonly Guid PlantaCliente = new("44444444-0000-0000-0000-000000000001");

    private static readonly Guid DominioMantIA = new("66666666-0000-0000-0000-000000000001");
    private static readonly Guid DominioClientePrincipal = new("66666666-0000-0000-0000-000000000002");
    private static readonly Guid DominioClienteGmail = new("66666666-0000-0000-0000-000000000003");

    private static readonly Guid InvitacionPrueba = new("77777777-0000-0000-0000-000000000001");

    private static readonly Guid UsuarioSuperAdmin = new("55555555-0000-0000-0000-000000000001");
    private static readonly Guid UsuarioAdminEmpresa = new("55555555-0000-0000-0000-000000000002");

    /// <summary>
    /// Identidades de relleno. No existen en Auth0: sirven para probar permisos, stock, órdenes,
    /// dígitos y bitácora sin depender del proveedor de identidad. El día que se conecte el login
    /// real estas dos filas se borran —los usuarios son la única entidad con baja física— y se
    /// crean las verdaderas, sin dejar residuo.
    /// </summary>
    private const string SubSuperAdmin = "auth0|semilla-superadmin";
    private const string SubAdminEmpresa = "auth0|semilla-adminempresa";

    private readonly IServiceProvider _servicios;
    private readonly IHostEnvironment _entorno;
    private readonly ILogger<SiembraInicial> _log;

    public SiembraInicial(
        IServiceProvider servicios, IHostEnvironment entorno, ILogger<SiembraInicial> log)
    {
        _servicios = servicios;
        _entorno = entorno;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_entorno.IsDevelopment())
        {
            _log.LogWarning(
                "La siembra inicial se salteo: el entorno es {Entorno} y solo corre en Development.",
                _entorno.EnvironmentName);
            return;
        }

        try
        {
            using var alcance = _servicios.CreateScope();
            await SembrarAsync(alcance.ServiceProvider, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No tumba la aplicacion. Una siembra fallida deja la base como estaba y el mensaje
            // dice que revisar; caerse al arrancar seria peor, porque tapa el error real detras
            // de un host que no levanta.
            _log.LogError(ex, "Fallo la siembra inicial. La base quedo sin tocar.");
        }
    }

    private async Task SembrarAsync(IServiceProvider alcance, CancellationToken ct)
    {
        var db = alcance.GetRequiredService<MantIADbContext>();

        if (alcance.GetRequiredService<ICurrentTenant>() is not CurrentTenant tenant)
        {
            _log.LogError(
                "ICurrentTenant no es CurrentTenant, asi que la siembra no puede posicionarse en " +
                "una empresa. Revisar el registro de servicios.");
            return;
        }

        // Sin esto, la primera corrida contra una base sin migrar falla con un error de tabla
        // inexistente que no dice nada util.
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
        {
            _log.LogWarning(
                "Hay migraciones sin aplicar. La siembra no corre hasta que se ejecute " +
                "'dotnet ef database update --project src/MantIA.DAL'.");
            return;
        }

        var creadas = 0;

        creadas += await SembrarPlanesAsync(db, ct);
        creadas += await SembrarEmpresasAsync(db, ct);

        // Cada bloque de tenant se guarda por separado: el contexto completa EmpresaId de toda alta
        // con el tenant activo, asi que mezclar dos empresas en un mismo SaveChanges mandaria las
        // filas de una a la otra.
        creadas += await SembrarPlataformaAsync(db, tenant, ct);
        creadas += await SembrarClienteAsync(db, tenant, ct);
        creadas += await CompletarCamposNuevosAsync(db, ct);

        tenant.EmpresaId = null;
        tenant.UsuarioId = null;

        if (creadas == 0)
        {
            _log.LogInformation("Siembra inicial: la base ya estaba sembrada, no se creo nada.");
            return;
        }

        _log.LogInformation(
            "Siembra inicial: {Cantidad} filas creadas o campos completados. Entrar como '{SuperAdmin}' o " +
            "'{AdminEmpresa}'. Son identidades de relleno: no existen en Auth0.",
            creadas, SubSuperAdmin, SubAdminEmpresa);
    }

    // ------------------------------------------------------------------ campos agregados despues

    /// <summary>
    /// Completa los campos que se agregaron al modelo despues de que la fila ya existia.
    ///
    /// <para><b>Solo rellena lo que esta en nulo. Nunca pisa un valor.</b> Esa es la linea que separa
    /// esto de una siembra que sobrescribe: si ajustaste un cupo a mano para probar algo, se respeta.
    /// El unico caso que resuelve es el de una columna que nacio despues que la fila y que, sin esto,
    /// queda vacia para siempre —y un cupo vacio significa sin limite, que es justo lo contrario de
    /// lo que se quiere—.</para>
    ///
    /// <para>Existe porque en desarrollo la base sobrevive a los cambios de modelo. Cuando el esquema
    /// deje de moverse, este metodo deberia desaparecer.</para>
    /// </summary>
    private static async Task<int> CompletarCamposNuevosAsync(MantIADbContext db, CancellationToken ct)
    {
        var empresas = await db.Empresas.IgnoreQueryFilters().ToListAsync(ct);
        var planes = await db.Planes.IgnoreQueryFilters().ToDictionaryAsync(p => p.Id, ct);

        var tocadas = 0;
        var ahora = DateTimeOffset.UtcNow;

        foreach (var empresa in empresas)
        {
            if (!planes.TryGetValue(empresa.PlanId, out var plan)) continue;

            if (empresa.MaxMaquinasHabilitadas is null)
            {
                empresa.MaxMaquinasHabilitadas = plan.MaxMaquinas;
                tocadas++;
            }

            if (empresa.MaxUsuariosHabilitados is null)
            {
                empresa.MaxUsuariosHabilitados = plan.MaxUsuarios;
                tocadas++;
            }

            if (empresa.MaxPlantasHabilitadas is null)
            {
                empresa.MaxPlantasHabilitadas = plan.MaxPlantas;
                tocadas++;
            }

            // El tope de ordenes se deja como esta: nulo es sin limite y es un valor legitimo, asi
            // que no hay forma de distinguir "todavia no se cargo" de "no tiene tope". Se completa
            // solo si la empresa es de prueba, donde no tener tope si seria un error.
            if (empresa.MaxOrdenesTrabajo is null && plan.EsPrueba)
            {
                empresa.MaxOrdenesTrabajo = plan.MaxOrdenesTrabajo;
                tocadas++;
            }

            if (empresa.InicioVigencia is null)
            {
                empresa.InicioVigencia = ahora;
                tocadas++;
            }

            // La vigencia nula tambien es legitima —MantIA no vence— asi que se completa solo si el
            // plan define una duracion y la empresa no es la propia plataforma.
            if (empresa.FinVigencia is null && empresa.Id != EmpresaMantIA && plan.DiasVigencia > 0)
            {
                empresa.FinVigencia = ahora.AddDays(plan.DiasVigencia);
                tocadas++;
            }

        }

        if (tocadas > 0) await db.SaveChangesAsync(ct);
        return tocadas;
    }

    // ------------------------------------------------------------------ plataforma

    private static async Task<int> SembrarPlanesAsync(MantIADbContext db, CancellationToken ct)
    {
        var creadas = 0;

        // Los planes son catalogo compartido, sin filtro de empresa. Igual se ignoran los filtros:
        // si alguno quedo dado de baja, hay que verlo y no volver a crearlo.
        var existentes = await db.Planes
            .IgnoreQueryFilters()
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (!existentes.Contains(PlanInterno))
        {
            db.Planes.Add(new Plan
            {
                Id = PlanInterno,
                Nombre = "Interno",
                Descripcion = "Plan del propio MantIA. No se comercializa.",
                MaxMaquinas = 0,
                MaxUsuarios = 50,
                MaxPlantas = 0,
                MaxOrdenesTrabajo = 0,
                PrecioMensual = 0m,

                // MantIA no vence. Es la unica empresa que puede no tener vigencia: si se venciera,
                // el personal de soporte quedaria en solo lectura y no podria ni reactivar clientes.
                DiasVigencia = 36_500,
            });
            creadas++;
        }

        if (!existentes.Contains(PlanPrueba))
        {
            db.Planes.Add(new Plan
            {
                Id = PlanPrueba,
                Nombre = "Prueba",
                Descripcion =
                    "Cuenta de evaluacion. Es un tenant real y lo que se cargue sobrevive al upgrade.",

                // El techo que hace que la prueba sea una prueba. Alcanza para cargar un sector y
                // ver el sistema funcionando con datos propios, que es lo que convence; no alcanza
                // para operar una planta gratis.
                MaxMaquinas = 5,
                MaxUsuarios = 3,
                MaxPlantas = 1,

                // Mil ordenes ABIERTAS. No es un limite comercial sino de operabilidad: para llegar
                // a mil sin cerrar ninguna hacen falta meses de abandono, asi que el numero solo
                // frena un uso automatizado o una carga descontrolada. Una orden es ademas lo que
                // menos recursos consume —"esta maquina necesita repuestos", "hay que cambiar esta
                // lamparita"— y no toca ni la ingesta ni el modelo.
                MaxOrdenesTrabajo = 1_000,

                PrecioMensual = 0m,
                DiasVigencia = 30,
                EsPrueba = true,
            });
            creadas++;
        }

        if (!existentes.Contains(PlanProfesional))
        {
            db.Planes.Add(new Plan
            {
                Id = PlanProfesional,
                Nombre = "Profesional",
                Descripcion = "Plan de referencia para pruebas: tres plantas y doscientas maquinas.",
                MaxMaquinas = 200,
                MaxUsuarios = 40,
                MaxPlantas = 3,

                // Sin tope de ordenes: acotar el trabajo de un cliente que paga no tiene sentido.
                MaxOrdenesTrabajo = null,

                PrecioMensual = 480_000m,
                DiasVigencia = 365,
            });
            creadas++;
        }

        if (creadas > 0) await db.SaveChangesAsync(ct);
        return creadas;
    }

    private static async Task<int> SembrarEmpresasAsync(MantIADbContext db, CancellationToken ct)
    {
        var creadas = 0;

        var existentes = await db.Empresas
            .IgnoreQueryFilters()
            .Select(e => e.Id)
            .ToListAsync(ct);

        // MantIA es un tenant mas, y tiene que serlo: los usuarios son entidades de empresa, asi
        // que el personal de la plataforma necesita una a la que pertenecer. Su aislamiento es el
        // mismo que el de cualquier cliente; lo que lo distingue es el rol de sus usuarios.
        if (!existentes.Contains(EmpresaMantIA))
        {
            db.Empresas.Add(new Empresa
            {
                Id = EmpresaMantIA,
                RazonSocial = "MantIA",
                TenantId = "org_mantia",
                PlanId = PlanInterno,
                MaxMaquinasHabilitadas = 0,
                MaxUsuariosHabilitados = 50,
                MaxPlantasHabilitadas = 0,
                MaxOrdenesTrabajo = 0,
                Estado = EstadoEmpresa.Activa,

                // Sin vencimiento: ver el comentario del plan Interno.
                InicioVigencia = DateTimeOffset.UtcNow,
                FinVigencia = null,
            });
            creadas++;
        }

        if (!existentes.Contains(EmpresaCliente))
        {
            db.Empresas.Add(new Empresa
            {
                Id = EmpresaCliente,
                RazonSocial = "Aceros del Litoral S.A.",
                TenantId = "org_aceros_litoral",
                PlanId = PlanProfesional,
                MaxMaquinasHabilitadas = 200,
                MaxUsuariosHabilitados = 40,
                MaxPlantasHabilitadas = 3,
                MaxOrdenesTrabajo = null,
                Estado = EstadoEmpresa.Activa,
                InicioVigencia = DateTimeOffset.UtcNow,
                FinVigencia = DateTimeOffset.UtcNow.AddYears(1),
            });
            creadas++;
        }

        if (creadas > 0) await db.SaveChangesAsync(ct);
        return creadas;
    }

    private static async Task<int> SembrarPlataformaAsync(
        MantIADbContext db, CurrentTenant tenant, CancellationToken ct)
    {
        tenant.EmpresaId = EmpresaMantIA;
        tenant.UsuarioId = null;

        var creadas = await SembrarDominioAsync(
            db, DominioMantIA, "mantia.com.ar", principal: true, ct);

        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Id == UsuarioSuperAdmin, ct))
            return creadas;

        // Sin nivel. El nivel recorta un rol, y el superadministrador es justamente el que no se
        // recorta: ponerle uno sugeriria que se le puede sacar algo por configuracion, y no es asi.
        db.Usuarios.Add(new Usuario
        {
            Id = UsuarioSuperAdmin,
            Auth0UserId = SubSuperAdmin,
            Email = "soporte@mantia.com.ar",
            Nombre = "Soporte",
            Apellido = "MantIA",
            Rol = RolSistema.SuperAdminMantIA,
            NivelPermisoId = null,
            Estado = EstadoGenerico.Activo,
        });

        await db.SaveChangesAsync(ct);
        return creadas + 1;
    }

    /// <summary>
    /// Un dominio habilitado. Acota a quien se puede invitar; no da acceso por si solo. Sin al menos
    /// uno cargado, la empresa no puede invitar a nadie y por lo tanto no entra nadie.
    /// </summary>
    private static async Task<int> SembrarDominioAsync(
        MantIADbContext db, Guid id, string dominio, bool principal, CancellationToken ct)
    {
        if (await db.DominiosEmpresa.IgnoreQueryFilters().AnyAsync(d => d.Id == id, ct))
            return 0;

        db.DominiosEmpresa.Add(new DominioEmpresa
        {
            Id = id,
            Dominio = dominio,
            EsPrincipal = principal,
        });

        await db.SaveChangesAsync(ct);
        return 1;
    }

    // ------------------------------------------------------------------ cliente de prueba

    private static async Task<int> SembrarClienteAsync(
        MantIADbContext db, CurrentTenant tenant, CancellationToken ct)
    {
        tenant.EmpresaId = EmpresaCliente;
        tenant.UsuarioId = null;

        var creadas = 0;

        creadas += await SembrarDominioAsync(
            db, DominioClientePrincipal, "acerosdellitoral.com.ar", principal: true, ct);

        // gmail.com como segundo dominio del cliente de prueba. No abre nada por si mismo: sigue
        // haciendo falta una invitacion nominal para entrar. Esta para poder probar el ingreso con
        // una cuenta de Google real sin tener un dominio corporativo a mano.
        creadas += await SembrarDominioAsync(
            db, DominioClienteGmail, "gmail.com", principal: false, ct);

        creadas += await SembrarNivelesAsync(db, ct);
        creadas += await SembrarPlantaAsync(db, ct);
        creadas += await SembrarAdministradorAsync(db, ct);
        creadas += await SembrarInvitacionDePruebaAsync(db, ct);

        return creadas;
    }

    private static async Task<int> SembrarNivelesAsync(MantIADbContext db, CancellationToken ct)
    {
        var existentes = await db.NivelesPermiso
            .IgnoreQueryFilters()
            .Where(n => n.EmpresaId == EmpresaCliente)
            .Select(n => n.Id)
            .ToListAsync(ct);

        var creadas = 0;

        // Jr y Sr son los dos que el sistema da por sentados al dar de alta un tenant. La jerarquia
        // no otorga permisos por si sola: solo sirve para detectar la incoherencia de un nivel
        // superior con menos permisos que uno inferior.
        if (!existentes.Contains(NivelJr))
        {
            db.NivelesPermiso.Add(new NivelPermiso
            {
                Id = NivelJr,
                Nombre = "Jr",
                Descripcion = "Nivel de entrada. Ejecuta, no decide.",
                Jerarquia = 1,
            });
            creadas++;
        }

        if (!existentes.Contains(NivelSr))
        {
            db.NivelesPermiso.Add(new NivelPermiso
            {
                Id = NivelSr,
                Nombre = "Sr",
                Descripcion = "Nivel pleno del rol dentro de la empresa.",
                Jerarquia = 2,
            });
            creadas++;
        }

        if (creadas > 0) await db.SaveChangesAsync(ct);
        return creadas;
    }

    private static async Task<int> SembrarPlantaAsync(MantIADbContext db, CancellationToken ct)
    {
        if (await db.Plantas.IgnoreQueryFilters().AnyAsync(p => p.Id == PlantaCliente, ct))
            return 0;

        // Una sola planta: las maquinas cuelgan de una y sin ninguna no se puede dar de alta nada.
        db.Plantas.Add(new Planta
        {
            Id = PlantaCliente,
            Nombre = "Planta San Lorenzo",
            Direccion = "Ruta Nacional 11 km 322",
            Localidad = "San Lorenzo, Santa Fe",
            Latitud = -32.745000m,
            Longitud = -60.735000m,
            Estado = EstadoGenerico.Activo,
        });

        await db.SaveChangesAsync(ct);
        return 1;
    }

    private static async Task<int> SembrarAdministradorAsync(MantIADbContext db, CancellationToken ct)
    {
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Id == UsuarioAdminEmpresa, ct))
            return 0;

        db.Usuarios.Add(new Usuario
        {
            Id = UsuarioAdminEmpresa,
            Auth0UserId = SubAdminEmpresa,
            Email = "administracion@acerosdellitoral.com.ar",
            Nombre = "Administracion",
            Apellido = "Aceros del Litoral",
            Rol = RolSistema.AdminEmpresa,
            NivelPermisoId = NivelSr,
            Estado = EstadoGenerico.Activo,
        });

        // Sin filas en UsuarioAlcance: vacio significa "todas las plantas de su empresa", que es lo
        // que corresponde a quien administra la empresa entera.
        await db.SaveChangesAsync(ct);
        return 1;
    }

    /// <summary>
    /// Una invitacion pendiente de verdad, para poder probar el primer ingreso completo con una
    /// cuenta de Google real.
    ///
    /// <para>Los dos usuarios de arriba se escriben directo en la base, que es lo que hace una
    /// siembra y lo que permite probar permisos sin depender de Auth0. Pero justamente por eso no
    /// ejercitan el camino de invitacion, que es el unico que va a existir en produccion. Esta fila
    /// esta para eso: cuando esa cuenta entre por primera vez, tiene que nacer sola su fila en
    /// usuarios con el identificador de Google ya atado.</para>
    ///
    /// <para>Cambiar el correo por el que se vaya a usar para probar.</para>
    /// </summary>
    private static async Task<int> SembrarInvitacionDePruebaAsync(
        MantIADbContext db, CancellationToken ct)
    {
        if (await db.Invitaciones.IgnoreQueryFilters().AnyAsync(i => i.Id == InvitacionPrueba, ct))
            return 0;

        db.Invitaciones.Add(new InvitacionUsuario
        {
            Id = InvitacionPrueba,
            Email = "exequielmatias32@gmail.com",
            Nombre = "Exequiel",
            Apellido = "Robledo",
            Rol = RolSistema.Supervisor,
            NivelPermisoId = NivelSr,
            Estado = EstadoInvitacion.Pendiente,

            // Larga a proposito: es una siembra de desarrollo y no tiene sentido que caduque en
            // medio de las pruebas.
            FechaVencimiento = DateTimeOffset.UtcNow.AddYears(1),
            InvitadaPorUsuarioId = UsuarioAdminEmpresa,
        });

        await db.SaveChangesAsync(ct);
        return 1;
    }
}
