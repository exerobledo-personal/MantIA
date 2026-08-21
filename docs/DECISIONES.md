# Bitácora de decisiones de lógica y arquitectura

Registro de todo cambio que altere **cómo se comporta el sistema**, no solo cómo está escrito.
Existe para que puedas revisar en un solo lugar lo que se decidió, y para separar con claridad lo
que vos definiste de lo que se resolvió sin consultarte.

## Cómo se usa

Cada entrada tiene un estado:

| Estado | Significado |
|---|---|
| **Aprobado** | Lo decidiste vos explícitamente. No se cambia sin que lo pidas. |
| **Revisar** | Se tomó sin consultarte porque no bloqueaba el avance. Está implementado y es reversible. Si alguna no te cierra, se revierte. |
| **Pendiente** | **No** está implementado. Necesita que decidas antes de escribir código. |

Regla de trabajo: **nada que cambie una regla de negocio se implementa en estado Pendiente.** Lo que
entra como *Revisar* es siempre reversible y está acotado a una capa; lo que toca el contrato del
negocio se pregunta antes.

Cuando revises una entrada, cambiale el estado a *Aprobado* o pedime que la revierta.

---

## Índice de lo que necesita tu atención

Si solo vas a leer una parte, leé esta.

| # | Decisión | Estado |
|---|---|---|
| D-03 | La baja lógica oculta las filas por defecto en todo el sistema | **Aprobado** — 21/08 |
| D-12 | Dos plantas de la misma empresa no pueden llamarse igual | **Aprobado** — 21/08 |
| D-15 | Un permiso de nivel específico gana sobre el genérico del rol | **Aprobado** — 21/08, y se extendió a permisos por usuario (D-34) |
| D-18 | Un usuario de una empresa dada de baja no puede entrar | **Aprobado** con modo suspendido (D-33) |
| D-25 | Contenido concreto del piso de permisos irrevocables | Revisar — 14 celdas sin validar |
| D-27 | Qué campos se omiten y cuáles se enmascaran en la bitácora | **Resuelto** — la clasificación ahora depende del alcance |
| D-32 | Baja de usuarios: lógica con identidad liberada | **Aprobado** — 21/08 |
| D-34 | Permisos nominales por usuario | Revisar — leé las tres reglas anti-filtración |
| D-39 | Administración de permisos repartida por ámbito | **Aprobado** — 21/08 |
| D-41 | La bitácora no vence nunca | **Aprobado** — revirtió mi retención por severidad |
| D-44 | Respaldo en el tenant y cola de drenaje | **Aprobado** — 21/08 |
| P-01 | Dimensión del vector de embeddings | **Resuelto: 768** |
| P-02 | Umbral de promoción | **Resuelto: tres niveles** |
| P-03 | Escala de máquinas por planta | Diferido — se ajusta con datos y se recalculan costos |
| P-04 | Certificados de mantenimiento de proveedores | Diferido a etapa 2 |
| P-10 | Hueco permanente en la cadena de sellos | **Pendiente** — leelo, es corto |

---

## 20/08/2026 — Capa de acceso a datos

### D-01 · Configuraciones agrupadas por ámbito · *Revisar*

Las 22 configuraciones de EF Core viven en tres archivos (`PlataformaConfigurations.cs`,
`EmpresaConfigurations.cs`, `OperacionConfigurations.cs`) en vez de un archivo por entidad. Una clase
por entidad se mantiene; solo cambia cómo se agrupan los archivos.

**Por qué:** 22 archivos de 15 líneas cada uno se vuelven difíciles de recorrer, y el agrupamiento
por ámbito refleja la misma división que el modelo de seguridad.

**Impacto:** ninguno funcional. Es organización de código.

### D-02 · Tres reglas se aplican por convención, no entidad por entidad · *Revisar*

El aislamiento por empresa, la baja lógica y el control de concurrencia los aplica el `DbContext`
recorriendo el modelo, no una línea escrita en cada entidad.

**Por qué:** si dependieran de que alguien se acuerde de configurarlas al agregar una entidad, la
primera que se olvide filtra datos de un cliente a otro. Una entidad nueva las hereda por el solo
hecho de derivar de `TenantEntity` o implementar `IBajaLogica` / `IConcurrencia`.

**Impacto:** para agregar una entidad privada de empresa, alcanza con derivar de `TenantEntity`.
Contrapartida: la configuración no está visible leyendo la entidad; hay que saber que existe la
convención. Está documentada en el propio `MantIADbContext`.

### D-03 · La baja lógica oculta las filas por defecto · **Aprobado**

Toda entidad con `IBajaLogica` lleva un filtro global: las filas dadas de baja **no aparecen** en
ninguna consulta salvo que se pidan explícitamente.

**Alcance:** `Empresa`, `Plan`, `Planta`, `Usuario`, `NivelPermiso`, `Maquina`, `Repuesto`.

**Por qué te lo marco:** es cómodo para las pantallas de trabajo y peligroso para los reportes. Una
orden de trabajo histórica que apunta a una máquina dada de baja va a cargar con la máquina en nulo
si nadie se acuerda de pedir las bajas. Como el producto se trata justamente de análisis histórico,
esto puede morder.

**Mitigación:** el filtro tiene nombre (ver D-04), así que una consulta de historial escribe
`IgnoreQueryFilters([MantIADbContext.FiltroBaja])` y recupera las bajas **sin** perder el
aislamiento por empresa.

**La alternativa era** no filtrar nada y obligar a cada consulta a escribir `Where(x => x.FechaBaja == null)`,
con el mismo problema de "el primero que se olvide" pero al revés: mostraría bajas donde no debe.

### D-04 · Filtros globales con nombre · *Revisar*

Los dos filtros se registran con clave: `"tenant"` y `"baja_logica"`.

**Por qué:** `IgnoreQueryFilters()` sin argumentos apaga **todos** los filtros. Con un solo filtro
anónimo, cualquier consulta que necesitara ver las bajas también habría quedado sin aislamiento de
empresa. Es una fuga de datos entre clientes esperando a pasar.

**Encontró un error real:** el `TenantResolver` ya hacía `IgnoreQueryFilters()` sin argumentos para
buscar el usuario antes de conocer la empresa. Al agregar el filtro de baja lógica sobre `Usuario`,
eso habría dejado entrar a un usuario dado de baja. Corregido en dos lugares: `TenantResolver` y el
`OnTicketReceived` de `MantIA.WEB/Program.cs`.

### D-05 · `ICurrentTenant` ahora también expone `UsuarioId` · *Revisar*

**Por qué:** `BaseEntity` tiene `CreadoPorUsuarioId` y `ModificadoPorUsuarioId` desde el rediseño de
la BE, pero nada los completaba. Ahora el `DbContext` los sella en `SaveChanges` leyendo el usuario
del contexto.

