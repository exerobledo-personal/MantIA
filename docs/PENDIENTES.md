# Pendientes de definir

Lo que **no** está implementado y necesita una decisión antes de escribir código. Lo ya decidido está
en `DECISIONES.md`.

| # | Qué falta | Urgencia |
|---|---|---|
| P-25 | Vincular dos formas de entrar de la misma persona | Con P-21 |
| P-26 | Modelo de facturación | Después de los primeros cobros |
| P-28 | Inventario, depósito y proveedores (doc propio) | Con la capa de operación |
| P-29 | Registrar MantOops en INPI, clase 42 | Antes de vender |
| P-27 | Marca parametrizable de plataforma y de cliente (D-82) | Con el panel |
| P-15 | Construir el panel de plataforma | Antes del primer cliente real |
| P-22 | Roles dentro del panel de plataforma | Con el equipo comercial |
| P-21 | Política de contraseñas, recuperación y doble factor | Antes de vender |
| P-20 | Versionado de documentos | Conversación dedicada |
| P-12 | Dónde viven los archivos en producción | Antes del primer cliente real |
| P-19 | Aviso de mora 30/60/90 y purga manual de tenants | Antes del primer cliente real |
| P-16 | Envío del correo de invitación | Antes de que un cliente sume gente solo |
| P-17 | Sitio institucional, landing y prospectos | Depende de P-24 |
| P-13 | ¿`Documentos` es un recurso propio de permisos? | Con P-20 |
| P-18 | Enganchar el control de cupos | Cuando exista la lógica de operación |
| P-14 | Extracción OCR de los certificados | Etapa 2 |
| P-03 | Escala de máquinas por planta | Con los primeros clientes |
| P-05 | Qué dispara el estado suspendido | Cuando exista el plan comercial |

Resueltos: P-01, P-02, P-04, P-06 a P-11.

---

### P-25 · Vincular dos formas de entrar de la misma persona

Si alguien entra con el SSO de su empresa y después crea una contraseña con el mismo correo, el
proveedor de identidad le asigna **dos identificadores distintos**. Para el sistema son dos personas,
y el índice único la va a rechazar en el segundo intento.

Dos salidas: obligar a una sola forma de entrar por persona —simple y molesto— o vincular las dos
identidades a un mismo usuario, que es lo que corresponde pero hay que construirlo. Se decide junto
con P-21.

### P-26 · Modelo de facturación

Hoy no existe: la escalera de mora se cuenta desde el vencimiento de la vigencia, que alcanza para
arrancar. Cuando haya cobranza real hacen falta facturas como entidad, con su estado de pago, y la
mora pasa a contarse desde ahí. Es otro módulo y puede esperar.

### P-27 · Marca parametrizable

D-82 fijó que ni el nombre, ni el logo, ni los colores van en el código. Falta construir: la
configuración de marca de la plataforma, y la carga de logo y marca de cada empresa cliente desde
"opciones avanzadas" al darla de alta. El almacén de documentos ya resuelve el guardado de los
archivos.

### P-28 · Inventario, depósito y proveedores

Diseño completo en `INVENTARIO-Y-DEPOSITO.md`. Lo que falta decidir de ahí: la moneda del historial
de precios, si el mínimo de stock se implementa también por depósito, si el plan acota la cantidad de
artículos, y quién puede ver los precios de compra — que es información comercial y probablemente
merezca una acción propia.

**El cambio estructural que trae:** el stock deja de ser un número por repuesto y pasa a ser por par
repuesto-depósito. Eso toca el libro mayor y obliga a sumar el depósito al catálogo de campos
sellados.

### P-29 · Registrar la marca en INPI

`mantoops.com` y `.com.ar` ya están registrados, pero el dominio no da ningún derecho sobre el
nombre. La marca en clase 42 sí. El caso de MantIA Industrial® muestra exactamente el costo de no
hacerlo a tiempo.

### P-15 · Construir el panel de plataforma

