# Arquitectura de MantIA

Documento de referencia técnica. Define cómo está organizada la solución, por qué, y qué
regla sigue cada decisión. Toda incorporación de código nueva se ajusta a esto.

---

## 1. Vista general

```
                    ┌──────────────────────────┐
   Navegador ──WS──>│  MantIA.WEB              │
                    │  Blazor Server           │──┐
                    └──────────────────────────┘  │
                                                  ├──> MantIA.BLL ──> MantIA.DAL ──┬─> PostgreSQL
   N8N ────────HTTP─>┌──────────────────────────┐ │      (negocio)   (persistencia)│   + pgvector
   Integraciones     │  MantIA.API              │─┘                                │
                     │  ASP.NET Core Web API    │                                  └─> MongoDB
                     └──────────────────────────┘                                      (bitácoras)

                          MantIA.BE — entidades, compartidas por todas las capas
```

### Regla de dependencias

Las referencias apuntan siempre hacia adentro, nunca hacia afuera:

```
WEB ──> BLL ──> DAL ──> BE
API ──> BLL
```

`BE` no referencia a nadie. `DAL` no conoce a `BLL`. `BLL` no conoce a `WEB` ni a `API`.
Si en algún momento hace falta romper esto, es señal de que la responsabilidad está en la
capa equivocada.

---

## 2. La decisión central: la WEB no consume la API

**`MantIA.WEB` llama a `MantIA.BLL` directamente. No pasa por HTTP.**

El motivo es concreto: Blazor Server **ya se ejecuta en el servidor**. Si la WEB consumiera
la API, el proceso estaría haciendo una llamada de red a sí mismo — serializar a JSON,
salir por el socket, volver a entrar, deserializar — para terminar en la misma BLL que
tenía a una referencia de distancia. Eso agrega latencia, duplica DTOs y multiplica los
puntos de falla, a cambio de nada.

**`MantIA.API` existe igual y es real, pero con otro consumidor:** N8N y las integraciones
externas. Su razón de ser es que hay procesos fuera del proceso web que necesitan entrar al
dominio: el pipeline de enriquecimiento del catálogo, el onboarding masivo, y a futuro
cualquier integración de terceros.

Esto cumple el requisito de "Servicios REST" de forma honesta: la API tiene clientes de
verdad, no fue creada para justificar una capa.

> **Condición que invalidaría esta decisión:** si alguna vez la interfaz pasara a
> Blazor WebAssembly, el cliente correría en el navegador y no podría alcanzar la BLL. En
> ese escenario la WEB tendría que consumir la API sí o sí. Se evaluó y **se descartó**
> (ver sección 9).

---

## 3. Las capas

### MantIA.BE — entidades del dominio

Clases planas, sin lógica y sin dependencias. Tres bases que **no son un detalle**:

```csharp
BaseEntity      → Id
TenantEntity    → Id + EmpresaId      ← dato PRIVADO de una empresa
CatalogEntity   → Id                  ← dato COMPARTIDO entre todas las empresas
```

Esa distinción es la expresión en código del diferencial del producto: el catálogo técnico
se comparte entre clientes, los datos operativos jamás. **Elegir la base correcta al crear
una entidad es una decisión de negocio, no de tipado.** Si dudás, es `TenantEntity`.

### MantIA.DAL — persistencia

Contiene el `MantIADbContext`, los repositorios y las migraciones. Es la única capa que
conoce Entity Framework Core.

### MantIA.BLL — reglas de negocio

Servicios que orquestan casos de uso. **No conoce EF Core**: habla con repositorios a
través de interfaces. Es la capa que se testea.

### MantIA.API — servicios REST

Controladores para N8N e integraciones. No tiene lógica propia: valida la entrada, llama a
la BLL y traduce el resultado.

### MantIA.WEB — interfaz

Blazor Server con MudBlazor. Las reglas de construcción de pantallas están en
`docs/CONTRATO-UI.md`.

---

## 4. Repositorios por agregado

**No usamos un `RepositorioGenerico<T>`.**

Un repositorio genérico sobre EF Core envuelve `DbSet` en métodos `Listar`, `Crear` y
`Modificar` que hacen exactamente lo mismo que `DbSet`. Es una capa que no decide nada:
en EF Core, `DbSet` **ya es** un repositorio y `DbContext` **ya es** un unit of work.

Usamos repositorios por agregado, con métodos que significan algo del dominio:

