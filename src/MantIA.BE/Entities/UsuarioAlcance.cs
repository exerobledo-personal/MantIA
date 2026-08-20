using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Alcance de datos de un usuario: que plantas puede ver. Es la tercera capa de
/// autorizacion, independiente del rol y del nivel. Un Supervisor Sr con alcance sobre
/// una sola planta no ve las maquinas de las otras, aunque su rol se lo permita.
/// <para>Sin filas para un usuario significa "todas las plantas de su empresa".</para>
/// </summary>
public class UsuarioAlcance : TenantEntity
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public Guid PlantaId { get; set; }
    public Planta? Planta { get; set; }
}
