# El motor de recomendaciones

Documento de diseño. Responde las cinco preguntas que dejó la corrección de la primera entrega: cómo
funciona el motor, qué datos usa, qué parte es IA y qué parte son reglas, cómo se valida la calidad
de las sugerencias, y cómo se construye el catálogo técnico en los primeros clientes.

---

## 1. Qué predice, concretamente

Tres cosas, y conviene nombrarlas por separado porque se calculan distinto:

| Predicción | Forma de la respuesta | Para qué sirve |
|---|---|---|
| **Ventana de mantenimiento** | "Este equipo entra en ventana entre el 12 y el 26 de septiembre" | Planificar la parada antes de que la imponga una rotura |
| **Repuestos que va a necesitar** | "Con probabilidad alta: rodamiento 6205, correa A-52" | Comprar antes, que es todo el ahorro |
| **Horas de uso hasta el próximo mantenimiento de una parte** | "Al módulo hidráulico le quedan ~380 h" | Decidir si aguanta hasta la parada programada |

Las tres se muestran siempre con su **origen** y su **confianza**. Una recomendación sin eso es una
opinión anónima, y una opinión anónima sobre plata no la sigue nadie.

---

## 2. El problema real: el arranque en frío

Un cliente nuevo tiene **cero historial**. Un modelo entrenado con sus datos no puede existir el día
uno, y sin embargo el sistema tiene que ser útil desde la primera pantalla o no se vende.

Por eso el motor **no es un modelo**: es una escalera de tres niveles, y cuál responde depende de
cuántos datos hay disponibles para esa máquina.

### Nivel 0 · Ficha del fabricante y reglas · *desde el minuto cero*

Sin ningún dato del cliente. Sale de la ficha del catálogo compartido: intervalos recomendados,
modos de falla típicos, repuestos asociados.

> *"Este compresor recomienda cambio de aceite cada 2.000 h. Lleva 1.850."*

Es determinista, explicable, no consume tokens y **ya resuelve un problema real**: las
recomendaciones del fabricante casi nunca se leen, y una alerta en pantalla las vuelve accionables.
Este nivel solo cubre el mantenimiento *preventivo*, no el predictivo — y está bien, porque es el que
la mayoría de las fábricas tampoco está haciendo.

### Nivel 1 · Estadística del catálogo compartido · *con otras empresas usando la misma ficha*

> *"En 23 empresas con este mismo modelo, el rodamiento delantero falla en promedio a las 4.200 h de
> operación. Este equipo lleva 3.900."*

**Este nivel es el diferencial de MantIA frente a un CMMS.** Un CMMS conoce solo lo que cargó su
propio dueño; acá el conocimiento se acumula entre clientes. Es también lo que hace que el producto
mejore para todos cada vez que entra una fábrica nueva.

Nada de esto expone datos de un cliente a otro: lo que se comparte son **estadísticas agregadas
sobre la ficha de máquina**, nunca filas. Un umbral mínimo de empresas antes de publicar una
estadística evita que con dos clientes se pueda deducir de quién salió el dato.

### Nivel 2 · Modelo ajustado al cliente · *con historial propio suficiente*

Ajusta lo anterior a cómo esa fábrica usa realmente sus equipos: turnos, carga, ambiente, calidad
del mantenimiento previo. Una tolva en un molino que trabaja tres turnos no se comporta como la
misma tolva en uno que trabaja uno.

**Las 2 o 3 órdenes de trabajo alcanzan para empezar a corregir el nivel 1, no para entrenar un
modelo.** Conviene ser honesto con eso: con tres puntos no hay curva de supervivencia. Lo que sí se
puede hacer con pocas órdenes es *calibrar* — mover la estimación del catálogo hacia lo que se
observa en esa planta.

### La regla de precedencia

Siempre responde **el nivel más alto que tenga datos suficientes**, y la pantalla dice cuál fue.
Cuando el nivel 2 y el nivel 0 se contradicen, gana el 2 pero se muestran los dos: que el fabricante
diga 2.000 h y en esa planta se rompa a 1.400 es información valiosa, no ruido.

