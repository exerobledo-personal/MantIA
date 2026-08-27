# Inventario, depósito y proveedores

Documento de diseño. Traduce a modelo lo que planteó el relevamiento de planta: sector y área,
número de parte, repuestos compartidos entre máquinas, proveedores con su historial de precios,
stock por depósito, y la diferencia entre un repuesto, un insumo y una herramienta.

**Hay un cambio estructural acá adentro** —el stock deja de ser un número por repuesto— y conviene
verlo antes de escribir código, porque toca el libro mayor y los dígitos verificadores que ya están
construidos.

---

## 1. Lo que ya está resuelto

**Repuestos compartidos entre máquinas.** `MaquinaRepuesto` ya es una relación de muchos a muchos: un
tornillo M8 puede estar asociado a cuarenta máquinas sin duplicarse. No hay que construir nada; hay
que **explotarlo**, que es distinto:

- En la ficha del repuesto, "lo usan N máquinas" y cuáles.
- Al dar de baja un repuesto, avisar qué máquinas quedan sin él.
- Y lo que más importa para el motor: **el punto de reposición de un consumible compartido no se
  calcula por máquina, se calcula sobre el consumo agregado.** Un tornillo que consumen cuarenta
  equipos se pide distinto que un rodamiento que usa uno solo.

**El libro mayor de movimientos** ya existe y es inmutable, con su dígito verificador. Todo lo que
sigue se apoya en él.

---

## 2. Sector y área

Hoy `Maquina.LineaSector` es texto libre. Eso significa que "Línea 1", "linea 1" y "L1" son tres
sectores distintos para el sistema, y que no se puede filtrar ni agrupar de forma confiable.

**Propuesta: `SectorPlanta`, jerárquico con padre opcional.** Una sola tabla cubre planta → sector →
área o línea, en los niveles que cada cliente necesite, y la máquina apunta al nodo más profundo.
Inventar dos entidades separadas —una para sector y otra para área— obliga a decidir por el cliente
cuántos niveles tiene su planta, y cada fábrica los tiene distintos.

Un beneficio que aparece solo: **el alcance de los usuarios puede bajar de planta a sector.** Hoy
`UsuarioAlcance` es por planta; el día que un supervisor deba ver solo su línea, la estructura ya
está.

---

## 3. Número de parte: son dos cosas distintas

**El número de parte del repuesto** ya existe (`Repuesto.NumeroParte`): es cómo lo llama el depósito
del cliente, y es único dentro de la empresa.

**El número de parte del fabricante en el despiece de una máquina es otro dato, y es de la
relación.** El mismo tornillo M8 es `TOR-M8-30` en el depósito y `pos. 47 / 0663-2015-40` en el
manual del compresor Atlas Copco. Si se guarda uno solo, el mantenedor que tiene el despiece abierto
no encuentra la pieza en el sistema, que es exactamente cuando la necesita.

**Propuesta:** `MaquinaRepuesto` gana `NumeroParteFabricante` y `Posicion`. Es lo que vuelve
utilizable un despiece y lo que después permite que la ingesta del catálogo cargue estos datos sola.

Aparte, `CatalogoMaquina` debería tener el **código de modelo del fabricante**, distinto del número
de serie: el de serie identifica *esa* máquina, el de modelo identifica a todas las iguales — y es la
clave con la que el catálogo compartido junta la experiencia entre clientes.

---

## 4. Proveedores y precios

Hoy `Repuesto.Proveedor` es un texto cifrado. Eso impide tres cosas que el relevamiento pide:
tener más de un proveedor por repuesto, comparar precios entre ellos, y guardar el historial.

### `Proveedor` como entidad

Razón social, CUIT, contacto, plazo de entrega habitual, estado. De la empresa, no compartido: la
relación comercial de un cliente no se comparte con otro nunca.

### `RepuestoProveedor` — quién vende qué

Muchos a muchos, con **el código con el que ese proveedor lo llama**, su plazo de entrega y si es el
preferido. Ese código propio no es un detalle: es lo que se escribe en el pedido de compra, y sin él
alguien lo busca a mano en un correo viejo cada vez.

### El precio va como historial, no como campo

**"Último precio de compra" es una consulta, no un dato guardado.** Un campo con el último precio
pierde la serie, y en Argentina la serie *es* la información: que un proveedor te viene aumentando
ocho por ciento por mes es algo que solo se ve mirando la sucesión, y es un argumento de negociación
concreto.

`PrecioProveedor`: fecha, proveedor, repuesto, cantidad, precio unitario, **moneda**.

**La moneda no es opcional acá.** Un precio en pesos de hace seis meses no es comparable con uno de
hoy, y compararlos sin decirlo produce conclusiones falsas. Hay dos salidas y conviene elegir una
temprano: guardar el tipo de cambio del día junto al precio, o guardar todo en dólares y convertir
para mostrar. La primera es más fiel a lo que pasó; la segunda es más simple de comparar.

