# Publicar la demo de MantIA en Render

Objetivo: una URL pública que el profesor abra desde su máquina el martes, sin instalar
nada. Render se eligió porque no pide tarjeta, toma el `Dockerfile` tal cual está y no
tiene tope de conexiones WebSocket, que es lo que usa Blazor Server.

---

## 1. Antes de empezar

El repositorio tiene que estar pusheado con estos archivos, que ya están escritos:

- `render.yaml` — la configuración del servicio
- `src/MantIA.WEB/Dockerfile` — cómo se construye la imagen
- `.dockerignore`

---

## 2. Crear el servicio

1. En Render: **New → Blueprint**.
2. Elegir el repositorio `exerobledo-personal/MantIA`. Si es la primera vez, Render pide
   autorización a GitHub.
3. Render lee `render.yaml` y muestra un servicio llamado **mantia-demo**. No hay que
   cargar nada a mano: rama, Dockerfile, variables de entorno y health check ya vienen
   definidos ahí.
4. **Apply**.

El primer build tarda entre 5 y 10 minutos, porque descarga el SDK de .NET 10 y restaura
los paquetes. Los siguientes son más rápidos por la caché de capas.

Al terminar queda una URL del estilo `https://mantia-demo.onrender.com`.

---

## 3. Verificar que quedó bien

1. Abrir la URL: tiene que aparecer la pantalla de acceso.
2. Entrar y recorrer dos o tres pantallas. Si la interfaz responde a los clics, el
   WebSocket está funcionando.
3. Abrir una ficha de máquina y apretar F5. Tiene que volver a cargar la misma ficha, no
   una pantalla de "no encontrado".
4. Abrir el mapa de plantas y confirmar que se ven las calles debajo de los marcadores.

Si algo falla, el log del build y el de la aplicación están en la pestaña **Logs** del
servicio.

---

## 4. Subdominio propio

El dominio está en Squarespace y la landing en Vercel. No hace falta tocar ninguna de las
dos: se agrega un subdominio nuevo que apunta a Render y la landing sigue igual.

1. En Render, dentro del servicio: **Settings → Custom Domains → Add**. Cargar por ejemplo
   `mantia.zylox.com` (ajustar al dominio real).
2. Render devuelve un valor de CNAME.
3. En Squarespace: **Settings → Domains → el dominio → DNS Settings → Add record**.
   - Type: `CNAME`
   - Host: `mantia`
   - Data: el valor que dio Render
4. Esperar la propagación (habitualmente minutos, a veces hasta una hora). Render emite el
   certificado HTTPS solo.

El plan gratuito incluye dos dominios propios.

---

## 5. Las dos limitaciones del plan gratuito

**Se duerme a los 15 minutos sin tráfico y tarda cerca de un minuto en despertar.**
Antes de presentar, abrir la URL y dejar una pestaña viva. Es la única precaución que
realmente importa.

**Hay 750 horas de instancia por mes.** Un solo servicio encendido todo el mes no las
supera, así que no es un problema salvo que se creen varios.

---

## 6. Sobre la latencia

Render no tiene región en Sudamérica; la más cercana es Ohio. Blazor Server manda cada
clic al servidor y espera la respuesta, así que se van a notar entre 120 y 150 ms de
demora por interacción. Es perfectamente usable para recorrer la aplicación, pero no se
va a sentir tan instantáneo como en local.

Si esa diferencia llegara a molestar, la alternativa es Azure App Service en la región
`brazilsouth`, que baja la latencia a unos 30 ms. Está documentado en
`docs/DEPLOY-AZURE.md`, con la contra de que el plan gratuito de Azure banca sólo cinco
conexiones WebSocket simultáneas y pide tarjeta al crear la cuenta.

---

## 7. Qué NO hace falta para esta demo

Ni PostgreSQL, ni MongoDB, ni N8N, ni Auth0 configurado. La maqueta tiene sus datos en
memoria. Las variables de Auth0 están puestas con valores de relleno sólo porque sin ellas
la aplicación no llega a arrancar.
