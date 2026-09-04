# Notas de Negocio — Sorteos LOTENAL (Lotería Nacional)

> Documento para futura integración con backend.
> Reglas extraídas del modelo `Sorteos.php` (backend) y del calendario
> oficial de Lotería Nacional.
> **No modificar el backend.** Esta es documentación de referencia únicamente.

## Modelo de Datos: tabla `sorteos`

| Campo                       | Tipo         | Descripción                                              |
|-----------------------------|--------------|----------------------------------------------------------|
| `id_sorteo`                 | int (PK)     | Identificador único del sorteo                           |
| `numero_sorteo`             | string(50)   | Número visible del sorteo (ej. "4024", "2894"). Único.  |
| `nombre_sorteo`             | string(200)  | Nombre completo (ej. "SORTEO MAYOR")                     |
| `tipo_sorteo`               | enum         | `loteria_nacional`, `sorteos_tec`, `zodiaco`, `sorteo_especial`, `universitarios`, `prorrapidos`, `rascables` |
| `subcodigo_sorteo`          | string(3)    | Subcódigo de 3 dígitos del barcode. Ver mapa abajo.      |
| `id_producto`               | int (FK)     | Relación a `productos.id_producto` (nullable)            |
| `fecha_sorteo`              | date         | Fecha de celebración del sorteo                          |
| `fecha_inicio_ventas`       | date (null)  | Inicio del periodo de ventas                             |
| `fecha_cierre_ventas`       | date (null)  | Cierre del periodo de ventas                             |
| `fecha_limite_pago`         | date (null)  | Límite para pago de premios                              |
| `fecha_vencimiento`         | date (null)  | Vencimiento del billete                                  |
| `precio_billete_completo`   | decimal      | Precio del billete completo                              |
| `precio_fraccion`           | decimal(null)| Precio por fracción                                      |
| `total_series`              | int (null)   | Total de series emitidas                                 |
| `total_billetes`            | int (null)   | Total de billetes por serie                              |
| `vigente`                   | bool         | 1 = sorteo activo/vigente                                |
| `celebrado`                 | bool         | 1 = sorteo ya celebrado                                  |
| `cerrado`                   | bool         | 1 = sorteo cerrado (no más ventas ni entregas)           |
| `sabana_cargada`            | bool         | 1 = sábana de premios cargada                            |

## Mapa de Subcódigos (barcode V4)

| Subcódigo | Tipo de Sorteo           | idProducto LOTENAL |
|-----------|--------------------------|--------------------|
| 140       | SORTEO MAYOR             | 1                  |
| 261       | SORTEO SUPERIOR          | 2                  |
| 516       | SORTEO ESPECIAL          | 5                  |
| 446       | SORTEO ZODIACO           | 4 (zodiaco)        |
| 448       | SORTEO ZODIACO ESPECIAL  | 4 (zodiacoe)       |

> Los tipos GRAN ESPECIAL, MAGNO y GORDITO NAVIDEÑO no tienen subcódigo
> asignado en el backend actualmente. Verificar al integrar.
> Magno = idProducto 3, Gordito = idProducto 6 en el calendario LOTENAL.

## Calendario Oficial LOTENAL

Fuente: https://www.loterianacional.gob.mx/Billete/CalendarioSorteos
API interna: `POST /Billete/JsCalendarioDatos` con parámetro `Idcalendario=1`
(sorteos tradicionales).

### Frecuencia de Sorteos (calendario 2026)

| Tipo                | Frecuencia             | Día típico        |
|---------------------|------------------------|-------------------|
| SORTEO MAYOR        | Semanal                | Martes            |
| SORTEO SUPERIOR     | Semanal (con saltos)   | Viernes           |
| SORTEO ZODIACO      | Semanal                | Domingo           |
| SORTEO ZODIACO ESP. | Aprox. cada 4 semanas  | Domingo (alterna) |
| SORTEO ESPECIAL     | Mensual                | Variable          |
| SORTEO MAGNO        | Eventos especiales     | 16 Sep, 31 Dic    |
| SORTEO GORDITO      | Navideño               | 24 Dic            |
| SORTEO GRAN ESP.    | No en calendario actual| —                 |

