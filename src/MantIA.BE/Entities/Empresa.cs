using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Empresa cliente. Cada una es un tenant con su espacio de datos aislado.</summary>
public class Empresa : BaseEntity, IBajaLogica
{
    public string RazonSocial { get; set; } = string.Empty;
    /// <summary>Identificador de organizacion en Auth0. Distinto de la clave primaria interna.</summary>
    public string TenantId { get; set; } = string.Empty;

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    // ------------------------------------------------------------------ cupos
    //
    // Los cuatro se copian del plan al dar de alta y a partir de ahi MANDAN ELLOS. Es lo que permite
    // acordar una excepcion con un cliente sin crear un plan a medida, y deja un solo numero que
    // mirar cuando algo se bloquea. Nulo significa sin limite; cero significa ninguno, que no es lo
    // mismo y por eso no se usa un entero con cero magico.
    //
    // El cupo BLOQUEA EL ALTA, nunca borra. Una empresa que quede por encima de su techo -porque le
    // bajaron el plan o porque se le vencio la prueba- conserva todo y solo deja de poder crear mas.

    public int? MaxMaquinasHabilitadas { get; set; }
    public int? MaxUsuariosHabilitados { get; set; }
    public int? MaxPlantasHabilitadas { get; set; }
    public int? MaxOrdenesTrabajo { get; set; }

    // ------------------------------------------------------------------ vigencia
    //
    // La prueba y el periodo pago son LA MISMA COSA y por eso comparten campos. Para una cuenta de
    // prueba, el fin es cuando se termina la prueba; para un cliente activo, cuando vence lo pagado.
    // En los dos casos, al llegar la fecha la empresa pasa a solo lectura y no pierde un dato.
    //
    // Eso hace que convertir una prueba en cliente no sea una migracion ni un alta nueva: es correr
    // una fecha y subir unos numeros, sobre el mismo tenant y los mismos datos que ya cargo.

    public DateTimeOffset? InicioVigencia { get; set; }

    /// <summary>Fin de la vigencia. Nulo es sin vencimiento, y solo deberia serlo MantIA.</summary>
    public DateTimeOffset? FinVigencia { get; set; }

    /// <summary>Vencida: la fecha pasó. La empresa entra y consulta, pero no puede cargar nada.</summary>
    public bool VigenciaVencida(DateTimeOffset ahora) =>
        FinVigencia is { } fin && fin <= ahora;

    /// <summary>Días que faltan para el vencimiento. Nulo si no vence.</summary>
    public int? DiasParaVencer(DateTimeOffset ahora) =>
        FinVigencia is { } fin ? (int)Math.Ceiling((fin - ahora).TotalDays) : null;

    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTimeOffset? FechaBaja { get; set; }

    /// <summary>
    /// Dominios de correo habilitados. Acotan a quien se puede invitar; no dan acceso por si solos.
    /// El principal sale del correo del Usuario 0 al dar de alta el cliente.
    /// </summary>
    public ICollection<DominioEmpresa> Dominios { get; set; } = [];

    public ICollection<Planta> Plantas { get; set; } = [];
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
