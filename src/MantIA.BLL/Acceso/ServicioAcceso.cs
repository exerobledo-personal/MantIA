using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.BLL.Auditoria;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Acceso;

public enum EstadoAcceso
{
    Autorizado,

    /// <summary>El proveedor de identidad no devolvió un sujeto. No hay nada que resolver.</summary>
    NoAutenticado,

    /// <summary>No hay usuario ni invitación vigente para esa identidad. El caso más común y el que más importa.</summary>
    SinInvitacion,

    /// <summary>Había invitación pero se pasó de fecha.</summary>
    InvitacionVencida,

    /// <summary>El correo no pertenece a ningún dominio habilitado de la empresa.</summary>
    DominioNoHabilitado,

    /// <summary>La empresa está dada de baja.</summary>
    EmpresaDeBaja
}

public record AccesoResuelto(
    EstadoAcceso Estado,
    Guid? EmpresaId = null,
    Guid? UsuarioId = null,
    string? Mensaje = null)
{
    public bool Autorizado => Estado == EstadoAcceso.Autorizado;
}

public interface IServicioAcceso
{
    /// <summary>
    /// Decide si una identidad puede entrar y, si puede, deja el contexto posicionado en su empresa.
    /// Es el <b>único</b> lugar donde se toma esa decisión.
    /// </summary>
    Task<AccesoResuelto> ResolverAsync(string? sub, string? email, CancellationToken ct = default);
}

/// <summary>
/// La puerta del sistema.
///
/// <para><b>Nadie entra sin una invitación nominal.</b> No hay registro público ni alta automática
/// por dominio: la única forma de existir en una empresa es que alguien con autoridad haya emitido
/// una invitación a un correo concreto. El Usuario 0 de cada cliente incluido — su invitación la
/// emite MantIA al dar de alta la empresa, en lugar del administrador.</para>
///
/// <para><b>Por qué la fila de usuario nace en el primer ingreso y no antes.</b> El acceso se
/// controla contra el identificador que asigna el proveedor de identidad, y ese identificador no se
/// conoce hasta que la persona entra por primera vez. Un administrador sabe el correo de su
/// empleado, no su <c>sub</c> de Google. Por eso se invita por correo y el identificador se ata
/// recién cuando por fin existe. Es también lo que hace que <c>usuarios</c> signifique "gente que
/// entró" y no "gente que quizás entre".</para>
///
/// <para><b>El dominio no abre nada.</b> Acota a quién se le puede emitir una invitación y se vuelve
/// a comprobar en cada ingreso, por si se le quitó a la empresa después de invitar. Tener el dominio
/// correcto y ninguna invitación deja a la persona afuera igual.</para>
///
/// <para><b>Todo intento fallido se registra.</b> Un rechazo dice mucho más que un ingreso: es lo
/// único que permite ver que alguien está probando. Va como <c>Sesion.Rechazo</c>, que el catálogo
/// clasifica como sensible.</para>
///
/// <para><b>Está en la capa de negocio a propósito.</b> Antes esta decisión estaba escrita dos veces
/// —una en el arranque de la aplicación web y otra en el resolvedor de tenant— y dos copias de una
/// regla de acceso siempre terminan divergiendo. La que se olvide de actualizarse es la que deja
/// entrar a quien no debe.</para>
/// </summary>
public class ServicioAcceso : IServicioAcceso
{
    /// <summary>Cuánto vive una invitación sin usarse.</summary>
    public static readonly TimeSpan VigenciaInvitacion = TimeSpan.FromDays(14);

    private readonly MantIADbContext _db;
    private readonly CurrentTenant _tenant;
    private readonly IBitacora _bitacora;

    public ServicioAcceso(MantIADbContext db, ICurrentTenant tenant, IBitacora bitacora)
    {
        _db = db;
        _tenant = (CurrentTenant)tenant;
        _bitacora = bitacora;
    }