> Zodiaco y Zodiaco Especial交替 los domingos: cuando hay Zodiaco Especial,
> ese domingo NO hay Zodiaco regular.

### Numeración observada (agosto-diciembre 2026)

| Tipo            | Rango de números    | Incremento |
|-----------------|---------------------|------------|
| Mayor           | 4024 → 4040         | +1 semanal |
| Superior        | 2894 → 2907         | +1 semanal |
| Zodiaco         | 1757 → 1769         | +1 semanal |
| Zodiaco Esp.    | 1754 → 1771         | +1 (misma serie que Zodiaco) |
| Especial        | 315 → 319           | +1 mensual |
| Magno           | 392, 393            | +1 por evento |
| Gordito         | 225                 | Anual       |

> **Importante**: Zodiaco y Zodiaco Especial comparten la misma secuencia
> de numeración (ej. No.1758 puede ser Zodiaco Especial el 23/08 mientras
> que el No.1759 es Zodiaco regular el 30/08).

## Estados de un Sorteo

| Estado     | Condición SQL                              | Significado                          |
|-----------|---------------------------------------------|--------------------------------------|
| Vigente   | `vigente = 1`                               | Activo para venta                    |
| Pendiente | `vigente = 1 AND celebrado = 0 AND fecha_sorteo >= today` | Activo y aún no celebrado |
| Celebrado | `celebrado = 1`                             | Ya se realizó el sorteo              |
| Cerrado   | `cerrado = 1`                               | No más ventas ni entregas            |

**Scope `pendientes()`**: son los sorteos disponibles para venta (vigentes, no celebrados, con fecha futura o de hoy).

## Regla de Disponibilidad Implementada (app)

**Regla actual (solo fecha)**:
```
EstaDisponible = FechaCelebracion >= hoy
```

Un sorteo con fecha de celebración ya pasada NO aparece como disponible.

Ejemplo con fecha actual 19/08/2026:
- Sorteo No.4024 (11/08/2026) → NO disponible (fecha pasada)
- Sorteo No.315 (18/08/2026) → NO disponible (fecha pasada)
- Sorteo No.2895 (21/08/2026) → Disponible
- Sorteo No.4025 (25/08/2026) → Disponible

**Regla futura (cuando se conecte al backend)**:
```
vigente = 1 AND celebrado = 0 AND fecha_sorteo >= today
```
El scope `pendientes()` del backend ya aplica estas tres condiciones.
Adicionalmente se podrá validar `fecha_cierre_ventas` y `cerrado`.

## Endpoints del Backend (relacionados)

| Método | Ruta                                     | Descripción                                    |
|--------|------------------------------------------|------------------------------------------------|
| GET    | `/api/sorteos?vigente=1`                 | Lista sorteos. Filtros: vigente, id_producto, tipo, id_cedis |
| GET    | `/api/sorteos-billetero/{id_billetero}`  | Sorteos asociados a un billetero               |
| GET    | `/api/almacen/sorteos-disponibles`       | Sorteos disponibles para entrega en almacén   |
| GET    | `/api/cierres-sorteo/sorteos-disponibles`| Sorteos disponibles para cierre               |
| GET    | `/api/volantes/nacional/sorteos`         | Sorteos para volantes de lotería nacional     |

## Reglas para Futura Integración

1. **Sorteos activos para venta**: usar `Scope::pendientes()` o `GET /api/sorteos?vigente=1`
   filtrando additionally por `celebrado=0` y `fecha_sorteo >= hoy`.

2. **Identificación del tipo**: usar `subcodigo_sorteo` (3 dígitos) para mapear
   el sorteo a su tipo en la app. El `tipo_sorteo` legacy también existe pero
   el subcódigo es el identificador V4 del barcode.

3. **Fecha de celebración**: campo `fecha_sorteo` (date). Cuando se consulta
   con `id_cedis`, el backend hace `COALESCE(ic.fecha_sorteo, s.fecha_sorteo)`
   para usar la fecha del inventario del CEDIS si difiere.

4. **Precio**: cada sorteo individual tiene su propio `precio_billete_completo`.
   Aunque los 8 tipos tienen precios fijos en la UI ($30, $40, etc.), el backend
   permite precios variables por sorteo individual.