---

## 3. Qué es IA y qué son reglas

La separación no es estética: define qué cuesta plata por consulta, qué es reproducible y qué se
puede auditar.

### Reglas de negocio — determinista, sin costo por consulta

- Umbrales del fabricante y su comparación contra horas de operación.
- Cálculo de stock mínimo y punto de reposición según plazo de entrega y consumo.
- Criticidad del equipo y priorización de alertas.
- Disparo de alertas de stock.
- Toda validación de negocio.

**Es el piso del sistema y tiene que poder funcionar solo.** Si el modelo se cae, el servicio de
modelo no responde o se agota el presupuesto de tokens, MantIA sigue siendo un buen gestor de
mantenimiento preventivo. Eso no es una degradación aceptable: es un requisito.

### Aprendizaje automático — entrenado, sin LLM

- Estimación de la ventana de mantenimiento (**cuándo**).
- Probabilidad de que un repuesto sea necesario en la próxima intervención.
- Detección de patrones de falla recurrentes entre equipos parecidos.

Son modelos chicos y clásicos —supervivencia, regresión, clasificación— entrenados por lote. **No
usan tokens y corren en milisegundos.** Es lo que sostiene el volumen.

### Modelos de lenguaje — solo en dos lugares

1. **Ingesta del catálogo:** leer un manual o una ficha técnica y extraer intervalos, códigos de
   parte y modos de falla.
2. **Normalización semántica:** convertir "hace un ruido raro el motor" y "ruido anormal en
   accionamiento" en el mismo modo de falla. Acá entran los vectores.

**Nunca en el camino de decisión operativa.** Ni para decidir si alertar, ni para calcular un
umbral, ni para recomendar una compra. Dos razones: cuesta por cada consulta y no es reproducible —
la misma pregunta puede dar dos respuestas distintas, y sobre una decisión de compra eso es
inaceptable.

---

## 4. Cómo se valida la calidad

La pregunta que más importa y la más fácil de contestar mal.

### Toda recomendación nace con evidencia

Cada una guarda **qué la disparó**: el origen (regla, estadística, modelo), los datos concretos que
usó, y la confianza. La pantalla lo muestra.

**Regla de oro: una recomendación que no se puede explicar no se muestra.** Un mantenedor con veinte
años de oficio no va a seguir una sugerencia que no entiende, y con dos que le fallen deja de mirar
la pantalla para siempre. La credibilidad se pierde una sola vez.

### El usuario decide y su decisión es el dato

Aceptar o rechazar. **El rechazo pide motivo**, y ese motivo es la señal de entrenamiento más
valiosa que existe: dice exactamente en qué se equivocó el sistema.

### Las métricas

| Métrica | Qué mide | Por qué importa |
|---|---|---|
| **Tasa de aceptación por origen** | Cuántas recomendaciones de cada nivel se aceptan | Si el nivel 2 acepta menos que el 0, el modelo está empeorando las cosas |
| **Acierto de repuesto** | Se recomendó X; ¿la orden posterior usó X? | Es medible sin preguntarle nada a nadie |
| **Anticipación** | Días entre la recomendación y la falla que la habría justificado | Recomendar el día antes no sirve: no da tiempo a comprar |
| **Falsos positivos** | Recomendaciones que vencieron sin que pasara nada | Alertar de más apaga el sistema tan rápido como alertar de menos |

### Backtesting antes de mostrar nada

Contra el historial ya cargado: **¿el modelo habría anticipado las fallas que efectivamente
ocurrieron?** Es la única forma de evaluar un modelo predictivo antes de ponerlo a predecir de
verdad, y se puede hacer con los datos del propio cliente antes de encender el nivel 2 para él.

---

## 5. De dónde salen los datos

### Lo que hay que corregir del plan original

