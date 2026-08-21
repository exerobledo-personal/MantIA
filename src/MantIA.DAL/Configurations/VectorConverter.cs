using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace MantIA.DAL.Configurations;

/// <summary>
/// Traduce entre el <c>float[]</c> que expone la entidad y el tipo <c>Vector</c> que entiende
/// el proveedor de PostgreSQL.
/// <para>
/// Es el precio de que <c>MantIA.BE</c> no referencie ninguna libreria de persistencia, y
/// tiene una consecuencia concreta que conviene tener presente: los operadores de distancia
/// de pgvector (<c>&lt;-&gt;</c>, <c>&lt;=&gt;</c>) <b>no se traducen desde LINQ</b> cuando la
/// propiedad pasa por un conversor. Las busquedas por similitud van con <c>FromSql</c>, y por
/// eso viven concentradas en un unico repositorio en lugar de repartidas por la capa de negocio.
/// </para>
/// </summary>
public class VectorConverter : ValueConverter<float[], Vector>
{
    public VectorConverter()
        : base(
            arreglo => new Vector(arreglo),
            vector => vector.ToArray())
    {
    }
}
