using MantIA.BE.Auditoria;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MantIA.DAL.Bitacora;

/// <summary>
/// Cómo se guarda un <see cref="EventoBitacora"/> y qué índices necesita la colección.
///
/// <para>El mapeo se declara acá y no con atributos en la entidad porque <c>MantIA.BE</c> no
/// referencia ninguna librería de persistencia. Es la misma razón por la que el embedding es
/// <c>float[]</c> y no <c>Vector</c>.</para>
/// </summary>
public static class MapeoBitacora
{
    private static bool _registrado;
    private static readonly Lock Candado = new();

    /// <summary>
    /// Registra el mapeo. Idempotente y disparado una sola vez al arrancar: volver a registrar una
    /// clase ya mapeada lanza en el driver.
    /// </summary>
    public static void Registrar()
    {
        lock (Candado)
        {
            if (_registrado) return;
            _registrado = true;

            if (BsonClassMap.IsClassMapRegistered(typeof(EventoBitacora))) return;

            BsonClassMap.RegisterClassMap<EventoBitacora>(mapa =>
            {
                mapa.AutoMap();
                mapa.MapIdMember(e => e.Id)
                    .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));

                // Los enums como texto, igual que en PostgreSQL: la base tiene que poder leerse a
                // ojo, y reordenar un enum en C# no puede cambiar el significado de lo ya escrito.
                mapa.MapMember(e => e.Alcance).SetSerializer(new EnumSerializer<AlcanceBitacora>(BsonType.String));
                mapa.MapMember(e => e.Tipo).SetSerializer(new EnumSerializer<TipoEvento>(BsonType.String));
                mapa.MapMember(e => e.Nivel).SetSerializer(new EnumSerializer<NivelLog>(BsonType.String));
                mapa.MapMember(e => e.Severidad).SetSerializer(new EnumSerializer<Severidad>(BsonType.String));

                // DateTimeOffset como documento conserva el desplazamiento. Guardarlo como ticks
                // perdería la zona, y la bitácora se lee desde husos distintos.
                mapa.MapMember(e => e.Fecha)
                    .SetSerializer(new DateTimeOffsetSerializer(BsonType.Document));
            });
        }
    }

    /// <summary>
    /// Crea los índices. Se llama al arrancar; <c>CreateMany</c> es idempotente.
    /// </summary>
    public static async Task PrepararAsync(IMongoClient cliente, OpcionesMongo opciones, CancellationToken ct = default)
    {
        Registrar();

        var coleccion = cliente
            .GetDatabase(opciones.BaseDeDatos)
            .GetCollection<EventoBitacora>(opciones.Coleccion);

        var claves = Builders<EventoBitacora>.IndexKeys;

        await coleccion.Indexes.CreateManyAsync(
        [
            // El indice importante. El numero lo entrega el contador atomico, asi que en teoria no
            // puede repetirse; el indice unico esta igual porque una cadena de auditoria con dos
            // eventos en la misma posicion es indefendible, y una restriccion de base es la unica
            // garantia que no depende de que el codigo este bien.
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.Cadena).Ascending(e => e.Secuencia),
                new CreateIndexOptions { Unique = true, Name = "cadena_secuencia_unico" }),

            // Eslabones sin cerrar. Es la consulta del trabajo de mantenimiento y tiene que ser
            // barata: corre cada treinta segundos aunque no haya nada que hacer.
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.Sellado).Ascending(e => e.Cadena).Ascending(e => e.Secuencia),
                new CreateIndexOptions { Name = "pendientes_de_sellado" }),

            // Bitacora de una empresa, del mas reciente al mas viejo. Es la pantalla.
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.EmpresaId).Descending(e => e.Fecha),
                new CreateIndexOptions { Name = "empresa_fecha" }),

            // Filtro por severidad: "mostrame solo lo critico del ultimo mes".
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.EmpresaId).Ascending(e => e.Severidad).Descending(e => e.Fecha),
                new CreateIndexOptions { Name = "empresa_severidad_fecha" }),

            // Todos los eventos de una misma operacion de negocio. Es lo que lee el rollback.
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.CorrelacionId),
                new CreateIndexOptions { Name = "correlacion", Sparse = true }),

            // Acciones de una persona en una ventana de tiempo: la consulta del rollback por usuario.
            new CreateIndexModel<EventoBitacora>(
                claves.Ascending(e => e.UsuarioId).Descending(e => e.Fecha),
                new CreateIndexOptions { Name = "usuario_fecha", Sparse = true }),

            // NO hay indice TTL: la bitacora no vence. Si alguna vez el volumen molesta se archiva
            // a mano, con una decision tomada en ese momento y no programada de antemano.
        ], ct);
    }
}