La idea de entrenar con "búsqueda online en páginas de mantenimiento" **no da datos de
entrenamiento**. Da **conocimiento genérico**: intervalos típicos, modos de falla conocidos,
repuestos asociados a un modelo. Eso es exactamente lo que necesita el catálogo para los niveles 0 y
1, y no es poco — pero no entrena un modelo de supervivencia, porque no tiene el par
*(condiciones → falló a las N horas)* que un modelo necesita.

**El dato de entrenamiento real son las órdenes de trabajo cerradas de los clientes.** Eso tiene una
consecuencia de planificación que conviene aceptar ahora: **la iteración 1 no entrena nada. Ingiere,
normaliza y aplica reglas.** El nivel 2 llega con los primeros clientes operando de verdad.

### Sobre qué se ingiere y de dónde

Se extraen **hechos** —intervalos, códigos de parte, modos de falla— y no se reproduce el texto de
los manuales. Además de ser lo correcto con material de terceros, un hecho estructurado sirve para
calcular y un párrafo copiado no.

### El rol de N8N

**Solo ingesta.** Trae y normaliza: fichas de catálogo, manuales, planillas de carga masiva,
certificados. El procesamiento, el análisis y la predicción quedan del lado de MantIA.

Es la división correcta: N8N es bueno moviendo y transformando datos entre sistemas, y es un lugar
malo para poner lógica de negocio que después hay que versionar, testear y auditar.

---

## 6. Iteración 1 — qué se construye primero

### Catálogo semilla de máquinas argentinas

Entre veinte y treinta fichas genéricas de los equipos más comunes en la industria argentina, con
foco inicial en agro y alimentos: **elevadores a cangilones, tolvas, cintas transportadoras, norias,
tambos, secadoras, chimangos, compresores, motores eléctricos, reductores, bombas**.

Cada ficha con sus modos de falla típicos, sus repuestos asociados y sus intervalos recomendados.
**Con eso, un cliente nuevo tiene nivel 0 desde el primer día sin cargar nada.**

### Carga masiva de máquinas

La fábrica manda su listado y se carga completo: código, nombre, número de serie, planta, línea y
ficha de catálogo. Dos cosas que hay que resolver bien:

**El apareo contra el catálogo.** "Compresor Atlas Copco GA37" en la planilla tiene que encontrar la
ficha correcta. Es un problema de coincidencia difusa, y donde falla debe quedar **pendiente de
apareo manual** en vez de inventar una ficha o dejar la máquina huérfana.

**La última fecha de mantenimiento general.** Sin ese dato, el sistema arranca recomendando todo lo
que ya se hizo la semana pasada, y esa es la peor primera impresión posible. Se pide por máquina en
la carga y se acepta "no sé", que también es información: significa arrancar por lo más crítico.

> **Falta en el modelo:** `Maquina` tiene `HorasOperacion` pero no una fecha de último mantenimiento
> general. Hay que agregarla.

### Encendido progresivo del predictivo

| Momento | Qué está activo |
|---|---|
| Alta de la máquina | Nivel 0: intervalos del fabricante contra horas declaradas |
| El catálogo tiene datos de otras empresas | Nivel 1: estadística agregada de la ficha |
| 2 o 3 órdenes cerradas sobre el equipo | Calibración del nivel 1 con lo observado en esa planta |
| Historial suficiente en la empresa | Nivel 2: modelo ajustado, previo backtesting |

---

## 7. Lo que queda por definir

- **El umbral de "historial suficiente"** para encender el nivel 2. Se fija con datos reales, no antes.
- **El mínimo de empresas** para publicar una estadística de catálogo sin que se pueda deducir de
  quién salió.
- **Dónde corre el entrenamiento** y con qué periodicidad.
- **El formato de la planilla de carga masiva.**
- **Qué pasa cuando el cliente no carga horas de operación**, que va a ser el caso más común: hoy es
  un entero que alguien tiene que actualizar a mano. Sin horas reales, el nivel 0 estima por
  calendario y pierde precisión.