```csharp
public interface ICatalogoRepository
{
    Task<CatalogoMaquina?> BuscarPorMarcaModeloAsync(string marca, string modelo);
    Task<IReadOnlyList<CatalogoMaquina>> PendientesDeEnriquecimientoAsync();
    Task<IReadOnlyList<FichaSimilar>> BuscarSimilaresAsync(float[] embedding, int top);
}
```

La última línea es la justificación más fuerte: **la búsqueda por similitud de pgvector
requiere SQL específico.** Si la BLL tocara el `DbContext` directamente, ese SQL quedaría
incrustado en la lógica de negocio. El repositorio es exactamente el lugar donde se esconde.

Beneficios concretos: la BLL nunca ve EF Core, el diagrama de clases de la DAL tiene
contenido real, y los tests unitarios se escriben contra interfaces sin levantar una base.

---

## 5. Multi-tenancy

Es la propiedad más crítica del sistema: **un cliente no puede ver datos de otro, nunca.**

La cadena completa:

```
Auth0 emite JWT  ──>  claims: role, tenant_id
                        │
                        ▼
                  TenantResolver  ──> resuelve la Empresa
                        │
                        ▼
                  ICurrentTenant.EmpresaId
                        │
                        ▼
     MantIADbContext aplica HasQueryFilter sobre toda TenantEntity
```

El filtro se aplica **en el contexto**, no en cada consulta. Eso significa que una consulta
mal escrita en la BLL no puede filtrar datos de otro tenant: el filtro ya está puesto más
abajo.

**Comportamiento ante fallo: denegar.** Si `EmpresaId` queda en null, `PermisoService`
deniega por diseño y `SaveChanges` lanza excepción. Preferimos que el sistema no funcione
antes que filtre datos.

`CatalogEntity` no lleva filtro: es compartida a propósito.

---

## 6. Modelo de seguridad

### Tres ejes, no uno

Lo que habitualmente se llama "rol" son en realidad tres conceptos independientes:

| Eje | Qué determina | Valores |
|---|---|---|
| **Ámbito** | Qué módulos ve el usuario | Operación · Empresa · Plataforma |
| **Rol** | Qué acciones puede ejecutar | Empleado · Supervisor · Gerente · AdminEmpresa · SuperAdminMantIA |
| **Nivel** | Cuánto se recorta ese rol | Jr · Sr (configurable por empresa) |

El **ámbito** explica por qué el menú tiene tres ramas mientras la matriz de permisos tiene
cinco filas: Empleado, Supervisor y Gerente comparten ámbito porque miran los mismos datos,
pero no pueden hacer lo mismo con ellos.

Los ejes son independientes: existe un Supervisor Jr y un Supervisor Sr, pero un
"Gerente Jr" no significa nada. Por eso no se colapsan.

> Esto **precisa** —no contradice— la sección 1.5.2 del documento de visión, que enumera
> cinco perfiles. Los cinco siguen siendo correctos: son el eje "Rol". Falta incorporar el
> ámbito como concepto explícito en el documento.

### Cómo se aplica

El permiso concreto es la combinación **rol + nivel + recurso + acción**, persistida en
`PermisoPorRolYNivel` y configurable por cada empresa (CU-007-003).

```
Usuario (Rol, NivelPermisoId)  ×  Recurso  ×  Acción
                    │
                    ▼
        PermisoService.PuedeAsync(...)     ← cacheado por tenant, invalidado al guardar
                    │
                    ▼
        IAuthorizationHandler custom  ──>  policy de ASP.NET Core
```

Tres capas de aplicación, y las tres son necesarias:

| Capa | Qué hace | Qué NO hace |
|---|---|---|
| **Interfaz** | Oculta lo que el usuario no puede usar | No es seguridad: es comodidad |
| **Ruta** | Bloquea el acceso por URL directa | — |
| **Servicio** | Verifica antes de ejecutar la operación | Es la única barrera real |

**La verificación en el servicio es obligatoria aunque la interfaz ya haya ocultado el
botón.** Ocultar un botón no impide que alguien invoque la operación por otro camino.

`SuperAdminMantIA` tiene bypass explícito en `PermisoService`, y ese bypass se audita
aparte precisamente por ser una excepción (`EventoBitacora.UsoBypass`).

### Cuándo se evalúa el permiso

El permiso se evalúa **en el momento de la acción**, no en el momento del login.

El token que emite Auth0 lleva **identidad, rol y empresa**, y nada más. Los permisos finos
—qué acciones sobre qué recursos— no viajan en el token y por eso no hay nada que refrescar
en él: cuando un administrador quita un permiso, la siguiente acción de ese usuario ya se
evalúa contra el estado nuevo, sin necesidad de que cierre sesión ni de que el sistema
propague nada hacia el cliente.

