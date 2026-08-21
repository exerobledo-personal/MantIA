namespace MantIA.BE.Auditoria;

/// <summary>
/// Cuánto pesa un evento para el negocio.
/// <para>
/// Es un eje distinto de <see cref="NivelLog"/>, y conviene no mezclarlos. <c>NivelLog</c> dice
/// cuánto le importa a un tecnico que mira logs: una excepcion es <c>Error</c> aunque no tenga
/// ninguna consecuencia. <c>Severidad</c> dice cuanto le importa a la empresa: borrar una orden
/// abierta es un evento perfectamente exitoso, sin ninguna excepcion, y es lo mas grave que puede
/// pasar en un dia normal.
/// </para>
/// <para>
/// La severidad decide tres cosas: si el evento se muestra destacado en la bitacora, cuanto tiempo
/// se conserva, y si dispara aviso al administrador de la empresa.
/// </para>
/// </summary>
public enum Severidad
{
    /// <summary>
    /// El dia a dia: consultar, listar, iniciar sesion. Alto volumen y poco valor individual.
    /// Se registra igual, porque el valor esta en el patron: diez consultas de un operario son
    /// ruido, doscientas a las tres de la manana no.
    /// </summary>
    Rutina,

    /// <summary>
    /// Cambia datos del negocio sin mover valor: crear una maquina, editar una descripcion,
    /// abrir una orden. Es lo que hay que poder reconstruir para responder "quien cargo esto".
    /// </summary>
    Operativa,

    /// <summary>
    /// Mueve stock, dinero o capacidades: cerrar una orden, ajustar inventario, cambiar la matriz
    /// de permisos, dar de alta un usuario. Son las acciones donde un error o un abuso tienen
    /// costo real.
    /// </summary>
    Sensible,

    /// <summary>
    /// Destruye o revierte: dar de baja una orden abierta, aplicar un rollback, usar el bypass de
    /// superadministrador, tocar una cuenta administradora. Se conserva sin limite de tiempo y
    /// merece revision humana.
    /// </summary>
    Critica
}