---

## 5. Stock por depósito — el cambio estructural

Hoy `Repuesto.StockActual` es **un número por repuesto**. Con depósitos deja de alcanzar: el mismo
tornillo puede tener 300 en el depósito central y 12 en el pañol de la línea de envasado, y "hay
312" no le sirve al que está parado frente al pañol vacío.

### Qué cambia

**`Deposito`** como entidad de la empresa, asociada a una planta. El depósito central, el pañol de
cada línea, el camión del mantenedor externo si hace falta.

**`StockPorDeposito`**: la existencia real es por par repuesto-depósito. Ahí vive el número que
importa.

**`MovimientoStock` gana el depósito.** Todo asiento dice de qué depósito salió o a cuál entró.

**Aparece un tipo de movimiento nuevo: `Transferencia`.** Mover cien tornillos del central al pañol
no es ni ingreso ni consumo: el stock total de la empresa no cambia. Registrarlo como consumo en uno
e ingreso en otro rompería la trazabilidad y ensuciaría el consumo real, que es justamente lo que
alimenta al motor de recomendaciones.

**`Repuesto.StockActual` queda como total denormalizado**, suma de todos los depósitos. Se conserva
porque el tablero y las alertas lo consultan todo el tiempo y no conviene sumar en cada lectura; ya
está protegido por control de concurrencia y por su dígito verificador.

### Dos cosas que hay que tocar y son fáciles de olvidar

**El catálogo de campos sellados.** `CamposSellados` incluye hoy `MovimientoStock` con sus campos
actuales. **Hay que agregarle el depósito**, o el dígito verificador dejaría de proteger a qué
depósito se imputó un movimiento — que con transferencias es precisamente el campo que alguien
tocaría.

**El mínimo de stock, ¿por repuesto o por depósito?** Los dos tienen sentido: el mínimo general de la
empresa, y el mínimo del pañol de una línea crítica. **Propuesta:** el mínimo vive en el repuesto, y
el depósito puede tener el suyo propio que lo sobrescribe. Es el mismo patrón que ya usamos para los
cupos de plan y empresa, y ya sabemos que funciona.

---

## 6. Repuesto, insumo y herramienta no son lo mismo

Hoy todo es `Repuesto`. Un rodamiento, un litro de aceite y una llave Stilson se comportan distinto y
mezclarlos produce reportes que no significan nada.

| Tipo | Qué le pasa al usarse | Cómo se modela |
|---|---|---|
| **Repuesto** | Se instala y deja de existir como stock | Libro mayor, consumo |
| **Insumo** | Se consume: aceite, trapos, electrodos | Igual que el repuesto |
| **Herramienta** | **No se consume.** Se saca del pañol y se devuelve | Registro de préstamo |

**La herramienta es la que rompe el modelo actual.** Si una llave sale del pañol y se registra como
consumo, el sistema cree que se gastó y va a recomendar comprar otra. Lo que hay que registrar es
**quién la tiene**, desde cuándo y si volvió.

**Propuesta:** un `TipoArticulo { Repuesto, Insumo, Herramienta }` sobre la misma entidad, y que las
herramientas no participen del libro mayor de consumo sino de un `PrestamoHerramienta` con estado,
persona y fechas. Distinguirlo desde el modelo es lo que evita que "faltan tres llaves" y "se
consumieron tres llaves" sean la misma fila.

El **código de barras** es un campo más en el artículo y conviene ponerlo ahora aunque no se use: es
gratis hoy y una migración con datos después. Escanear al sacar y al devolver es la etapa 2 del
préstamo, y ahí el modelo ya va a estar listo.

---

## 7. Qué se rompe de lo que ya existe

Honestamente, para que no aparezca a mitad de camino:

- **`Repuesto.Proveedor` desaparece** como texto y pasa a ser la relación con `Proveedor`. Hay que
  migrar lo que haya cargado.
- **`Maquina.LineaSector` desaparece** como texto y pasa a `SectorPlanta`. Misma migración.
- **`CamposSellados` cambia**: el depósito entra en el dígito de los movimientos.
- **El cupo del plan no contempla artículos.** Con repuestos, insumos y herramientas juntos el
  volumen crece bastante. Falta decidir si `MaxArticulos` es un tope más o si el inventario no se
  acota.

---

## 8. Lo que queda por definir

- **Moneda del historial de precios**: tipo de cambio junto al precio, o todo en dólares.
- **Mínimo por depósito**: si se implementa desde el principio o solo el general.
- **Cupo de artículos** en el plan.
- **Quién puede ver los precios de compra.** Es información comercial sensible: un mantenedor
  necesita saber qué hay en stock, no necesariamente a cuánto se compró. Probablemente sea una acción
  propia sobre el recurso de repuestos, del mismo modo que `Controlar` lo es sobre órdenes.