**Impacto:** `TenantResolver` setea el usuario junto con la empresa. En procesos desatendidos
—ingesta del catálogo, corridas del modelo— queda nulo, y eso es información útil: distingue un
cambio hecho por una persona de uno hecho por el sistema.

### D-06 · La fecha de creación no se reescribe al modificar · *Revisar*

En una entidad modificada, `FechaCreacion` y `CreadoPorUsuarioId` se marcan como no modificados
aunque vengan con otro valor desde afuera.

**Por qué:** un modelo de vista que llegue de la interfaz con esos campos en cero borraría el origen
del registro sin que nada avise. Es el tipo de pérdida que se descubre meses después.

### D-07 · La columna de Auth0 se llama `auth0_user_id` · *Revisar*

Única columna con nombre explícito en todo el modelo: la convención `snake_case` corta
`Auth0UserId` como `auth0user_id` porque el `0` rompe la detección de mayúsculas.

**Nota:** ya habías tenido este problema — existía una migración vieja llamada
`FixAuth0UserIdColumn`. Ahora queda resuelto en la configuración, no en un parche.

### D-08 · Distancia coseno y no euclidiana para los embeddings · *Revisar*

El índice HNSW usa `vector_cosine_ops`.

**Por qué:** para comparar textos, lo que importa es la dirección del vector, no su magnitud. Dos
descripciones de la misma falla, una de tres palabras y otra de treinta, tienen magnitudes muy
distintas y la misma orientación semántica. Con distancia euclidiana la más larga parecería siempre
más lejana.

**Impacto:** las consultas de similitud tienen que usar el operador `<=>`. Si alguna usa `<->`, el
índice no se aplica y la búsqueda cae a recorrido completo sin avisar.

### D-09 · Precisión decimal fijada por campo · *Revisar*

| Campo | Tipo | Por qué |
|---|---|---|
| `Planta.Latitud` / `Longitud` | `numeric(9,6)` | Seis decimales son ~11 cm. Alcanza para ubicar una planta y es más barato que un tipo geográfico |
| `Repuesto.CostoUnitario`, `OrdenTrabajoRepuesto.CostoUnitarioAlConsumo`, `Plan.PrecioMensual` | `numeric(14,2)` | Dinero. Dos decimales, hasta 12 dígitos enteros |
| `Recomendacion.Confianza` | `numeric(4,3)` | Valor entre 0 y 1 con tres decimales |
| `OrdenTrabajo.HorasResolucion` | `numeric(8,2)` | Horas con fracción |

**A revisar:** si en algún momento se factura en una moneda con más de dos decimales, `numeric(14,2)`
queda corto. Hoy `Plan.Moneda` está fijo en ARS.

### D-10 · Borrado restringido salvo en relaciones de composición · *Revisar*

Regla por defecto: `Restrict`. Nada se borra en cascada, porque el historial operativo tiene que
sobrevivir a la baja del registro que lo originó.

Excepciones deliberadas, donde el hijo no tiene sentido sin el padre:

| Relación | Comportamiento | Por qué |
|---|---|---|
| `CatalogoMaquina` → fallas y repuestos sugeridos | Cascade | Se reescriben completos en cada reingesta |
| `OrdenTrabajo` → sus líneas de repuesto | Cascade | La línea no existe fuera de su orden |
| `Usuario` → su alcance de plantas | Cascade | El alcance es un atributo del usuario |
| `Maquina` / `Repuesto` → su vínculo `MaquinaRepuesto` | Cascade | Es una relación, no una entidad con vida propia |
| `NivelPermiso` → sus celdas de la matriz | Cascade | **Ver abajo** |

**El último te lo marco:** borrar un nivel de permiso borra todas sus celdas de la matriz. Como
`NivelPermiso` tiene baja lógica, en la práctica nunca se borra físicamente, así que la cascada no
debería dispararse nunca. Está por si alguien borra a mano.

### D-11 · Índices elegidos por consulta concreta · *Revisar*

Cada índice responde a una pantalla o proceso específico, no a "por las dudas". Los que implican una
**regla de negocio** son los únicos que cambian comportamiento:

| Índice único | Regla que impone |
|---|---|
| `empresa + codigo` en Máquina | El código interno no se repite dentro de la empresa |
| `empresa + numero_parte` en Repuesto | Un número de parte identifica un solo repuesto |
| `empresa + numero` en Orden de trabajo | El número de OT no se repite |
| `marca + modelo` en Catálogo | Una sola ficha por modelo. Sostiene el efecto de red |
| `dominio` y `tenant_id` en Empresa | Únicos en toda la plataforma |
| `auth0_user_id` en Usuario | Único global: una persona es un solo usuario del sistema |
| `usuario + planta` en Alcance | No se asigna dos veces la misma planta |
| `orden + repuesto` en línea de OT | Un repuesto aparece una vez por orden |
| `empresa + rol + nivel + recurso + acción` en la matriz | **Una sola celda por combinación** |

El último importa más de lo que parece: sin él, dos filas contradictorias para el mismo rol harían
que el permiso dependa del orden en que la base devuelva las filas.

### D-12 · Dos plantas de la misma empresa no pueden llamarse igual · **Aprobado**

Índice único `empresa + nombre` en `Planta` y en `NivelPermiso`.

**Por qué te lo marco:** es una regla de negocio que nunca planteaste. Es razonable —dos plantas
llamadas "Planta Norte" son indistinguibles en cualquier selector— pero una empresa con plantas en
ciudades distintas podría legítimamente querer repetir un nombre. Si te parece de más, se quita.

### D-13 · La solicitud de rollback vive en PostgreSQL, no en MongoDB · *Revisar*

**Por qué:** la bitácora va a Mongo porque es escritura secuencial de alto volumen y esquema
variable. Una solicitud de rollback es lo contrario: una entidad transaccional, con estados,
aprobación de dos personas y claves foráneas a usuarios. Es un flujo de trabajo, no un evento.

### D-14 · Orden de evaluación de un permiso · *Revisar*

Cuatro pasos, y el orden es la decisión:

1. **Superadministrador de MantIA** pasa por encima de todo. Excepción explícita, se audita aparte.
2. **Frontera estructural** (`CatalogoPermisos.EsCombinacionValida`). Si la combinación no es válida
   para el rol, se deniega **sin mirar la matriz**.
3. **Piso irrevocable** (`PermisosMinimos.EsMinimo`). Se concede aunque la matriz diga lo contrario.
4. **Matriz** configurada por la empresa.

**Por qué ese orden:** el paso 2 va antes que todo lo configurable para que ninguna edición de la
matriz pueda darle a un `AdminEmpresa` la capacidad de cerrar una orden de trabajo. El paso 3 va
antes que la matriz para que una fila con `Concedido = false` que llegue por una migración vieja o
por SQL directo no deje al tenant sin salida.

### D-15 · Precedencia del nivel sobre el rol · **Aprobado**

Si existe una celda para el nivel exacto del usuario y otra genérica del rol, gana la del nivel.