Esta es la razón de fondo por la que la matriz es dato y no claim. Si los permisos viajaran
en el token, revocar uno obligaría a invalidar tokens, y entre la revocación y el vencimiento
quedaría una ventana en la que el permiso quitado sigue siendo válido. Con evaluación al
momento de la acción esa ventana no existe.

La caché de la matriz es por tenant y se invalida al guardar; sirve para no ir a la base en
cada verificación, no para decidir cuándo aplica un cambio.

### Separación de funciones: el alcance de AdminEmpresa

`AdminEmpresa` opera **solo sobre el ámbito Empresa**. Administrar la empresa —usuarios,
plantas, niveles, matriz de permisos— y operar sobre ella —cerrar o modificar una orden de
trabajo— son funciones distintas y no pueden convivir en el mismo rol.

El riesgo concreto: quien cierra una orden decide qué repuestos se consumieron y con qué
costo. Si además administra la matriz de permisos, puede concederse esa capacidad, usarla y
quitársela después. Es el camino clásico a la manipulación de presupuestos, y no se tapa con
una matriz por defecto restrictiva, porque el mismo rol puede editar esa matriz.

La supervisión que el administrador sí necesita se resuelve aparte, con
`CatalogoPermisos.ConsultaFueraDeAmbitoDe`: una lista cerrada de recursos operativos que el
rol puede **consultar** aunque queden fuera de su ámbito. La única acción admitida ahí es
`Consultar` —ni siquiera `Exportar`, que es sacar datos y no supervisar—, de modo que ninguna
edición de la matriz puede escalar esa lectura a una capacidad de intervención.

### Piso irrevocable por rol

No hay matriz por defecto: qué permisos tiene cada rol al crear una empresa lo decide el
cliente, y va a variar mucho entre una planta de veinte personas y una de trescientas.

Lo que sí está definido en código es el **piso**: `Seguridad/PermisosMinimos.cs` enumera las
celdas que la pantalla muestra bloqueadas y que el servicio rechaza si alguien intenta
desactivarlas por API o por SQL. Un permiso entra en esa lista solo si quitarlo produce uno
de tres efectos:

| Motivo | Qué pasa si se quita | Ejemplo |
|---|---|---|
| **Bloqueo** | Nadie dentro de la empresa puede volver a concederlo | `AdminEmpresa` sin `Permisos.Configurar` congela el tenant |
| **Rendición** | Alguien puede actuar sin que nadie vea lo que hizo | `AdminEmpresa` sin `BitacoraEmpresa.Consultar` |
| **Razón de ser** | El rol queda vacío: el usuario entra y no puede hacer nada | `Empleado` sin `Ordenes.Consultar` |

El criterio es estrecho a propósito. Cosas que en la práctica casi siempre se van a conceder
—que un supervisor cierre órdenes, por ejemplo— quedan afuera, porque son decisiones de
organización del cliente. **Una lista de mínimos larga es una matriz por defecto disfrazada.**

`PermisosMinimos.EsMinimo` lo consulta también el evaluador, *antes* de mirar la matriz: si
una fila con `Concedido = false` llegara a la base por una migración vieja o por SQL directo,
el permiso se concede igual.

---

## 6-bis. Auditoría, datos sensibles y rollback

### Dos bitácoras, no una

La carpeta pide trazabilidad; el modelo la separa por destinatario, porque no son el mismo
lector ni el mismo dato.

| Bitácora | Alcance | Quién la lee | Qué registra |
|---|---|---|---|
| **Empresa** | Un tenant | `AdminEmpresa` | Acciones de los usuarios de esa empresa |
| **Plataforma** | Transversal | `SuperAdminMantIA` | Altas y bajas de empresas, cambios sobre cuentas admin, usos del bypass |

`EventoBitacora` distingue además el **tipo**: transacción (cambio de dominio), auditoría
(cambio de configuración o de permisos) y excepción (error). Los tres van al mismo motor
—MongoDB— porque los tres son escritura secuencial de alto volumen y esquema variable.

### Integridad antes que confidencialidad

Para una bitácora, **el ataque relevante no es leerla: es alterarla**. Un registro que se
puede editar sin dejar rastro no sirve como evidencia, esté cifrado o no.