Aplicación aparte, subdominio propio, expuesta pero endurecida (D-76). Alta de empresas con Usuario 0
y cambio de plan ya están construidos como servicios: falta la aplicación y las pantallas. También
van acá la vista de integridad, la bitácora de plataforma, la purga de tenants y la ingesta del
catálogo.

Depende de P-22, porque el reparto de permisos cambia qué pantallas ve cada uno.

### P-22 · Roles dentro del panel de plataforma

Si el panel lo va a usar un equipo comercial, `SuperAdminMantIA` como rol único deja de alcanzar: no
todo el que da de alta un cliente debería poder purgar un tenant ni usar el bypass sobre datos de
clientes.

Mínimo dos perfiles: **comercial** —alta de empresas, cambio de plan, prospectos, ver cuentas— y
**plataforma** —purga, integridad, bypass, ingesta del catálogo—. Falta decidir si son roles nuevos
del sistema o niveles dentro del actual.

### P-21 · Usuario y contraseña además de SSO

No todos los clientes van a entrar con Google. Auth0 soporta conexiones de usuario y contraseña
conviviendo con las sociales, así que técnicamente es configuración.

**El modelo de acceso no cambia**: se invita por correo y la identidad se ata en el primer ingreso.
Lo único que cambia es de dónde sale el identificador.

**Pero hay una consecuencia que no es negociable.** Con Google, el correo llega verificado por Google.
Con una conexión de contraseña, la persona escribe el correo que quiere: si no se exige verificación
antes de consumir la invitación, cualquiera puede reclamar la invitación de otro con solo saber la
dirección, y todo el modelo de acceso se cae. **Verificación de correo obligatoria** para esa
conexión, sin excepción.

Faltan además política de contraseñas, recuperación, y si el doble factor es obligatorio o sugerido.

### P-20 · Versionado de documentos

Conversación dedicada, con amendment aparte. Lo que conviene tener presente al tenerla:

**Una parte ya está resuelta sin querer.** El almacén direcciona por el SHA-256 del contenido: subir
un archivo distinto produce otra ruta, así que **es imposible pisar un archivo existente**. Lo viejo
sobrevive por construcción y no por disciplina.

**Lo que falta es la cadena de versiones en la ficha.** Hoy un documento apunta a un contenido y
punto: si se sube una corrección, queda un documento nuevo suelto y nada dice que reemplaza al
anterior. Falta que cada versión apunte a la que reemplaza, que la lista muestre la vigente con su
historial atrás, y que quede registrado quién reemplazó qué y por qué.

**Sobre guardar los archivos en Postgres:** se puede, pero infla cada copia de respaldo con binarios
que no cambian nunca, castiga cualquier consulta que traiga la fila entera y complica mudarse
después. La extensión de vectores no tiene nada que ver con esto: sirve para búsqueda semántica del
*texto* extraído, que sí va en la base.

### P-12 · Dónde viven los archivos en producción

Apareció una idea nueva: que los archivos vivan en un servidor del propio cliente, con la aplicación
en la nube resolviendo cada tenant.

**Se puede, y no lo recomiendo.** La aplicación en la nube tendría que alcanzar una máquina dentro de
la red del cliente, lo que obliga a abrir un camino de entrada a su red o a montar un túnel por
cliente; los respaldos pasan a ser responsabilidad de ellos y el reclamo cuando se pierda un archivo
va a llegar igual; y cada instalación se vuelve un caso distinto de soportar. Además, como igual
tienen que llegar al catálogo compartido y al servidor de modelo, la conectividad no se evita: solo
se agrega una pieza más que puede fallar.

**Recomiendo almacenamiento de objetos en la nube, un solo despliegue.** Lo inhouse queda como opción
empresarial mucho más adelante, para un cliente grande que lo exija por política, y cotizado aparte
porque cuesta soporte real.

### P-19 · Aviso de mora y purga de tenants

**El aviso es una escalera, no un mensaje.** Correo automático a los 30, 60 y 90 días de mora, más el
de baja definitiva, con contenido editable desde la aplicación. El texto se define con un abogado
cuando esté cerrada la forma del contrato.

