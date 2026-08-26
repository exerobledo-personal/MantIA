# Decisiones tomadas

Registro de todo cambio que altera **cómo se comporta el sistema**, no solo cómo está escrito.
Acá vive lo ya decidido; lo que falta definir está en `PENDIENTES.md`.

| Estado | Significado |
|---|---|
| **Aprobado** | Lo decidió Exequiel explícitamente. No se cambia sin que lo pida. |
| *Revisar* | Se tomó sin consultar porque no bloqueaba el avance. Está implementado y es reversible. |

Regla de trabajo: **nada que cambie una regla de negocio se implementa sin estar decidido.** Lo que
entra como *Revisar* es siempre reversible y está acotado a una capa; lo que toca el contrato del
negocio se pregunta antes.

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


---

## 21/08/2026 (cierre de jornada) — Confirmaciones y numeración sin huecos

### D-25 (revisión) · El piso de permisos queda confirmado · **Aprobado**

Confirmado el criterio: hace falta un perfil que pueda ejecutar cada operación de base. Asignar y
desasignar permisos se resuelve después, sobre esa base ya existente.

### D-50 · La numeración nunca se reserva por adelantado · **Aprobado**

Resuelve lo que estaba anotado como P-10 (hueco permanente en la cadena) y corrige mi posición
anterior, que era equivocada.

**La regla, en tus palabras:** si alguien empieza a cargar algo y lo cancela a la mitad, no hay que
guardar un buffer por si acaso. **El número se genera cuando está confirmado que el recurso debe
existir**, nunca antes.

Cambia dos cosas:

**En PostgreSQL** —numeración de órdenes y reportes— la regla se cumple llamando al numerador
**dentro de la misma transacción** que crea el documento. El contador es una tabla, no una secuencia,
así que su incremento participa de la transacción: si algo falla o se cancela, el `ROLLBACK` deshace
también el incremento y el número vuelve a estar disponible.

> Una secuencia de PostgreSQL **no** serviría: quedan fuera de la transacción a propósito, para no
> serializar a quien las usa, y por eso dejan huecos. La tabla es más lenta y a cambio la serie es
> continua. Para un comprobante que ve el cliente, esa es la propiedad que importa.

**En MongoDB** se dio vuelta el orden de la escritura:

```
antes:  pedir numero al contador  →  insertar          (si moría en el medio: hueco permanente)
ahora:  insertar el evento        →  numerar y sellar  (el numero solo se escribe sobre algo que existe)
```

Un evento con secuencia cero está guardado pero sin numerar. La numeración es un paso corto,
serializado por cadena y **fuera del camino crítico**: lo que tiene que escalar es la escritura del
evento, y esa sigue siendo un insert sin coordinación con nadie.

Desapareció la colección de contadores. Es un paso atrás respecto del autonumérico atómico que
habíamos puesto, y es el correcto: el contador era justamente lo que hacía posible el hueco.

### D-51 · Las pantallas muestran lo justo y filtran por campos no cifrados · *Revisar*

Criterio de interfaz que se desprende del cifrado por campo: los listados traen las columnas
necesarias y los filtros se ofrecen **solo sobre campos en claro**.

No es una limitación que haya que disimular, es la consecuencia visible de una decisión de seguridad:
sobre una columna cifrada no hay `WHERE`, `ORDER BY` ni índice. Diseñar las pantallas sabiendo eso
evita el caso feo — un filtro que existe en la interfaz y se resuelve trayendo la tabla entera a
memoria.

### D-52 · Los documentos se separaron en dos · **Aprobado**

`DECISIONES.md` guarda lo decidido y `PENDIENTES.md` lo que falta definir. El archivo único había
llegado a mil líneas y buscar en él costaba más de lo que ayudaba.

### D-53 · Dos juegos de llaves separados · **Aprobado**

Sellar la bitácora y cifrar campos usan **llaves distintas**, en secciones de configuración distintas
(`Auditoria:Sello` y `Auditoria:Cifrado`), cada una con su versión y su propia rotación.

**Por qué importa.** Con una sola llave, quien la obtenga por cualquier vía —un volcado de
configuración, un descuido en un entorno de prueba— puede a la vez leer los datos cifrados **y**
falsificar la cadena de auditoría que debería delatarlo. Con dos, comprometer una no da la otra:
puede leer, pero no borrar sus huellas.

