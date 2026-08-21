using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MantIA.DAL.Context;

/// <summary>
/// Fabrica que usa <c>dotnet ef</c> para construir el contexto al generar o aplicar
/// migraciones.
/// <para>
/// Existe para que las migraciones se generen desde este proyecto y no desde MantIA.WEB.
/// Sin ella, <c>dotnet ef</c> tiene que levantar el <c>Program.cs</c> de la aplicacion
/// completa, que exige configuracion de Auth0 y termina fallando por un motivo que no tiene
/// nada que ver con la base de datos.
/// </para>
/// <para>
/// El tenant que inyecta es vacio a proposito: en tiempo de diseno solo se lee el modelo,
/// nunca se ejecuta una consulta. La cadena de conexion tampoco se usa para conectarse al
/// generar una migracion; solo tiene que ser sintacticamente valida para que el proveedor
/// arme el modelo relacional.
/// </para>
/// </summary>
public class MantIADbContextFactory : IDesignTimeDbContextFactory<MantIADbContext>
{
    private const string ConexionPorDefecto =
        "Host=localhost;Port=5432;Database=mantia;Username=mantia;Password=dev_local_pwd";

    public MantIADbContext CreateDbContext(string[] args)
    {
        // Permite apuntar a otra base sin tocar codigo:
        //   dotnet ef database update -- "Host=...;Database=..."
        // o con la variable de entorno MANTIA_CONNECTION.
        var conexion =
            args.FirstOrDefault(a => a.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            ?? Environment.GetEnvironmentVariable("MANTIA_CONNECTION")
            ?? ConexionPorDefecto;

        var opciones = new DbContextOptionsBuilder<MantIADbContext>()
            .UseNpgsql(conexion, npgsql => npgsql.UseVector())
            // Tiene que coincidir con lo que registran MantIA.WEB y MantIA.API. Si la
            // migracion se generara sin esta linea, las columnas quedarian en PascalCase y
            // la aplicacion buscaria snake_case contra una base que no las tiene.
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MantIADbContext(opciones, new CurrentTenant());
    }
}
