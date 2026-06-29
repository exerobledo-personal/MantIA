namespace MantIA.BE.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class TenantEntity : BaseEntity
{
    public Guid EmpresaId { get; set; }
}

public abstract class CatalogEntity : BaseEntity
{
}