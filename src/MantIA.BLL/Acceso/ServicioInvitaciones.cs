using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.BLL.Auditoria;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Acceso;

public enum RechazoInvitacion
{
    Ninguno,
    CorreoInvalido,
    DominioNoHabilitado,
    YaEsUsuario,
    YaInvitado,
    NivelInexistente,
    RolNoPermitido
}

public record ResultadoInvitacion(
    InvitacionUsuario? Invitacion, RechazoInvitacion Rechazo, string Detalle)
{
    public bool Exitoso => Invitacion is not null;

    public static ResultadoInvitacion Ok(InvitacionUsuario i) =>
        new(i, RechazoInvitacion.Ninguno, string.Empty);
}

public interface IServicioInvitaciones
{
    /// <summary>Emite una invitación dentro de la empresa del contexto actual.</summary>
    Task<ResultadoInvitacion> InvitarAsync(
        string email, string nombre, string apellido, RolSistema rol,
        Guid? nivelPermisoId, CancellationToken ct = default);

    Task<bool> RevocarAsync(Guid invitacionId, string motivo, CancellationToken ct = default);

    Task<IReadOnlyList<InvitacionUsuario>> PendientesAsync(CancellationToken ct = default);
}

/// <summary>
/// Emisión y revocación de invitaciones dentro de una empresa.
///
/// <para><b>Cuatro comprobaciones antes de emitir, y ninguna es de formulario.</b> Que el correo
/// pertenezca a un dominio habilitado; que esa persona no sea ya usuario de alguna empresa; que no
/// tenga otra invitación pendiente; y que el rol pedido se pueda otorgar desde una empresa. Las dos
/// del medio existen porque una identidad pertenece a una sola empresa: sin ellas, dos empresas
/// podrían invitar al mismo correo y cuál gana dependería de quién entre primero.</para>
///
/// <para><b>Revocar no borra.</b> La invitación queda con su motivo. Quién habilitó a quién, cuándo,
/// y quién lo dio de baja después es exactamente lo que hay que poder reconstruir si algo pasa.</para>
/// </summary>
public class ServicioInvitaciones : IServicioInvitaciones
{
    private readonly MantIADbContext _db;
    private readonly ICurrentTenant _tenant;
    private readonly IBitacora _bitacora;

    public ServicioInvitaciones(MantIADbContext db, ICurrentTenant tenant, IBitacora bitacora)
    {
        _db = db;
        _tenant = tenant;
        _bitacora = bitacora;
    }

    public async Task<ResultadoInvitacion> InvitarAsync(
        string email, string nombre, string apellido, RolSistema rol,
        Guid? nivelPermisoId, CancellationToken ct = default)
    {
        if (_tenant.EmpresaId is not { } empresaId)
            throw new InvalidOperationException("No hay empresa en el contexto: no se puede invitar.");

        var correo = email.Trim().ToLowerInvariant();
        var dominio = DominioEmpresa.De(correo);

        if (dominio is null)
            return new ResultadoInvitacion(
                null, RechazoInvitacion.CorreoInvalido, $"'{email}' no es una dirección de correo.");

        // El superadministrador de plataforma no se otorga desde una empresa. Es la frontera entre
        // administrar un cliente y administrar el producto, y tiene que ser estructural: si se
        // pudiera invitar, cualquier administrador de empresa se promovería a sí mismo.
        if (rol == RolSistema.SuperAdminMantIA)
            return new ResultadoInvitacion(
                null, RechazoInvitacion.RolNoPermitido,
                "El rol de superadministrador de MantIA no se otorga desde una empresa.");

        if (!await _db.DominiosEmpresa.AnyAsync(d => d.Dominio == dominio, ct))
            return new ResultadoInvitacion(
                null, RechazoInvitacion.DominioNoHabilitado,
                $"El dominio '{dominio}' no está habilitado en esta empresa.");

        // Sin filtro de empresa a propósito en las dos consultas que siguen: lo que se comprueba es
        // que esa identidad no exista en NINGUNA empresa, y con el filtro puesto la respuesta sería
        // siempre que no, dejando pasar el conflicto hasta el índice único.
        if (await _db.Usuarios
                .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
                .AnyAsync(u => u.Email == correo, ct))
            return new ResultadoInvitacion(
                null, RechazoInvitacion.YaEsUsuario,
                $"{correo} ya es usuario del sistema. Una identidad pertenece a una sola empresa.");

        if (await _db.Invitaciones
                .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
                .AnyAsync(i => i.Email == correo && i.Estado == EstadoInvitacion.Pendiente, ct))
            return new ResultadoInvitacion(
                null, RechazoInvitacion.YaInvitado,
                $"Ya hay una invitación pendiente para {correo}.");

        if (nivelPermisoId is { } nivel && !await _db.NivelesPermiso.AnyAsync(n => n.Id == nivel, ct))
            return new ResultadoInvitacion(
                null, RechazoInvitacion.NivelInexistente, "El nivel de permiso no existe en esta empresa.");

        var invitacion = new InvitacionUsuario
        {
            Email = correo,
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Rol = rol,
            NivelPermisoId = nivelPermisoId,
            Estado = EstadoInvitacion.Pendiente,
            FechaVencimiento = DateTimeOffset.UtcNow.Add(ServicioAcceso.VigenciaInvitacion),
            InvitadaPorUsuarioId = _tenant.UsuarioId,
        };

        _db.Invitaciones.Add(invitacion);
        await _db.SaveChangesAsync(ct);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Usuarios",
                Accion: "Alta",
                RecursoId: invitacion.Id,
                Descripcion: $"Se invito a {correo} con rol {rol}.",
                EstadoPosterior: rol.ToString(),
                EmpresaAfectadaId: empresaId),
            ct);

        return ResultadoInvitacion.Ok(invitacion);
    }

    public async Task<bool> RevocarAsync(
        Guid invitacionId, string motivo, CancellationToken ct = default)
    {
        var invitacion = await _db.Invitaciones
            .FirstOrDefaultAsync(i => i.Id == invitacionId, ct);

        // Una invitación ya aceptada no se revoca: lo que hay que dar de baja en ese caso es el
        // usuario, que es otra operación con otras consecuencias.
        if (invitacion is null || invitacion.Estado != EstadoInvitacion.Pendiente) return false;

        invitacion.Estado = EstadoInvitacion.Revocada;
        invitacion.MotivoRevocacion = motivo;

        await _db.SaveChangesAsync(ct);

        await _bitacora.RegistrarAsync(
            new AccionAuditada(
                Recurso: "Usuarios",
                Accion: "Baja",
                RecursoId: invitacion.Id,
                Descripcion: "Se revoco una invitacion pendiente.",
                Motivo: motivo,
                ObjetoEstabaVivo: true),
            ct);

        return true;
    }

    public async Task<IReadOnlyList<InvitacionUsuario>> PendientesAsync(CancellationToken ct = default) =>
        await _db.Invitaciones
            .Where(i => i.Estado == EstadoInvitacion.Pendiente)
            .OrderBy(i => i.FechaVencimiento)
            .ToListAsync(ct);
}
