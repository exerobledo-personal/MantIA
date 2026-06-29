using MantIA.BE.Common;

namespace MantIA.BE.Entities;

// Maquina operativa, PRIVADA por tenant. EmpresaId viene de TenantEntity.
public class Maquina : TenantEntity
{
    public Guid? PlantaId { get; set; }            // FK a Planta (entidad aun no creada; nullable por ahora)
    public string Nombre { get; set; } = string.Empty;
    public string? NumeroSerie { get; set; }

    // FK al catalogo compartido (marca y modelo se obtienen de aca, no se duplican)
    public Guid CatalogoMaquinaId { get; set; }
    public CatalogoMaquina? CatalogoMaquina { get; set; }

    public string Estado { get; set; } = "operativa";
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaBaja { get; set; }
}