5. **Sede scoping**: al consultar sorteos disponibles, pasar `id_cedis` del
   usuario para obtener solo los sorteos con inventario en su CEDIS.

6. **Cierre de ventas**: verificar `fecha_cierre_ventas` antes de permitir
   vender un sorteo. Si la fecha ya pasó, el sorteo no debería aparecer.

7. **Sorteo cerrado**: si `cerrado=1`, no permitir ventas ni entregas.

8. **Relación con billetes**: `billetes_loteria` tiene FK `id_sorteo`.
   Cada billete pertenece a un sorteo específico.

9. **Numeración de Zodiaco**: Zodiaco y Zodiaco Especial comparten la misma
   secuencia de números de sorteo. Al filtrar por tipo, usar el `subcodigo_sorteo`
   (446 vs 448) para distinguir, no el número.

10. **Calendario oficial vs backend**: el calendario LOTENAL puede tener sorteos
    que el backend aún no registre (y viceversa). El backend es la fuente de
    verdad para disponibilidad real (inventario, vigente, cerrado).

## Pantallas de la App (flujo LOTENAL)

1. **Pantalla 6 (SorteosLotenalPage)**: muestra 8 tipos de sorteo con precio.
   No consulta backend. Datos estáticos.

2. **Pantalla 7.x (SorteosActivosPage)**: muestra sorteos activos del tipo
   seleccionado. Cada botón muestra: No. de sorteo + fecha de celebración.
   Filtra por fecha >= hoy (EstaDisponible).
   **Futuro**: consultar `GET /api/sorteos?vigente=1` filtrando por
   `subcodigo_sorteo` o `tipo_sorteo` del tipo seleccionado.
   Al seleccionar un sorteo navega a la pantalla 8 (Seleccionar Ciudad)
   pasando `sorteoId` + `tipoSorteoId`.

3. **Pantalla 8 (SeleccionCiudadPage)**: ciudades/CEDIS donde se vende el
   sorteo seleccionado. Ver reglas abajo.

4. **Pantallas 9.1/9.2 (AgregarBoletosPage)**: tiendas/boletos disponibles
   del sorteo elegido en la ciudad elegida. Ver reglas abajo.

## Pantalla 8 — Seleccionar Ciudad (reglas)

**Regla UI (implementada)**: la opción **"CUALQUIER CIUDAD"** es SIEMPRE el
primer elemento de la lista; después van las ciudades/CEDIS correspondientes
en orden alfabético. Formato de botón: `CIUDAD, ESTADO` (texto centrado).

**Datos actuales (mock)**: las 9 ciudades del mockup provienen del sistema
anterior Sr. Billetero (Cardenas, Chihuahua, Coatzacoalcos, Cordoba,
Minatitlan, Oaxaca, Orizaba, Pijijiapan, Tapachula).

### Endpoint backend para integrar

`GET /api/cedis` (CedisController::actionIndex):
- Ordena por `nombre_cedis` ASC.
- Filtro opcional `id_zona`.
- **Sede-scoping**: usuario `corporativo` ve todos los CEDIS; usuario con
  `id_cedis` ve solo su CEDIS. `pagination=false` (lista completa).

### Hallazgos al integrar (verificados en BD real)

1. **Desincronización modelo/tabla**: el modelo `Cedis.php` declara atributos
   `ciudad`/`estado`, pero la tabla real `cedis` NO tiene esas columnas
   (tiene `codigo_org_ln`/`numero_tienda_ln`). La ciudad va embebida en
   `nombre_cedis` (ej. "CEDIS Puebla"). El endpoint devolvería
   `ciudad: null`. **Resolver en el backend antes de consumir** (añadir
   columnas, derivar la ciudad de `nombre_cedis`, o usar catálogo).

2. **CEDIS reales en BD (sep-2026)**: CEDIS Puebla (zona 1), CEDIS Toluca
   (zona 2), CEDIS Guadalajara (zona 3), CEDIS Monterrey (zona 4) — no
   coinciden con las ciudades del mockup (Sr. Billetero operaba en
   Tabasco/Veracruz/Chiapas). La lista real saldrá del endpoint.