**Por qué te lo marco:** es una regla nueva. Antes el código exigía coincidencia exacta de nivel, con
lo cual "Supervisor puede consultar" y "Supervisor Jr no puede consultar" daban un resultado que
dependía del orden en que la base devolviera las filas. Había que decidir algo; elegí que lo más
específico gane, que es lo que hace cualquier sistema de permisos. Confirmalo.

### D-16 · La caché de permisos se invalida al guardar · *Revisar*

`IPermisoService` ahora expone `Invalidar(empresaId)`. La expiración de 10 minutos queda como red de
contención, no como el mecanismo de propagación.

**Por qué:** es lo que sostiene el modelo de "el permiso se evalúa en el momento de la acción". Si el
cambio dependiera del vencimiento de la caché, habría hasta 10 minutos en los que un permiso quitado
sigue funcionando.

### D-17 · Un usuario dado de baja no tiene permisos · *Revisar*

`UsuarioActual.PuedeAsync` no ignora el filtro de baja lógica: si el usuario está dado de baja, la
consulta no lo encuentra y el permiso se deniega.

### D-18 · Un usuario de una empresa dada de baja no puede entrar · **Aprobado**

El `TenantResolver` distingue tres casos: la empresa no existe, la empresa está dada de baja, o el
dominio del correo no corresponde. El segundo devuelve un mensaje propio y **bloquea el acceso**.

**Por qué te lo marco:** corta el acceso de un cliente entero. Es lo correcto —una cuenta dada de
baja no debería seguir operando— pero conviene definir si hay un período de gracia de solo lectura
para que el cliente exporte sus datos antes de perder el acceso. Hoy no lo hay.

### D-19 · `UseVector` en el arranque de WEB y API · *Revisar*

Sin esto la aplicación genera la tabla bien pero **falla al leer la columna de embeddings en tiempo
de ejecución**, que es la peor forma de descubrirlo.

### D-20 · Migraciones reiniciadas · **Aprobado**

Se borraron las seis migraciones de junio. Una sola `ModeloInicial` con las 22 entidades.

Desde acá rige la regla: **una migración aplicada no se edita nunca más.**

### D-21 · Enums guardados como texto · **Aprobado**

`HasConversion<string>` sobre `varchar(40)`, aplicado por convención a los 16 enums.

### D-22 · El embedding es `float[]` en la entidad · **Aprobado**

`MantIA.BE` no referencia ninguna librería de persistencia. La DAL convierte a `Vector`.

**Consecuencia concreta a tener presente:** los operadores de distancia de pgvector **no se traducen
desde LINQ** cuando la propiedad pasa por un conversor. Las búsquedas por similitud van con
`FromSql`, y por eso deberían vivir concentradas en un solo repositorio y no repartidas por la capa
de negocio.

---

## 19-20/08/2026 — Seguridad, auditoría y rollback

### D-23 · `AdminEmpresa` opera solo sobre el ámbito Empresa · **Aprobado**

Pedido tuyo, por separación de funciones y riesgo de manipulación de presupuestos.

### D-24 · Consulta fuera de ámbito · *Revisar*

Para que el administrador no quede ciego, `CatalogoPermisos.ConsultaFueraDeAmbitoDe` define una lista
cerrada de recursos operativos que puede **consultar** aunque estén fuera de su ámbito: máquinas,
repuestos, stock, alertas, órdenes, recomendaciones y reportes.

La única acción admitida es `Consultar` — ni siquiera `Exportar`, porque exportar es sacar datos del
sistema y no supervisar. Como la restricción es estructural, ninguna edición de la matriz puede
escalar esa lectura a capacidad de intervención.

**Es mi resolución a tu pedido, no tu pedido.** Vos dijiste que administrar y operar son cosas
distintas; esto es cómo se sostiene eso sin dejar al admin sin visibilidad.

### D-25 · Contenido del piso de permisos irrevocables · *Revisar* ⚠

14 celdas en `PermisosMinimos.cs`, clasificadas por motivo:

| Rol | Celdas irrevocables |
|---|---|
| Empleado | `Ordenes.Consultar`, `Maquinas.Consultar` |
| Supervisor | las anteriores + `Alertas.Consultar` |
| Gerente | `Ordenes.Consultar`, `Reportes.Consultar`, `Recomendaciones.Consultar` |
| AdminEmpresa | `Permisos.Consultar`, `Permisos.Configurar`, `Usuarios.Consultar`, `BitacoraEmpresa.Consultar` |
| SuperAdminMantIA | `BitacoraPlataforma.Consultar`, `Empresas.Consultar` |

**Criterio:** entra solo si quitarlo produce bloqueo (nadie puede volver a concederlo), pérdida de
rendición de cuentas (alguien actúa sin que nadie lo vea) o vaciamiento del rol (el usuario entra y
no puede hacer nada). Cosas que en la práctica casi siempre se van a conceder —que un supervisor
cierre órdenes— quedaron **afuera** a propósito: una lista de mínimos larga es una matriz por defecto
disfrazada.

**Revisá especialmente `Gerente`:** le puse `Recomendaciones.Consultar` como irrevocable porque la
decisión de compra anticipada se toma en ese nivel. Si en tu modelo el que decide la compra es otro
rol, hay que moverlo.

### D-26 · La bitácora se protege con cadena de hash, no con cifrado · *Revisar*

Cada evento guarda `Secuencia`, `HashAnterior` y `Hash`. El hash de cada entrada incluye el de la
anterior: modificar o borrar un evento intermedio rompe la cadena de ahí en adelante.

**Por qué:** para una bitácora, el ataque relevante no es leerla sino **alterarla**. Un registro que
se puede editar sin dejar rastro no sirve como evidencia, esté cifrado o no. El cifrado en reposo lo
provee el motor de base de datos; no es algo que la aplicación deba reimplementar.

### D-27 · Clasificación de datos sensibles · **Corregido** (ver revisión del 21/08)

`Auditoria/DatosSensibles.cs` clasifica campo por campo en público / enmascarado / omitido, y se
aplica **al escribir en la bitácora**, no en la base operativa.

Hoy: se omiten el identificador de Auth0, el costo unitario de repuestos y el precio de los planes;
se enmascara el correo.

**Por qué te lo marco:** lo que se omite **se pierde para siempre** en el registro de auditoría. Si
mañana hace falta auditar quién cambió el costo de un repuesto —que es exactamente el escenario de
manipulación de presupuestos que te preocupaba—, con el costo omitido no se puede reconstruir.
Puede que convenga enmascararlo en vez de omitirlo, o registrar solo la magnitud del cambio.

### D-28 · El rollback nunca borra historial · *Revisar*

Tres reglas: cada acción revertida genera su propio evento de bitácora; la aprobación tiene que venir
de un usuario distinto al que solicita; y una reversión parcial queda en estado `AplicadoParcial`
registrando qué no se pudo revertir, en vez de fallar entera o decir que se aplicó.

