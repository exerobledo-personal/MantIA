using MantIA.BE.Common;

namespace MantIA.BE.Auditoria;

/// <summary>A que bitacora pertenece un evento. Son dos flujos con lectores distintos.</summary>
public enum AlcanceBitacora
{
    /// <summary>Visible para el administrador de la empresa. Solo eventos de su tenant.</summary>
    Empresa,
    /// <summary>Visible unicamente para MantIA. Altas de empresas, cambios sobre cuentas
    /// administradoras, y uso del bypass de superadministrador.</summary>
    Plataforma
}

public enum TipoEvento
{
    /// <summary>Cambio de estado o de datos sobre una entidad del negocio.</summary>
    Transaccion,
    /// <summary>Inicio de sesion, alta de usuario, cambio de permisos.</summary>
    Auditoria,
    /// <summary>Error del sistema.</summary>
    Excepcion
}

public enum NivelLog { Debug, Info, Warning, Error }

/// <summary>
/// Entrada de bitacora. Se persiste en MongoDB, no en PostgreSQL: volumen alto, escritura
/// secuencial y esquema variable.
///
/// <para><b>Es inmutable y encadenada.</b> Cada entrada incluye el hash de la anterior, de
/// modo que la bitacora forma una cadena: alterar o borrar un evento del medio invalida
/// todos los hashes posteriores y la manipulacion queda en evidencia.</para>
///
/// <para>Para una bitacora de auditoria, la integridad importa mas que la confidencialidad:
/// de nada sirve cifrar un registro que alguien puede reemplazar. Se hace lo primero con la
/// cadena de hashes, y lo segundo con el cifrado en reposo del motor.</para>
/// </summary>
public class EventoBitacora
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public AlcanceBitacora Alcance { get; set; }
    public TipoEvento Tipo { get; set; }
    public NivelLog Nivel { get; set; } = NivelLog.Info;

    /// <summary>Empresa afectada. Nulo en eventos de plataforma que no aplican a un tenant.</summary>
    public Guid? EmpresaId { get; set; }

    public Guid? UsuarioId { get; set; }
    public string? UsuarioEmail { get; set; }
    public string? RolAlMomento { get; set; }

    /// <summary>Clave del recurso del catalogo de permisos y accion ejecutada.</summary>
    public string Recurso { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public Guid? RecursoId { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>
    /// Estado de la entidad antes y despues, serializado. Es lo que hace posible el rollback:
    /// sin el estado anterior no hay a donde volver.
    /// <para>Los campos clasificados como sensibles se guardan enmascarados
    /// (ver <see cref="DatosSensibles"/>).</para>
    /// </summary>
    public string? EstadoAnterior { get; set; }
    public string? EstadoPosterior { get; set; }

    public bool Exitoso { get; set; } = true;
    public string? MotivoFallo { get; set; }

    /// <summary>Marca cuando la accion se ejecuto usando el bypass de superadministrador.</summary>
    public bool UsoBypass { get; set; }

    public string? DireccionIp { get; set; }
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    // ---- Cadena de integridad ----
    /// <summary>Hash del evento inmediatamente anterior de la misma cadena.</summary>
    public string? HashAnterior { get; set; }
    /// <summary>Hash de este evento, calculado sobre su contenido mas <see cref="HashAnterior"/>.</summary>
    public string Hash { get; set; } = string.Empty;
    /// <summary>Posicion en la cadena. Un salto de numero delata una eliminacion.</summary>
    public long Secuencia { get; set; }
}