3. **Columna `ciudad` en otras tablas**: `tiendas_sucursales` sí tiene
   `ciudad` (el scoping de tiendas es por `id_tienda`). Si la pantalla
   termina listando ciudades de expendios, esa es la fuente correcta.

4. **"Cualquier ciudad"**: no existe en el backend; es una opción de UI
   del flujo Sr. Billetero ( IdCiudad = 0 ). Su semántica de negocio
   (¿todos los CEDIS del usuario? ¿sin filtro en consulta de billetes?)
   debe confirmarse con el área de negocio al integrar.

## Pantallas 9.1/9.2 — Agregar Boletos (reglas)

Aparece tras seleccionar la ciudad (pantalla 8). Recibe vía navegación:
`sorteoId` + `tipoSorteoId` + `ciudadId` (0 = "Cualquier ciudad").

### Reglas UI (implementadas)

1. **Header**: flecha ←, título "Agregar Boletos", carrito blanco en la
   esquina superior derecha. Ambos carritos (header y fila) son UI-only:
   su función corresponde a las pantallas 10.x (cantidad de boletos) y
   11 (carrito de compras).

2. **Fila normal (9.1, tipos no zodiacales)**: línea 1 = número de
   billete + fracciones (ej. "0/20"); línea 2 = CIUDAD, ESTADO. Botón
   carrito oscuro a la derecha de cada fila.

3. **Fila zodiaco (9.2, tipos 3 y 4)**: añade la línea de SIGNO en
   mayúsculas entre el número y la ciudad: número + fracciones /
   SIGNO / CIUDAD, ESTADO.

4. **Filtro por ciudad (viene de la pantalla 8)**: "Cualquier ciudad"
   (IdCiudad = 0) muestra TODAS las filas; una ciudad específica
   muestra solo sus filas.

5. **Selector de signo (solo zodiaco)**: fila filtro "Signo Aleatorio ▼"
   (flecha pegada al texto, alineada a la izquierda como en el mockup)
   sobre la lista; al tocarla se despliega un selector de 13 opciones
   ("Signo Aleatorio" SIEMPRE primero + 12 signos en orden zodiacal,
   texto centrado, mismo estilo que la pantalla 8). "Signo Aleatorio" =
   sin filtro (mismo patrón que "Cualquier ciudad"); un signo
   específico filtra la lista por su nombre en mayúsculas. Al abrirse,
   el selector CUBRE todo el contenido bajo el header (fondo blanco,
   lista a pantalla completa — panel central del mockup 9.2). Se
   cierra al elegir una opción O al tocar fuera de las opciones
   (incluidos los espacios entre opciones); cerrar fuera NO cambia el
   filtro. Defensa anti "tap fantasma": los primeros 600 ms tras la
   carga se ignora el toggle, porque en Android el tap que elige la
   ciudad (pantalla 8) puede atravesar la transición y golpear la fila
   filtro recién creada, abriendo el selector sin que el usuario lo
   pida (bug reportado en inspección; ver
   AgregarBoletosViewModel.MsIgnorarToggleTrasCarga).

6. **Orden de la lista**: por número de billete ascendente por VALOR
   numérico (no lexicográfico: "739" va antes que "1861"; el mockup
   muestra 739 primero).

7. **Signos sin acentos**: el selector usa los nombres tal cual
   aparecen en Sr. Billetero ("Geminis", "Cancer", "Escorpion",
   "Sagitario").

### Datos actuales (mock)

- **9.1 (normal)**: 9 filas transcritas verbatim del mockup (números
  2168–45449, fracciones "0/17" a "0/20", 6 ciudades).
- **9.2 (zodiaco)**: 9 filas = unión de las dos capturas del mockup
  (números 739–6151, fracciones "0/19"/"0/20", 6 signos). Nota: el
  filtro Aries devuelve 4 filas (incluye la 4208 de la otra captura;
  en el mockup filtrado se ven 3). Corrección tras inspección: la fila
  4918 dice "ESCORPION" (transcripción inicial errónea: "ESCORPIO";
  verbatim del mockup ampliado: "Escorpion" — regla 7).
- Las ciudades del mockup que NO están en el catálogo de la pantalla 8
  (TUXTLA GUTIERREZ, TEHUACAN, VILLAHERMOSA) usan IdCiudad 10–12: solo
  aparecen en modo "Cualquier ciudad" (desde la pantalla 8 no se pueden
  seleccionar).

