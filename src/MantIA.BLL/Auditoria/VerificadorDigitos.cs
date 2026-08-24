using System.Reflection;
using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.BE.Seguridad;
using MantIA.DAL.Context;
using MantIA.DAL.Seguridad;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.BLL.Auditoria;

public enum TipoFallaDigito
{
    /// <summary>La fila no coincide con su digito: alguien la edito por fuera de la aplicacion.</summary>
    DigitoInvalido,

    /// <summary>La fila existe y no tiene digito. O se inserto por fuera, o el digito se borro.</summary>
    DigitoFaltante,

    /// <summary>Hay digito y no hay fila. Alguien borro el registro y se olvido del digito.</summary>
    FilaFaltante,

    /// <summary>El digito se calculo con un formato canonico anterior. Hay que recalcularlo.</summary>
    FormatoDesactualizado,

    /// <summary>El digito dice estar calculado con una llave que no esta configurada.</summary>
    LlaveDesconocida
}

public record FallaDigito(string Tabla, Guid FilaId, TipoFallaDigito Tipo, string Detalle);

/// <summary>Resultado de una foto vertical de una tabla de una empresa.</summary>
public record ResultadoFoto(
    Guid EmpresaId,
    string Tabla,
    long Secuencia,
    long Filas,
    long FilasAnteriores,
    IReadOnlyList<FallaDigito> Fallas)
{
    public bool Integra => Fallas.Count == 0;

    /// <summary>
    /// Cuantas filas desaparecieron respecto de la foto anterior. Un numero positivo no es por si
    /// solo una manipulacion —estas tres tablas no borran, pero una purga de tenant si—: es lo que
    /// hay que ir a contrastar contra la bitacora.
    /// </summary>
    public long FilasPerdidas => Math.Max(0, FilasAnteriores - Filas);
}

public interface IVerificadorDigitos
{
    /// <summary>
    /// Recorre una tabla de una empresa, comprueba fila por fila contra su digito y cierra una foto
    /// vertical encadenada a la anterior.
    /// </summary>
    Task<ResultadoFoto> TomarFotoAsync(Guid empresaId, string tabla, CancellationToken ct = default);

    /// <summary>Tablas bajo el regimen de digito verificador.</summary>
    IEnumerable<string> Tablas();
}

/// <summary>
/// Comprueba los digitos verificadores y deja constancia de lo que encuentra.
///
/// <para><b>Que puede afirmar y que no.</b> Si una fila no verifica, alguien la escribio sin pasar
/// por la aplicacion: no hay forma de producir ese estado operando normalmente. Si verifica, lo unico
/// que se sabe es que no fue alterada por quien no tiene la llave de verificacion. Ningun mecanismo
/// con llave protege de quien tiene la llave; para ese caso la defensa es que la llave viva en otro
/// lado que la base y que las fotos se publiquen fuera del sistema.</para>
///
/// <para><b>Por que la foto se guarda aunque todo este bien.</b> Justamente porque casi siempre esta
/// bien. La serie de fotos es la evidencia: dice cuantas filas habia en cada momento y que en cada
/// uno de esos momentos alguien miro. Sin la serie, la primera vez que falta una fila no habria con
/// que comparar.</para>
/// </summary>
public class VerificadorDigitos : IVerificadorDigitos
{
    /// <summary>Filas por lote. Acota la memoria del rastreador en tablas grandes.</summary>
    private const int Lote = 500;

    private readonly MantIADbContext _db;
    private readonly ICurrentTenant _tenant;
    private readonly ISelladorFilas _sellador;
    private readonly IProtectorDatos _protector;

    public VerificadorDigitos(
        MantIADbContext db, ICurrentTenant tenant, IProtectorDatos protector)
    {
        _db = db;
        _tenant = tenant;
        _protector = protector;
        _sellador = new SelladorFilas(protector);
    }

    public IEnumerable<string> Tablas() => CamposSellados.Entidades();

    public async Task<ResultadoFoto> TomarFotoAsync(
        Guid empresaId, string tabla, CancellationToken ct = default)
    {
        if (!CamposSellados.SeSella(tabla))
            throw new ArgumentException($"La entidad '{tabla}' no lleva digito verificador.", nameof(tabla));

        // La foto se escribe como una fila mas de la empresa, con su filtro de tenant y su clave
        // foranea. Quien llame tiene que estar posicionado en esa empresa: si el contexto apunta a
        // otra, la foto se guardaria del lado equivocado y ademas la lectura de las filas devolveria
        // un conjunto que no es el que se esta resumiendo.
        if (_tenant.EmpresaId != empresaId)
            throw new InvalidOperationException(
                $"Se pidio la foto de la empresa {empresaId} con el contexto posicionado en " +
                $"{_tenant.EmpresaId?.ToString() ?? "ninguna"}.");

        var tipo = TipoDe(tabla);

        var anterior = await _db.SellosTabla
            .Where(s => s.Tabla == tabla)
            .OrderByDescending(s => s.Secuencia)
            .FirstOrDefaultAsync(ct);

        var digitos = await DigitosDe(tabla, ct);

        var fallas = new List<FallaDigito>();
        var acumulado = new AcumuladorVertical(
            tabla, empresaId, anterior?.Digito,
            tramo => _protector.Digito(tramo, _protector.VersionDigito));

        var vistos = await RecorrerAsync(tipo, tabla, empresaId, digitos, acumulado, fallas, ct);

        // Lo que quedo en el diccionario es digito sin fila. Se ordena para que el informe sea
        // estable entre corridas; no entra en el calculo del digito vertical porque ese se calcula
        // sobre lo que la base devuelve, en su propio orden.
        foreach (var huerfano in digitos.Keys.Where(id => !vistos.Contains(id)).OrderBy(id => id))
            fallas.Add(new FallaDigito(
                tabla, huerfano, TipoFallaDigito.FilaFaltante,
                "Existe el digito verificador pero no la fila que protege."));

        var version = _protector.VersionDigito;

        var foto = new SelloTabla
        {
            EmpresaId = empresaId,
            Tabla = tabla,
            Secuencia = (anterior?.Secuencia ?? 0) + 1,
            Filas = acumulado.Filas,
            FilasConDigitoInvalido = acumulado.Invalidas,
            Digito = _sellador.CalcularVertical(acumulado, version),
            DigitoAnterior = anterior?.Digito,
            VersionLlave = version,
            VersionFormato = CanonicalizacionFila.Version,
            CalculadoEn = DateTimeOffset.UtcNow,
        };

        _db.SellosTabla.Add(foto);
        await _db.SaveChangesAsync(ct);

        return new ResultadoFoto(
            empresaId, tabla, foto.Secuencia, foto.Filas, anterior?.Filas ?? foto.Filas, fallas);
    }