### D-29 · Las alertas de stock se persisten, no se calculan · *Revisar*

En la maqueta, `DatosDemo.Alertas()` derivaba las alertas del stock contra el umbral en cada
consulta. En el modelo real, `AlertaStock` es una entidad que se guarda y se marca resuelta.

**Por qué:** hay que poder responder "cuántas veces estuvimos en quiebre el mes pasado" aunque hoy el
stock esté cubierto. Una alerta calculada desaparece sin dejar rastro en cuanto se repone.

**Impacto:** hace falta un proceso que genere y resuelva alertas. No existe todavía.

### D-30 · El stock se lleva por libro mayor · *Revisar*

`MovimientoStock` es inmutable: nunca se edita ni se borra, y un error se corrige agregando un
movimiento de ajuste. `Repuesto.StockActual` es una denormalización que se actualiza en la misma
transacción.

**Por qué:** es la respuesta al problema de concurrencia sin bloquear recursos. Dos operaciones
simultáneas sobre el mismo repuesto insertan filas distintas y no compiten; lo único que se coordina
es el contador. Como la operación de negocio es "sumar N" y no "escribir N", el reintento converge.

**Invariante verificable:** la suma de los movimientos de un repuesto debe dar exactamente su
`StockActual`. Conviene una prueba que lo compruebe.

---

## 21/08/2026 — Auditoría, cifrado y permisos nominales

### D-31 · La bitácora se sella con HMAC, no con hash simple · *Revisar*

`ProtectorDatos.Sellar` usa **HMAC-SHA256** con una llave versionada, en lugar de SHA-256 pelado.

**Por qué cambió respecto de D-26:** un hash simple lo recalcula cualquiera que pueda escribir en la
base. Altera el evento, recalcula la cadena entera desde ahí y no queda rastro. El HMAC necesita
además la llave, que vive en la configuración de la aplicación y no en el motor de datos: quien
tenga acceso a la base y no a la llave puede romper la cadena, pero no puede falsificarla.

**Rotación:** cada evento guarda `VersionLlave`. Se rota cambiando `VersionActual`, y las llaves
viejas **nunca se borran** del diccionario — sin ellas, los eventos firmados con esa versión pasan a
ser inverificables, que en la práctica es lo mismo que haberlos perdido.

**Las llaves no van en el repositorio.** En desarrollo, `dotnet user-secrets`; en producción,
variables de entorno o el almacén de secretos del proveedor. Se generan con `openssl rand -base64 32`.

### D-32 · Baja de usuarios: lógica con identidad liberada · **Aprobado**

Al dar de baja un usuario: se marca `FechaBaja`, se borran físicamente sus permisos y su alcance de
plantas, y la fila queda. Los índices únicos de `Auth0UserId` y de `empresa + email` pasaron a ser
**parciales** (`WHERE fecha_baja IS NULL`).

**Consecuencia:** si esa persona vuelve, se crea una fila nueva, con identificador nuevo y cero
permisos — hay que dárselos de nuevo, que era el requisito. La fila vieja sigue sosteniendo todo el
historial: quién cerró cada orden, quién resolvió cada alerta, quién creó cada registro. El acceso
se corta porque el login filtra las bajas.

**Confirmado:** ninguna entidad tiene baja física salvo las relaciones puras —`UsuarioAlcance`,
`PermisoPorUsuario`, `MaquinaRepuesto`—, que no tienen valor histórico propio.

### D-33 · Empresa suspendida: modo de solo lectura · *Revisar*

`EstadoEmpresa` ya tenía tres valores. Ahora significan algo distinto cada uno:

| Estado | Quién entra | Qué puede hacer |
|---|---|---|
| `Activa` | Todos | Todo lo que le permita su rol |
| `Suspendida` | Todos | **Solo `Consultar` y `Exportar`.** Ve máquinas, órdenes y gráficos, exporta reportes, no carga ni modifica nada |
| `Baja` | Solo `SuperAdminMantIA` | Nada operativo |

Se aplica en `PermisoService`, como paso 2 de la evaluación, y no en cada pantalla: si dependiera de
que cada módulo se acuerde de preguntarlo, alcanza con que uno se olvide para que la suspensión no
signifique nada.

**Queda para cuando armes el plan comercial:** qué dispara automáticamente el pase a `Suspendida`
(días de mora, aviso previo) y si hay un aviso visible dentro de la aplicación explicando por qué no
se puede cargar nada.

### D-34 · Permisos nominales por usuario · *Revisar* ⚠

Entidad nueva `PermisoPorUsuario`. Concede o quita un permiso **a una persona concreta**, por encima
de su rol y su nivel.

**Las tres reglas que impiden que sea un agujero** — es lo que te pido que revises:

1. **Excepción en grado, nunca en ámbito.** La frontera estructural se evalúa contra el *rol* del
   usuario, no contra su fila nominal, y se evalúa **antes**. Un permiso nominal solo puede mover una
   casilla dentro de lo que su rol ya podía alcanzar: no existe forma de darle a un operario un
   recurso del ámbito Empresa, ni a un administrador la capacidad de cerrar una orden.
2. **Nadie edita sus propios permisos nominales.** Sin esto, quien administra permisos se concede lo
   que quiera y la separación de funciones deja de existir.
3. **No puede revocar un mínimo del rol.** El piso de D-25 se evalúa antes que la excepción.

Además: exige motivo escrito, genera evento de severidad crítica, y tiene `VigenteHasta` opcional.

**Sobre el vencimiento:** lo agregué porque casi todas estas excepciones nacen temporales —una
licencia, un cierre de mes, una auditoría— y nadie se acuerda de quitarlas después. Un permiso que
se apaga solo es la única defensa práctica contra la acumulación silenciosa de privilegios. El
vencimiento se compara **al evaluar**, no al leer de la base: una excepción que vence a las 15:00
deja de aplicar a las 15:00, no cuando expire la caché.

**Orden de evaluación completo, actualizado:**

```
1. SuperAdmin              → concede
2. Empresa suspendida      → deniega si no es Consultar/Exportar
3. Frontera estructural    → deniega si el ROL no alcanza el recurso
4. Piso irrevocable        → concede
5. Excepción nominal       → concede o deniega, si está vigente
6. Matriz: nivel exacto    → concede o deniega
7. Matriz: rol genérico    → concede o deniega
   (nada de lo anterior)   → deniega
```

### D-35 · Severidad de eventos, derivada y no elegida · *Revisar*

Cuatro niveles: `Rutina`, `Operativa`, `Sensible`, `Critica`. **La deriva `CatalogoEventos` del par
recurso/acción, no la elige quien escribe el evento** — si cada módulo eligiera la suya, la escala
dejaría de significar algo a la tercera pantalla.

