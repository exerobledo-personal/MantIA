# Pendientes de definir

Lo que **no** está implementado y necesita una decisión antes de escribir código. Lo ya decidido está
en `DECISIONES.md`.

| # | Qué falta definir | Urgencia |
|---|---|---|
| P-11 | Atado por fila: interceptores o DV de tabla | Se puede postergar — el atado por columna ya está |
| P-04 | Certificados de proveedores: pantalla y flujo diseñados, falta construir | Después del MVP |

Resueltos y movidos a `DECISIONES.md`: P-01 dimensión del vector, P-02 umbral de promoción,
P-03 escala por planta, P-05 estados de morosidad, P-06 numeración, P-07 usuario de demostración,
P-08 bitácora caída, P-09 permisos por ámbito, P-10 huecos en la cadena.

---

### P-11 · Integridad de datos cifrados — falta el atado por fila

**Ya implementado: el atado por columna.** Cada valor cifrado va atado a `Entidad.Campo`, y ese
contexto entra en el cálculo de la etiqueta de autenticación sin guardarse en ningún lado. Mover un
texto cifrado a otra columna o a otra tabla deja de funcionar. Verificado:

```
descifra en su propia columna          : True
movido a OTRA columna: rechazado       : OK
mismo valor, otra columna, otro cifrado: True
```

**Lo que falta: atarlo también al identificador de la fila.** Hoy, copiar la descripción cifrada de
la OT #5 sobre la OT #9 sigue funcionando: misma tabla, misma columna, mismo contexto.

**Por qué no se hizo junto con lo anterior.** El cifrado se aplica con un conversor de EF Core, y un
conversor recibe únicamente el valor: no sabe a qué fila pertenece. Para incluir el identificador
hace falta mover el cifrado a interceptores de guardado y materialización, lo que implica:

- Recorrer las entidades en cada `SaveChanges`, cifrar, y **restaurar el texto en claro después**, o
  el objeto en memoria del que llamó queda con el texto cifrado adentro.
- Un camino de lectura separado que descifre al materializar.
- Unas 150 líneas que se meten en cada lectura y cada escritura del dominio.

**Y hay una limitación que ningún diseño evita:** los campos **deterministas** —`Usuario.Email` y
`Auth0UserId`— no pueden atarse a la fila. Se cifran así justamente para poder buscarlos por
igualdad, y una consulta como "traeme el usuario con este correo" no conoce todavía la fila. Para
esos dos, el atado por columna es el techo.

**Alternativa que cubre lo mismo y más:** el DV vertical y horizontal en tabla aparte. Un dígito por
fila que incluya el identificador detecta la reubicación igual que el AAD, y además cubre los campos
**no cifrados**, que el AAD no toca. Acotado a `movimientos_stock`, `ordenes_trabajo_repuesto` y
`repuestos`.

**Mi recomendación:** hacer el DV de tabla y no los interceptores. Se consigue la misma protección
contra reubicación, se suma la de los campos en claro, y el mecanismo queda afuera del camino de
lectura y escritura del dominio en lugar de atravesarlo entero.

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