    public async Task<AccesoResuelto> ResolverAsync(
        string? sub, string? email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sub))
            return new AccesoResuelto(EstadoAcceso.NoAutenticado);

        var correo = email?.Trim().ToLowerInvariant();

        // Búsqueda por identidad ANTES de conocer el tenant: es la única excepción legítima al
        // filtro de empresa, porque justamente estamos averiguando cuál es. Se ignora solo ese
        // filtro, por nombre: con IgnoreQueryFilters() a secas también se apagaría el de baja
        // lógica y un usuario dado de baja podría volver a entrar.
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
            .FirstOrDefaultAsync(u => u.Auth0UserId == sub, ct);

        return usuario is not null
            ? await EntrarAsync(usuario, correo, ct)
            : await AceptarInvitacionAsync(sub, correo, ct);
    }

    // ------------------------------------------------------------------ ingreso de alguien ya dado de alta

    private async Task<AccesoResuelto> EntrarAsync(
        Usuario usuario, string? correo, CancellationToken ct)
    {
        var empresa = await _db.Empresas
            .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
            .FirstOrDefaultAsync(e => e.Id == usuario.EmpresaId, ct);

        // "No existe" y "está dada de baja" son dos problemas distintos y la persona merece saber
        // cuál de los dos le tocó: uno se resuelve con soporte, el otro con administración.
        if (empresa is null)
            return await RechazarAsync(
                EstadoAcceso.EmpresaDeBaja, usuario.EmpresaId, correo,
                "La empresa del usuario no existe.", ct);

        if (empresa.FechaBaja is not null)
            return await RechazarAsync(
                EstadoAcceso.EmpresaDeBaja, empresa.Id, correo,
                $"La cuenta de {empresa.RazonSocial} está dada de baja. Contactate con MantIA.", ct);

        // Se vuelve a comprobar el dominio en cada ingreso y no solo al invitar: una empresa puede
        // haber dado de baja un dominio después, y las personas de ese dominio tienen que dejar de
        // entrar sin que nadie las revoque una por una.
        if (!await DominioHabilitadoAsync(empresa.Id, correo, ct))
            return await RechazarAsync(
                EstadoAcceso.DominioNoHabilitado, empresa.Id, correo,
                $"El correo {correo} no pertenece a ningún dominio habilitado de {empresa.RazonSocial}.",
                ct);

        Posicionar(empresa.Id, usuario.Id);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Sesion",
                Accion: "Ingreso",
                RecursoId: usuario.Id,
                UsuarioEmail: correo,
                RolAlMomento: usuario.Rol.ToString(),
                EmpresaAfectadaId: empresa.Id),
            ct);

        return new AccesoResuelto(EstadoAcceso.Autorizado, empresa.Id, usuario.Id);
    }

    // ------------------------------------------------------------------ primer ingreso

    private async Task<AccesoResuelto> AceptarInvitacionAsync(
        string sub, string? correo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return await RechazarAsync(
                EstadoAcceso.SinInvitacion, null, correo,
                "El proveedor de identidad no informó un correo, así que no hay con qué buscar la invitación.",
                ct);

        var invitacion = await _db.Invitaciones
            .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
            .FirstOrDefaultAsync(
                i => i.Email == correo && i.Estado == EstadoInvitacion.Pendiente, ct);

        if (invitacion is null)
            return await RechazarAsync(
                EstadoAcceso.SinInvitacion, null, correo,
                "No hay ninguna invitación para este correo. Pedile a la persona que administra tu empresa que te dé de alta.",
                ct);

        var ahora = DateTimeOffset.UtcNow;

        // La invitación vencida NO se marca como revocada acá. Revocar es un acto de alguien; vencer
        // es el paso del tiempo, y confundirlos haría que la bitácora dijera que un administrador
        // hizo algo que no hizo.
        if (!invitacion.Vigente(ahora))
            return await RechazarAsync(
                EstadoAcceso.InvitacionVencida, invitacion.EmpresaId, correo,
                $"La invitación venció el {invitacion.FechaVencimiento:dd/MM/yyyy}. Hay que emitir una nueva.",
                ct);

        var empresa = await _db.Empresas
            .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
            .FirstOrDefaultAsync(e => e.Id == invitacion.EmpresaId, ct);

        if (empresa is null || empresa.FechaBaja is not null)
            return await RechazarAsync(
                EstadoAcceso.EmpresaDeBaja, invitacion.EmpresaId, correo,
                "La empresa que te invitó está dada de baja.", ct);

        if (!await DominioHabilitadoAsync(empresa.Id, correo, ct))
            return await RechazarAsync(
                EstadoAcceso.DominioNoHabilitado, empresa.Id, correo,
                $"El correo {correo} ya no pertenece a un dominio habilitado de {empresa.RazonSocial}.",
                ct);

        // Desde acá se escribe, así que el contexto tiene que estar posicionado: el guardado
        // completa la empresa de toda alta con el tenant activo y sin él lanza.
        Posicionar(empresa.Id, null);

        var usuario = new Usuario
        {
            Auth0UserId = sub,
            Email = correo,
            Nombre = invitacion.Nombre,
            Apellido = invitacion.Apellido,
            Rol = invitacion.Rol,
            NivelPermisoId = invitacion.NivelPermisoId,
            Estado = EstadoGenerico.Activo,
            UltimoAcceso = ahora,
        };

        _db.Usuarios.Add(usuario);

        invitacion.Estado = EstadoInvitacion.Aceptada;
        invitacion.UsuarioId = usuario.Id;
        invitacion.FechaAceptacion = ahora;

        // Un solo guardado: o queda el usuario creado y la invitación consumida, o no queda nada.
        // Partido en dos, un corte en el medio dejaría una invitación abierta con usuario ya creado,
        // que es una habilitación duplicada.
        await _db.SaveChangesAsync(ct);

        Posicionar(empresa.Id, usuario.Id);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Usuarios",
                Accion: "Alta",
                RecursoId: usuario.Id,
                Descripcion:
                    $"Primer ingreso: se acepto la invitacion y se creo el usuario con rol {usuario.Rol}.",
                UsuarioEmail: correo,
                RolAlMomento: usuario.Rol.ToString(),
                EmpresaAfectadaId: empresa.Id),
            ct);

        return new AccesoResuelto(EstadoAcceso.Autorizado, empresa.Id, usuario.Id);
    }

    // ------------------------------------------------------------------ mecanica

    /// <summary>
    /// El correo pertenece a alguno de los dominios de la empresa. Sin dominios cargados no entra
    /// nadie: es la respuesta segura para una empresa a medio configurar.
    /// </summary>
    private async Task<bool> DominioHabilitadoAsync(Guid empresaId, string? correo, CancellationToken ct)
    {
        var dominio = DominioEmpresa.De(correo);
        if (dominio is null) return false;

        return await _db.DominiosEmpresa
            .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
            .AnyAsync(d => d.EmpresaId == empresaId && d.Dominio == dominio, ct);
    }

    private void Posicionar(Guid? empresaId, Guid? usuarioId)
    {
        _tenant.EmpresaId = empresaId;
        _tenant.UsuarioId = usuarioId;
    }

    /// <summary>
    /// Deja constancia del rechazo y devuelve el resultado. Va como acción fallida de
    /// <c>Sesion.Rechazo</c>, que el catálogo clasifica como sensible y la agravante de fallo sube
    /// un escalón más.
    /// </summary>
    private async Task<AccesoResuelto> RechazarAsync(
        EstadoAcceso estado, Guid? empresaId, string? correo, string mensaje, CancellationToken ct)
    {
        // El contexto queda sin posicionar: un rechazo no debe dejar el tenant abierto, ni siquiera
        // durante lo que dura el resto del pedido.
        Posicionar(null, null);

        try
        {
            await _bitacora.RegistrarAsync(
                new AccionAuditada(
                    Recurso: "Sesion",
                    Accion: "Rechazo",
                    Descripcion: mensaje,
                    Exitoso: false,
                    MotivoFallo: estado.ToString(),
                    UsuarioEmail: correo,
                    EmpresaAfectadaId: empresaId),
                ct);
        }
        catch (Exception)
        {
            // Si la bitácora no está disponible, el rechazo sigue siendo un rechazo. Dejar entrar a
            // alguien porque no se pudo anotar que no debía entrar sería exactamente al revés.
        }

        return new AccesoResuelto(estado, empresaId, null, mensaje);
    }
}