El protector **rechaza arrancar si las dos llaves son iguales**. Es el error que alguien va a cometer
copiando y pegando, y falla ruidoso en vez de dar una falsa sensación de separación. Verificado:

```
con la llave de sello NO se descifra   : OK
rechaza usar la misma llave para ambas : OK
abre un valor cifrado con la llave vieja: True
```

Idealmente cada juego vive en un almacén distinto y con distinto responsable. Aunque hoy estén los
dos en la misma configuración, tenerlos separados desde el modelo permite moverlos después sin tocar
una línea de código.

**Lo que esto NO resuelve, y quedó escrito en el código:** con la llave en la mano, ningún mecanismo
con llave detiene a nadie. La defensa real contra ese caso es publicar periódicamente el hash de la
punta de cada cadena fuera del sistema — se puede reescribir la base entera, pero no un hash que ya
se publicó ayer en otro lado.

### D-54 · Escala: 50 máquinas por planta en el plan base, 100 en el más alto · **Aprobado**

Cierra lo que estaba como P-03. Las fábricas dividen sus activos, así que 50 por planta cubre el
caso normal y 100 es el techo razonable.

**Un detalle de modelado que hay que resolver al sembrar los planes:** hoy `Plan.MaxMaquinas` es un
total, y vos definiste el límite **por planta**. Son cosas distintas: con `MaxPlantas = 3` y 50 por
planta, el total es 150. Propongo renombrar a `MaxMaquinasPorPlanta` y que el total quede derivado —
es más claro de explicar comercialmente y más fácil de verificar al dar de alta una máquina, porque
la validación mira una sola planta en lugar de contar todo el tenant.

**Lo que sigue vigente aunque los números bajen:** los listados de máquinas y repuestos nacen con
paginación y búsqueda del lado del servidor. Con 50 filas no se nota, pero reconvertir después una
pantalla que carga todo en memoria es bastante más caro que hacerla bien la primera vez.

### D-55 · Escalonamiento de morosidad · **Aprobado**

Cierra lo que estaba como P-05.

| Mes de deuda | Estado | Qué puede hacer el cliente |
|---|---|---|
| 1 y 2 | `Activa` | Todo. Avisos dentro de la aplicación |
| 3 | `Suspendida` | **Solo lectura**: consulta, ve gráficos, exporta reportes. No carga ni modifica nada |
| 4 | `Baja` | Nada. Solo entra `SuperAdminMantIA` |
| 5 | `Baja` | Los datos se conservan un mes más y después son elegibles para purga |

El modo de solo lectura ya está implementado (D-33). Lo que falta construir es el proceso que mueve
el estado según los meses de deuda, y eso depende del módulo de facturación, que no existe todavía.

> **Una tensión que hay que resolver antes de purgar nada.** Dijimos que la bitácora no vence nunca
> (D-41), y borrar los datos de un tenant borraría también su bitácora. Son dos reglas que chocan.
> Mi recomendación: al purgar, **conservar la bitácora de plataforma** —altas, bajas y cambios sobre
> la cuenta, que son registro de MantIA sobre su propia operación— y purgar solo los datos
> operativos y la bitácora de empresa. Queda anotado para cuando llegue el momento; no hace falta
> decidirlo ahora.

### D-56 · Usuario de demostración sin credenciales · **Aprobado**

Cierra lo que estaba como P-07. Un acceso de demostración **sin credenciales**, que entra
directamente a la versión de maqueta con los datos que hoy están en `DatosDemo`.

Es factible y es la opción más limpia de las dos: no hay usuario real que aprovisionar, no hay
contraseña que rote ni se filtre, y no toca Auth0. La maqueta ya funciona entera con datos en
memoria y su estado ya es por circuito de Blazor (`AddScoped`), así que dos visitantes simultáneos
no se pisan.

**Cómo se implementa:** una ruta propia —`/demo`— que arma un `ClaimsPrincipal` sintético con un rol
de solo demostración y registra los servicios de maqueta en vez de los reales. **No pasa por
`TenantResolver` ni toca la base**, así que no hay forma de que una sesión de demostración vea o
escriba datos de un cliente: no es que esté prohibido, es que no tiene por dónde.

