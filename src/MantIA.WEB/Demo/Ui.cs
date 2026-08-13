using MudBlazor;

namespace MantIA.WEB.Demo;

public static class Ui
{
    public static Color ColorDe(Criticidad criticidad) => criticidad switch
    {
        Criticidad.Critica => Color.Error,
        Criticidad.Alta => Color.Warning,
        Criticidad.Media => Color.Info,
        _ => Color.Default
    };

    public static string TextoDe(Criticidad criticidad) => criticidad switch
    {
        Criticidad.Critica => "Crítica",
        Criticidad.Alta => "Alta",
        Criticidad.Media => "Media",
        _ => "Baja"
    };

    public static Color ColorDe(EstadoMaquina estado) => estado switch
    {
        EstadoMaquina.Operativa => Color.Success,
        EstadoMaquina.EnMantenimiento => Color.Warning,
        EstadoMaquina.Detenida => Color.Error,
        _ => Color.Default
    };

    public static string TextoDe(EstadoMaquina estado) => estado switch
    {
        EstadoMaquina.EnMantenimiento => "En mantenimiento",
        _ => estado.ToString()
    };

    public static string IconoDe(EstadoMaquina estado) => estado switch
    {
        EstadoMaquina.Operativa => Icons.Material.Filled.CheckCircle,
        EstadoMaquina.EnMantenimiento => Icons.Material.Filled.Build,
        EstadoMaquina.Detenida => Icons.Material.Filled.ReportProblem,
        _ => Icons.Material.Filled.PowerSettingsNew
    };

    public static Color ColorDe(EstadoOrden estado) => estado switch
    {
        EstadoOrden.Abierta => Color.Info,
        EstadoOrden.EnCurso => Color.Warning,
        EstadoOrden.Cerrada => Color.Success,
        _ => Color.Default
    };

    public static string TextoDe(EstadoOrden estado) => estado switch
    {
        EstadoOrden.EnCurso => "En curso",
        _ => estado.ToString()
    };

    public static Color ColorDe(Prioridad prioridad) => prioridad switch
    {
        Prioridad.Urgente => Color.Error,
        Prioridad.Alta => Color.Warning,
        Prioridad.Media => Color.Info,
        _ => Color.Default
    };

    public static Color ColorDe(EstadoRecomendacion estado) => estado switch
    {
        EstadoRecomendacion.Aceptada => Color.Success,
        EstadoRecomendacion.Rechazada => Color.Error,
        _ => Color.Info
    };

    public static Color ColorDe(EstadoGenerico estado) =>
        estado == EstadoGenerico.Activo ? Color.Success : Color.Default;

    public static Color ColorDe(EstadoEnriquecimiento estado) => estado switch
    {
        EstadoEnriquecimiento.Completado => Color.Success,
        EstadoEnriquecimiento.EnProceso => Color.Info,
        EstadoEnriquecimiento.Fallido => Color.Error,
        _ => Color.Warning
    };

    public static string TextoDe(EstadoEnriquecimiento estado) => estado switch
    {
        EstadoEnriquecimiento.EnProceso => "En proceso",
        _ => estado.ToString()
    };

    public static Color ColorDe(EstadoServicio estado) => estado switch
    {
        EstadoServicio.Operativo => Color.Success,
        EstadoServicio.Degradado => Color.Warning,
        _ => Color.Error
    };

    public static Color ColorDe(NivelLog nivel) => nivel switch
    {
        NivelLog.Error => Color.Error,
        NivelLog.Warning => Color.Warning,
        NivelLog.Info => Color.Info,
        _ => Color.Default
    };

    public static Color ColorDe(TipoMantenimiento tipo) => tipo switch
    {
        TipoMantenimiento.Correctivo => Color.Error,
        TipoMantenimiento.Preventivo => Color.Info,
        _ => Color.Tertiary
    };

    public static Color ColorDe(OrigenRecomendacion origen) =>
        origen == OrigenRecomendacion.Modelo ? Color.Tertiary : Color.Primary;

    public static string TextoDe(OrigenRecomendacion origen) =>
        origen == OrigenRecomendacion.Modelo ? "Modelo de IA" : "Regla de negocio";

    public static string IconoDe(OrigenRecomendacion origen) =>
        origen == OrigenRecomendacion.Modelo ? Icons.Material.Filled.Psychology : Icons.Material.Filled.Rule;

    public static string Moneda(decimal valor) => "$ " + valor.ToString("N0", Cultura);

    public static string Numero(decimal valor) => valor.ToString("0.##", Cultura);

    public static string Fecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy") ?? "—";

    public static string FechaHora(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy HH:mm") ?? "—";

    public static string Relativo(DateTime fecha)
    {
        var diferencia = DateTime.Now - fecha;
        if (diferencia.TotalMinutes < 1) return "recién";
        if (diferencia.TotalMinutes < 60) return $"hace {(int)diferencia.TotalMinutes} min";
        if (diferencia.TotalHours < 24) return $"hace {(int)diferencia.TotalHours} h";
        if (diferencia.TotalDays < 30) return $"hace {(int)diferencia.TotalDays} d";
        return fecha.ToString("dd/MM/yyyy");
    }

    private static readonly System.Globalization.CultureInfo Cultura = new("es-AR");
}