Por eso cada evento guarda `Secuencia`, `HashAnterior` y `Hash`: el hash de cada entrada
incluye el de la anterior, de modo que modificar o borrar un evento intermedio rompe la
cadena de ahí en adelante y la verificación lo detecta. El cifrado en reposo lo provee el
motor de base de datos; no es algo que la aplicación deba reimplementar.

### Clasificación de datos sensibles

`Auditoria/DatosSensibles.cs` clasifica campo por campo en tres niveles —público,
enmascarado, omitido— y esa clasificación se aplica **al escribir en la bitácora**.

El motivo es específico: una bitácora se consulta, se exporta y se comparte mucho más que la
base operativa. Es el lugar donde un dato sensible termina filtrándose sin que nadie lo note.
El identificador de Auth0 y el costo unitario de un repuesto se omiten; el correo se
enmascara.

### Rollback de acciones

Caso de uso previsto: alguien con permisos legítimos hace daño deliberado —"me despidieron,
borro esta orden"—. `SolicitudRollback` cubre la reversión con tres reglas:

1. **No borra historia.** Cada acción revertida genera su propio evento de bitácora. La
   bitácora crece, nunca se reescribe.
2. **Cuatro ojos.** `AprobadaPorUsuarioId` debe ser distinto de `SolicitadaPorUsuarioId`.
3. **Reversión parcial explícita.** Si algunos eventos no se pueden revertir —porque el
   estado posterior ya cambió— la solicitud queda en `AplicadoParcial` y registra cuáles
   quedaron afuera, en lugar de fallar entera o mentir que se aplicó.

La reversión es posible porque `EventoBitacora` guarda `EstadoAnterior` y `EstadoPosterior`
serializados: el evento no dice solo "se modificó la orden X", dice cómo estaba antes.

---

## 7. Datos

| Motor | Qué guarda | Por qué ahí |
|---|---|---|
| **PostgreSQL** | Todo el dominio operativo y el catálogo | Relacional, transaccional, multi-tenant por filtro |
| **pgvector** | Embeddings del catálogo y de las órdenes cerradas | Normalización semántica: agrupar descripciones equivalentes de la misma falla |
| **MongoDB** | Bitácoras de transacciones, auditoría y excepciones | Volumen alto, escritura secuencial, esquema variable |

Sobre pgvector, conviene tener presente qué problema resuelve: **no es búsqueda, es
normalización.** Dos técnicos describen la misma falla de forma distinta —"fuga entre
placas por junta vencida" y "pérdida en el paquete de placas"— y sin agrupar
semánticamente el contador de frecuencia nunca alcanza el umbral que convierte una
observación en conocimiento del catálogo.

### Aislamiento del conocimiento compartido

El catálogo se comparte, pero **una falla en una planta no es una falla del modelo**. Por eso
el conocimiento tiene dos niveles:

1. **Ficha de referencia** — del fabricante y del modelo de lenguaje. Compartida. Sin datos
   de clientes.
2. **Evidencia agregada** — derivada de órdenes cerradas reales. **Privada de cada empresa
   hasta que se corrobora.** Asciende a conocimiento compartido sólo al superar un umbral
   (por ejemplo, tres empresas distintas y cinco eventos).

La promoción produce únicamente el modo de falla y su frecuencia. **Nunca el texto original
ni la identidad de la empresa.**

---

## 8. Entornos

| Entorno | Dónde | Para qué |
|---|---|---|
| **Local** | `docker-compose.yml` — PostgreSQL+pgvector, MongoDB, N8N | Desarrollo diario |
| **Demo** | Render, contenedor Docker | Mostrar la maqueta sin costo |
| **Producción** | Azure App Service + Azure Database for PostgreSQL | Destino definido en el documento de visión |

**El desarrollo se hace contra Docker local.** Escribir entidades y migraciones implica
romper y recrear la base muchas veces; hacerlo contra la nube es más lento y consume
crédito sin aportar nada.

Notas de Azure para cuando llegue el momento:

- pgvector está soportado en Flexible Server: se habilita en la allowlist y después
  `CREATE EXTENSION vector`.
