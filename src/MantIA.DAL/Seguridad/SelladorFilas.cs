using System.Globalization;
using System.Text;
using MantIA.BE.Entities;
using MantIA.BE.Seguridad;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MantIA.DAL.Seguridad;

public interface ISelladorFilas
{
    /// <summary>Version de llave con la que se calcula de ahora en adelante.</summary>
    string VersionActual { get; }

    /// <summary>Verdadero si la entidad esta bajo el regimen de digito verificador.</summary>
    bool Protege(string entidad);

    /// <summary>Calcula el digito de una fila a partir de sus valores actuales en memoria.</summary>
    string Calcular(EntityEntry entrada, string version);

    /// <summary>
    /// Comprueba una fila contra su digito guardado. Devuelve falso tambien cuando el digito fue
    /// calculado con un formato o una llave que ya no existen: en ese caso no se puede afirmar que
    /// la fila este bien, y afirmarlo seria peor que el error que este mecanismo evita.
    /// </summary>
    bool Verifica(EntityEntry entrada, SelloFila sello, out string motivo);

    /// <summary>Cierra una foto vertical a partir de un acumulado de digitos de fila.</summary>
    string CalcularVertical(AcumuladorVertical acumulado, string version);

    /// <summary>Comparacion en tiempo constante de dos digitos.</summary>
    bool Coinciden(string a, string b);
}

/// <summary>
/// Calcula los digitos verificadores. No toca la base: recibe entidades ya cargadas y devuelve
/// texto. Separarlo del contexto es lo que permite usar exactamente el mismo codigo al escribir
/// —donde el digito se genera— y al auditar —donde se comprueba—; si fueran dos implementaciones,
/// un desacuerdo entre ellas se veria como una manipulacion que nunca ocurrio.
/// </summary>
public class SelladorFilas : ISelladorFilas
{
    private readonly IProtectorDatos _protector;

    public SelladorFilas(IProtectorDatos protector) => _protector = protector;

    public string VersionActual => _protector.VersionDigito;

    public bool Protege(string entidad) => CamposSellados.SeSella(entidad);

    public string Calcular(EntityEntry entrada, string version)
    {
        var entidad = entrada.Metadata.ClrType.Name;
        var id = IdDe(entrada);

        // Los valores salen del rastreador de cambios y NO de la columna: son los del dominio, en
        // claro, antes de que el conversor de cifrado los transforme. Es deliberado. Lo que hay que
        // proteger es el significado —que la cantidad diga 4 y no 40—, no la representacion; y un
        // campo cifrado con nonce aleatorio produce un texto distinto cada vez que se guarda, con lo
        // cual sellar la representacion daria un digito nuevo en cada escritura aunque nada cambie.
        return _protector.Digito(
            CanonicalizacionFila.Canonizar(entidad, id, campo => entrada.Property(campo).CurrentValue),
            version);
    }

    public bool Verifica(EntityEntry entrada, SelloFila sello, out string motivo)
    {
        if (sello.VersionFormato != CanonicalizacionFila.Version)
        {
            motivo = $"La fila fue sellada con el formato '{sello.VersionFormato}' y el vigente es " +
                     $"'{CanonicalizacionFila.Version}'. Hay que recalcularla antes de poder verificarla.";
            return false;
        }

        try
        {
            var esperado = Calcular(entrada, sello.VersionLlave);
            if (Coinciden(esperado, sello.Digito))
            {
                motivo = string.Empty;
                return true;
            }

            motivo = "El contenido de la fila no coincide con su digito verificador.";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            motivo = ex.Message;
            return false;
        }
    }

    public string CalcularVertical(AcumuladorVertical acumulado, string version) =>
        _protector.Digito(acumulado.Cerrar(), version);

    /// <summary>
    /// Comparacion en tiempo constante. Comparar digitos con == filtra informacion por lo que tarda
    /// en encontrar la primera diferencia, y con suficientes intentos eso alcanza para reconstruir
    /// un digito valido caracter por caracter.
    /// </summary>
    public bool Coinciden(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diferencia = 0;
        for (var i = 0; i < a.Length; i++) diferencia |= a[i] ^ b[i];
        return diferencia == 0;
    }

    internal static Guid IdDe(EntityEntry entrada) =>
        entrada.Property(nameof(BE.Common.BaseEntity.Id)).CurrentValue is Guid id
            ? id
            : throw new InvalidOperationException(
                $"La entidad {entrada.Metadata.ClrType.Name} no tiene un Id de tipo Guid y no se " +
                "puede sellar por fila.");
}

/// <summary>
/// Va juntando las filas de una tabla para cerrar una foto vertical.
///
/// <para><b>Por que existe en lugar de concatenar todo y hashear al final.</b> Una tabla de stock de
/// un cliente grande tiene cientos de miles de filas; armar una sola cadena con todas seria decenas
/// de megabytes en memoria por cada foto. Aca se pliega cada tanto: se acumula un tramo, se lo
/// reduce a un digito y ese digito arranca el tramo siguiente. El resultado sigue dependiendo de
/// todas las filas y de su orden, que es lo unico que hace falta.</para>
/// </summary>
public class AcumuladorVertical
{
    /// <summary>Filas por tramo. Acota la memoria sin multiplicar el numero de plegados.</summary>
    private const int FilasPorTramo = 1000;

    private readonly Func<string, string> _plegar;
    private readonly StringBuilder _tramo = new(64 * 1024);

    private string _acumulado;
    private int _enTramo;

    public AcumuladorVertical(
        string tabla, Guid empresaId, string? digitoAnterior, Func<string, string> plegar)
    {
        _plegar = plegar;

        // El encabezado ata la foto a su serie: una foto de una tabla no vale para otra, ni la de
        // una empresa para otra, ni una foto vieja para reemplazar a la actual.
        var sb = new StringBuilder(256);
        Campo(sb, CanonicalizacionFila.Version);
        Campo(sb, tabla);
        Campo(sb, empresaId.ToString("N"));
        Campo(sb, digitoAnterior);
        _acumulado = sb.ToString();
    }

    public long Filas { get; private set; }
    public long Invalidas { get; private set; }

    /// <summary>Suma una fila. El orden en que se llama forma parte del resultado.</summary>
    public void Agregar(Guid filaId, string digito, bool valida)
    {
        Campo(_tramo, filaId.ToString("N"));
        Campo(_tramo, digito);
        Campo(_tramo, valida ? "1" : "0");

        Filas++;
        if (!valida) Invalidas++;

        if (++_enTramo < FilasPorTramo) return;

        Plegar();
    }

    /// <summary>Cierra el acumulado y devuelve la cadena canonica final de la foto.</summary>
    public string Cerrar()
    {
        if (_enTramo > 0) Plegar();

        var sb = new StringBuilder(_acumulado.Length + 64);
        sb.Append(_acumulado);
        Campo(sb, Filas.ToString(CultureInfo.InvariantCulture));
        Campo(sb, Invalidas.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    private void Plegar()
    {
        _acumulado = _plegar(_acumulado + _tramo);
        _tramo.Clear();
        _enTramo = 0;
    }

    private static void Campo(StringBuilder sb, string? valor)
    {
        if (valor is null)
        {
            sb.Append("-|");
            return;
        }

        sb.Append(valor.Length.ToString(CultureInfo.InvariantCulture))
          .Append(':')
          .Append(valor)
          .Append('|');
    }
}
