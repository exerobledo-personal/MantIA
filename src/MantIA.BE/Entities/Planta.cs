using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Instalacion fisica de una empresa. Define el alcance de datos de los usuarios
/// operativos: un usuario ve las maquinas de las plantas que tiene asignadas.
/// </summary>
public class Planta : TenantEntity, IBajaLogica
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;

    // Se usan para el mapa consolidado del panel de empresa.
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }

    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTimeOffset? FechaBaja { get; set; }

    public ICollection<Maquina> Maquinas { get; set; } = [];
}