Lo único a cuidar: que la aplicación muestre en todo momento que se está en modo demostración, para
que nadie cargue datos reales ahí creyendo que quedan guardados.

### D-57 · Toda entidad de tenant apunta a su empresa, y nunca en cascada · *Revisar*

Detectado leyendo la base real, no el código. En la captura de `usuarios` aparecía esto:

```
fk_usuarios_empresas_empresa_id FOREIGN KEY (empresa_id) REFERENCES empresas(id) ON DELETE CASCADE
```

Dos problemas de una:

**1. Cascada donde no corresponde.** Borrar una fila de `empresas` borraba todos sus usuarios y sus
plantas sin preguntar. Va en contra de todo lo demás: las bajas son lógicas (D-03), la purga de un
tenant es manual y deliberada (D-55), y el historial tiene que sobrevivir. Ahora es `Restrict`: ese
borrado falla, que es exactamente lo que tiene que pasar.

**2. Solo dos de veinte entidades tenían clave foránea a `empresas`.** EF la había descubierto
únicamente en `usuarios` y `plantas`, porque son las dos con navegación declarada desde `Empresa`.
En las otras dieciocho, `empresa_id` era un uuid suelto: una fila con una empresa inexistente era
posible y nada la detectaba.

**Es el mismo patrón que D-02 y por eso se resolvió igual:** la clave foránea ahora la aplica el
`DbContext` por convención sobre `TenantEntity`, no una línea escrita en cada entidad. La integridad
dejó de depender de un detalle de cómo se escribió la clase.

Resultado: 19 de 20 entidades con clave foránea `Restrict`. La única excepción es
`eventos_pendientes`, con su motivo escrito en el código: el respaldo de bitácora guarda también
eventos de plataforma, que no pertenecen a ninguna empresa, y una clave foránea los rechazaría justo
cuando el sistema está degradado —el único momento en que esa tabla se usa—.

Las cascadas que quedan son las nueve deliberadas de D-10, todas relaciones de composición.

**Hay que regenerar la migración.**

### D-58 · La purga de un tenant es manual · **Aprobado**

Nada borra una empresa automáticamente. El mes 5 de D-55 no dispara una purga: marca que los datos
**pueden** purgarse, y alguien decide. La cascada de D-57 hacía justamente lo contrario, y ahora la
base lo impide.

Cuando se purgue, la bitácora se trata así:

| Qué | Se purga |
|---|---|
| Datos operativos del tenant | Sí |
| Bitácora de empresa | Sí |
| **Bitácora de plataforma** | **No** — altas, bajas y cambios sobre la cuenta son registro de MantIA sobre su propia operación, no del cliente |

Así se respeta a la vez que la bitácora no vence (D-41) y que un cliente que se va deja de tener sus
datos en el sistema.

### D-59 · La demostración va por ruta, no por subdominio · **Aprobado**

`/demo` en la misma aplicación. Un subdominio se puede agregar después como un `CNAME` que apunta al
mismo lugar, y es puramente cosmético.

**Por qué no separar la instancia**, que sería el argumento fuerte a favor del subdominio: se
propone para aislar la demostración de los datos reales, y ese aislamiento ya está garantizado por
construcción — la ruta no pasa por `TenantResolver` ni abre el contexto de datos. Una instancia
aparte agregaría un despliegue, un certificado y una configuración más que mantener, para conseguir
algo que ya se tiene.

Navegarla es idéntico a la aplicación real: son las mismas pantallas con los servicios de maqueta en
lugar de los reales. Lo único que cambia es una franja permanente indicando que se está en modo
demostración.

### D-60 · Cada valor cifrado queda atado a su columna · **Aprobado**

Implementado el atado por contexto (AAD de AES-GCM). Al cifrar se pasa `Entidad.Campo`, que **no se
guarda** pero entra en el cálculo de la etiqueta de autenticación. Un valor cifrado en una columna no
descifra en otra.

```
descifra en su propia columna          : True
movido a OTRA columna: rechazado       : OK
mismo valor, otra columna, otro cifrado: True
determinista movido: rechazado         : OK
```