Es un eje distinto de `NivelLog`: una excepción es `Error` para un técnico aunque no tenga ninguna
consecuencia; borrar una orden abierta es un evento perfectamente exitoso y es lo más grave que
puede pasar en un día normal.

**Las agravantes son lo que hace útil a la escala.** El mismo par pesa distinto según el contexto:

| Agravante | Efecto |
|---|---|
| Uso del bypass de superadministrador | Crítica, siempre |
| Acción destructiva sobre algo **vivo** (OT abierta o en curso) | Crítica |
| La acción falló | Sube un escalón — un intento fallido dice más que uno exitoso |
| Exigía motivo y llegó vacío | Sube un escalón |

Y la severidad decide la **retención**: Rutina 90 días, Operativa 1 año, Sensible 5 años, Crítica sin
vencimiento. Sin política de retención la bitácora crece sin límite y las consultas del administrador
se vuelven inusables por el ruido de las consultas rutinarias.

### D-36 · Acciones que exigen motivo escrito · *Revisar*

`Ordenes.Baja`, `Maquinas.Baja`, `Repuestos.Baja`, `Usuarios.Baja`, `Empresas.Baja`,
`Permisos.Configurar`, `Rollback.Alta`, `Rollback.Decidir`.

**Es regla de negocio, no de auditoría:** la capa de servicio rechaza la operación si el motivo viene
vacío. Es la respuesta directa a "eliminó una OT abierta sin justificación" — deja de ser posible: o
escribe por qué, o no la borra.

### D-37 · Una cadena de sellos por empresa, más una de plataforma · *Revisar*

No hay una cadena global.

**Por qué:** cada evento necesita el hash del anterior, así que una cadena única serializa todas las
escrituras de todos los clientes. Con una cadena por tenant, el volumen de un cliente no frena a los
demás, y verificar la integridad de una empresa no obliga a recorrer los eventos de otra.

### D-38 · Un punto único de escritura de bitácora · *Revisar*

`IBitacora` es el único camino. `IRepositorioBitacora` deliberadamente **no tiene métodos de
modificación ni de borrado**: si los tuviera, tarde o temprano alguien los usaría "para corregir un
typo" y la cadena dejaría de verificar sin que nadie entienda por qué.

El orden interno tampoco es arbitrario: primero enmascara, después cifra, y recién al final sella
**lo que efectivamente se guarda**. Sellar el texto en claro obligaría a descifrar todo para
verificar la cadena, y volvería cara una operación que debería poder correr periódicamente.

### D-27 (revisión) · La clasificación de sensibilidad depende del alcance · *Revisar*

Corregido lo que te había marcado como error mío.

El costo de un repuesto estaba omitido "porque revela márgenes", pero faltaba preguntarse
**a quién**. Un evento de ámbito empresa lo lee el administrador de esa misma empresa, que ya conoce
sus propios costos: ocultárselo no protege nada y volvía inauditable exactamente el escenario que
originó todo esto.

Ahora cada campo tiene dos políticas:

| Campo | En bitácora de empresa | En bitácora de plataforma |
|---|---|---|
| `Repuesto.CostoUnitario` | Público | Omitido |
| `OrdenTrabajoRepuesto.CostoUnitarioAlConsumo` | Público | Omitido |
| `Repuesto.Proveedor` | Público | Omitido |
| `Usuario.Email` | Enmascarado | Enmascarado |
| `Usuario.Nombre` / `Apellido` | Público | Enmascarado |
| `Usuario.Auth0UserId` | Omitido | Omitido |
| `Plan.PrecioMensual` | Omitido | Público |

Un campo omitido no se borra: se reemplaza por `[omitido]`. "No se registró el valor" y "el valor
estaba vacío" son dos hechos distintos, y el segundo puede ser justo lo que se está auditando.

---

## 21/08/2026 — Regla de otorgamiento y bitácora sobre MongoDB

### D-39 · Nadie otorga un permiso que no tiene · *Revisar* ⚠ **cambio lógico fuerte**

Regla tuya, implementada en `GestorPermisos`. Pero **aplicada al pie de la letra se traba sola**, y
tuve que resolverlo. Esto es lo que te tengo que marcar.

**El bloqueo:** el `AdminEmpresa` es el único que administra permisos, y su ámbito es Empresa
(D-23). Por lo tanto nunca tiene `Ordenes.Cerrar` — *no puede tenerlo*, por separación de funciones.
Con la regla estricta, entonces, **nadie en la empresa podría concederle nunca a un supervisor la
capacidad de cerrar órdenes**. La operación entera quedaría sin forma de configurarse.

**Cómo lo resolví:** distinguiendo **asignar** de **ejercer**.

| Situación | Regla |
|---|---|
| El recurso está **dentro** del ámbito del otorgante | Aplica entera: no reparte lo que no tiene |
| El recurso está **fuera** de su ámbito | Puede asignar, sin poder ejercer nunca |

Un `AdminEmpresa` puede habilitar a un supervisor a cerrar órdenes, y sigue sin poder cerrar una él
mismo. Eso preserva la separación de funciones —que era el objetivo— en lugar de romperla por
exceso de celo.

Además, la regla se evalúa contra los **permisos reales** del otorgante, no contra su rol nominal:
si él mismo recibió la capacidad por una excepción, puede transmitirla; si nunca la tuvo, no puede
fabricarla.

**Alternativa que descarté, y que quiero que consideres:** repartir la administración de permisos
por ámbito. Un recurso nuevo `PermisosOperacion` en ámbito Operación, que un Gerente pueda tener,
para que sea él —y no el administrador de la empresa— quien configure los permisos operativos. Con
eso la regla aplicaría estricta en todos los casos, sin excepción, y modelaría mejor cómo funciona
una fábrica de verdad: quien reparte permisos de mantenimiento es el jefe de mantenimiento, no el
administrativo. **No lo implementé porque inventa estructura organizacional que vos no definiste.**
Si te cierra, lo hago y la regla queda sin la excepción del cuadro de arriba.

Los otros cinco controles de `GestorPermisos`, sin sorpresas: quien otorga debe poder administrar
permisos; nadie edita los propios; la frontera estructural del rol de destino; el piso irrevocable;
y motivo escrito obligatorio.

### D-40 · La secuencia de la bitácora la ordena un índice único, no un bloqueo · *Revisar*

Cada evento lleva el hash del anterior y un número de secuencia, así que dos operaciones simultáneas
de la misma empresa leen el mismo último eslabón y las dos intentan ser la siguiente.

Se resuelve con un índice único sobre `(cadena, secuencia)`: **la segunda escritura la rechaza la
base, no código nuestro**, y el repositorio reintenta releyendo el eslabón. Es la misma idea que el
libro mayor de stock — dejar que la restricción decida el orden en lugar de bloquear por las dudas.

Nunca se saltea un número: un hueco en la secuencia es indistinguible de un borrado.

