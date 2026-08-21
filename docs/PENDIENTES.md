# Pendientes de definir

Lo que **no** está implementado y necesita una decisión antes de escribir código. Lo ya decidido está
en `DECISIONES.md`.

| # | Qué falta definir | Urgencia |
|---|---|---|
| P-11 | AAD contra reubicación, y el DV de tabla | **Antes de que haya datos reales** |
| P-04 | Certificados de proveedores: pantalla y flujo diseñados, falta construir | Después del MVP |

Resueltos y movidos a `DECISIONES.md`: P-01 dimensión del vector, P-02 umbral de promoción,
P-03 escala por planta, P-05 estados de morosidad, P-06 numeración, P-07 usuario de demostración,
P-08 bitácora caída, P-09 permisos por ámbito, P-10 huecos en la cadena.

---

### P-11 · Integridad de datos cifrados — queda el punto 2

Los otros dos puntos ya se resolvieron y están en `DECISIONES.md` (D-53).

**Lo que falta: que alguien mueva un valor cifrado de una fila a otra.** Hoy las dos filas
descifran perfecto — GCM protege el contenido, no la posición.

Dos caminos, no excluyentes, y conviene hacer los dos en este orden:

| Camino | Cómo funciona | Qué cuesta |
|---|---|---|
| **AAD** (*additional authenticated data*) | Al cifrar se pasa un contexto —tabla, columna, id de fila— que no se guarda pero entra en la etiqueta. Mover el valor hace fallar el descifrado | Ninguna columna nueva. Hay que resolver que el conversor de EF conozca el id de la fila al cifrar, que hoy no lo tiene a mano |
| **DV vertical y horizontal en tabla** | Un dígito por fila —que incluye el id— y otro por columna sobre el conjunto. Modificar una celda obliga a recalcular los dos, y la inconsistencia delata el cambio | Una tabla extra y recálculo del DV de columna en cada escritura, que es la parte cara |

El DV horizontal cubre además los campos **no cifrados**, que AAD no toca. Acotado a las tres tablas
donde una alteración tiene consecuencia económica directa: `movimientos_stock`,
`ordenes_trabajo_repuesto` y `repuestos`.

**Advertencia:** aplicar AAD sobre datos ya escritos los vuelve indescifrables. Hay que hacerlo
**antes de que exista el primer dato real**, o migrar descifrando con el esquema viejo y recifrando
con el nuevo. Hoy la base está vacía, así que la ventana está abierta.

### P-04 · Certificados de mantenimiento de proveedores — diseñado, falta construir

No es necesario para el MVP, pero el diseño queda cerrado para poder sumarlo cuando convenga.

**El flujo, en tres pasos:**

1. **Subida.** Desde la ficha de la máquina, pestaña *Documentos*: se arrastra el PDF o la foto, se
   elige el tipo —certificado de mantenimiento, informe de servicio, garantía—, el proveedor y la
   fecha de intervención. Opcionalmente se lo vincula a una orden de trabajo existente.
2. **Extracción.** Un proceso en N8N saca el texto. Los que llegan escaneados pasan por OCR. El
   resultado queda como borrador con estado *pendiente de validación*, nunca directo al catálogo.
3. **Validación.** Alguien de operación revisa lo extraído en una pantalla de dos columnas —el
   documento a la izquierda, lo que el sistema entendió a la derecha, editable— y confirma. Recién
   ahí el texto se vectoriza y alimenta la normalización de fallas.

**Por qué la validación no es opcional.** Un certificado escaneado y pasado por OCR trae errores, y
ese texto va a terminar influyendo en qué repuestos recomienda el sistema. Que un dato sin revisar
entre al motor es exactamente el tipo de cosa que hace perder credibilidad a las recomendaciones.

**Lo que hay que construir:** entidad `DocumentoMaquina` (tipo, proveedor, fecha de intervención,
estado de extracción, texto extraído, referencia al binario), almacenamiento del archivo fuera de
PostgreSQL —disco o almacenamiento de objetos, no la base—, el flujo de OCR en N8N, y las dos
pantallas.

**Detalle que conviene no olvidar:** el texto extraído es texto libre escrito por terceros, así que
entra en `CamposCifrados` como aleatorio, igual que las descripciones de orden.