Cero cambios de esquema — el contexto no ocupa un byte en la base—, así que no hace falta regenerar
la migración.

**Un efecto secundario que vale la pena:** en modo determinista, el mismo correo guardado en dos
columnas distintas ahora produce dos textos cifrados distintos. Antes se veían iguales y eso filtraba
información entre tablas.

**Lo que sigue sin cubrir** —copiar un valor cifrado a otra fila de la misma columna— quedó en
`PENDIENTES.md` con el motivo técnico: un conversor de EF Core recibe el valor y nada más, no sabe a
qué fila pertenece. Atarlo a la fila obliga a mover el cifrado a interceptores, y para los campos
deterministas es directamente imposible, porque la consulta que busca por igualdad todavía no conoce
la fila.

### D-61 · Dígito verificador de fila en tabla aparte (DV horizontal) · **Aprobado — implementado**

Lo que pediste en P-11, construido. Tres tablas bajo el régimen: `movimientos_stock`,
`ordenes_trabajo_repuesto` y `repuestos`. El catálogo de qué campos entran está en
`CamposSellados.cs`, una línea por tabla.

**Cómo funciona.** Al guardar, el contexto calcula un HMAC-SHA256 sobre la forma canónica de la fila
y lo escribe en `sellos_fila`, **dentro de la misma transacción**. Si se escribiera después, existiría
una ventana donde la fila está y su dígito no, y toda verificación que cayera ahí reportaría una
manipulación inexistente.

**Por qué en tabla aparte y no en una columna.** Tres razones: editar una fila a mano obliga a tocar
dos tablas en vez de una; la tabla de dígitos puede tener permisos propios —hasta otro esquema con
otro rol de base— mientras las operativas siguen abiertas; y sumar o sacar una tabla del régimen es
una línea en el catálogo, no una migración.

**Se sella el valor del dominio, no el de la columna.** Un campo cifrado se sella por su contenido en
claro. Lo que hay que proteger es el significado —que la cantidad diga 4 y no 40—, no la
representación; y además un campo con cifrado aleatorio produce texto distinto en cada escritura, así
que sellar lo almacenado daría un dígito nuevo cada vez aunque nada haya cambiado.

**Esto cubre lo que quedó abierto en D-60:** el identificador de la fila entra en el cálculo, así que
copiar un valor de la OT #5 a la OT #9 rompe el dígito. Cubre además los campos **no cifrados**
—cantidad, costo, saldo—, que son justamente los que alguien tocaría para inflar un presupuesto y que
el AAD no alcanzaba.

### D-62 · Foto vertical encadenada por tabla (DV vertical) · **Aprobado — implementado**

El dígito de fila detecta que una fila cambió; no detecta que **desapareció**. Quien borre el
movimiento y su dígito juntos deja las dos tablas perfectamente consistentes. Por eso `sellos_tabla`:
cada pasada resume todas las filas de una tabla de una empresa —cuántas son y cuáles son— y encadena
esa foto con la anterior, igual que la bitácora.

**Es una foto periódica y no un valor que se mantiene al día.** Recalcular el resumen de una tabla
entera en cada escritura es carísimo y serializa todas las escrituras contra una misma fila. Corre en
un trabajo de fondo cada 6 horas (configurable en `Verificacion:Intervalo`).

**Lo que el intervalo significa.** Entre dos fotos, un cambio legítimo y uno ilegítimo se ven igual.
Lo que los separa es la bitácora de ese rato. El vertical prueba que *algo* pasó y acota *cuándo*; la
bitácora dice si eso fue una operación real. Achicar el intervalo mejora la precisión del "cuándo" y
cuesta un recorrido completo por empresa.

**Los hallazgos van a la bitácora**, no solo al log del servidor: el log rota, no está encadenado y
no lo lee el cliente. Se registran como acción fallida de `Integridad.Verificar`, que la escala de
severidad sube a **crítica**.

### D-63 · Tercer juego de llaves: `Auditoria:Verificacion` · **Aprobado — implementado**

Los dígitos verificadores usan una llave propia, distinta de la de sellado de bitácora y de la de
cifrado. La bitácora vive en Mongo y los dígitos en PostgreSQL, y no siempre los administra la misma
persona: con una sola llave, quien pueda tocar el motor operativo puede además rehacer los sellos de
la bitácora que deberían delatarlo.

