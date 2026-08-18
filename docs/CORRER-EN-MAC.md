# Levantar MantIA en una Mac

Sí, corre. No hay nada del proyecto atado a Windows: Blazor Server, MudBlazor, EF Core
y Npgsql son multiplataforma, y .NET 10 soporta tanto Apple Silicon como Intel.

Lo único que cambia es el IDE: **Visual Studio for Mac fue discontinuado por Microsoft**
(el anuncio es de agosto de 2023 y el soporte terminó el 31 de agosto de 2024). En Mac se
usa VS Code o Rider.

---

## 0. Antes que nada: pushear desde la PC

Los cambios de estos días están commiteados pero conviene confirmar que están en GitHub,
porque la Mac va a clonar desde ahí:

```bash
git push origin feature/backend-mantia
```

---

## 1. Instalar el SDK de .NET 10

**Usá el instalador oficial, no Homebrew.** El cask de Homebrew tiene un problema conocido
de convivencia de versiones: al instalar una versión termina dejando otra como
predeterminada, y eso genera errores de compilación difíciles de diagnosticar.

1. Entrar a https://dotnet.microsoft.com/download/dotnet
2. Elegir **.NET 10.0**
3. Descargar el instalador de macOS para tu arquitectura:
   - **Arm64** si tu Mac es M1/M2/M3/M4
   - **x64** si es Intel
4. Ejecutarlo.

Verificar:

```bash
dotnet --list-sdks
```

Tiene que aparecer una línea que empiece con `10.0.`. Si no aparece nada, abrí una terminal
nueva: el instalador agrega el PATH pero la sesión abierta no lo ve.

Requisito de sistema: macOS 14 "Sonoma" o posterior.

---

## 2. Clonar el repositorio

**No lo clones dentro de `~/Documents` ni `~/Desktop`** si tenés activado el sync de
iCloud Drive. Es exactamente el mismo problema que nos hizo perder el `.git` en Windows con
OneDrive: iCloud descarga los archivos bajo demanda y deja punteros vacíos donde git espera
archivos reales.

```bash
mkdir -p ~/dev && cd ~/dev
git clone https://github.com/exerobledo-personal/MantIA.git
cd MantIA
git checkout feature/backend-mantia
```

---

## 3. Certificado de desarrollo

Una sola vez por máquina:

```bash
dotnet dev-certs https --trust
```

Te va a pedir la contraseña del sistema. Sin esto, el navegador rechaza `https://localhost`.

---

## 4. Levantar la aplicación

```bash
dotnet run --project src/MantIA.WEB
```

La primera vez tarda unos minutos porque restaura MudBlazor y el resto de los paquetes.

Después queda escuchando en:

- http://localhost:5141
- https://localhost:7158

Para detenerla, `Ctrl+C`.

---

## 5. Qué NO hace falta

La maqueta funciona entera con datos en memoria. **No necesitás PostgreSQL, ni MongoDB, ni
N8N, ni Auth0 configurado.** El `docker-compose.yml` está para cuando arranquemos el
backend real; hoy podés ignorarlo.

Si más adelante lo querés levantar, instalás Docker Desktop para Mac y corrés
`docker compose up -d` desde la raíz.

---

## 6. Editor

| Opción | Comentario |
|---|---|
| **VS Code + extensión C# Dev Kit** | Gratis. Es lo que Microsoft recomienda hoy en Mac. Alcanza de sobra para este proyecto. |
| **JetBrains Rider** | Pago, pero gratuito con licencia de estudiante. Es lo más parecido a Visual Studio de Windows: mejor debugger, mejor navegación y mejor soporte de Razor. |

Con tu correo de la UAI podés pedir la licencia académica de JetBrains. Si venís cómodo con
Visual Studio en Windows, Rider te va a resultar mucho más familiar que VS Code.

---

## 7. Si algo falla

| Síntoma | Causa habitual |
|---|---|
| `dotnet: command not found` | Terminal abierta antes de instalar. Cerrala y abrí una nueva. |
| `NETSDK1045: The current .NET SDK does not support targeting net10.0` | Está instalado un SDK más viejo. Revisar con `dotnet --list-sdks`. |
| El navegador dice que el certificado no es válido | Falta correr `dotnet dev-certs https --trust`. |
| Errores raros de git o archivos que "desaparecen" | El repo quedó dentro de una carpeta sincronizada por iCloud. Moverlo a `~/dev`. |
| El puerto ya está en uso | Otra instancia corriendo. `lsof -ti:5141 \| xargs kill`. |

---

## 8. Trabajar en las dos máquinas

Mientras el proyecto sea de una sola persona, la regla simple alcanza: **antes de empezar
en una máquina, `git pull`; antes de cambiar de máquina, `git push`.** Lo que no está
pusheado no existe para la otra.

---

## 9. Sin instalar nada: las dos opciones online

Si preferís no tocar la Mac, hay dos caminos y resuelven cosas distintas.

### Solo mirar la aplicación → Render

Es lo que ya está configurado en `docs/DEPLOY-RENDER.md`. Una vez publicada, la URL se
abre desde cualquier navegador: la Mac, el celular, la máquina del profesor. No hace falta
instalar absolutamente nada.

**Limitación:** no podés editar código. Es la aplicación corriendo, no un entorno de
trabajo.

### Programar en el navegador → GitHub Codespaces

Levanta un contenedor Linux en la nube con el SDK de .NET 10 ya instalado, y te da VS Code
dentro del navegador. Editás, compilás, corrés y depurás sin instalar nada en la Mac.

El repositorio ya tiene el `.devcontainer/devcontainer.json` que define ese entorno: SDK
10.0, Docker adentro, las extensiones de C# y el puerto 5141 publicado automáticamente.

**Cómo se usa:**

1. En GitHub, en la página del repositorio: botón verde **Code** → pestaña **Codespaces**
   → **Create codespace on feature/backend-mantia**.
2. Esperar unos minutos la primera vez (arma el contenedor y restaura los paquetes).
3. En la terminal integrada:
   ```bash
   dotnet run --project src/MantIA.WEB
   ```
4. Codespaces detecta el puerto 5141 y ofrece abrirlo. La URL que da es HTTPS y soporta
   WebSockets, así que Blazor Server funciona normalmente.

**Lo que hay que saber del plan gratuito:** las cuentas personales de GitHub Free incluyen
**120 horas-núcleo de cómputo y 15 GB-mes de almacenamiento**. Ojo con la unidad: se mide
en horas-núcleo, así que una máquina de 2 núcleos consume 2 unidades por hora y esas 120
horas equivalen a unas **60 horas reales de uso mensual**. El codespace se apaga solo
después de un rato de inactividad, pero conviene detenerlo a mano al terminar. Si se agota
la cuota y no hay tarjeta cargada, el uso se bloquea hasta el mes siguiente.

### Cuál conviene

| Necesidad | Opción |
|---|---|
| Que el profesor vea la app el martes | Render |
| Mostrarla vos desde la Mac sin instalar nada | Render |
| Escribir código desde la Mac sin instalar nada | Codespaces |
| Trabajar seguido, muchas horas por semana | Instalar el SDK local (secciones 1 a 4) |

Las tres conviven sin problema: el repositorio es el mismo.
