using MantIA.BE.Common;

namespace MantIA.BE.Entities;


public class CatalogoMaquina : CatalogEntity
{
	public string Marca { get; set; } = string.Empty;
	public string Modelo { get; set; } = string.Empty;
	public string? FallasComunes { get; set; }
	public string? RepuestosSugeridos { get; set; }
	public string? IntervalosMantenimiento { get; set; }
	public string EstadoEnriquecimiento { get; set; } = "pendiente";
	public DateTime? FechaUltimoEnriquecimiento { get; set; }
}