El arranque **falla** si dos de los tres juegos comparten una llave. Repetirla anula exactamente la
separación que justifica tenerlos separados, y es un error de configuración silencioso: el día que se
comete, todo funciona igual de bien.

### D-64 · Documentos adjuntos a las máquinas · **Aprobado — implementado**

Entidad `DocumentoMaquina`, colgada de la máquina y no de la orden. Muchos de estos documentos no
tienen orden que los origine —una habilitación, un manual, la foto de la placa— y los que sí la tienen
igual se buscan por equipo. La orden queda como referencia opcional.

**El archivo no va en la base.** Va a `IAlmacenDocumentos`, direccionado por el SHA-256 de su
contenido. Guardar binarios en PostgreSQL infla las copias de respaldo, castiga cada consulta que
traiga la fila entera y complica cualquier mudanza posterior. Como la ruta sale del hash y no del
nombre, dos personas que suben el mismo certificado escriben un solo archivo, y el nombre original
—que puede traer acentos o barras— nunca toca el disco.

**El hash es su dígito verificador.** Un archivo no se edita, se reemplaza. Antes de entregar una
descarga se recalcula el SHA-256 y se compara: si no coincide, no se entrega y queda registrado. El
sistema no puede ser el que distribuye el certificado falso.

**Vencimientos.** Certificados, habilitaciones y garantías vencen. `FechaVencimiento` con índice, y
la pestaña destaca lo vencido y lo que vence dentro de 30 días. Una habilitación caída se descubre en
una inspección si nadie avisa antes.

**Cifrado:** `Emisor`, `NumeroDocumento` y `Descripcion` entran en `CamposCifrados` como aleatorios
—el emisor de un certificado ES el proveedor del cliente—. El `Titulo` queda en claro porque es por
lo que se busca en la lista.

### D-65 · Control de tipo de archivo por firma, no por extensión · **Aprobado — implementado**

Lista **blanca**: pdf, jpg, png, webp, docx, xlsx. Enumerar lo prohibido es una carrera que se pierde
siempre; enumerar lo permitido falla del lado correcto.

**Ni la extensión ni el tipo declarado se creen**: los dos los elige quien sube. Se comprueban los
primeros bytes del contenido. Un archivo cuya firma no coincide con su extensión se rechaza aunque
las dos cosas por separado estén permitidas —eso no es un error de tipeo— y el rechazo **queda
anotado en la bitácora**, porque el que lo intenta no lo va a mencionar.

Nada comprimido, nada con macros (`.docm`, `.xlsm`), nada ejecutable. Un ZIP hace que el control de
tipos deje de significar algo: lo que importa es lo que hay adentro y eso no se ve desde afuera.
Tope de 20 MB por archivo.

**Límite conocido:** docx y xlsx son ZIP por dentro, así que su firma es la de ZIP. Distingue un
documento real de un ejecutable renombrado, no de otro ZIP. Por eso las variantes con macros están
fuera de la lista y no adentro con una excepción.

### D-66 · Nadie entra sin invitación nominal · **Aprobado — implementado**

No hay registro público ni alta automática por dominio. La única forma de existir en una empresa es
que alguien con autoridad haya emitido una invitación a un correo concreto. El Usuario 0 de cada
cliente incluido: su invitación la emite MantIA al dar de alta la empresa.

**Por qué hay una tabla de invitaciones y no se crea el usuario directo.** El acceso se controla
contra el identificador que asigna el proveedor de identidad, y ese identificador **no se conoce
hasta el primer ingreso**. Un administrador sabe el correo de su empleado, no su `sub` de Google.
Sin este paso intermedio, aprovisionar a alguien sería adivinar un dato que todavía no existe — o
sea que, tal como estaba, no se podía dar de alta a nadie.

Entonces: se invita por correo, y en el primer ingreso el correo se cruza con la invitación y recién
ahí nace la fila en `usuarios` con el identificador real ya atado. `invitaciones` significa "quién
está habilitado a entrar" y `usuarios` significa "quién efectivamente entró".

Las invitaciones **vencen a los 14 días**. Una invitación abierta para siempre es una llave que quedó
puesta: la persona que se fue antes de entrar, el correo mal escrito, el alta que nunca se completó.