- **La extensión se llama `vector`, no `pgvector`.** Es un tropiezo habitual.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` es obligatorio detrás de cualquier proxy que
  termine TLS. Sin eso la aplicación arma sus URLs absolutas con `http`.

---

## 9. Fuera de alcance

### Operación offline

**Descartada.** Blazor Server ejecuta en el servidor y el navegador sólo mantiene un
WebSocket: sin conexión no hay dónde ejecutar la aplicación. Soportar offline exigiría
migrar a WebAssembly o a una app híbrida.

Pero el costo real no es la migración: **es la sincronización.** Dos técnicos que cierran
órdenes sin conexión consumiendo el mismo repuesto generan un conflicto de stock que
requiere reglas de resolución explícitas. Eso agrava un problema que todavía está abierto
incluso en modo conectado (ver sección 10).

> **Corregir en el documento de visión:** la sección 2.1.4 afirma que la conectividad
> disponible "elimina la necesidad de desarrollar capacidades offline", mientras que la
> sección 1.9 declara como valor nuclear el diseño para "entornos con baja conectividad".
> Las dos no pueden ser ciertas a la vez. La redacción correcta del valor nuclear es
> **"tolerancia a interrupciones breves de conectividad"**, que es lo que el sistema
> efectivamente ofrece mediante la reconexión automática del circuito.

### También fuera de la Fase 1

Integración con ERP de terceros, aplicación móvil nativa, gestión de compras, y facturación.

---

## 10. Deuda conocida del modelo actual

### Resuelta al completar `MantIA.BE`

| Deuda | Cómo se resolvió |
|---|---|
| **Estados como texto libre** | 16 enums en `Common/Enums.cs`, persistidos |
| **Sin control de concurrencia** | `IConcurrencia` sobre `xmin` de PostgreSQL, más el patrón de libro mayor: `MovimientoStock` es inmutable, así dos operaciones simultáneas insertan filas distintas y solo compite el contador denormalizado |
| **Sin campos de auditoría** | `BaseEntity` con `FechaCreacion` / `CreadoPorUsuarioId` / `FechaModificacion` / `ModificadoPorUsuarioId`, más el subsistema de bitácora de la sección 6-bis |
| **Colecciones serializadas a texto** | `CatalogoFallaComun` y `CatalogoRepuestoSugerido` como entidades relacionales |
| **El modelo va detrás de la interfaz** | `Maquina` reconciliada con el modelo de vista |
| **FKs pendientes** | `Plan` y `Planta` creadas |
| **Sin columna de embedding** | `EvidenciaModelo.Embedding` como `float[]`; la DAL lo mapea a `vector` |

### Pendiente, anotado y no construido

**Escala de máquinas por planta.** El supuesto actual —del orden de 15 máquinas por planta—
viene de la maqueta, no del campo: una planta real puede estar bastante por encima, y 150 ya
es un número alto para una sola. El rango es demasiado ancho para fijarlo ahora, y afecta
tres cosas a la vez:

- los límites de `Plan` (`MaxMaquinas`), que hoy asumen el extremo bajo;
- las pantallas de listado, que hoy renderizan todo sin paginación ni búsqueda del lado del
  servidor;
- el agrupamiento: con volumen alto la unidad de trabajo deja de ser la máquina y pasa a ser
  la **línea de producción**, que hoy es un texto en `Maquina` y debería ser una entidad.

Se decide con datos de los primeros clientes, no antes. Lo que sí hay que evitar es escribir
ahora consultas o pantallas que solo funcionen con el extremo bajo del rango.

**Certificados de mantenimiento de proveedores.** Cuando un proveedor externo interviene una
máquina entrega un certificado o informe de servicio. Hoy no hay dónde guardarlo, y es
información de primera mano sobre el historial de esa máquina: qué se cambió, cuándo, y con
qué repuesto.

Encaja como un tipo de documento adjunto a `Maquina` —y opcionalmente a una `OrdenTrabajo`—
que además entra a la capa semántica: el texto del certificado se vectoriza y alimenta la
misma normalización de fallas que hoy solo se nutre de órdenes cerradas. Es la vía más
directa para que el catálogo técnico de un cliente nuevo arranque con contenido real en lugar
de esperar a que se acumulen órdenes propias.

Falta definir: entidad `DocumentoMaquina`, almacenamiento del binario (fuera de PostgreSQL),
extracción de texto —muchos llegan escaneados— y si la vectorización es automática o requiere
validación previa.

---

## 11. Convenciones

- **Idioma:** dominio y código de negocio en español; términos técnicos del framework en
  inglés cuando no tienen traducción establecida.
- **Migraciones:** Code First, versionadas, nunca editadas después de aplicadas.
- **Fechas:** `DateTimeOffset` en UTC. La conversión a hora local es responsabilidad de la
  interfaz.
- **Identificadores:** `Guid` generados en la aplicación.
- **Comentarios:** explican *por qué*, no *qué*. El código ya dice qué hace.
