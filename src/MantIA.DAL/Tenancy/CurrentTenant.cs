namespace MantIA.DAL.Tenancy;

public interface ICurrentTenant
{
    Guid? EmpresaId { get; }
}

public class CurrentTenant : ICurrentTenant
{
    public Guid? EmpresaId { get; set; }
}