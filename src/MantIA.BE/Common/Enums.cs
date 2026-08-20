namespace MantIA.BE.Common;

// Los estados se modelan como enums y no como texto libre: un error de tipeo en un
// "activo" tiene que romper la compilacion, no llegar a produccion. La DAL los persiste
// como string para que la base siga siendo legible a ojo.

public enum EstadoEmpresa { Activa, Suspendida, Baja }

public enum EstadoGenerico { Activo, Inactivo }

public enum EstadoMaquina { Operativa, EnMantenimiento, Detenida, Inactiva }

public enum Criticidad { Baja, Media, Alta, Critica }

public enum EstadoEnriquecimiento { Pendiente, EnProceso, Completado, Fallido }

public enum EstadoOrden { Abierta, EnCurso, Cerrada, Cancelada }

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
