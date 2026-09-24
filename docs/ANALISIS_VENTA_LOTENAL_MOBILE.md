# Análisis — Vender → Lotería Nacional (LOTENAL) en Mi Cachito Mobile

Fecha: 2026-09-24 (v3 — incorpora el flujo confirmado oficialmente por Lotería
Nacional 2026-09-24: asignación por billetero, sin concentrador)
Tipo: AUDITORÍA + PROPUESTA SIN IMPLEMENTACIÓN (patrón audit-then-fix)
Fuentes: backend Yii2 dev-mobile, BD real Docker (SELECT/SHOW verificados en
vivo 2026-09-24), Desktop dev (src/ui), Mobile master (a494b4d), docs/ Mobile,
skill refs (entrega-billeteros, sorteos-billetero-integration, lna-data-flow,
billetes-loteria-inventario-architecture, numero-sorteo-validation,
cierres-sorteo-dotacion, integracion-decisiones), 2 auditorías sub-agente
(Desktop entrega/devolución; Mobile pantallas/servicios/mocks) 2026-09-24.
Cero archivos de código modificados. Sin migraciones, sin commits de código.

> NOTA v3: la v2 (2026-09-15) se basaba en el modelo CONCENTRADOR ("Mi Cachito
> App" como billetero-pool). El flujo confirmado por Lotería Nacional el
> 2026-09-24 es de ASIGNACIÓN POR BILLETERO (cada billetero con usuario de la
> app ve SOLO sus billetes). La BD verificada jamás sembró un concentrador
> (0 filas tipo_config='MICACHITO_APP') y el modelo por-billetero es el que
> ya vive en el código (`billetes_loteria.id_billetero_actual`). El historial
> v2 se conserva marcado como SUPERSEDED; todo lo válido de v2 (trazabilidad,
> matriz, infra, decisiones de UI/ticket) se mantiene.

---

## 0. MODELO VIGENTE v3 — asignación por billetero, mismo backend/BD

Desktop y Mobile están conectados AL MISMO backend y BD. Para los billeteros
que tienen usuario de la aplicación, el flujo confirmado por Lotería es:

```
CEDIS Desktop → E. Entrega a billeteros → Entrega (consigna)
  → se selecciona el billetero
  → se escanean/asignan series y/o cachitos
  → Dotación → Billetero (id_billetero)
  → Disponible en Mobile (solo SI ese billetero tiene usuario app;
     los que no tienen app simplemente no lo consultan)
  → Venta o Devolución
```

Reglas confirmadas:
- Los billetes se cargan al billetero desde CEDIS Desktop mediante
  `CEDIS → E. Entrega a billeteros → Entrega` (se selecciona el billetero y
  se escanean series/cachitos). Aplica para productos físicos y escaneables.
- Los billetes asignados deben aparecer en `Mobile → Vender → Lotería
  Nacional`: la app muestra ÚNICAMENTE el inventario realmente asignado a
  ese billetero, con seguimiento individual de billetes/series/cachitos.
- Al vender desde Mobile: el billete se descuenta del inventario disponible,
  Desktop refleja el mismo cambio (misma BD) y el billete NO vuelve a
  aparecer como disponible en ningún canal.
- Si no se vende: debe poder devolverse desde la app; la devolución se
  refleja en Desktop y conserva la trazabilidad individual.
- NO existe un inventario paralelo de Mobile: la fuente de verdad es la
  estructura existente del backend/BD — principalmente el estado individual
  de `billetes_loteria` (ver §L y principio §D.0).

Trazabilidad objetivo por billete:
`Recepción → Dotación (numero_sorteo) → CEDIS → Billetero → Disponible → Venta o Devolución`

### 0-HIST. MODELO CONCENTRADOR (v2, 2026-09-15 — SUPERSEDED)

`Mi Cachito App` como billetero concentrador que recibe las dotaciones y los
usuarios Mobile venden DESDE ese pool compartido. SUPERSEDED por §0 v3 porque:
(a) Lotería confirmó el flujo por-billetero; (b) verificado 2026-09-24: la BD
no tiene NINGUNA fila `billeteros.tipo_config='MICACHITO_APP'` (la "Fase 0"
de v2 jamás se ejecutó); (c) el mecanismo real ya implementado
(`estatus='asignado'` + `id_billetero_actual`) es por-billetero. Se conserva
como historial: implicaciones de ciudad-como-dato-operativo (→ P3), atomicidad
por transacción de venta (FOR UPDATE, sigue vigente → §B), y las respuestas
§J-HIST.

---

## A. FLUJO ACTUAL (Desktop + backend, verificado)

### A.1 Recepción física (Almacén → CEDIS)
Dos puertas, ambas terminan en `paquetes_recibidos` (estatus 'verificado'):
Recepción Paquetería (`recepcion_paqueteria_window.py`, folio+detalle
agregado por dotación) y LNA (`lna_window.py`, escaneo 34/35 dígitos; GAP
conocido: agrega por (id_sorteo, numero_sorteo) y pierde el dato individual
en recepción — se recupera al escanear la entrega con reconstruir_vigesimos).

### A.2 Liberación a inventario (E-TRASPASO)
`Movimientos de Almacén → Nuevo Movimiento` → POST `/api/movimientos-almacen`
tipo `E-TRASPASO`:
- `inventario_cedis.aumentar(cantidad,'disponible')` por dotación
  (id_cedis, id_sorteo, numero_sorteo) — stock AGREGADO en unidades de cachito.
- Crea `billetes_loteria` individuales: **una fila por vigésimo**
  (fraccion_vigesimo 0=billete completo, 1-19 cachitos), precio =
  `sorteos.precio_fraccion` (MovimientosAlmacenController.php:540-585).
  Unique key real (m260831): (id_sorteo, COALESCE(numero_sorteo,''),
  numero_billete, fraccion_vigesimo, serie) + codigo_qr/codigo_barras UNIQUE.
  Índice sobre id_billetero_actual (la consulta por billetero es indexada).

### A.3 Entrega/dotación al billetero (consigna) — con estado LIVE
`Entrega a Billeteros` (entrega_billeteros_window.py, EntregaDialog) → POST
`/api/movimientos-almacen` con movimiento tipo 'consigna' (folio
MA-Ymd-####; header: folio, id_cedis, id_billetero, id_sorteo, fecha, turno,
estatus 'pendiente') + detalles por boleto (numero_billete, serie, fraccion,
cantidad, precio, signo, numero_sorteo, codigo_barras_completo, id_billete
cuando el backend ya lo conoce; duplicados legacy 'cant'/'cantidad' y
'subtotal'/'total'; el backend normaliza precio=precio_fraccion y expande
cantidad>1 a filas por vigésimo — expandirDetallesVigesimos).

**HALLAZGO CLAVE (verificado, MovimientosAlmacenController.php:408-410): la
sincronización de billetes ocurre AL CREAR el movimiento 'pendiente'**:
`BilletesLoteria::sincronizarPorMovimiento` marca cada billete
`estatus='asignado'`, `ubicacion_actual='billetero'`,
`id_billetero_actual=<billetero>` (FOR UPDATE, exige 'disponible' previo) y
`descontarInventarioPorMovimiento` mueve inventario_cedis
disponible→asignada. El botón "Aplicar" (cambiar-estatus 'aplicada') SOLO
cambia el estatus administrativo del movimiento; NO re-sincroniza.

Por lo tanto:
- `billetes_loteria` representa el estado **LIVE** del billete.
- La asignación a billetero existe DESDE que se guarda la consigna (no
  requiere "Aplicar").
- El estatus administrativo del movimiento (pendiente/aplicada/liquidada/
  cancelada) es una CAPA DIFERENTE.
- Mobile debe consultar el estado LIVE del billete y NO depender del estatus
  administrativo del movimiento.

Comprobante de entrega: 2 tickets (FACTURA PAGARÉ + DESGLOSE) client-side
(`comprobante_entrega.py`).

### A.4 Venta (POS Almacén → CEDIS)
`NuevoVentaDialog` → POST `/api/ventas` (actionCrear): dos caminos:
- (a) **con id_billete** (el que Mobile debe usar SIEMPRE): FOR UPDATE +
  estatus IN ('disponible','asignado') → 'vendido', `ubicacion='vendido'`,
  id_venta, fecha_venta; escribe `ventas` + `ventas_detalle` (con id_billete).
  **NO toca id_billetero_actual** (verificado L141-142/179-180: la venta
  conserva la atribución al billetero — de ella depende la comisión del
  cierre).
- (b) legacy sin id_billete (precio del cliente, sin trazabilidad individual;
  en la BD dev las ventas 73/75 quedaron sin id_billete). Mobile NO debe
  usarlo (riesgo R4).
Comisión: NO se calcula en la venta; se materializa al CIERRE DE SORTEO
(A.10). Folio `V-Ymd-RAND` colisionable (patrón generarFolio secuencial
disponible). Ticket: REMISIÓN PAGARÉ HTML client-side.

### A.5 Sorteos disponibles
NO existe regla de fechas en backend (`fecha_inicio_ventas`/
`fecha_cierre_ventas` NULL y sin uso). La disponibilidad EMERGE del
inventario/dotación real. "Próximo + 2 adelantados" es práctica operativa.
Para Mobile: mostrar SOLO sorteos con dotación asignada AL BILLETERO de la
sesión (v3; antes: al concentrador). Sin regla artificial de fechas.

### A.6 Consulta por billetero
`SorteosBilleteroController::actionIndex`: sorteos donde el billetero tiene
billetes (`id_billetero_actual=X` GROUP BY sorteo ORDER BY fecha_sorteo).
**En v3 esta query ES directamente la que Mobile necesita** (apuntando al
billetero de la sesión, no a un tercero). Verificación en vivo 2026-09-24
con el billetero 11 (GABRIELA, expendio e2e_dot): la query
`billetes_loteria WHERE id_billetero_actual=11 AND estatus='asignado' GROUP
BY id_sorteo, numero_sorteo` devuelve sorteo 41 (Mayor) con dotaciones 4024
(2 fracciones) y 4025 (3 fracciones), precios de sorteos.precio_fraccion —
sin ningún cambio de esquema.

### A.7 Alta de billeteros y credenciales Mobile (identidad)
`ContCedisClientesController` (permiso contabilidad.clientes, ventana
Clientes de Contabilidad CEDIS): INSERT completo en `billeteros` con
id_cedis, id_zona, clave, nombre, comision_porcentaje, retencion_isr,
fondo_ahorro, tipo_config, categoria, estatus. La MISMA ventana crea las
credenciales Mobile: pestaña **Expendios** (nuevo_cliente_window.py:431-477,
tabla usuario/password) → POST/PUT `cont-cedis/clientes/{id}/expendios` →
INSERT en `billeteros_expendios` contra el `id_billetero` EXISTENTE (FK
directa). Ver §K para la cadena completa de identidad.

### A.8 Catálogo de productos LN (pantalla 6)
`productos` tipo loteria_nacional = 7 reales: Mayor(600), Superior(800),
Zodiaco(400), Magno(2400), Zodiaco Esp(700), Especial(1200), Gran Especial
(5000). Verificado: precio_unitario/20 = EXACTO el precio del mockup 6
($30/$40/$20/$120/$35/$60/$250). El mock tiene además "GORDITO NAVIDEÑO"
($120) que NO existe en BD (producto estacional por crear cuando exista).
Zodiaco y Zodiaco Esp son productos LN (encabezado LOTERIA NACIONAL).

### A.9 Devolución de billetero (estado LIVE inmediato + capa administrativa)
`DevolucionBilleterosDialog` (devolucion_billeteros_dialog.py) → POST
`/api/movimientos-almacen` tipo 'devolucion' (mismo endpoint/contrato que la
consigna; header id_billetero; detalles con signo_codigo). Efectos
verificados:
- **LIVE inmediato**: billetes → `estatus='devuelto'`,
  `ubicacion_actual='devolucion'`, `id_billetero_actual=NULL` (quién devolvió
  queda registrado en `movimientos_almacen.id_billetero`); inventario_cedis
  → bucket 'devuelta' (APARTE — dominio confirmado: boletos devueltos se
  mantienen aparte; solo vuelven a 'disponible' vía cancelación válida).
- **Capa administrativa**: el movimiento nace 'pendiente' y se liquida
  DESPUÉS desde `movimientos_sin_aplicar_window.py:2245-2352`
  (POST almacen/aplicar-movimiento → 'liquidado'). La liquidación NO
  re-sincroniza billetes.
- DECISIÓN de fase (F4): si Mobile reproduce el flujo administrativo
  pendiente→liquidado o registra solo el estado LIVE + movimiento. El estado
  del billete es el mismo en ambos casos.

### A.10 Cierre de sorteo (materialización financiera)
`CierresSorteoController::actionCrear` (almacen.cierre_sorteo): por dotación
(numero_sorteo):
1. Bulk-update `asignado` → `vendido` (lo no devuelto se PRESUME vendido).
2. `cierres_sorteo` (totales por dotación, incluye total_devuelto).
3. `cierres_billeteros` por billetero: agrega `WHERE
   bl.id_billetero_actual = b.id_billetero AND NOT NULL` — comisión con
   `billeteros.comision_porcentaje`, saldo = monto_vendido − comisión −
   total_devuelto.
4. `caja_movimientos` 'cobro_billetero' (deuda incremental, folio CS-...).
5. Marca `sorteos.cerrado=1` cuando no quedan dotaciones sin cierre.

Consecuencias para Mobile: la devolución debe ocurrir ANTES del cierre
(ver R2/R9); la venta de una dotación cerrada debe rechazarse (409).

### A.11 Cancelaciones (dos semánticas distintas — riesgo R3)
- **Cancelar CONSIGNA** (cambiar-estatus 'cancelado'): `revertirPorMovimiento`
  (BilletesLoteria.php:455) RETIRA el material al billetero (billetes vuelven
  a 'disponible'/'cedis', inventario se repone). Mobile debe re-consultar
  disponibilidad y dejar de mostrar esos billetes (ver §F5).
- **Cancelar VENTA** (actionCancelar/actionCancelarDiaria): billetes
  'vendido' → 'disponible'/'cedis' con `id_billetero_actual=NULL` e
  `id_tienda_actual=NULL`. PROBLEMA para billetes vendidos desde Mobile:
  el billetero sigue teniendo el físico y la atribución se pierde. La
  cancelación Mobile debe devolver a 'asignado' al MISMO billetero (ver D4/D5
  y punto de detención D3).

---

## B. FLUJO ESPERADO MOBILE (v3 — por billetero)

```
Desktop: recepción → E-TRASPASO → E. Entrega a Billeteros → consigna
        al BILLETERO X (con usuario app) → billetes 'asignado',
        id_billetero_actual=X (LIVE desde el guardado)
Mobile: login (usuario de expendio de X) → sesión → id_billetero=X
        → GET /api/mobile/ventas/sorteos (PROPUESTA de nombre; sorteos
          con material LIVE de X)
        → GET /api/mobile/ventas/billetes?id_sorteo= (series +
          fracciones libres de X)
        → carrito (9.x/10.x/11) → confirmar
        → POST /api/mobile/ventas {items por fracción (id_billete exacto)}
          — id_billetero vendedor = sesión; precio server-side
          (precio_fraccion); billetes 'asignado'→'vendido' (FOR UPDATE
          = anti-doble-venta); id_billetero_actual SE CONSERVA
        → VentaExitosa (folio+resumen+comisión estimada) → ticket BT
        → si no se vende: devolución (F4) → 'devuelto' + trazabilidad
```

Reglas duras (v2 vigentes + v3):
- La app NUNCA genera inventario; vende SOLO filas asignadas al billetero
  de la sesión (`id_billetero_actual=<sesión>`, `estatus='asignado'`).
- **No vincular Mobile con CEDIS por nombre**: el usuario Mobile opera sobre
  el MISMO `id_billetero` al que CEDIS asigna los billetes (cadena §K).
- id_billetero del vendedor SIEMPRE de la sesión
  (mobile_sesiones → billeteros_expendios → billeteros), jamás del request.
  Precio jamás del cliente (server-side precio_fraccion).
- Unidad visual 9.x: **1 fila = 1 serie (numero_billete+serie[+signo])** con
  sus ≤20 fracciones libres; NO una fila por cachito. Venta de 20/20
  (completo) y parciales permitida (decisión v2 #3/#6).
- id_tienda = NULL en ventas mobile (decisión v2 #4).
- Comisión estimada calculada por el BACKEND (misma lógica Desktop). En v3
  la fuente natural es `comision_porcentaje` DEL PROPIO BILLETERO de la
  sesión (la misma fila que el cierre usa por id_billetero_actual) —
  confirmar con negocio al exponerla (P2-resuelta-por-diseño).
- Dotación cerrada (cierres_sorteo) → rechazar venta Y devolución (409).
- Ciudad: pantalla 8 se conservaba en v2 como picker operativo (decisión #2);
  en v3 su rol debe RE-VALIDARSE (P3) — el inventario ya es del billetero,
  no de un pool sin ciudad.

---

## C. LO QUE YA EXISTE (resumen; verificado 2026-09-24)

- **Identidad completa (v2 "Fase 0" — HECHA)**: POST /api/mobile/auth/login
  (usuario/password de billeteros_expendios → token mobile_sesiones),
  verify, logout, sesiones, revocar; GET/PUT api/mobile/expendios (+permisos
  tiene_lotenal/tiene_prod_digitales/tiene_tiempo_aire — compuerta de la
  pestaña Vender). BaseMobileController::getIdBilleteroActual() =
  expendioActual->id_billetero. La sesión Mobile guarda IdBilletero,
  ClaveBilletero, NombreCompleto, permisos int 0/1 (NO bool — pitfall
  JsonException documentado) y reglas comerciales (comisión, límites);
  id_cedis NO viaja (se resuelve server-side).
- **Gestión → Sorteos REAL** (a494b4d, 2026-09-24): GET /api/mobile/sorteos
  (histórico del billetero: UNION pista física movimientos_almacen consigna/
  devolución por dotación + pista financiera cartera_billeteros + pagos,
  con atribución de dotación en cascada) y /{id} detalle. Patrón
  servicio+DTO+VM async con manejo de errores/vacío REUTILIZABLE para
  Vender (copiar a MobileVentasService).
- **Consulta de premios por QR** (mobile/premios/consultar): parser V4
  portado con 3 correcciones (serie '00' tradicional, monto por cachito,
  serie cruda) — SOLO LECTURA.
- **Pagos/recibos, estado de cuenta, fondo de ahorro, facturación**:
  endpoints mobile reales existentes.
- Backend/BD: todas las tablas del dominio + flujo completo ejercitado con
  datos e2e (billetero 11 con 2 dotaciones asignadas EN VIVO). Lógica
  reutilizable: sincronizarPorMovimiento/revertirPorMovimiento,
  InventarioCedis::descontar/aumentar/revertir, generarFolio,
  normalizarPrecioFraccion, expandirDetallesVigesimos, actionCrear/cancelar
  (camino (a) por id_billete), verificar-cachito/verificar-serie, query
  sorteos-por-billetero (A.6), alta de billeteros+expendios (A.7), cierre de
  sorteo con comisión por id_billetero_actual (A.10).
- Desktop: flujo completo operado (entrega, devolución, venta, cierre).
- Mobile: pantallas 6→7.x→8→9.x→10.x→11 íntegras (mock), infraestructura
  ApiClient/SessionService/navegación/entidades, BilleteParser.cs (port
  fiel del parser Desktop + validación cruzada longitud↔subcódigo que el
  Desktop aún NO tiene) + SorteoIdentificador (subcódigos de EDICIÓN —
  limitación documentada: cambian por sorteo), cámara/zxing-cpp probada,
  descubrimiento BT (BondedDevices+permisos), TicketPdfService,
  IImpresoraService/ImpresoraService (§H).

## D. LO QUE FALTA (v3)

**D.0 PRINCIPIO: Mobile NO debe crear una segunda fuente de inventario.**
La fuente de verdad es el estado individual de `billetes_loteria` (+
inventario_cedis para stock agregado). Cualquier endpoint nuevo consulta/
modifica ESA fuente y mantiene la trazabilidad existente. Prohibido: tabla
mobile_inventario, cache persistente de disponibilidad, o réplica local.

Backend (dev-mobile) — solo agregar (ninguna tabla nueva obligatoria):
1. `GET /api/mobile/ventas/sorteos` (NOMBRE PROPUESTO — decidir en F1):
   sorteos+dotaciones con material LIVE del billetero de la sesión
   (`billetes_loteria` estatus='asignado' AND id_billetero_actual=sesión,
   GROUP BY id_sorteo+numero_sorteo; fecha_sorteo, precio_fraccion,
   subcódigo/tipo para colorear UI). Query tipo A.6. NO reusar
   /api/mobile/sorteos (ver §N).
2. `GET /api/mobile/ventas/billetes?id_sorteo=&numero_sorteo=` — series del
   billetero agrupadas por (numero_billete, serie[, signo]) con conteo y
   lista de fracciones libres (estatus='asignado') y precio_fraccion, para
   seleccionar 20/20, tiras de 5 o individuales.
3. `VentaService` — extracción de actionCrear (validar ítems, precio
   server-side, totales, folio robusto, marcar billetes camino (a)
   CONSERVANDO id_billetero_actual) con paridad exacta; actionCrear delega
   (MISMO contrato Desktop). Estrategia id_usuario_captura (NOT NULL) para
   ventas mobile: usuario de sistema dedicado — decisión P8.
4. `POST /api/mobile/ventas` — sesión→vendedor; ítems por fracción con
   id_billete exacto; id_tienda NULL; validar dotación no cerrada (409);
   respuesta con folio, desglose, comisión estimada y datos de ticket
   (cliente=PÚBLICO EN GENERAL, sin whatsapp — decisión v2 #8).
5. `POST /api/mobile/ventas/{id}/cancelar` — venta→'cancelada', billetes
   'vendido'→'asignado' **al MISMO billetero** (ubicacion='billetero',
   id_billetero_actual=sesión — NO 'disponible'/cedis como Desktop, R3),
   reposición simétrica de inventario. Semántica DISTINTA de
   actionCancelar Desktop, aislada en el flujo mobile.
6. `GET /api/mobile/ventas` + `/{id}` — historial y reimpresión.
7. `POST /api/mobile/ventas/devolucion` (F4, nombre propuesto) — devolución
   del billetero: replicar contrato movimientos-almacen tipo 'devolucion'
   (o reusarlo) con guard de cierre de dotación (R2/R9) y escritura LIVE
   inmediata (A.9). Decisión P9: flujo administrativo pendiente→liquidado.
8. Migración aditiva OPCIONAL `ventas.ciudad_venta varchar(100) NULL` —
   solo si la pantalla 8 sobrevive la re-validación P3 (antes era requisito
   del modelo pool; en v3 el inventario ya tiene dueño). REQUIERE APROBACIÓN.

Mobile (master) — conectar, UI intacta:
9. ApiEndpoints (+MobileVentas) + MobileVentasService + DTOs (patrón
   SorteosService de Gestión; NO tocar SorteosService).
10. SorteosActivosVM (sorteos del billetero), AgregarBoletosVM (series/
    fracciones reales, fila=serie), CarritoComprasVM.VenderAsync → POST
    real, VentaExitosaLotenalPage (patrón Tec + folio/comisión/total).
11. Devolución 6.x real (F4): sustituir DevolucionService en-memoria;
    ojo: ListaSorteosDevolucionViewModel usa el MISMO mock
    SorteosActivosLotenalData que Vender 7.x (§E).
12. Impresión real: SPP+ESC/POS sobre IImpresoraService (F6).
13. Historial DetalleVenta con datos reales; saldo real de sesión.

NO falta: dotación al billetero (consigna Desktop existente tal cual),
comisión (lógica de cierre existente), inventario individual (por vigésimo,
existente), precios (precio_fraccion), identidad (auth hecha), trazabilidad
(cadena existente §L).

## E. DATOS/MOCK: sustituir vs NO tocar

Sustituir (exclusivos Vender LN): `TiendasDisponiblesData` (filas 9.x → GET
billetes del billetero), `SorteosLotenalData` como fuente de 7.x (→ GET
ventas/sorteos; pantalla 6 → productos reales §A.8 — pendiente v2 #9), saldo
"$0.00" hardcodeado.
Se CONSERVA (decisión v2 #2, sujeta a P3): pantalla 8 + catálogo de ciudades
como PICKER operativo; contenido real de ciudades pendiente (las 9 del
mockup son territorio Sr. Billetero).
`SignosZodiacoData` queda estático (catálogo inmutable 12 signos).
NO TOCAR: `SorteosTecData`, `TiempoAireData`, `ExpendiosDemoData`, y
CRÍTICO `SorteosActivosLotenalData` COMPARTIDO hoy por Vender (7.x y 11) Y
Devolución 6.1 (lista de sorteos + FechaSorteoCodigo valida la fecha del
QR); Gestión Sorteos YA NO lo usa (migrado a SorteosService real en
a494b4d). Al sustituir el consumo de Vender hay que sustituir
SIMULTÁNEAMENTE el de Devolución 6.1 o dejarle el mock vivo — NUNCA borrar
el archivo mientras Devolución siga en fase interfaz. Ningún dato de BD se
toca desde Mobile.

## F. PROPUESTA TÉCNICA (archivos)

Backend (dev-mobile):
- NUEVO `models/services/VentaService.php`: lógica extraída de actionCrear
  (paridad exacta) + variante mobile (precio server, id_tienda NULL,
  cancelación al mismo billetero). VentasController::actionCrear delega.
- NUEVO `controllers/api/mobile/VentasController.php` (actionSorteos,
  actionBilletes, actionCrear, actionIndex, actionView, actionCancelar,
  actionDevolucion) extendiendo BaseMobileController (getIdBilleteroActual).
- `config/web.php`: bloque nuevo /api/mobile/ventas* (rutas Desktop intactas;
  /api/mobile/sorteos de Gestión intacto).
- MIGRACIÓN aditiva opcional `ventas.ciudad_venta` (solo si P3 la mantiene;
  aprobación previa; INSERTs Desktop explícitos no se rompen).
- CERO tablas nuevas: atomicidad = FOR UPDATE existente; identidad = FK
  existente; estado LIVE = billetes_loteria existente.

Mobile (master):
- `Api/ApiEndpoints.cs` (+MobileVentas), NUEVO `Services/MobileVentasService.cs`,
  DTOs `Models/Responses/`.
- VMs existentes conectados (SorteosActivos, AgregarBoletos, CarritoCompras;
  SeleccionCiudad pasa ciudad al carrito/venta si P3 la mantiene), NUEVA
  `Views/VentaExitosaLotenalPage.xaml`+VM.
- `Platforms/Android/Services/ImpresoraService.cs`: SPP+ESC/POS (F6).
- Pantalla 6 alimentada por productos LN reales (7; colores/subcódigo
  mapeados como hoy, pendiente v2 #9).

## G. IMPACTO DESKTOP (riesgo y mitigación)

1. **Migración `ventas.ciudad_venta` (BAJO, OPCIONAL en v3)**: solo si P3 la
   mantiene; columna NULL aditiva; ventas Desktop quedan NULL; INSERTs
   Desktop enumeran columnas explícitas.
2. **VentaService (MEDIO)**: único cambio en código Desktop compartido
   (actionCrear). Paridad exacta + regresión Desktop 44/4 + E2E Docker de
   venta/cancelación ANTES de abrir el endpoint mobile. Si la paridad
   falla: DETENERSE.
3. **Cancelación mobile al mismo billetero (semántica distinta, AISLADA)**:
   Desktop cancel → 'disponible'/cedis + billetero NULL; Mobile cancel →
   'asignado'/mismo billetero. Vive SOLO en el controller/service mobile;
   actionCancelar Desktop intacto.
4. **Cierre de sorteo (CERO cambio para el MVP)**: con id_billetero_actual
   conservado en los billetes vendidos por Mobile, el cierre existente
   agrupa y comisiona a CADA billetero con SU comisión — flujo Desktop
   intacto. (Riesgos R1/R2 del cierre documentados en §RIESGOS como fixes
   Desktop separados, con aprobación.)
5. Rutas nuevas aditivas; auth mobile intacto; aislamiento: sesión→vendedor,
   precio server-side, dotación cerrada→409.
6. Para el MVP de Vender: CERO cambios obligatorios en Desktop/BD (todo el
   estado compartido emerge de las mismas tablas).

## H. FLUJO DE IMPRESIÓN (actualizado v3)

Infraestructura EXISTENTE y PROBADA en el flujo de Sorteos Tec (verificado
2026-09-24): `IImpresoraService` (contrato ImprimirAsync +
ObtenerDispositivosEnlazadosAsync) + `ImpresoraService` Android — permisos
runtime BLUETOOTH_CONNECT, adaptador, BondedDevices, mensajes de error
amigables — ejercitada desde DetalleVentaViewModel (Tec),
DispositivosEnlazadosPage, Cuenta y Expendios; `TicketPdfService` (PDF
compartible, ya funciona); spec NOTAS_TICKETS_VENTA.md.
**NO existe todavía**: socket SPP (UUID 00001101-...), protocolo ESC/POS,
selección/guardado de impresora, reimpresión — fase F6.
Propuesta: EXTENDER IImpresoraService (NO segunda infraestructura): SPP +
ESC/POS texto directo (58/80 ancho configurable), impresora guardada en
Preferences, spike en impresora física antes de F6. La impresión de billetes
Lotería Nacional puede REUTILIZAR esta infraestructura, sujeto a validar
posteriormente el FORMATO específico de LOTENAL (contenido/diseño del
ticket vs Tec). Errores (regla: venta registrada UNA vez): sin impresora/
conexión perdida → aviso + "Reintentar impresión" + "Ver ticket" (PDF
compartible) + reimpresión desde historial. NO implementar impresión en
esta actualización del plan.

## I. PLAN DE IMPLEMENTACIÓN (fases v3; mapea las v2 Fase 0/A-F)

- **F0 — Identidad y relación billetero ↔ usuario Mobile** (VERIFICADA, sin
  código): cadena id_billetero probada en BD (§K, billetero 11 ↔ e2e_dot).
  Restante: decidir estrategia id_usuario_captura (P8) y preparar billetero
  de prueba E2E con material (ya existe en dev). La "Fase 0 datos" de v2
  (crear concentrador) queda OBSOLETA.
- **F1 — Inventario LIVE disponible para Vender (backend lectura)**:
  GET /api/mobile/ventas/sorteos + /api/mobile/ventas/billetes (nombres
  propuestos) contra billetes_loteria LIVE del billetero de la sesión.
  E2E Docker (billetero 11, dotaciones 4024/4025). Commit.
- **F2 — Consulta de sorteos/billetes disponibles (mobile lectura)**: VMs
  7.x/9.x/10.x con datos reales (fila=serie, fracciones libres), pantalla 6
  con productos reales; sustitución de mocks coordinada con Devolución 6.1
  (§E) → emulador → inspección → commit.
- **F3 — Venta individual y descuento de inventario**:
  (a) backend: VentaService paridad (regresión Desktop 44/4 ANTES de abrir
  endpoint; si falla, parar) → POST venta mobile + cancelar (al mismo
  billetero) + historial;
  (b) mobile: VenderAsync→POST, VentaExitosaLotenal (folio, total, comisión
  estimada), historial real → inspección → commit.
- **F4 — Devolución de billetes no vendidos**: backend POST devolución con
  guard de cierre de dotación (R2/R9) y estado LIVE inmediato (A.9);
  decisión P9 (flujo administrativo); mobile 6.x real (mock compartido §E)
  → inspección → commit.
- **F5 — Sincronización y consistencia Desktop ↔ Mobile**: manejo 409
  (boleto ya vendido por otro canal) + re-consulta de disponibilidad;
  reacción a cancelación de consigna Desktop (revertirPorMovimiento retira
  material → app deja de mostrar); bloqueo de venta/devolución en dotación
  cerrada; verificación cruzada Desktop ve ventas Mobile (grids/reportes).
- **F6 — Impresión**: spike impresora física → SPP+ESC/POS + guardado +
  errores + reimpresión; formato LOTENAL a validar (§H).
- **F7 — Pruebas E2E y regresión Desktop**: E2E completo contra Docker
  (entrega Desktop → disponibilidad app → venta app → visible Desktop →
  devolución app → visible Desktop), regresión Desktop 44/4 completa,
  limpieza de mocks LN sin uso, saldo real, pendientes v2 #7/#9/#12.

## MATRIZ (actualizada v3)

| Funcionalidad | Ya existe | Reutilizable | Falta | Dónde |
|---|---|---|---|---|
| Identidad billetero↔app | Auth Fase 0 completa (FK id_billetero) | Sí, tal cual | NADA (verificada §K) | — |
| Sorteos LN | Catálogo sorteos+productos (7 LN reales) + query por-billetero (A.6) | Query sí | Endpoint mobile del billetero (propuesta /api/mobile/ventas/sorteos) | mobile/VentasController |
| Disponibilidad LIVE | billetes_loteria 'asignado'+id_billetero_actual (desde guardado de consigna) | Sí | Exponerla por serie/fracción | F1 |
| Inventario individual | billetes_loteria 1 fila/vigésimo + inventario_cedis | Sí | Consulta del billetero por serie | actionBilletes |
| Venta | actionCrear camino (a) | Lógica sí; contrato no | VentaService + endpoint mobile (sesión, precio server, tienda NULL) | F3 |
| Anti doble-venta | FOR UPDATE camino (a) | Sí | NADA (transacción de venta) | — |
| Cancelación mobile | actionCancelar Desktop | Patrón sí | Semántica al MISMO billetero (aislada) | F3 |
| Devolución | Consigna/devolución Desktop completas (A.9) | Contrato sí | Endpoint mobile + guard cierre + decisión P9 | F4 |
| Comisión | billeteros.comision_porcentaje + cierre (A.10) | Sí (misma fórmula; % del propio billetero) | Exponer estimada en respuesta | F3 |
| Folio | V-Ymd-RAND (débil) | Patrón generarFolio | Folio robusto en VentaService | F3 |
| Ticket | Spec 5.2 + TicketPdfService + PÚBLICO GENERAL | Sí | Datos reales + reimpresión | F3/F6 |
| Impresión BT | Descubrimiento+permisos probados (Tec) | Sí | SPP+ESC/POS+formato LOTENAL | F6 |
| Historial | DetalleVenta mock; GET /api/ventas | Patrón sí | Endpoint historial mobile + conectar | F3 |
| Trazabilidad individual | Cadena completa §L | Sí | Vender SIEMPRE con id_billete (R4) | F3 |

## J-HIST. RESPUESTAS AL MODELO CONCENTRADOR (v2 — historial, superseded §0)

Se conservan como historial de v2. Qué sobrevive en v3: J.4 (quién es dueño
tras la venta → en v3 el propio billetero, id_billetero_actual conservado);
J.5 (ciudad → P3 re-validar); J.6 (anti doble-venta FOR UPDATE → vigente);
J.7 (cancelar → al mismo billetero, no al pool); J.8 (Desktop intacto →
vigente); J.9 (tablas nuevas → ninguna); J.10 (reutilizable → vigente).
Obsoletas: J.1/J.2/J.3 (crear/dotar/consultar el concentrador).

- J.1 Crear "Mi Cachito App": vía ContCedisClientesController con
  tipo_config='MICACHITO_APP', uno por CEDIS. OBSOLETO: 0 filas en BD;
  v3 no usa concentrador.
- J.2 Dotación al concentrador: consigna Desktop tal cual. OBSOLETO: v3
  dota a CADA billetero.
- J.3 Consultar inventario del pool: GET /api/mobile/sorteos + billetes.
  OBSOLETO: v3 consulta al billetero de la sesión; /api/mobile/sorteos ya
  materializó semántica HISTÓRICA de Gestión (§N).
- J.4 Reservas/duelo: sin reservas previas; claim atómico en la transacción
  de venta. VIGENTE. En v3 los vendidos MANTIENEN id_billetero_actual=
  <billetero de la sesión> (su propio material; el cierre lo agrupa y
  comisiona a él); el VENDEDOR queda en ventas.id_billetero (misma persona
  en v3) + bitácora mobile.
- J.5 Ciudad: propuesta ventas.ciudad_venta (migración aditiva, aprobación).
  En v3: OPCIONAL, sujeta a P3.
- J.6 Doble venta: FOR UPDATE + 'asignado'→'vendido'; 409 + refresco UI.
  VIGENTE.
- J.7 Cancelar: 'vendido'→'asignado' al dueño del stock. VIGENTE con
  matiz v3: el dueño es el MISMO billetero de la sesión (no un concentrador).
- J.8 Desktop intacto: rutas aditivas; diferencias aisladas en controller
  mobile. VIGENTE.
- J.9 Tablas nuevas: ninguna; migración aditiva 1 (ciudad, ahora opcional).
- J.10 Reutilizable sin cambios: consigna+comprobante, cierre, verificar-
  cachito/serie, normalizarPrecioFraccion, generarFolio,
  expandirDetallesVigesimos, auth, ApiClient/SessionService/navegación,
  entidades, descubrimiento BT, TicketPdfService, FormatosFecha, pantallas
  6/7.x/8/9.x/10.x/11 (solo se conectan). VIGENTE.

---

## K. IDENTIDAD BILLETERO ↔ USUARIO MOBILE (crítico, verificado 2026-09-24)

Cadena EXACTA (ningún eslabón usa el nombre):

```
billeteros (id_billetero PK, clave_billetero, nombre_completo,
            comision_porcentaje, limite_credito, id_cedis, ...)
  ↑ FK id_billetero (1:N)
billeteros_expendios (id_expendio PK, id_billetero FK, usuario UNIQUE,
            password hash, autorizado, tipo_config)
  ↑ FK id_expendio (1:N sesiones)
mobile_sesiones (id_sesion, id_expendio, token, fecha_expiracion,
            fecha_revocacion)
```

- Desktop identifica al billetero por `id_billetero` (combo de GET
  billeteros; la consigna lo escribe en movimientos_almacen.id_billetero y
  sincronizarPorMovimiento lo copia a billetes_loteria.id_billetero_actual).
- Mobile identifica al billetero por la SESIÓN → expendio →
  `BaseMobileController::getIdBilleteroActual()` (L111) =
  expendioActual->id_billetero. El usuario Mobile NO es una identidad
  separada: es una credencial HIJA del billetero.
- Las credenciales se crean DESDE la ficha del cliente en Desktop (pestaña
  Expendios, §A.7) contra el id_billetero EXISTENTE — el enlace es
  automático por FK. El nombre de usuario Mobile puede ser cualquier cosa
  y NO afecta al enlace.
- **Prueba controlada (solo lectura, BD dev Docker)**: billetero 11 GABRIELA
  ROMERO SOLIS (BIL011, activo) ↔ expendio 12 usuario "e2e_dot"
  (autorizado=1, 1 sesión activa) ↔ material sorteo 41 dotaciones
  4024/4025 con billetes 126-130 estatus='asignado',
  id_billetero_actual=11. La relación existe HOY y funciona sin cambios.
- Billeteros SIN expendio (verificado: 2-7, 9, 10, ... en dev) simplemente
  no aparecen en la app — coincide con el flujo confirmado ("habrá casos
  donde no tengan la app"); su material vive igual en Desktop.
- ¿Puede enlazarse por id_billetero? SÍ y YA EXISTE (FK directa). No se
  encontró NINGUNA asociación por nombre en el código (riesgo descartado
  empíricamente; se documenta como regla para no introducirla jamás).
- **Por qué NO enlazar por nombre**: nombre_completo SIN constraint UNIQUE
  (dos "Juan Pérez" = ambigüedad); renombrar/accentos/typos romperían el
  enlace; la FK ya está indexada, es estable y ya la usan auth, expendios,
  sorteos mobile, pagos y reportes. El nombre es dato de display, nunca
  clave. Relación correcta y segura: `id_billetero` (la existente).

## L. TRAZABILIDAD INDIVIDUAL (verificada en BD y código)

La cadena completa por billete/cachito existe HOY:

```
Recepción: paquetes_recibidos (id_paquete, estatus 'verificado')
  → paquetes_recibidos_detalle (id_sorteo, numero_sorteo, cantidad...)
  → E-TRASPASO crea billetes_loteria (una fila por vigésimo) +
    inventario_cedis (stock agregado por dotación)
→ Dotación: numero_sorteo (de la RECEPCIÓN, no del barcode)
→ CEDIS: ubicacion_actual='cedis', estatus='disponible'
→ Billetero: consigna → estatus='asignado', id_billetero_actual=X,
    ubicacion_actual='billetero' + movimientos_almacen(_detalle) con folio
→ Disponible en Mobile: mismo registro (estatus='asignado' AND
    id_billetero_actual=X de la sesión)
→ Venta: estatus='vendido', ubicacion='vendido', id_venta, fecha_venta,
    ventas_detalle.id_billete (id_billetero_actual SE CONSERVA)
  o → Devolución: estatus='devuelto', ubicacion='devolucion',
    movimientos_almacen tipo 'devolucion' (id_billetero = quién devolvió;
    id_billetero_actual pasa a NULL)
→ Cierre: asignado remanente → 'vendido' (presunción); cierres_sorteo +
    cierres_billeteros + caja_movimientos por id_billetero_actual
```

Claves de identificación del billete individual: `id_billete` PK ( Mobile
debe usarlo SIEMPRE al vender), unique (id_sorteo, COALESCE(numero_sorteo,
''), numero_billete, fraccion_vigesimo, serie), codigo_qr y codigo_barras
UNIQUE. numero_sorteo = la DOTACIÓN asignada en recepción (sorteos.
numero_sorteo es el código de PRODUCTO — no confundir; label de reportes
'num_recepción - nombre').

**ADVERTENCIA de semántica de `serie` (hallazgo)**:
`movimientos_almacen_detalle.serie` es SOBRESCRITO por el backend con el
numero_sorteo de la dotación (normalización al guardar), mientras que la
serie FÍSICA del billete vive SOLO en `billetes_loteria.serie`. NO asumir
que `serie` significa lo mismo en ambas tablas: en movimientos detalle
transporta la dotación; en billetes_loteria es la serie física (01-20 LN;
signo 01-12 Zodiaco). La atribución de dotación de
SorteosMobileController (Gestión) ya implementa la cascada correcta
(mad.serie validada contra billetes_loteria, fallback por
numero_billete/fracción, ambigüedad → NULL sin inventar).

Vínculo débil (estadística, no rompe la cadena): ventas_detalle.id_billete
es nullable y el camino legacy de venta no lo llena (ventas 73/75 en dev
sin id_billete) — Mobile vende SIEMPRE por id_billete (R4) y el historial
debe tolerar filas legacy.

## M. ACLARACIÓN OFICIAL DE LOTERÍA NACIONAL (2026-09-24) — alcance

Puntos confirmados y su estado:
1. Carga de billetes al billetero desde CEDIS Desktop vía
   `CEDIS → E. Entrega a billeteros → Entrega`, seleccionando el billetero
   y escaneando series/cachitos. → FLUJO ACTUAL: ya implementado en
   Desktop (§A.3); Mobile solo CONSULTA el resultado. Nada que construir
   en Desktop.
2. Aplica tanto para productos físicos como escaneables. → FLUJO ACTUAL:
   LN/Zodiaco escaneables con trazabilidad individual por vigésimo; Rasca/
   Prorra físicos por paquete (fila on-the-fly al escanear la entrega —
   registrarBilletesRascaProrraAlEscanear; codigo_barras UNIQUE). El MVP
   Vender LN se enfoca en escaneables; validar después si Rasca/Prorra
   entran al alcance Mobile (P12).
3. Los billetes asignados deben aparecer en la APP. → A IMPLEMENTAR: F1/F2
   (solo si el billetero tiene usuario; los demás operan igual por Desktop).
4. Ventas electrónicas: el proveedor/billetero trabaja con las numeraciones
   correspondientes y existe el concepto de garantía/pagaré electrónico
   asociado al límite autorizado. La garantía/pagaré electrónico se
   configura desde `CEDIS → Catálogos → Clientes → Editar → Garantías`.
   → FLUJO ACTUAL Desktop (verificado): pestaña Garantías de
   NuevoClienteWindow (nuevo_cliente_window.py:228) → `billeteros_garantias`
   (id_billetero, documento, monto); `billeteros.limite_credito` expuesto en
   datosBilletero() Mobile; `ventas_electronicas` (tipo_venta enum
   'TAE','SB LN', numeraciones en folio_transaccion/destino) existe para
   TAE y sabana LN electrónica. NO se asume que requiera implementación en
   Mobile: POR VALIDAR POSTERIORMENTE (P12) si la app debe mostrar
   garantía/límite/numeraciones electrónicas. No se inventan reglas al
   respecto.

## N. SEPARACIÓN VENDER vs GESTIÓN (endpoint distinto)

`Gestión → Sorteos` y `Vender → Lotería Nacional` tienen objetivos distintos:

- **Gestión → Sorteos** (ya real, a494b4d): HISTÓRICO — dotaciones,
  entregas/devoluciones por folio, pagos, saldos, pendientes; información
  financiera/operativa del sorteo del billetero. Endpoint:
  `GET /api/mobile/sorteos` (+/{id}) — semántica HISTÓRICA (UNION pista
  física movs + pista financiera cartera/pagos).
- **Vender → Lotería Nacional**: inventario LIVE disponible — billetes
  asignados al billetero, series/cachitos individuales, selección para
  venta, descuento al vender, devolución de no vendidos, sincronización
  inmediata con Desktop.

Por lo tanto: **NO reutilizar `/api/mobile/sorteos` como endpoint de
inventario de Vender** (semánticas incompatibles; rompería Gestión).
`/api/mobile/sorteos` queda como consulta histórica de Gestión. Endpoint
específico para Vender: **propuesta** `/api/mobile/ventas/sorteos` (nombre
definitivo a decidir en F1 — alternativas: /api/mobile/inventario/sorteos,
/api/mobile/disponibilidad). No implementar todavía.

## RIESGOS E INCONSISTENCIAS (R1-R9, verificados 2026-09-24)

- **R1 (ALTO, preexistente Desktop)**: el cierre NO acredita devoluciones
  por billetero en cierres_billeteros: la devolución NULLifica
  id_billetero_actual y la agregación filtra NOT NULL → total_entregado
  sub-registrado y total_devuelto=0 por billetero (discrepancia vs el
  pagaré impreso en la entrega por el monto completo). El saldo_pendiente
  sale correcto (los devueltos no cuentan como deuda), pero el registro
  financiero no refleja las devoluciones del billetero. Verificar con
  prueba controlada antes de F4; fix Desktop SEPARADO con aprobación.
- **R2 (ALTO)**: movimientos-almacen (ruta de devolución de billeteros) NO
  valida cierres_sorteo (solo DevolucionesTiendaController L215-237 lo
  hace): se puede devolver DESPUÉS del cierre y descuadrar los totales ya
  materializados. El endpoint mobile de devolución DEBE llevar guard de
  cierre (patrón DevolucionesTienda); fix Desktop del guard global =
  separado, con aprobación.
- **R3 (MEDIO)**: cancelar venta Desktop devuelve boletos a
  'disponible'/'cedis' y NULLifica id_billetero_actual — si el billete fue
  vendido por Mobile (o consignado), el billetero sigue teniendo el físico
  y se pierde la atribución. La cancelación Mobile devuelve a 'asignado'
  al MISMO billetero (D5); el comportamiento Desktop queda documentado y
  su cambio es decisión aparte.
- **R4 (MEDIO)**: camino legacy de venta sin id_billete (ventas 73/75 en
  dev). Mobile vende SIEMPRE por id_billete; historial tolera legacy.
- **R5 (MEDIO)**: `ventas.id_usuario_captura` NOT NULL y Mobile no tiene
  usuario Desktop. Estrategia requerida (P8): usuario de sistema dedicado
  (p.ej. 'MOBILE_APP') o equivalente — decisión antes de F3.
- **R6 (BAJO, bug Desktop)**: entrega_billeteros_window.py:1110-1113 —
  código muerto abre un SEGUNDO DevolucionBilleterosDialog sin user_data.
  Fix menor separado.
- **R7 (BAJO)**: regla de disponibilidad divergente — mock 7.x filtra por
  fecha (EstaDisponible); el modelo real emerge del inventario. Se resuelve
  al conectar F2.
- **R8 (INFO)**: el concentrador de v2 nunca se sembró (0 filas) — el
  modelo por-billetero confirmado por Lotería es el que ya está
  implementado. La "Fase 0 datos" de v2 queda obsoleta.
- **R9 (INFO)**: el cierre presume asignado=vendido → la devolución Mobile
  debe ocurrir ANTES del cierre (guard 409 en F4; referencia
  DevolucionesTienda L215-237).

## DECISIONES RESUELTAS

V2 (2026-09-15), conservadas salvo marca:
1. ~~Concentrador~~ — SUPERSEDED por flujo Lotería v3 (§0). El modelo es
   por-billetero.
2. Ciudad: SÍ se usa en Mobile (dato operativo); pantalla 8 SE CONSERVA
   → v3: SUJETA A RE-VALIDACIÓN (P3).
3. 9.x: 1 fila = 1 serie (≤20 fracciones, 4 tiras de 5); disponibilidad
   real. VIGENTE.
4. id_tienda: NULL por ahora. VIGENTE.
5. Comisión estimada: SÍ, fuente backend, misma lógica Desktop. v3: la
   fuente es comision_porcentaje DEL PROPIO billetero (misma fila que el
   cierre) — confirmar al exponerla.
6. Billete completo 20/20: SÍ, y parciales, respetando disponibilidad.
   VIGENTE.
7. Sorteos celebrados: PENDIENTE deliberada (P4).
8. Ticket: cliente PÚBLICO EN GENERAL, SIN whatsapp. VIGENTE.
9. Pantalla 6: 7 productos reales (precios/20 = mockup exacto); Gordito
   Navideño no existe (crear cuando el negocio lo tenga); mapeo
   colores/subcódigo en app vs backend (P6).
10. Impresora: extender infra existente (NO duplicar); ESC/POS texto
    recomendado; spike con impresora física. VIGENTE (actualizado §H:
    descubrimiento probado en Tec).
11. Consigna vs entrega: mantener 'consigna'. VIGENTE.
12. Vencimiento: PENDIENTE (P5) — propuesta job post-cierre, NO implementar.

V3 (2026-09-24, derivadas del flujo confirmado + investigación):
13. Identidad por `id_billetero` (FK existente; jamás por nombre) — §K.
14. Vender y Gestión usan endpoints DISTINTOS (/api/mobile/sorteos queda
    como histórico de Gestión; Vender con endpoint propio propuesto) — §N.
15. Fuente de verdad = estado LIVE de billetes_loteria; Mobile NO crea
    inventario paralelo — §D.0.
16. Disponibilidad Mobile = billetes 'asignado' del billetero de la sesión,
    vigente desde el GUARDADO de la consigna (no desde "Aplicar") — §A.3.
17. Venta Mobile SIEMPRE por id_billete, conservando id_billetero_actual.
18. Devolución Mobile escribe estado LIVE inmediato; capa administrativa
    pendiente→liquidado = decisión P9.

## DECISIONES AÚN PENDIENTES (no determinables; no se inventan)

P1. ~~Comisión/ISR/fondo del CONCENTRADOR~~ — OBSOLETA (sin concentrador).
P2. Fuente del % de comisión estimada — RESUELTA POR DISEÑO v3 (propio
    billetero); confirmación de negocio al mostrarla en UI.
P3. Ciudad/pantalla 8 en el modelo por-billetero: ¿se conserva como paso
    operativo o se elimina? (si se conserva → catálogo real de ciudades +
    migración opcional ciudad_venta).
P4. Restricción de venta de sorteos celebrados (deliberada).
P5. Proceso de vencimiento (deliberada; propuesta job post-cierre).
P6. Pantalla 6: mapeo colores/subcódigo en app vs backend (menor).
P7. "Plaza" del ticket (¿ciudad del CEDIS?) — campo del PDF 5.2.
P8. Estrategia `ventas.id_usuario_captura` para ventas Mobile (usuario de
    sistema dedicado vs equivalente) — bloquea F3.
P9. Devolución Mobile: ¿reproducir el flujo administrativo
    pendiente→liquidado de Desktop o solo estado LIVE + movimiento?
    — bloquea el diseño de F4 (no el guard de cierre).
P10. Migración opcional `ventas.ciudad_venta` — depende de P3.
P11. Nombre definitivo del endpoint de Vender (propuesta
    /api/mobile/ventas/sorteos) — F1.
P12. Alcance posterior: garantía/pagaré electrónico, límite autorizado y
    numeraciones de ventas electrónicas en Mobile (§M.4); Rasca/Prorra en
    Vender Mobile (§M.2). POR VALIDAR CON NEGOCIO — nada asumido.

## PUNTOS DE DETENCIÓN (antes de implementar)

D1. Migración `ventas.ciudad_venta` (opcional, depende de P3) → requiere OK.
D2. VentaService toca actionCrear (Desktop) → paridad + regresión completa
    antes de abrir endpoint mobile; si falla, paro y reporto.
D3. Cancelación mobile al MISMO billetero (semántica distinta de Desktop)
    → aislada en mobile; confirmar que se quiere así.
D4. Venta Mobile conserva id_billetero_actual (la cancelación Desktop no) —
    confirmado por diseño v3; cualquier cambio a actionCancelar Desktop
    queda FUERA del MVP.
D5. Fixes Desktop preexistentes (R1 cierre/devueltos, R2 guard cierres
    global, R6 bug diálogo) → AUDIT-THEN-FIX: audit ya hecho; implementar
    SOLO con aprobación explícita y por separado del MVP Mobile.

## ESTADO DE VALIDACIÓN (v3)

HECHOS CONFIRMADOS (Lotería Nacional, 2026-09-24): flujo por-billetero §0;
carga desde E. Entrega a billeteros con escaneo de series/cachitos; billetes
asignados aparecen en la app; venta descuenta y se refleja en Desktop;
devolución reflejada en Desktop con trazabilidad; garantía/pagaré
electrónico configurado desde Clientes→Garantías (flujo actual Desktop).

HALLAZGOS TÉCNICOS (verificados en BD/código 2026-09-24): cadena de
identidad por FK (§K); estado LIVE al guardar consigna (§A.3); devolución
LIVE inmediata + capa administrativa (§A.9); cierre presume vendido y
agrupa por id_billetero_actual (§A.10); serie con significados distintos
movimientos_detalle vs billetes_loteria (§L); /api/mobile/sorteos con
semántica histórica (§N); concentrador jamás sembrado (R8); riesgos R1-R9.

DECISIONES PENDIENTES: P3-P12 (arriba). El documento queda orientado a
implementación futura (F0-F7); nada de lo anterior está implementado en
esta revisión.