Por eso el sello se calcula **dentro** del repositorio y no antes: el número de secuencia solo se
conoce al insertar, y quien pierde la carrera tiene que volver a sellarse con el eslabón correcto.

### D-41 · Retención por severidad, ejecutada por Mongo · *Revisar*

`EventoBitacora.ExpiraEn` se calcula al escribir desde `CatalogoEventos.RetencionMinima`, y un índice
TTL sobre ese campo hace que Mongo borre solo. Los eventos críticos tienen `ExpiraEn` nulo y no
vencen nunca.

**Por qué fecha concreta y no política a interpretar después:** si mañana se acorta la política de
retención, los eventos ya escritos conservan el plazo con el que se registraron. **Cambiar la
política no puede ser una forma de borrar el pasado.**

### D-42 · `IRepositorioBitacora` se mudó de BLL a DAL · *Revisar*

Cambio de ubicación, no de diseño. La implementación es sobre MongoDB y vive en la capa de datos;
como el flujo de dependencias del proyecto es WEB → BLL → DAL → BE, el puerto tiene que estar en
DAL para que la implementación pueda estar ahí también.

La interfaz sigue sin métodos de modificación ni de borrado, que era lo importante.

### D-43 · Una cadena bloqueada no tumba la aplicación · *Revisar*

Si MongoDB no está disponible al arrancar, se registra el error y la aplicación levanta igual.
Dejarla sin arrancar por un índice que se puede crear después convierte una falla parcial en una
total.

**Contrapartida a decidir (ver P-08):** qué pasa si Mongo se cae *en caliente*, con la aplicación ya
andando y alguien cerrando una orden.

---

## 21/08/2026 (tarde) — Correcciones sobre lo anterior

### D-39 (revisión) · La regla queda estricta: la administración se reparte por ámbito · **Aprobado**

Se aprobó `PermisosOperacion`, y con eso **desaparece la excepción** que había tenido que meter.

| Recurso nuevo | Ámbito | Quién |
|---|---|---|
| `Permisos` | Empresa | `AdminEmpresa` — usuarios, niveles, plantas |
| `PermisosOperacion` | Operación | `Gerente` — máquinas, repuestos, stock, órdenes |

Cada jefe reparte lo que él mismo puede hacer. El gerente de mantenimiento habilita a cerrar órdenes
porque él cierra órdenes; el administrador da de alta usuarios porque él da de alta usuarios.
Ninguno alcanza el terreno del otro. Verificado:

```
AdminEmpresa puede configurar PermisosOperacion?  False
Gerente puede configurar Permisos (empresa)?      False
```

`PermisosOperacion.Consultar` y `.Configurar` son **mínimos irrevocables del Gerente**, para que
ninguna empresa nazca sin quien reparta. La matriz operativa inicial la carga MantIA en el alta, con
el bypass de superadministrador, que se audita.

**Detalle menor a tener presente:** `PermisosOperacion` está en ámbito Operación, así que un
Supervisor o incluso un Empleado *podrían* recibirlo por matriz. No lo tienen por defecto y la regla
3 exige que quien lo otorgue lo tenga, pero si te parece que solo el Gerente debería poder tenerlo,
se restringe.

### D-41 (revisión) · La bitácora no vence nunca · **Aprobado**

Revertida mi decisión de retención por severidad. Se eliminaron `EventoBitacora.ExpiraEn`,
`CatalogoEventos.RetencionMinima` y el índice TTL de MongoDB.

**Tu argumento, que es el correcto:** un registro de auditoría vale justamente el día que pasa algo
raro, y ese día nadie sabe de antemano cuándo es. Guardar texto es barato; no poder reconstruir un
incidente no lo es. Si alguna vez el volumen molesta, se archiva a mano con una decisión tomada en
ese momento — no se programa el olvido por adelantado.

La severidad sigue existiendo, pero ahora **solo para filtrar, destacar y priorizar avisos**. El
ruido de las consultas rutinarias se resuelve con filtros en la pantalla, que es barato y
reversible, y no borrando registros, que no lo es.

### D-40 (revisión) · El número lo asigna la base, con sellado en dos tiempos · **Aprobado**

Reemplazado el lazo de reintentos por un **contador atómico** (`$inc` sobre una colección de
contadores): una sola operación entrega el siguiente número, sin leer antes, sin carrera y sin
reintentos. Escala con la concurrencia en lugar de degradarse con ella.

**La tensión que eso crea, porque es real y conviene que la tengas clara:** una cadena de hashes es
secuencial por naturaleza — cada eslabón contiene el hash del anterior. Si el número se entrega en
paralelo, el evento 7 puede llegar antes que el 6, y no puede sellarse hasta que el 6 exista. **No
se puede tener las dos cosas a la vez.**

Se resuelve escribiendo en dos tiempos:

1. **Se guarda el hecho** con su número, sin sellar. La acción ya quedó registrada.
2. **Se sella** recorriendo desde el último eslabón cerrado hacia adelante, mientras los números
   estén completos. Lo hace el mismo pedido que escribió, así que en operación normal el evento
   queda sellado en el mismo instante.

El sellado es idempotente y con carrera segura: la actualización solo aplica si el evento sigue sin
sellar. Un evento sin sellar ya es evidencia de que la acción ocurrió; lo que le falta es la prueba
de que nadie lo movió de lugar.

### D-44 · Respaldo en el tenant y cola de drenaje · **Aprobado**

Implementado tal como lo planteaste.

```
escribir evento
   ├─ Mongo responde ......................... listo
   └─ Mongo no responde
         ├─ PostgreSQL responde ............. queda en evento_pendiente, la operación sigue
         └─ PostgreSQL tampoco .............. se propaga el error: el sistema está caído igual
```

Un trabajo de fondo (`MantenimientoBitacora`, cada 30 s) hace dos cosas en este orden: **primero
drena el respaldo**, porque un evento sin reflejar es un hecho que todavía no está en la bitácora, y
eso es peor que un eslabón sin sellar; **después sella** lo pendiente.

Es un ciclo, no una cola con estado: cada vuelta mira la realidad y hace lo que falta. Si se cae en
el medio, la siguiente retoma sin recordar nada.

**Dos consecuencias que hay que asumir:**

- El respaldo se borra **después** de confirmar la escritura en Mongo. Si el proceso muere entre una
  cosa y la otra, el peor caso es un evento duplicado —que se ve en la bitácora— en lugar de uno
  perdido, que no se ve.
- Un evento que pasó por el respaldo conserva su fecha real, pero su **posición en la cadena refleja
  el momento del drenaje**. La cadena garantiza que nadie alteró el registro, no que el orden
  coincida con el reloj.

### D-45 · Numeración de documentos · **Aprobado**

Formato `OT-2026-00001`, serie por empresa y por año. Cinco dígitos, y si alguna vez se pasa el
formato crece solo en lugar de truncar.