### D-67 · El dominio acota a quién se puede invitar, no da acceso · **Aprobado — implementado**

`Empresa.Dominio` se reemplaza por la tabla `dominios_empresa`, con uno marcado como principal.

**Tres cosas cambian y las tres importan.** Una empresa puede tener más de un dominio, que es el caso
de la fábrica que se fusionó y arrastra dos. Dos empresas distintas pueden tener el **mismo** dominio,
que antes el índice único global impedía. Y el dominio dejó de resolver el tenant: ahora solo limita
a qué direcciones se les puede emitir una invitación, y se vuelve a comprobar en cada ingreso por si
la empresa lo dio de baja después.

**Esto es lo que hace viable `gmail.com` como dominio de una empresa**, que era el pedido. Con el
modelo anterior habría significado que cualquier cuenta de Gmail del mundo resuelve a ese tenant. Con
invitación nominal obligatoria no abre nada: sigue sin entrar nadie que el administrador no haya
invitado por su dirección exacta.

Sin dominios cargados no entra nadie. Es la respuesta correcta para una empresa a medio configurar.

### D-68 · Una identidad pertenece a una sola empresa · **Aprobado**

Se mantiene el índice único global sobre `usuarios.auth0_user_id`, y se agrega uno equivalente sobre
`invitaciones.email` filtrado a las pendientes. Sin ese segundo índice, dos empresas podrían invitar
al mismo correo y cuál gana dependería de quién entre primero.

**El costo, asumido:** para probar dos empresas hacen falta dos cuentas de Google distintas, y una
persona que trabaje para dos clientes necesita dos correos. **Lo que se gana:** nunca se puede operar
sobre el tenant equivocado por error, y no hace falta un selector de empresa al ingresar.

### D-69 · La decisión de acceso vive en un solo lugar · **Aprobado — implementado**

Estaba escrita dos veces: en el evento de login de Auth0 y en `TenantResolver`. Dos copias de una
regla de acceso siempre terminan divergiendo, y la que se olvide de actualizarse es la que deja
entrar a quien no debe. Ahora las dos llaman a `IServicioAcceso`.

El acceso se decide **antes** de crear la sesión: si no está habilitado, el pipeline se detiene y
nunca llega a existir una cookie. Dejarlo entrar y bloquearlo después en cada pantalla sería confiar
en que ninguna se olvide de preguntar.

**Todo rechazo se registra**, con el mismo mensaje que ve la persona, así soporte y usuario hablan del
mismo hecho. Un rechazo dice mucho más que un ingreso: es lo único que permite ver que alguien está
probando.

### D-70 · Panel de plataforma como aplicación aparte, fuera de internet público · **Aprobado — pendiente de construir**

El SuperAdmin no opera desde la misma web que los clientes. Proyecto propio, subdominio propio,
alcanzable solo por VPN o lista blanca de IP, doble factor obligatorio y sin sesión compartida.

**Lo que más protege un panel de administración no es cómo esté programado: es que no sea
alcanzable.** Detrás de una VPN, la mayor parte de los ataques deja de existir porque no hay a qué
golpear. Todo lo demás viene después.

**Se descarta la palabra "backdoor" a propósito.** Una puerta trasera es un camino no documentado que
saltea los controles — es contra lo que se defiende el sistema, no algo que convenga construir. Lo
que se construye es una superficie administrativa separada, con más controles y no con menos.

**Y "impenetrable" no existe.** Quien tenga acceso al motor de base puede hacer lo que quiera, y lo
único que cierra ese caso es publicar periódicamente la punta de las cadenas fuera del sistema. El
panel separado sube mucho el costo de entrar; no lo vuelve imposible.

### D-71 · Cuatro superficies separadas, no una aplicación · **Aprobado — pendiente de construir**

El embudo comercial completo es: sitio institucional → landing → cotización → contacto y briefing →
demo → venta → alta del cliente. Eso reparte en cuatro superficies con dueños, ritmos y riesgos
distintos, y conviene que sean cuatro despliegues:

