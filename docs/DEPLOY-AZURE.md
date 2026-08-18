# Publicar la demo de MantIA en Azure App Service

Objetivo: que el profesor entre a una URL desde su propia máquina y recorra las 37
pantallas, sin instalar nada y sin depender de que tu notebook esté prendida.

Todo lo que sigue está verificado contra la documentación de agosto de 2026 y, donde
se aclara, contra una prueba real ejecutada sobre el runtime .NET 10.

---

## 1. Qué se publica

Solo el proyecto `src/MantIA.WEB`. Es una app Blazor Server autosuficiente: los datos
de la maqueta viven en memoria (`Demo/DatosDemo.cs`), así que **no hace falta base de
datos, ni Auth0 configurado, ni N8N** para que la demo funcione punta a punta.

La app arranca aunque `Auth0:ClientId` esté en `PENDIENTE_DE_CONFIGURAR`: lo único que
no va a funcionar es la ruta `/login` real, que en la demo no se usa.

---

## 2. Elegir el plan

| Plan | Costo | WebSockets | Always On | Sirve para la demo |
|------|-------|-----------|-----------|--------------------|
| **F1 Free (Linux)** | $0 | **5 conexiones simultáneas** | No | Sí, con cuidados |
| B1 Basic (Linux) | ~USD 13/mes | 350 | Sí | Sí, sin cuidados |

Blazor Server mantiene **una conexión WebSocket abierta por pestaña**. Con F1 tenés
5 en total: alcanza para vos y el profesor, no para una clase entera abriendo la URL
al mismo tiempo.

Los dos cuidados del plan gratuito:

1. **No hay Always On.** Después de 20 minutos sin tráfico la app se duerme y el
   primer request tarda entre 30 y 60 segundos. **Abrí la URL 5 minutos antes de
   presentar** y dejá una pestaña viva.
2. **Cuota de 60 minutos de CPU por día.** Si se agota, el sitio devuelve 403 hasta
   el día siguiente. No ensayes la demo veinte veces el mismo día.

> Sos alumno de la UAI: fijate si podés activar **Azure for Students**, que da crédito
> sin tarjeta de crédito. Con ese crédito el plan B1 sale gratis y te evitás los dos
> problemas de arriba. Es el camino que recomiendo si te lo aprueban a tiempo.

---

## 3. Crear el recurso (una sola vez)

Desde el portal, o con Azure CLI:

```bash
az login

az group create \
  --name rg-mantia-demo \
  --location brazilsouth

az appservice plan create \
  --name plan-mantia-demo \
  --resource-group rg-mantia-demo \
  --is-linux \
  --sku F1

az webapp create \
  --name mantia-demo-uai \
  --resource-group rg-mantia-demo \
  --plan plan-mantia-demo \
  --runtime "DOTNETCORE:10.0"
```

`--name` tiene que ser único en todo Azure: la URL va a quedar
`https://mantia-demo-uai.azurewebsites.net`.

Antes de correrlo, confirmá que el runtime existe con ese nombre exacto:

```bash
az webapp list-runtimes --os-type linux | grep -i dotnet
```

Si `DOTNETCORE:10.0` todavía no apareciera, el plan B es publicar la imagen del
`Dockerfile` que está en `src/MantIA.WEB/Dockerfile` (ver sección 7).

---

## 4. Configuración de la app

```bash
az webapp config appsettings set \
  --name mantia-demo-uai \
  --resource-group rg-mantia-demo \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    Auth0__Domain=mantia-tfi.us.auth0.com \
    Auth0__ClientId=PENDIENTE_DE_CONFIGURAR \
    Auth0__ClientSecret=PENDIENTE_DE_CONFIGURAR

az webapp config set \
  --name mantia-demo-uai \
  --resource-group rg-mantia-demo \
  --web-sockets-enabled true
```

