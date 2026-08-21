using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>Qué se numera. Cada tipo lleva su propia serie.</summary>
public enum TipoDocumento
{
    OrdenTrabajo,
    Reporte
}

/// <summary>
/// Contador de numeración legible por empresa, tipo y año.
///
/// <para><b>Por qué existe una tabla en lugar de contar filas.</b> La maqueta numeraba las órdenes
/// contando cuántas había: con dos altas simultáneas, las dos cuentan lo mismo y las dos se creen
/// la número 47. El índice único rechaza a la segunda y el usuario ve un error por algo que hizo
/// bien.</para>
///
/// <para>Acá el número lo entrega la base, con un <c>UPDATE ... RETURNING</c> atómico: una sola
/// operación, sin leer antes, sin carrera. Es la misma idea que el contador de la bitácora.</para>
///
/// <para><b>Por qué se reinicia por año.</b> "OT-2026-00001" dice cuándo se abrió sin abrir la
/// ficha, y evita que el número crezca sin techo. El año va en el número, no solo en el contador,
/// para que dos órdenes de años distintos nunca colisionen.</para>
/// </summary>
public class ContadorDocumento : TenantEntity
{
    public TipoDocumento Tipo { get; set; }
    public int Anio { get; set; }

    /// <summary>Último número entregado. El próximo es este más uno.</summary>
    public long Ultimo { get; set; }

    /// <summary>
    /// Arma el número legible. Cinco dígitos alcanzan para 99.999 órdenes por año en una empresa;
    /// si alguna vez se pasa, el formato crece solo en lugar de truncar.
    /// </summary>
    public static string Formatear(TipoDocumento tipo, int anio, long numero) =>
        $"{Prefijo(tipo)}-{anio}-{numero:00000}";

    private static string Prefijo(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.OrdenTrabajo => "OT",
        TipoDocumento.Reporte => "REP",
        _ => "DOC"
    };
}