| Superficie | Dónde | Por qué separada |
|---|---|---|
| Sitio institucional y landing | `mantia.com.ar` | Cambia todas las semanas y lo toca marketing. Mezclado con el producto, corregir un título obliga a desplegar el sistema entero |
| Producto | `app.mantia.com.ar` | Detrás de Auth0 e invitación. Cambia cuando cambia el software |
| Panel de plataforma | tercer subdominio | D-70: fuera de internet público |
| Demo | ruta o subdominio propio | Datos de maqueta, sin base real. Ya existe |

**El formulario de cotización es la única escritura a la base expuesta a internet abierto de todo el
sistema.** Todo lo demás está detrás de autenticación y de una invitación nominal. Eso lo convierte
en la superficie más golpeada por defecto — no por interés en MantIA, sino porque los formularios
públicos se llenan solos de basura automatizada. Necesita límite por IP, protección anti-bot, y
escribir en una tabla que **no** pertenezca al modelo de tenants.

**Un prospecto no es una empresa.** Va como entidad de plataforma, sin `EmpresaId` ni filtro de
aislamiento, con los estados del embudo real —Nuevo, Contactado, En briefing, Demo otorgada, Ganado,
Perdido— y el motivo cuando se pierde, que es el dato que después explica por qué no se vende. Al
cerrar la venta se convierte en empresa con `IServicioAltaEmpresa`, y queda el vínculo entre los dos
para poder medir cuántas cotizaciones terminan en cliente.

**Es dato personal de gente que todavía no es usuaria**, así que va cifrado igual que el resto. El
correo determinista, para poder detectar que la misma persona cotizó tres veces.

### D-72 · La demo se accede con enlace por prospecto · **Aprobado — pendiente de construir**

Después del briefing, no desde la landing. A cada prospecto se le genera un enlace propio con
vencimiento.

**Por qué no abierta.** La demo es una etapa de la venta y no parte del folleto. Con enlace propio se
sabe quién la abrió, cuándo y cuántas veces, que es información concreta para decidir a quién volver
a llamar. Abierta a todos, ese dato no existe.

No usa Auth0 ni crea usuarios ni toca la base operativa: sigue siendo la maqueta con datos ficticios.
El token solo abre la puerta de la maqueta, así que filtrarlo no expone nada de ningún cliente.

### D-73 · La prueba es un tenant real y acotado, no una maqueta · **Aprobado — implementado**
### *(reemplaza a D-72)*

D-72 decía que la demo era la maqueta con datos ficticios, accedida por un enlace con token. **Estaba
mal.** La demo es una cuenta real con topes: hasta 5 máquinas, 3 usuarios, 1 planta y 20 órdenes
abiertas, durante 30 días — todo ajustable caso por caso.

**Por qué la versión correcta es mejor, y no es un detalle.** Lo que el prospecto carga durante la
prueba **sobrevive a la compra**. Con una maqueta, todo lo que cargó se pierde justo cuando decide
pagar, que es el peor momento posible para pedirle que vuelva a cargar sus máquinas a mano. Acá el
upgrade no mueve un dato: es el mismo tenant con otros números.

El alta de una prueba es la misma operación que el alta de cualquier cliente, con el plan Prueba:
correo del Usuario 0 y listo. El upgrade es `CambiarPlanAsync`, que corre una fecha y sube unos
topes.

### D-74 · Una empresa tiene una vigencia, sea prueba o cliente pago · **Aprobado — implementado**

La fecha de fin de la prueba y la fecha de renovación de un cliente activo **son el mismo campo**.
`InicioVigencia` y `FinVigencia` en `Empresa`, ajustables caso por caso.

Al llegar la fecha, la empresa pasa a **solo lectura**: entra, consulta, exporta, y no puede cargar
nada. Es el mismo mecanismo que ya existía para la suspensión comercial (D-33), extendido con un
segundo motivo. Se resuelven juntos a propósito — para el cliente es la misma experiencia y para el
código es la misma regla; separarlos invitaría a que alguna pantalla contemple uno y se olvide del
otro.

Un solo mecanismo en vez de dos, y convertir una prueba en cliente deja de ser una transición
especial.

### D-75 · Los cupos se aplican de verdad, y bloquean el alta sin borrar nada · **Aprobado — implementado**
### *(supera a D-54)*

