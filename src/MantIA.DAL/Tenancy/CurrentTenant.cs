namespace MantIA.DAL.Tenancy;

/// <summary>
/// Quien esta operando y en nombre de que empresa. Se resuelve una vez por request o por
/// circuito de Blazor, y el contexto de datos lo lee para aislar el tenant y para sellar
/// los campos de auditoria.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// Empresa del usuario autenticado. Nulo mientras no se resolvio: en ese estado el
    /// contexto no permite escribir y las consultas no devuelven nada.
    /// </summary>
    Guid? EmpresaId { get; }

    /// <summary>
    /// Usuario autenticado, para sellar quien creo o modifico cada registro. Nulo en procesos
    /// desatendidos (ingesta del catalogo, corridas del modelo), y eso es informacion util:
    /// distingue un cambio hecho por una persona de uno hecho por el sistema.
    /// </summary>
    Guid? UsuarioId { get; }
}

public class CurrentTenant : ICurrentTenant
{
    public Guid? EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
}