Sobre `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`: App Service termina el TLS en su
front-end y le pasa la request a la app por HTTP plano. Sin esa variable, la app cree
que la conexión es `http` y arma cualquier URL absoluta (por ejemplo el callback de
Auth0) apuntando a `http://`.

**Verificado en este entorno** sobre .NET 10, mandando `X-Forwarded-Proto: https` a
una app mínima con `UseHttpsRedirection()`:

- sin la variable → `scheme=http`
- con la variable → `scheme=https`

Y no requiere tocar `Program.cs`: `WebApplication.CreateBuilder` la lee solo.

Los dos guiones bajos de `Auth0__Domain` no son un error de tipeo: así es como App
Service traduce una sección anidada de `appsettings.json`.

---

## 5. Publicar

### Opción A — desde Visual Studio (la más rápida para el 18/08)

Botón derecho sobre `MantIA.WEB` → Publish → Azure → Azure App Service (Linux) →
elegís `mantia-demo-uai` → Publish. Tarda unos minutos y te abre la URL sola.

### Opción B — desde la línea de comandos

```bash
dotnet publish src/MantIA.WEB/MantIA.WEB.csproj -c Release -o ./publicado
cd publicado && zip -r ../app.zip . && cd ..

az webapp deploy \
  --name mantia-demo-uai \
  --resource-group rg-mantia-demo \
  --src-path app.zip \
  --type zip
```

### Opción C — automático desde GitHub

Ya está el workflow en `.github/workflows/build-y-deploy.yml`. Para activarlo:

1. En el portal de Azure, en el Web App: **Get publish profile** (descarga un `.xml`).
2. En GitHub → Settings → Secrets and variables → Actions:
   - Secret `AZURE_WEBAPP_PUBLISH_PROFILE` = el contenido completo del `.xml`.
   - Variable `AZURE_WEBAPP_NAME` = `mantia-demo-uai`.
   - Variable `RAMA_DEMO` = la rama que querés publicar.

Mientras esas variables no existan, el trabajo de despliegue se saltea solo y el
workflow igual te sirve como **verificación de compilación**, que hoy es lo que más
falta hace: GitHub sí tiene acceso a NuGet.

---

## 6. Checklist del día de la demo

- [ ] Abrir la URL 5 minutos antes y dejar una pestaña abierta (evita el arranque frío).
- [ ] Confirmar que el WebSocket conectó: si abajo aparece el cartel de reconexión,
      recargar. Con F1 hay 5 conexiones; cerrá las pestañas que no uses.
- [ ] Recorrer una vez el flujo de alta de máquina para "calentar" el circuito.
- [ ] Tener el proyecto abierto en Visual Studio como plan B por si se cae la red.

---

## 7. Plan B: contenedor

`src/MantIA.WEB/Dockerfile` construye la imagen desde la raíz del repo:

```bash
docker build -f src/MantIA.WEB/Dockerfile -t mantia-web:local .
docker run --rm -p 8080:8080 mantia-web:local
```

Sirve igual para App Service en modo contenedor y para Azure Container Apps, que
escala a cero y tiene un tramo gratuito mensual más generoso que F1. Es también el
entregable de "dockerización" que falta en el TFI.

---

## 8. Lo que todavía no está resuelto

- **Auth0 real.** Cuando se configure, hay que agregar
  `https://mantia-demo-uai.azurewebsites.net/callback` en Allowed Callback URLs y
  `https://mantia-demo-uai.azurewebsites.net/` en Allowed Logout URLs.
- **PostgreSQL.** La demo no lo necesita. Cuando el backend empiece a persistir,
  la opción barata es Azure Database for PostgreSQL Flexible Server (Burstable B1ms)
  con la extensión `vector` habilitada, o seguir con el contenedor de
  `docker-compose.yml` para desarrollo.
- **Región.** `brazilsouth` es la de menor latencia desde Buenos Aires. Si F1 no
  tuviera cupo ahí, `eastus` funciona igual para una demo.