**El problema que había:** `MaxMaquinas`, `MaxUsuarios`, `MaxPlantas` y `MaxMaquinasHabilitadas`
existían como campos y se mostraban en pantalla —el "12 / 200" del panel— pero **nada los aplicaba**.
Se podían cargar quinientas máquinas con un plan de doscientas. Una cuenta de prueba "de hasta 5
máquinas" no significaba nada.

Ahora los aplica `IControlCupos`, y las reglas son tres:

**Manda el número de la empresa, no el del plan.** El del plan es el valor por defecto que se copia
al dar de alta. Después se ajusta por acuerdo comercial sin inventar un plan nuevo para cada cliente,
y queda un solo número que mirar cuando algo se bloquea.

**Bloquea el alta, nunca borra.** Una empresa por encima de su techo —le bajaron el plan, se le venció
la prueba, alguien se equivocó al cargar— conserva todo y solo deja de poder crear más. Bajar un plan
no puede destruir el trabajo de nadie.

**Cuenta lo vivo.** Lo dado de baja no ocupa lugar: el cliente paga por lo que opera, no por su
historial. Las órdenes cuentan solo las abiertas y en curso, y las invitaciones pendientes cuentan
como usuario — si no, una empresa con el cupo lleno podría invitar a veinte personas y el rechazo
aparecería recién en el primer ingreso de cada una.

**Esto supera a D-54**, que había aprobado renombrar `Plan.MaxMaquinas` a `MaxMaquinasPorPlanta`. Si
el tope del plan fuera por planta y el de la empresa total, no serían comparables y "manda el de la
empresa" dejaría de significar algo. Queda como **tope total por empresa**. Si hace falta un límite
por planta, se agrega aparte como una restricción más, no como el mismo número.

### D-76 · El panel de plataforma va expuesto y endurecido, no detrás de una red · **Aprobado — reemplaza el cerrojo de red de D-70**

D-70 decía "fuera de internet público, por VPN o lista blanca de IP". **El argumento en contra es
bueno y gana:** el superadministrador no va a ser una persona sino un equipo comercial. Una lista
blanca no escala a gente que trabaja desde lugares distintos, y una VPN suma fricción y costo por
cada persona que entra al equipo.

Queda: **aplicación aparte, subdominio propio, accesible desde internet, endurecida.** Doble factor
obligatorio, conexión de identidad separada de la de clientes, sesión corta, sin cookie compartida, y
cada acción a la cadena de plataforma con severidad crítica. Es lo que hace todo el mundo con su
panel interno.

Lo que se mantiene de D-70: que sea una **aplicación separada**. Eso no era por la red — era para que
una vulnerabilidad en una pantalla de operación no quede a un paso del panel que administra a todos
los clientes. Esa razón sigue en pie.

El cerrojo de red queda como endurecimiento futuro y opcional, para el día que el panel opere datos
de clientes y no solo cuentas.

### D-77 · El tope de órdenes cuenta las abiertas y es de cien en la prueba · **Aprobado**

Una orden es lo que menos recursos consume del sistema: es manejo interno —"esta máquina necesita
repuestos", "hay que cambiar esta lamparita"— y no toca la ingesta ni el modelo. En los planes pagos
no lleva tope; en la prueba son cien abiertas, que en la práctica es no tener tope y solo existe como
freno ante un uso automatizado.

**Cuenta las abiertas y en curso, nunca el histórico.** Una empresa tiene que poder ver todas las
órdenes que creó, sean de hoy o de hace cinco años. Nada las borra ni las archiva.

### D-78 · La siembra completa los campos que nacieron después de la fila · **Aprobado — implementado**

Solo rellena lo que está en nulo y nunca pisa un valor. Resuelve el caso de una columna agregada al
modelo cuando la fila ya existía: sin esto queda vacía para siempre, y un cupo vacío significa sin
límite, que es justo lo contrario de lo que se busca.

Dos excepciones deliberadas: el tope de órdenes solo se completa en cuentas de prueba, y la vigencia
solo en empresas que no sean MantIA — en los dos casos el nulo es un valor legítimo y no hay forma de
distinguirlo de "todavía no se cargó".

Es andamiaje de desarrollo. Cuando el esquema deje de moverse, este método debería desaparecer.
