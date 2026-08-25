# Pendientes de definir

Lo que **no** está implementado y necesita una decisión antes de escribir código. Lo ya decidido está
en `DECISIONES.md`.

| # | Qué falta definir | Urgencia |
|---|---|---|
| P-12 | Dónde viven los archivos en producción: disco o almacenamiento de objetos | Antes del primer cliente real |
| P-13 | ¿`Documentos` es un recurso propio de permisos o va con `Maquinas`? | Antes de la demo |
| P-14 | Extracción OCR de los certificados (etapa 2, ya diseñada en D-64) | Después del MVP |
| P-15 | Construir el panel de plataforma como aplicación aparte (D-70) | Antes del primer cliente real |
| P-16 | Envío del correo de invitación | Antes de que un cliente sume gente solo |

Resueltos y movidos a `DECISIONES.md`: P-01 dimensión del vector, P-02 umbral de promoción,
P-03 escala por planta, P-05 estados de morosidad, P-06 numeración, P-07 usuario de demostración,
P-08 bitácora caída, P-09 permisos por ámbito, P-10 huecos en la cadena, **P-11 dígitos
verificadores (D-61, D-62, D-63)**, **P-04 documentos de máquina (D-64, D-65)**.

---

### P-12 · Dónde viven los archivos en producción

Hoy está `AlmacenDocumentosLocal`: sistema de archivos, direccionado por hash de contenido. Alcanza
para desarrollo y para un despliegue en un solo servidor.

**Con más de una instancia deja de alcanzar**, porque el archivo que subió una no está en el disco de
la otra. Las opciones son un volumen compartido —simple, pero es un punto único de falla y hay que
respaldarlo aparte— o almacenamiento de objetos tipo S3 o Azure Blob, que resuelve replicación y
respaldo pero suma una dependencia y un costo por GB.

La interfaz `IAlmacenDocumentos` ya está pensada para que el cambio sea una clase nueva y una línea
de registro. **No hace falta decidirlo hoy**, pero sí antes de que un cliente real cargue documentos:
migrar archivos después es incómodo.

**Dos cosas que no dependen de esa decisión y ya están:** la raíz configurable tiene que quedar fuera
de `wwwroot` —si cae adentro, los archivos son descargables sin pasar por permisos— y el hash del
contenido se verifica antes de cada descarga.

### P-13 · ¿`Documentos` es un recurso propio de permisos?

Ahora mismo los documentos usan los permisos de `Maquinas`: quien puede consultar la máquina ve sus
papeles, quien puede modificarla los adjunta. Es defendible —el documento es un atributo del activo—
y tiene la ventaja de no agregar celdas a la matriz.

**El argumento en contra:** un certificado de proveedor tiene información comercial que no todo el
que consulta una máquina necesariamente debería ver, y adjuntar un papel no es lo mismo que cambiar
la criticidad del equipo.

Si va como recurso propio, hay que agregarlo a `CatalogoPermisos` con ámbito Operación, decidir el
piso irrevocable por rol en `PermisosMinimos` y regenerar la matriz por defecto. **Decisión tuya**:
si decís que sí, lo hago; si no, queda como está y se documenta el criterio.

Lo mismo aplica a `Integridad`: hoy los hallazgos se registran en la bitácora con ese recurso pero no
existe como recurso protegible, así que **no hay pantalla para verlos**. Cuando la haya, necesita una
celda de permiso, y el candidato natural es que solo la vea AdminEmpresa y el SuperAdmin.

### P-15 · Construir el panel de plataforma

D-70 fijó el dónde: aplicación aparte, subdominio propio, fuera de internet público, doble factor
obligatorio y sin sesión compartida con la web de clientes. Falta construirlo.

Lo que tiene que poder hacer, y nada más que eso: dar de alta empresas con su Usuario 0 —el servicio
ya está, `IServicioAltaEmpresa`—, suspender y reactivar clientes, ver la bitácora de plataforma,
verificar la integridad de los dígitos, e ingerir el catálogo compartido. **No** opera datos de
clientes: para eso está el bypass, que ya se audita como crítico.

Queda por definir la infraestructura: si va VPN o lista blanca de IP, y si el subdominio se resuelve
con el mismo despliegue o con uno separado. Depende de dónde termine hosteado.

### P-16 · Envío del correo de invitación

Hoy la invitación se crea en la base y no le llega nada a la persona: hay que avisarle por afuera.
Para probar alcanza, pero un cliente que sume diez personas necesita que el sistema mande el correo.

Falta elegir el proveedor —el candidato natural es el mismo por el que salgan las alertas de stock,
para no sumar dos— y decidir si el correo lleva un enlace con token de un solo uso o simplemente
avisa "ya podés entrar con tu cuenta de Google". **Lo segundo es más seguro y más simple**: no hay
token que robar ni que expirar, porque la habilitación ya vive en la base y la identidad la prueba
Google.

### P-14 · Extracción OCR de los certificados

Sigue vigente lo diseñado: subida → extracción en N8N con OCR → validación humana en pantalla de dos
columnas antes de que el texto entre al motor. La entidad ya tiene dónde apoyarse; falta el flujo y la
segunda pantalla.

**Por qué la validación no es opcional.** Un certificado escaneado y pasado por OCR trae errores, y
ese texto va a terminar influyendo en qué repuestos recomienda el sistema. Que un dato sin revisar
entre al motor es exactamente lo que hace perder credibilidad a las recomendaciones.

**Cuando se haga:** el texto extraído es texto libre escrito por terceros, así que entra en
`CamposCifrados` como aleatorio, igual que las descripciones de orden.
