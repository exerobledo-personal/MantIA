using MantIA.BE.Auditoria;
using MantIA.DAL.Bitacora;
using MantIA.DAL.Seguridad;
using MantIA.DAL.Tenancy;
using Microsoft.Extensions.Options;

namespace MantIA.BLL.Auditoria;

/// <summary>Datos que aporta quien ejecuta la accion. Todo lo demas lo completa la bitacora.</summary>
public record AccionAuditada(
    string Recurso,
    string Accion,
    Guid? RecursoId = null,
    string? Descripcion = null,
    string? Motivo = null,
    string? EstadoAnterior = null,
    string? EstadoPosterior = null,
    bool Exitoso = true,
    string? MotivoFallo = null,
    bool UsoBypass = false,
    bool ObjetoEstabaVivo = false,
    Guid? CorrelacionId = null,
    string? DireccionIp = null,
    /// <summary>Correo de quien ejecuta. Se guarda enmascarado; lo usan los eventos de sesion,
    /// donde todavia no hay un usuario resuelto al que referirse por identificador.</summary>
    string? UsuarioEmail = null,
    /// <summary>Rol al momento de la accion. Importa porque el rol puede cambiar despues.</summary>
    string? RolAlMomento = null,
    /// <summary>Empresa afectada, para eventos de plataforma sobre un tenant que no es el propio.</summary>
    Guid? EmpresaAfectadaId = null);

public interface IBitacora
{
    /// <summary>Registra una accion. Devuelve el evento sellado tal como quedo almacenado.</summary>
    Task<EventoBitacora> RegistrarAsync(AccionAuditada accion, CancellationToken ct = default);

    /// <summary>
    /// Abre una correlacion para agrupar todos los eventos de una misma operacion de negocio.
    /// Se usa asi: <c>using var op = bitacora.Operacion(out var id);</c> y los eventos que se
    /// registren dentro comparten identificador.
    /// </summary>
    IDisposable Operacion(out Guid correlacionId);
}

/// <summary>
/// Punto unico de escritura de la bitacora.
///
/// <para><b>Nada escribe eventos por su cuenta.</b> Si cada modulo armara su propio evento, la
/// severidad, el enmascarado y el sello dependerian de que cada uno se acuerde de aplicarlos, y
/// alcanza con que uno se olvide para que la cadena deje de valer.</para>
///
/// <para>El orden de las operaciones no es arbitrario: primero se enmascara, despues se cifra y
/// recien al final se sella <b>lo que efectivamente se guarda</b>. Si se sellara el texto en claro,
/// verificar la cadena obligaria a descifrar todo, y el dia que se rote una llave la verificacion
/// dejaria de ser una operacion barata.</para>
/// </summary>
public class Bitacora : IBitacora
{
    private readonly IRepositorioBitacora _repositorio;
    private readonly IProtectorDatos _protector;
    private readonly ICurrentTenant _tenant;
    private readonly OpcionesAuditoria _opciones;

    private readonly AsyncLocal<Guid?> _correlacion = new();

    public Bitacora(
        IRepositorioBitacora repositorio,
        IProtectorDatos protector,
        ICurrentTenant tenant,
        IOptions<OpcionesAuditoria> opciones)
    {
        _repositorio = repositorio;
        _protector = protector;
        _tenant = tenant;
        _opciones = opciones.Value;
    }

    public IDisposable Operacion(out Guid correlacionId)
    {
        var previa = _correlacion.Value;
        correlacionId = Guid.NewGuid();
        _correlacion.Value = correlacionId;
        return new Tramo(this, previa);
    }