### Punto abierto del modelo de datos

Cada fila mezcla dos conceptos: la tienda/sucursal donde el boleto está
disponible y el boleto mismo (número, fracciones y —en zodiaco— signo).
Los títulos de las pantallas 10.x ("botón carrito a lado de cada
tienda") indican que en Sr. Billetero la fila ERA una tienda. Al
integrar hay que definir qué representa exactamente la fila y qué
consulta la alimenta.

### Endpoints backend para integrar (verificado en BD, sep-2026)

- `GET /api/tiendas` (TiendasController::actionIndex): filtro
  `id_cedis` + sede-scoping. `tiendas_sucursales` SÍ tiene `ciudad` y
  `estado` (además de `permite_ventas`, que debería filtrar la lista a
  tiendas autorizadas a vender).
- `billetes_loteria` (verificado con DESCRIBE):
  - `numero_billete` varchar(30) y `fraccion_vigesimo` int → número y
    fracción; las fracciones disponibles de un número se cuentan por
    `estatus`.
  - `signo_codigo` varchar(10) y `signo_nombre` varchar(20) → el signo
    de los billetes zodiacos vive AQUÍ (no en el sorteo).
  - `estatus` enum ('disponible','asignado','vendido',...) e
    `ubicacion_actual` enum ('cedis','tienda','billetero',...) con
    `id_tienda_actual`/`id_cedis_actual` → disponibilidad y ubicación
    real de cada billete.
- **No existe** un endpoint "disponibilidad de boletos por ciudad":
  evaluar añadirlo en el backend o componer la consulta desde el app
  (billetes del `id_sorteo` con `estatus='disponible'`, uniendo
  `tiendas_sucursales` por `id_tienda_actual` para ciudad/estado).

## Pantalla 11 — Carrito de Compras (reglas)

Se abre desde el carrito del header de Agregar Boletos (badge = número
de REGISTROS, un registro por tienda con selección). Un registro por
tienda: sorteo ("{tipo} {número}", ej. "SUPERIOR 2894"), tienda
(ciudad, estado), cantidad, precio individual (precio del sorteo /
fracciones del billete) y total (cantidad × precio). Botón Eliminar
debajo de cada registro.

### Reglas (implementadas, solo estado local)

0. **Estilo (pulido sep-2026)**: fondo blanco, textos negros y botón
   Vender VERDE (#4CB050). El precio por cachito es el precio del
   sorteo de la fuente existente (SorteosActivosLotenalData, alineada
   al backend): MAYOR $30, SUPERIOR $40, ZODIACO $20, ZODIACO
   ESPECIAL $35, ESPECIAL $60, GRAN ESPECIAL $250, MAGNO $120 y
   GORDITO NAVIDEÑO $120. No se duplica la fuente: el registro toma
   _sorteo.Precio tal cual (antes se dividía entre fracciones, lo que
   daba precios incorrectos). Nombre corto del sorteo en la tarjeta:
   NombreSorteo sin el prefijo "SORTEO " (ej. "GRAN ESPECIAL 208").

1. **Eliminar**: quita el registro, descuenta de Cantidad/Total de la
   barra inferior y la tienda de origen vuelve a "0/{total}" (limpia
   Seleccionadas; el total disponible NO cambia).
2. **Vender** (sin backend por ahora): por cada registro, la tienda de
   origen descuenta los boletos vendidos de su total disponible
   (20 → 19 con una venta de 1), limpia la selección, vacía el carrito
   y regresa a Agregar Boletos. "Los boletos pasan al inventario de la
   sucursal" aún no tiene representación visual.
3. **Barra inferior** (#303030): "Cantidad: {suma de boletos}" y
   "Total: ${suma de importes}" a la izquierda; botón "Vender" (verde
   muy oscuro #0F2212) a la derecha.
4. **Estado vacío**: "Sin boletos en el carrito" centrado.

### Detalles del mockup 11 OCR (baja resolución, campos no legibles)

- Header morado #4125F4, título "Carrito de Compras", carrito con badge.
- Tarjeta: círculo verde (color del tipo de sorteo Superior), texto
  "Superior 2894", valores "20.00" / "40.0" y "Eliminar" (OCR parcial:
  "Ellminar"); layout tomado del spec del usuario (Sorteo, Cantidad,
  Precio individual, Total, Eliminar debajo).
- Barra inferior #303030 con "$0.0"-izquierda y botón oscuro derecha;
  OCR ruidoso ("Ondor" ≈ "Vender"); label "Vender" del spec del usuario.

## Pantalla 10.1/10.2 — Cantidad de Cachitos (reglas)

Aparece al tocar el carrito de una fila en 9.1/9.2. Es un OVERLAY sobre
la misma pantalla AgregarBoletos (la lista queda atenuada detrás,
header morado oscurecido a #1C116F, badge "00" junto al carrito del
header). El mismo overlay cubre ambos mockups (10.1 = estado inicial,
10.2 = tras pulsar "Realiz.").

### Reglas UI (implementadas)

1. **Diálogo "Cantidad de / Cachitos"**: tarjeta blanca centrada,
   título negro en 2 líneas; campo subrayado (#EBE8F7) con valor
   inicial "1"; a la derecha del campo el número de DISPONIBLES en
   gris claro (#BFC5DF) — se DERIVA de las fracciones de la fila
   seleccionada (FraccionesTotal, ej. "0/20" → 20): NO hay datos
   hardcoded. Botones Cancelar (blanco, texto rojo #D9534F) y
   Aceptar (verde #4CB050, texto blanco).

2. **Teclado numérico (formato teléfono, 4 filas)**: teclas #FCFCFA
   sobre fondo #E3E3D9; filas [1][2][3] / [4][5][6] / [7][8][9] /
   [Borrar][0][Realizado]. Borrar y Realizado beige #C8C7B3. El campo
   inicia VACÍO y las teclas agregan dígitos (máx. 2). "Realizado"
   OCULTA el teclado (el diálogo queda con Aceptar a la vista) y
   muestra el toast "AQUÍ ELEGIRÁS {n} DE {disp} / DISPONIBLES"
   (mockup 10.2). NO aplica la cantidad: Aceptar la aplica.

3. **Toast** (solo tras "Realiz."): banner blanco inferior 2 líneas:
   "AQUÍ ELEGIRÁS {n} DE {disp}" / "DISPONIBLES", con n = cantidad
   confirmada y disp = disponibles de la fila.

4. **Aceptar** aplica la cantidad a la fila: el registro pasa de
   "0/20" a "n/20" (Seleccionadas de la tienda; mockup: "El registro
   cambia de 0/20 a 1/20"). Si el campo quedó vacío tras "Realizado",
   usa la cantidad confirmada. **Cancelar** o tocar el scrim cierran
   sin aplicar. Defensa anti tap-fantasma al cerrar: los primeros
   600 ms tras abrir se ignora el toque del scrim (mismo patrón que
   el selector de signos).

5. **Teclas máx 2 dígitos**; el campo inicia VACÍO (captura desde 0;
   un "0" inicial se reemplaza por el siguiente dígito).

### Pendientes de OCR (ilegibles en los mockups, NO inventados)

- ~~Los 4 botones beige del teclado~~ RESUELTO por spec del usuario
  (sep-2026): la fila inferior es [Borrar][0][Realizado] en formato
  teléfono; "Realizado" oculta el teclado. Los 2 botones beige
  superiores del mockup se eliminaron al rediseñar el teclado.
- La franja de 5 círculos beige entre diálogo y teclado: contenido
  interior ilegible (posiblemente fracciones seleccionables o un
  resumen). Se muestran como círculos vacíos.
- El glifo pequeño junto a "Realiz." (parte inferior del botón, muy
  tenue en ambos mockups) no se pudo transcribir.

### Interpretación de flujo (10.1 → 10.2)

10.1 muestra el estado inicial (campo "1", "20" disponibles, sin
toast). 10.2 muestra el estado tras "Realiz." con cantidad 5: campo
vacío, "16" disponibles, toast "AQUÍ ELEGIRÁS 5 DE 16 / DISPONIBLES".
La lectura del flujo: el teclado captura → "Realiz." confirma y
muestra toast → Aceptar acumula y cierra / Cancelar descarta.
