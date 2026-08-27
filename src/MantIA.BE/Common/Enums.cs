namespace MantIA.BE.Common;

// Los estados se modelan como enums y no como texto libre: un error de tipeo en un
// "activo" tiene que romper la compilacion, no llegar a produccion. La DAL los persiste
// como string para que la base siga siendo legible a ojo.

/// <summary>
/// Situacion operativa de la empresa. Responde "puede trabajar", no "por que".
/// El motivo vive en <see cref="EstadoCobranza"/>, y separarlos es lo que permite distinguir en la
/// base y en pantalla un cliente que se fue al dia de uno al que se le corto por falta de pago.
/// </summary>
public enum EstadoEmpresa
{
    Activa,

    /// <summary>Entra y consulta, no puede cargar nada. El motivo lo dice la cobranza.</summary>
    Suspendida,

    /// <summary>Se dio de baja. Con cobranza al dia es una baja voluntaria y limpia.</summary>
    Baja
}

/// <summary>
/// Escalera de mora. Es un campo propio y no un calculo al vuelo por dos razones: se ve en la
/// pantalla del cliente y en la del panel sin recalcular nada, y deja constancia de en que escalon
/// estaba la cuenta cuando se le mando cada aviso.
/// <para>
/// Hasta que exista modelo de facturacion, los escalones se cuentan desde el vencimiento de la
/// vigencia. Cuando haya cobranza real, cambia la fuente del calculo y nada mas.
/// </para>
/// </summary>
public enum EstadoCobranza
{
    AlDia,

    /// <summary>Primer aviso. La empresa sigue trabajando normal.</summary>
    Mora30,

    /// <summary>Segundo aviso. Sigue trabajando: cortar a los sesenta dias no da tiempo a nadie.</summary>
    Mora60,

    /// <summary>Tercer aviso y corte: la empresa pasa a solo lectura y conserva todo.</summary>
    Mora90,

    /// <summary>Se dio por perdida. Estado terminal de la escalera; el tenant sigue existiendo.</summary>
    Incobrable
}

public enum EstadoGenerico { Activo, Inactivo }

public enum EstadoMaquina { Operativa, EnMantenimiento, Detenida, Inactiva }

public enum Criticidad { Baja, Media, Alta, Critica }

public enum EstadoEnriquecimiento { Pendiente, EnProceso, Completado, Fallido }

/// <summary>
/// Ciclo de vida de una orden de trabajo.
/// <para>
/// <b>Solicitada existe por una razon concreta.</b> Cualquier empleado de cualquier area puede
/// reportar algo —una cinta trabada, una lampara quemada—, y eso todavia NO es una orden de trabajo:
/// es un pedido que mantenimiento tiene que mirar. Sin este estado, el tiempo medio de resolucion,
/// las ordenes por maquina y el historial que alimenta al modelo predictivo se llenan de pedidos que
/// nunca fueron validos, y ese historial es justamente el dato de entrenamiento.
/// </para>
/// <para>
/// Quien tiene permiso de Controlar crea directamente en Abierta: no tiene sentido que se apruebe a
/// si mismo un pedido.
/// </para>
/// </summary>
public enum EstadoOrden
{
    /// <summary>Reportada por alguien sin permiso de control. Espera revision de mantenimiento.</summary>
    Solicitada,

    /// <summary>Aceptada: hay trabajo que hacer. Es donde nace una orden creada por mantenimiento.</summary>
    Abierta,

    EnCurso,
    Cerrada,

    /// <summary>Se revisó la solicitud y no corresponde. Lleva motivo y no vuelve atras.</summary>
    Rechazada,

    /// <summary>Era una orden valida y se dio de baja. Distinto de Rechazada.</summary>
    Cancelada
}

public enum TipoMantenimiento { Correctivo, Preventivo, Predictivo }

public enum Prioridad { Baja, Media, Alta, Urgente }

public enum EstadoAlerta { Activa, Resuelta }

public enum EstadoRecomendacion { Activa, Aceptada, Rechazada }

/// <summary>Distingue lo que dispara una regla determinista de lo que proyecta el modelo.</summary>
public enum OrigenRecomendacion { Regla, Modelo }

public enum TipoReporte { Stock, Fallas, Consumo, Ordenes }

public enum EstadoReporte { Activo, Eliminado }

/// <summary>
/// Tipo de asiento en el libro de movimientos de stock. Todo movimiento es inmutable:
/// un error se corrige con un asiento de <see cref="Ajuste"/>, nunca editando el anterior.
/// </summary>
public enum TipoMovimientoStock
{
    /// <summary>Reposicion, compra o devolucion a stock. Suma.</summary>
    Ingreso,
    /// <summary>Consumo por una orden de trabajo. Resta.</summary>
    Consumo,
    /// <summary>Correccion manual por recuento fisico. Suma o resta.</summary>
    Ajuste,
    /// <summary>Baja por rotura, vencimiento o extravio. Resta.</summary>
    Merma
}

/// <summary>Los cinco perfiles del sistema. Es el eje "Rol" del modelo de seguridad.</summary>
public enum RolSistema
{
    Empleado,
    Supervisor,
    Gerente,
    AdminEmpresa,
    SuperAdminMantIA
}

/// <summary>
/// Eje "Ambito": que familia de modulos ve un rol. Es una frontera ESTRUCTURAL, definida
/// en codigo y no configurable por el cliente: administracion no ejecuta tareas
/// operativas, y operacion no administra la empresa.
/// </summary>
public enum Ambito { Operacion, Empresa, Plataforma }
