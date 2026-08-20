using MantIA.BE.Common;

namespace MantIA.BE.Entities;

/// <summary>
/// Ficha tecnica de un modelo de maquina. <b>Compartida entre todas las empresas.</b> Es el
/// activo acumulativo del producto: cada modelo que registra un cliente queda disponible
/// para los siguientes.
/// <para>
/// Contiene UNICAMENTE conocimiento de referencia del fabricante y del modelo de lenguaje.
/// La evidencia derivada de operaciones reales vive en <see cref="EvidenciaModelo"/>, y solo
/// asciende aca cuando se corrobora entre varias empresas.
/// </para>
/// </summary>
public class CatalogoMaquina : CatalogEntity
{
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Categoria { get; set; }

    public ICollection<CatalogoFallaComun> FallasComunes { get; set; } = [];
    public ICollection<CatalogoRepuestoSugerido> RepuestosSugeridos { get; set; } = [];

    public string? IntervalosMantenimiento { get; set; }

    public EstadoEnriquecimiento Estado { get; set; } = EstadoEnriquecimiento.Pendiente;
    public DateTimeOffset? FechaUltimoEnriquecimiento { get; set; }
    /// <summary>Version del prompt con el que se enriquecio. Permite re-ingestar lo que quedo viejo.</summary>
    public string? VersionIngesta { get; set; }
    public string? UltimoError { get; set; }
}

/// <summary>Falla documentada de un modelo. Antes era texto plano y no se podia consultar.</summary>
public class CatalogoFallaComun : CatalogEntity
{
    public Guid CatalogoMaquinaId { get; set; }
    public CatalogoMaquina? CatalogoMaquina { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    /// <summary>Cuantas empresas distintas la corroboraron. Es el umbral de promocion.</summary>
    public int EmpresasQueLaCorroboraron { get; set; }
    public int EventosRegistrados { get; set; }
}

/// <summary>Repuesto que el fabricante asocia al modelo.</summary>
public class CatalogoRepuestoSugerido : CatalogEntity
{
    public Guid CatalogoMaquinaId { get; set; }
    public CatalogoMaquina? CatalogoMaquina { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? NumeroParteReferencia { get; set; }
    public Criticidad CriticidadSugerida { get; set; } = Criticidad.Media;
}

/// <summary>
/// Representacion vectorial de un texto del sistema, para agrupar descripciones
/// equivalentes de la misma falla.
/// <para>
/// El vector se declara como <c>float[]</c> y no con el tipo de la libreria de pgvector
/// a proposito: <c>MantIA.BE</c> no referencia a nadie. Es la capa de acceso a datos la
/// que lo mapea a una columna <c>vector</c>.
/// </para>
/// </summary>
public class EvidenciaModelo : BaseEntity
{
    /// <summary>Empresa que origino la observacion. Nula cuando ya fue promovida a conocimiento comun.</summary>
    public Guid? EmpresaId { get; set; }

    public Guid CatalogoMaquinaId { get; set; }
    public CatalogoMaquina? CatalogoMaquina { get; set; }

    /// <summary>Orden de la que salio. Nunca se expone fuera de la empresa que la genero.</summary>
    public Guid? OrdenTrabajoId { get; set; }

    public string TextoOriginal { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }

    /// <summary>Modo de falla normalizado al que se agrupo esta observacion.</summary>
    public string? ModoFallaNormalizado { get; set; }

    public bool Promovida { get; set; }
    public DateTimeOffset? FechaPromocion { get; set; }
}