El número lo entrega PostgreSQL con un `INSERT ... ON CONFLICT DO UPDATE ... RETURNING`: una sola
operación atómica. La maqueta contaba filas, y con dos altas simultáneas las dos se creen la número
47 — el índice único rechaza a la segunda y el usuario ve un error por algo que hizo bien.

**Sobre los huecos:** si la transacción que pidió el número después falla, ese número queda sin usar.
Es el comportamiento correcto — devolverlo al contador reintroduce la carrera que se estaba
evitando. Una numeración con huecos es normal en cualquier sistema con comprobantes; una con
duplicados no lo es.

Sirve también para reportes (`REP-2026-00008`) y para lo que venga.

---

## 21/08/2026 (cierre) — Cifrado por campo

### D-46 · Se cifran campos, no tablas · **Aprobado**

Corregido el malentendido. Antes cifraba el documento entero de estado en la bitácora; ahora hay un
catálogo, `CamposCifrados`, que dice campo por campo qué se guarda cifrado y con qué nivel.

**Dos niveles, y la diferencia es la que decide si el sistema sigue funcionando:**

| Nivel | Se puede buscar / indexar | Para qué |
|---|---|---|
| **Determinista** | Sí, por igualdad | Campos por los que hay que buscar: `Usuario.Email`, `Usuario.Auth0UserId`. Sin esto, cifrar el correo rompe el login |
| **Aleatorio** | No | Texto libre que se lee entero y se muestra: descripciones, motivos, proveedor |

El determinista revela qué filas tienen el mismo valor, aunque no cuál es. Para un correo es un
intercambio razonable; para una descripción no haría falta y por eso va aleatorio.

**Lo que NO se cifra, con su motivo:**

| Campo | Por qué queda en claro |
|---|---|
| `Repuesto.CostoUnitario` | Se suma. "Cuánto vale el stock inmovilizado" es una de las cifras que justifica el producto, y sobre un campo cifrado no hay `SUM` |
| `Criticidad`, `Severidad`, `Estado` | Son los ejes de filtrado de todas las pantallas. Cifrarlos obliga a traer la tabla entera a memoria para mostrar "las alertas críticas del mes" |
| `Usuario.Nombre` / `Apellido` | Se ordenan alfabéticamente en cada selector |

**Sobre `Criticidad`, que mencionaste como ejemplo:** ahí discrepo y te lo marco. Es justo el campo
que más se filtra, y cifrarlo mata el índice que acabamos de poner. Si te parece que igual tiene que
ir cifrado, se hace, pero el listado de alertas pasa a resolverse en memoria.

**Detalle importante ya verificado:** el esquema que genera EF es **idéntico** con y sin cifrado
—todas las columnas afectadas son `text` igual—, así que las migraciones generadas en tiempo de
diseño, donde no hay llaves, coinciden exactamente con lo que espera la aplicación en ejecución.

El conversor **tolera datos preexistentes en claro**: al leer, si el valor no tiene el prefijo de
cifrado lo devuelve tal cual. Sin eso, activar el cifrado sobre una tabla con datos rompería todas
las filas viejas de golpe.

Esto es distinto del cifrado en reposo del motor y los dos conviven: el motor protege del robo del
archivo; esto protege de quien tenga una sesión legítima contra la base —un administrador de
infraestructura, un volcado de respaldo— y no debería ver ciertos valores.

### D-47 · El número de secuencia salió del sello · **Aprobado**

Tenías razón en que estaban mezclados. `CanonicalizacionEvento` ya no incluye `Secuencia`.

Son dos mecanismos independientes: **el número lo asigna el contador de la base para ordenar; el
sello protege el contenido.** Atarlos hacía que el sello dependiera del lugar que le tocó al evento
en la fila, cuando lo que hay que proteger es lo que dice. El orden lo garantiza igual el encadenado
por hash, que es más fuerte: reordenar rompe la cadena entera.

Efecto práctico: un evento que pasó por el respaldo local y se refleja después no necesita
resellarse por haber caído en otra posición.

### D-48 · Historial de órdenes de trabajo · **Aprobado**

Tabla `historial_orden_trabajo`, append-only. **El update sigue permitido: se registra, no se
bloquea.** Cada cambio deja qué campo, de qué a qué, quién y cuándo.

`FechaCreacion` y `FechaModificacion` ya venían de `BaseEntity` y las sella el contexto en cada
guardado; el historial cuenta lo que pasó en el medio.

**Por qué existe si ya está la bitácora.** Son dos cosas distintas y las dos hacen falta:

| | Bitácora | Historial de OT |
|---|---|---|
| Alcance | Transversal, todo el sistema | Una orden |
| Motor | MongoDB | PostgreSQL, al lado de la orden |
| Responde | "Qué hizo esta persona" | "Qué le pasó a esta orden" |
| Consulta | Filtro sobre un log | `JOIN` barato |

El supervisor que abre la OT-2026-00047 quiere ver su línea de tiempo ahí mismo, no filtrar un log
de auditoría. `HistorialOrdenTrabajo.EventoBitacoraId` enlaza las dos: desde la línea de tiempo se
salta al registro completo con su sello, sin duplicar la información.

Borrado `Restrict` y no `Cascade`: el historial sobrevive a la orden.

### D-49 · Los tests de permisos ya no necesitan base · *Revisar*

`PermisosTests` levantaba PostgreSQL, buscaba una "Empresa Demo" sembrada a mano y comparaba contra
nombres de recurso que ya no existen — de ahí los cinco errores de compilación.

Lo reescribí contra las reglas estructurales: ámbitos, piso irrevocable, separación de funciones.
Corre en milisegundos, no depende de nada externo, y prueba lo que de verdad no puede romperse sin
que nadie se dé cuenta. `MultiTenantTests` sigue necesitando base y quedó como estaba.

---

## Pendientes: necesito que decidas

### P-01 · Dimensión del vector de embeddings · **Resuelto: 768**

Elegido `multilingual-e5-base` (768 dimensiones), corriendo local, sin tokens.

**Por qué el más pesado y no el liviano de 384:** el trabajo que tiene que hacer el vector acá no es
buscar, es *agrupar descripciones de la misma falla escritas distinto*. "Fuga entre placas por junta
vencida" y "pérdida en el paquete de placas del sector de regeneración" tienen que caer juntas, y ahí
es exactamente donde el modelo chico se queda corto en español técnico. Si el agrupamiento falla, el
contador de frecuencia nunca llega al umbral y el catálogo compartido no se construye — que es el
diferencial entero del producto.

El costo del 768 es el doble de espacio y algo más de CPU por embedding. Eso es barato; un catálogo
que no agrupa, no.

**Sigue siendo revisable con datos reales:** cuando haya un puñado de órdenes cerradas de verdad, se
mide cuántos pares que un técnico consideraría "la misma falla" quedan efectivamente juntos. Si 384
alcanza, se baja y se ahorra. Antes de eso es especulación.

### P-02 · Umbral de promoción · **Resuelto: tres niveles**

