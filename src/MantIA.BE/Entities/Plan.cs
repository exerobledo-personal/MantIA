using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Plan de suscripcion. Compartido: lo define MantIA, no el cliente.</summary>
public class Plan : CatalogEntity, IBajaLogica
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // Topes por defecto. Al dar de alta una empresa se copian a sus propios campos, y a partir de
    // ahi manda el de la empresa: es lo que permite negociar una excepcion comercial sin inventar un
    // plan nuevo para cada cliente. Todos son TOTALES por empresa, no por planta.
    public int MaxMaquinas { get; set; }
    public int MaxUsuarios { get; set; }
    public int MaxPlantas { get; set; }

    /// <summary>
    /// Tope de ordenes de trabajo. Nulo es sin limite, que es lo normal en un plan pago: solo la
    /// prueba lo acota. Cero seria "ninguna", que es un plan que no sirve para nada.
    /// </summary>
    public int? MaxOrdenesTrabajo { get; set; }

    /// <summary>
    /// Cuantos dias dura la vigencia que se asigna al dar de alta con este plan. En la prueba es el
    /// largo de la prueba; en un plan pago, el periodo hasta la renovacion.
    /// </summary>
    public int DiasVigencia { get; set; } = 365;

    /// <summary>Es el plan de prueba. No se cobra y no se renueva solo.</summary>
    public bool EsPrueba { get; set; }

    public decimal PrecioMensual { get; set; }
    public string Moneda { get; set; } = "ARS";

    public DateTimeOffset? FechaBaja { get; set; }
}
