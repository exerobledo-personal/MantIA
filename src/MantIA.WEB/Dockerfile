# Imagen de MantIA.WEB (Blazor Server).
# Se construye desde la raiz del repositorio, no desde esta carpeta:
#   docker build -f src/MantIA.WEB/Dockerfile -t mantia-web:local .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /origen

# Se copia todo src/ antes de restaurar. Se pierde algo de cache entre builds,
# pero se elimina el modo de falla mas comun: que falte un .csproj en la lista
# y la restauracion se caiga a mitad de camino.
COPY src/ src/

RUN dotnet restore src/MantIA.WEB/MantIA.WEB.csproj

RUN dotnet publish src/MantIA.WEB/MantIA.WEB.csproj \
      -c Release \
      -o /publicado \
      --no-restore \
      /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publicado .

# Render, App Service y Container Apps inyectan el puerto por la variable PORT.
# Si no viene, se usa 8080, que es lo habitual en local.
#
# reloadConfigOnChange=false es obligatorio en contenedores: por defecto .NET vigila
# appsettings.json con un FileSystemWatcher, y eso consume una instancia de inotify
# del kernel. En hosts compartidos el limite (128 por usuario) suele estar agotado y
# la aplicacion revienta en WebApplication.CreateBuilder, antes de existir.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_hostBuilder__reloadConfigOnChange=false \
    DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet MantIA.WEB.dll"]