Tu pedido fue un intermedio entre "lo que se replica en muchas fábricas" y "lo que el sistema relevó
como problema conocido a nivel general". Eso da tres niveles, no dos:

| Nivel | Umbral | Dónde se ve | Cómo se presenta |
|---|---|---|---|
| **Privado** | 1 evento | Solo la empresa que lo generó | Su propio historial |
| **Observado** | 2 empresas y 3 eventos | Ficha del catálogo, etiquetado | "Reportado por 2 empresas — sin confirmar" |
| **Confirmado** | 3 empresas y 5 eventos | Ficha del catálogo, como característica del modelo | "Falla común de este modelo" |

**Por qué el nivel intermedio resuelve el problema.** Con dos niveles había que elegir entre un
umbral bajo —que ensucia el catálogo con casos aislados— y uno alto, que no promueve nunca y deja el
efecto de red sin arrancar. El nivel *observado* deja ver la señal temprano, **con la incertidumbre
declarada en pantalla**, sin afirmar que es una característica del modelo. El usuario decide qué
hacer con esa información; el sistema no le miente sobre cuánto la respalda.

Los dos números siguen midiendo cosas distintas: las **empresas** evitan que un solo cliente con mal
mantenimiento contamine el catálogo, los **eventos** evitan promover algo que pasó una vez de
casualidad en cada lado.

**Configurable y auditable**, las dos cosas: los umbrales viven en configuración de plataforma y cada
promoción deja su evento en la bitácora, con cuántas empresas y cuántos eventos la respaldaban en ese
momento. En la defensa vas a poder responder "porque tres empresas distintas lo reportaron cinco
veces" en lugar de "porque el modelo lo decidió".

**Falta decidir una sola cosa:** si el umbral es el mismo para todas las categorías. Un compresor
tiene mucha más población instalada que una máquina especial, y con el mismo número la segunda no
promueve nunca. Se puede resolver después, cuando haya datos para calibrar.

### P-03 · Escala de máquinas por planta · *Diferido*

Se ajusta con datos de los primeros clientes, recalculando los costos para que siga siendo rentable.

Lo que hay que evitar desde ya, y esto sí es ahora: **no escribir consultas ni pantallas que solo
funcionen con el extremo bajo del rango.** Concretamente, los listados de máquinas y de repuestos
tienen que nacer con paginación y búsqueda del lado del servidor aunque hoy se prueben con quince
filas. Reconvertir después una pantalla que carga todo en memoria es bastante más caro que hacerla
bien la primera vez.

Queda anotado que con volumen alto la unidad de trabajo deja de ser la máquina y pasa a ser la
**línea de producción**, hoy un texto suelto en `Maquina`.

### P-04 · Certificados de mantenimiento de proveedores · *Etapa 2*

Fuera de alcance de esta etapa. Queda registrado el diseño para cuando se retome: documento adjunto
a `Maquina` —y opcionalmente a una `OrdenTrabajo`— que entra a la capa semántica, con el texto
vectorizado alimentando la misma normalización de fallas que hoy solo se nutre de órdenes cerradas.

Falta definir en su momento: entidad `DocumentoMaquina`, dónde se guarda el binario (fuera de
PostgreSQL), extracción de texto —muchos llegan escaneados— y si la vectorización es automática o
requiere validación previa.

### P-05 · Disparadores del estado suspendido

D-33 dejó implementado el modo de solo lectura. Falta la parte comercial: qué lo dispara
automáticamente (días de mora, aviso previo), si hay un plan económico de contención antes de
suspender, y qué mensaje ve el usuario dentro de la aplicación explicando por qué no puede cargar
nada. Sin ese mensaje, la suspensión se va a leer como que el sistema está roto.

### P-08 · Bitácora no disponible en caliente · **Resuelto** — ver D-44

Ni bloqueante ni best-effort: respaldo en la base del cliente y cola de drenaje. La operación nunca
se frena mientras la base principal esté viva, y ningún evento se pierde.

### P-09 · Administración de permisos repartida por ámbito · **Resuelto** — ver D-39 (revisión)

### P-06 · Numeración de órdenes de trabajo · **Resuelto** — ver D-45

### P-10 · Qué hacer con un hueco permanente en la cadena

Consecuencia directa del contador atómico (D-40) y la única esquina que dejó abierta.

Si un pedido toma el número 47 y muere antes de insertar el evento —el proceso se cae, el contenedor
se reinicia—, el 47 nunca existe. El sellado se corta ahí y **todo lo posterior queda sin sellar para
siempre**, esperando un eslabón que no va a llegar.

Los eventos siguen guardados y siguen siendo legibles; lo que se pierde es la prueba de integridad
del tramo. Tres salidas:

| Salida | Comentario |
|---|---|
| **Lápida automática** | Pasados N minutos sin que aparezca, se inserta un evento "hueco confirmado" en esa posición y la cadena continúa. Queda registrado que hubo un salto y por qué |
| **Lápida manual** | Igual, pero lo confirma una persona desde el módulo de superadministrador. Más control, y alguien tiene que estar mirando |
| **Dejarlo abierto** | El tramo posterior nunca se sella. Honesto pero inútil: en la práctica anula la verificación de esa empresa desde ese punto |

Me inclino por la primera con N alto —quince minutos, digamos— porque el caso normal es que el evento
aparezca en milisegundos, y una lápida es en sí misma información de auditoría: dice que hubo una
caída en ese momento. Pero es tu llamada.

### P-07 · Usuario de demostración

Quedó en buffer: credenciales que abran la versión de maqueta en lugar de la real. Se resolvería con
un claim de Auth0 y registro condicional de servicios, pero no está diseñado.

---

## Cambios que NO se hicieron y por qué

Para que quede constancia de lo que se evaluó y se descartó.

| Qué | Por qué no |
|---|---|
| **Capacidad offline con sincronización** | Decisión tuya: es un proyecto web, exigir acceso offline no tendría sentido. Contradice §2.1.4 del documento de visión, que además contradice a §1.9 — hay que corregir el texto |
| **Bloqueo pesimista de recursos para el stock** | Se resolvió con libro mayor (D-30). Bloquear filas serializa las operaciones y no escala |
| **Enums nativos de PostgreSQL** | Cada cambio exige SQL manual (`ALTER TYPE ... ADD VALUE`) que no corre dentro de una transacción. Demasiada fricción en la etapa donde más cambia el modelo |
| **Matriz de permisos por defecto** | Decisión tuya: cada cliente define la suya. Se reemplazó por el piso irrevocable (D-25) |
| **Bloqueo de rutas por URL** | Decisión tuya: sin autorización real en el servidor, una guarda del lado del cliente aparenta seguridad donde no la hay. Se implementa cuando esté la capa MVC |
| **Cifrar la bitácora** | Ver D-26: la integridad importa más que la confidencialidad, y el cifrado en reposo es del motor |
