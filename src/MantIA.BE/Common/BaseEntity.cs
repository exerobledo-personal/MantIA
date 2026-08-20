namespace MantIA.BE.Common;

/// <summary>
/// Raiz de toda entidad persistida. Incluye los campos de auditoria que exige la
/// bitacora de transacciones: quien creo el registro, quien lo modifico por ultima vez
/// y cuando. Las fechas son <see cref="DateTimeOffset"/> en UTC; la conversion a hora
/// local es responsabilidad de la interfaz.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreadoPorUsuarioId { get; set; }

    public DateTimeOffset? FechaModificacion { get; set; }
    public Guid? ModificadoPorUsuarioId { get; set; }
}

/// <summary>
/// Entidad PRIVADA de una empresa. El <c>DbContext</c> aplica un filtro global sobre
/// <see cref="EmpresaId"/>, de modo que una consulta mal escrita en la capa de negocio
/// no puede filtrar datos de otro cliente.
/// <para>
/// Si dudas entre esta base y <see cref="CatalogEntity"/>, es esta.
/// </para>
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid EmpresaId { get; set; }
}

/// <summary>
/// Entidad COMPARTIDA entre todas las empresas de la plataforma. No lleva filtro de
/// tenant a proposito: es el activo comun que constituye el efecto de red del catalogo.
/// Nunca debe contener datos operativos de un cliente.
/// </summary>
public abstract class CatalogEntity : BaseEntity
{
}

/// <summary>
/// Marca una entidad que necesita control de concurrencia optimista. En PostgreSQL,
/// <see cref="Version"/> se mapea contra la columna de sistema <c>xmin</c>: si dos
/// transacciones intentan escribir la misma fila, la segunda falla en lugar de pisar
/// silenciosamente a la primera.
/// </summary>
public interface IConcurrencia
{
    uint Version { get; set; }
}

/// <summary>
/// Marca una entidad que se da de baja logicamente. Nunca se borra fisicamente: el
/// historial operativo tiene que sobrevivir a la baja del registro que lo origino.
/// </summary>
public interface IBajaLogica
{
    DateTimeOffset? FechaBaja { get; set; }
}
