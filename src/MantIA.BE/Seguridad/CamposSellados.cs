namespace MantIA.BE.Seguridad;

/// <summary>
/// Qué filas llevan dígito verificador y qué campos entran en el cálculo.
///
/// <para><b>Para qué sirve, que es distinto de lo que hace el cifrado.</b> El cifrado impide leer un
/// valor; el dígito verificador impide <b>cambiarlo sin que se note</b>. Son problemas separados y
/// hay campos que necesitan el segundo y no el primero: la cantidad de un movimiento de stock o el
/// costo de un repuesto están en claro a propósito —se suman y se filtran— y son justamente los que
/// alguien tocaría para manipular un presupuesto.</para>
///
/// <para><b>Por qué solo tres tablas.</b> Sellar todo cuesta una escritura extra por cada escritura y
/// no aporta en la mayoría de los casos. Se sellan las tres donde una alteración tiene consecuencia
/// económica directa y donde nada más la detectaría: el libro de stock, las líneas de repuesto de una
/// orden y el inventario. En el resto, un cambio a mano se descubre igual porque contradice a la
/// bitácora.</para>
///
/// <para><b>Se sella el valor del dominio, no lo que hay en la columna.</b> Un campo cifrado se sella
/// por su contenido en claro: lo que interesa proteger es el significado —que la cantidad diga 4 y no
/// 40—, no la representación. Verificar exige por lo tanto leer la fila con la aplicación, que es
/// donde de todos modos vive la llave.</para>
/// </summary>
public static class CamposSellados
{
    /// <summary>Entidad y campos que entran en el dígito, en este orden exacto.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> Politica =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // El libro mayor de stock. Es inmutable por diseño, asi que cualquier cambio en una fila
            // ya escrita es, por definicion, una alteracion.
            ["MovimientoStock"] =
                ["EmpresaId", "RepuestoId", "Tipo", "Cantidad", "SaldoResultante",
                 "OrdenTrabajoId", "FechaMovimiento"],

            // Que repuestos y a que costo se consumieron al cerrar una orden. Es el numero que
            // termina en un presupuesto.
            ["OrdenTrabajoRepuesto"] =
                ["EmpresaId", "OrdenTrabajoId", "RepuestoId", "Cantidad", "CostoUnitarioAlConsumo"],

            // El inventario y su valuacion.
            ["Repuesto"] =
                ["EmpresaId", "NumeroParte", "StockActual", "StockMinimo", "CostoUnitario",
                 "Criticidad", "Estado"],
        };

    public static bool SeSella(string entidad) => Politica.ContainsKey(entidad);

    public static IReadOnlyList<string> CamposDe(string entidad) =>
        Politica.TryGetValue(entidad, out var campos) ? campos : [];

    public static IEnumerable<string> Entidades() => Politica.Keys;
}