    /// <summary>Digitos guardados de la tabla, sin rastrear: solo se leen para comparar.</summary>
    private async Task<Dictionary<Guid, SelloFila>> DigitosDe(string tabla, CancellationToken ct) =>
        await _db.SellosFila
            .AsNoTracking()
            .Where(s => s.Tabla == tabla)
            .ToDictionaryAsync(s => s.FilaId, ct);

    /// <summary>
    /// Recorre la tabla en lotes y devuelve los identificadores encontrados.
    /// <para>
    /// La entidad se resuelve por reflexion porque el catalogo de tablas selladas es una lista de
    /// nombres: agregar una tabla al regimen tiene que ser una linea en el catalogo y nada mas. El
    /// metodo generico existe para que la consulta la arme EF con el tipo real.
    /// </para>
    /// </summary>
    private Task<HashSet<Guid>> RecorrerAsync(
        Type tipo, string tabla, Guid empresaId,
        Dictionary<Guid, SelloFila> digitos, AcumuladorVertical acumulado,
        List<FallaDigito> fallas, CancellationToken ct) =>
        (Task<HashSet<Guid>>)typeof(VerificadorDigitos)
            .GetMethod(nameof(RecorrerTipoAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(tipo)
            .Invoke(this, [tabla, empresaId, digitos, acumulado, fallas, ct])!;

    private async Task<HashSet<Guid>> RecorrerTipoAsync<T>(
        string tabla, Guid empresaId,
        Dictionary<Guid, SelloFila> digitos, AcumuladorVertical acumulado,
        List<FallaDigito> fallas, CancellationToken ct)
        where T : TenantEntity
    {
        var vistos = new HashSet<Guid>();
        var desde = 0;

        while (true)
        {
            // IgnoreQueryFilters a proposito, con el filtro de empresa puesto a mano: la foto tiene
            // que incluir las filas dadas de baja. Si no, una baja logica se veria exactamente igual
            // que una fila borrada a mano, que es lo unico que este mecanismo busca distinguir.
            var lote = await _db.Set<T>()
                .IgnoreQueryFilters()
                .Where(e => e.EmpresaId == empresaId)
                .OrderBy(e => e.Id)
                .Skip(desde)
                .Take(Lote)
                .ToListAsync(ct);

            if (lote.Count == 0) break;

            foreach (var fila in lote)
            {
                vistos.Add(fila.Id);

                if (!digitos.TryGetValue(fila.Id, out var sello))
                {
                    fallas.Add(new FallaDigito(
                        tabla, fila.Id, TipoFallaDigito.DigitoFaltante,
                        "La fila existe y no tiene digito verificador."));
                    acumulado.Agregar(fila.Id, "-", valida: false);
                    continue;
                }

                var valida = _sellador.Verifica(_db.Entry(fila), sello, out var motivo);

                if (!valida)
                    fallas.Add(new FallaDigito(tabla, fila.Id, Clasificar(sello, motivo), motivo));

                acumulado.Agregar(fila.Id, sello.Digito, valida);
            }

            desde += lote.Count;

            // Se sueltan las entidades del lote: sin esto, recorrer una tabla grande termina con
            // todas sus filas en memoria y ademas las deja marcadas para escribir si algo las toco.
            _db.ChangeTracker.Clear();
        }

        return vistos;
    }

    private static TipoFallaDigito Clasificar(SelloFila sello, string motivo) =>
        sello.VersionFormato != CanonicalizacionFila.Version ? TipoFallaDigito.FormatoDesactualizado
        : motivo.Contains("llave", StringComparison.OrdinalIgnoreCase) ? TipoFallaDigito.LlaveDesconocida
        : TipoFallaDigito.DigitoInvalido;

    private Type TipoDe(string tabla) =>
        _db.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType.Name == tabla)?.ClrType
        ?? throw new InvalidOperationException(
            $"'{tabla}' figura en el catalogo de filas selladas pero no esta en el modelo.");
}