    public Task<EventoBitacora> RegistrarAsync(AccionAuditada accion, CancellationToken ct = default)
    {
        var version = _protector.VersionSello;
        var alcance = CatalogoEventos.AlcanceDe(accion.Recurso);
        var empresaId = accion.EmpresaAfectadaId ?? _tenant.EmpresaId;

        var evento = new EventoBitacora
        {
            Alcance = alcance,
            Cadena = EventoBitacora.CadenaDe(alcance, empresaId),
            Tipo = TipoDe(accion),
            EmpresaId = empresaId,
            UsuarioId = _tenant.UsuarioId,
            Recurso = accion.Recurso,
            Accion = accion.Accion,
            RecursoId = accion.RecursoId,
            Descripcion = accion.Descripcion,
            Motivo = accion.Motivo,
            Exitoso = accion.Exitoso,
            MotivoFallo = accion.MotivoFallo,
            UsoBypass = accion.UsoBypass,
            DireccionIp = accion.DireccionIp,
            CorrelacionId = accion.CorrelacionId ?? _correlacion.Value,
            VersionLlave = version,
            RolAlMomento = accion.RolAlMomento,
        };

        evento.Severidad = CatalogoEventos.SeveridadDe(
            accion.Recurso,
            accion.Accion,
            accion.Exitoso,
            accion.UsoBypass,
            accion.ObjetoEstabaVivo,
            sinMotivo: string.IsNullOrWhiteSpace(accion.Motivo));

        evento.Nivel = accion.Exitoso ? NivelLog.Info : NivelLog.Warning;

        // 1. Enmascarar. El correo nunca entra en claro: la bitacora se exporta y se comparte
        //    mucho mas que la base operativa, y es donde un dato personal termina filtrandose.
        evento.UsuarioEmail = accion.UsuarioEmail is null
            ? null
            : DatosSensibles.Enmascarar(accion.UsuarioEmail);

        // 2. Cifrar, campo por campo y no el documento entero. Se cifra lo que puede contener
        //    texto libre o valores del dominio; lo que sirve para filtrar y ordenar queda en claro,
        //    porque si no la pantalla de bitacora tendria que traerse la coleccion completa a
        //    memoria para mostrar "lo critico de este mes".
        evento.Descripcion = Proteger(evento.Descripcion, nameof(evento.Descripcion));
        evento.Motivo = Proteger(evento.Motivo, nameof(evento.Motivo));
        evento.MotivoFallo = Proteger(evento.MotivoFallo, nameof(evento.MotivoFallo));
        evento.EstadoAnterior = Proteger(accion.EstadoAnterior, nameof(evento.EstadoAnterior));
        evento.EstadoPosterior = Proteger(accion.EstadoPosterior, nameof(evento.EstadoPosterior));

        // 3. Sellar. El sello se calcula dentro del repositorio porque depende del numero de
        //    secuencia, y ese numero solo se conoce al insertar: si dos operaciones del mismo
        //    tenant compiten, la que pierde tiene que volver a sellarse con el eslabon correcto.
        return _repositorio.AgregarAsync(
            evento,
            (e, hashAnterior) => _protector.Sellar(
                CanonicalizacionEvento.Canonizar(e, hashAnterior), version),
            ct);
    }

    // Cifra con la llave de CIFRADO, distinta de la de sellado. El evento guarda la version de
    // sellado en VersionLlave; la de cifrado no hace falta guardarla porque el protector prueba
    // todas las que tenga configuradas.
    private string? Proteger(string? valor, string campo) =>
        string.IsNullOrEmpty(valor) || !_opciones.CifrarEstados
            ? valor
            : _protector.Cifrar(valor, $"{nameof(EventoBitacora)}.{campo}");

    // NO se cifran: Alcance, Tipo, Nivel, Severidad, Recurso, Accion, EmpresaId, UsuarioId, Fecha.
    // Son los ejes por los que se consulta la bitacora. Cifrarlos convierte cada filtro en un
    // recorrido completo de la coleccion, y la pantalla de auditoria deja de ser usable justo
    // cuando mas se la necesita.

    private static TipoEvento TipoDe(AccionAuditada accion) =>
        !accion.Exitoso ? TipoEvento.Excepcion
        : accion.Recurso is "Permisos" or "Usuarios" or "Niveles" or "Sesion" or "Rollback"
            ? TipoEvento.Auditoria
            : TipoEvento.Transaccion;

    private sealed class Tramo(Bitacora duenio, Guid? previa) : IDisposable
    {
        public void Dispose() => duenio._correlacion.Value = previa;
    }
}