**Falta decidir contra qué se cuentan esos días.** Hoy la empresa tiene una fecha de fin de vigencia y
nada más: no hay modelo de facturación, así que no existe "factura impaga". Se puede contar desde el
vencimiento de la vigencia, que no cuesta nada y alcanza para arrancar, o construir facturas como
entidad, que es correcto pero es otro módulo. **Recomiendo lo primero** hasta que haya cobranza real.

**La purga es un botón y una decisión personal.** "Eliminar definitivamente el tenant", manual y
arbitraria, nunca automática (D-55). Lo que sí se automatiza es el aviso interno: "este tenant
consume recursos y no registra órdenes hace X". Conviene exportarle lo cargado antes de borrar.

### P-16 · Envío del correo de invitación

Falta el proveedor —conviene que sea el mismo por el que salgan las alertas de stock, para no sumar
dos servicios— y depende de P-24 para el dominio del remitente. Un correo de invitación que sale de
una casilla de Gmail personal no ayuda a vender.

### P-17 · Sitio institucional, landing y prospectos

El sitio como despliegue aparte, la entidad de prospecto con los estados del embudo, y el formulario
de cotización con sus protecciones — va a ser la única escritura a la base expuesta a internet
abierto de todo el sistema. Bloqueado por P-24.

### P-13 · ¿`Documentos` es un recurso propio de permisos?

Se resuelve junto con P-20. Hoy usan los permisos de `Maquinas`. Lo mismo aplica a `Integridad`, que
se registra en la bitácora pero no existe como recurso protegible, así que hoy **no hay pantalla
posible** para ver los hallazgos.

### P-18 · Enganchar el control de cupos

`IControlCupos` limita cuántas máquinas, usuarios, plantas y órdenes abiertas puede tener una empresa
según su plan. Está construido y **no lo llama nadie**, porque no existen todavía los servicios de
alta a los que engancharlo. Se resuelve cuando esté la lógica de operación.

### P-14 · Extracción OCR de los certificados

Etapa 2. Subida, extracción en N8N con OCR, validación humana antes de que el texto entre al motor.

### P-03 · Escala de máquinas por planta

Se ajusta con datos de los primeros clientes. Lo que hay que evitar desde ya: pantallas que solo
funcionen con el extremo bajo del rango. Los listados nacen con paginación del lado del servidor
aunque hoy se prueben con quince filas.

### P-05 · Disparadores del estado suspendido

D-33 dejó el modo solo lectura y D-74 sumó el vencimiento de vigencia. Falta la parte comercial, que
se resuelve junto con P-19.

---

## Cambios que NO se hicieron y por qué

| Qué | Por qué no |
|---|---|
| **Capacidad offline** | Decisión tuya: es un proyecto web. Contradice §2.1.4 del documento de visión, que además contradice a §1.9 |
| **Bloqueo pesimista para el stock** | Se resolvió con libro mayor (D-30). Bloquear filas serializa y no escala |
| **Enums nativos de PostgreSQL** | Cada cambio exige `ALTER TYPE` manual fuera de transacción |
| **Matriz de permisos por defecto** | Cada cliente define la suya. Se reemplazó por el piso irrevocable (D-25) |
| **Cifrar la bitácora entera** | D-26: la integridad importa más. Se cifra campo por campo (D-46) |
| **Interceptores de EF para atar el cifrado a la fila** | Se hizo el DV de fila en tabla aparte (D-61) |
| **Retención de bitácora por severidad** | Decisión tuya: siempre queda el registro |
| **Registro público de empresas** | D-66: detrás de cada cliente hay un contrato |
| **Alta automática por dominio** | D-67: el dominio acota a quién se puede invitar, no da acceso |
| **Una identidad en varias empresas** | D-68: dos cuentas para probar, a cambio de no operar nunca sobre el tenant equivocado |
| **VPN o lista blanca para el panel** | D-76: no escala a un equipo comercial. Queda como endurecimiento futuro |
| **Tope histórico de órdenes** | D-77: una empresa ve todas las órdenes que creó, sean de hoy o de hace cinco años |
