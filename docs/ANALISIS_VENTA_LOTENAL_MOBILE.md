# Análisis — Vender → Lotería Nacional (LOTENAL) en Mi Cachito Mobile

Fecha: 2026-09-15 (v2 — incorpora correcciones de negocio del usuario: modelo concentrador)
Tipo: AUDITORÍA + PROPUESTA SIN IMPLEMENTACIÓN (patrón audit-then-fix)
Fuentes: backend Yii2 dev-mobile (a79b91f) + BD real Docker (DESCRIBE/SELECT verificados), Desktop (src/ui), Mobile master (2f188a5), docs/ Mobile, skill refs (entrega-billeteros, sorteos-billetero-integration, lna-data-flow, billetes-loteria-inventario-architecture, numero-sorteo-validation, cierres-sorteo-dotacion, integracion-decisiones).
Cero archivos de código modificados.

---

## 0. MODELO CONCENTRADOR (corregido por el usuario, 2026-09-15)

`Mi Cachito App` NO es el usuario final: es el **billetero concentrador** que
recibe las dotaciones desde Desktop. Los usuarios/billeteros de Mobile venden
DESDE ese pool compartido; la ciudad es un dato operativo de la venta, no del
inventario físico.

```
DESKTOP → ALMACÉN/AGENCIA → Dotación física LN
   → BILLETERO "MI CACHITO APP" (concentrador)     ← consigna estándar
   → Inventario disponible (pool)
   → Usuarios/billeteros Mobile (N, sin dotación propia)
   → cada uno selecciona ciudad (dato operativo)
   → venden billetes/cachitos del pool
```

Implicaciones verificadas contra BD/código:
- Los usuarios Mobile NO necesitan dotación propia: sus filas `billeteros`
  (vía `billeteros_expendios`) son solo identidad/credenciales.
- La disponibilidad se consulta contra `billetes_loteria.id_billetero_actual
  = <concentrador>` AND `estatus='asignado'` — NO contra el billetero de la
  sesión (diferencia clave vs v1 de este documento).
- La atomicidad entre usuarios concurrentes se resuelve en la transacción de
  la venta (SELECT FOR UPDATE, patrón existente), no con reservas previas.
- Ciudad: dato de la venta (persistencia propuesta en §J.5), NO del CEDIS
  (verificado: `cedis` no tiene columna ciudad; la única columna ciudad del
  backend está en `tiendas_sucursales` con un solo valor 'Puebla').

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
  Unique key real aplicado (m260831): (id_sorteo, COALESCE(numero_sorteo,''),
  numero_billete, fraccion_vigesimo, serie). NOTA dev: la BD no tiene aún
  datos reales de 20 fracciones por billete (fraccion 0 dominante, 25 filas) —
  el E2E de Mobile necesitará sembrar un sorteo con granularidad real.

### A.3 Entrega/dotación al billetero (consigna)
`Entrega a Billeteros` → POST `/api/movimientos-almacen` con movimiento
tipo 'consigna' (folio MA-Ymd-#### secuencial; endpoint viejo
`/api/almacen/entregas/billetero` responde 410 deprecado) + detalles
(numero_billete, serie=numero_sorteo de la dotación, fraccion, cantidad,
precio). El backend NORMALIZA el precio (`normalizarPrecioFraccion` fuerza
`sorteos.precio_fraccion`, ignora el del cliente). Efectos:
`BilletesLoteria::sincronizarPorMovimiento` → cada billete `estatus='asignado'`,
`ubicacion_actual='billetero'`, `id_billetero_actual=<id>` (FOR UPDATE, exige
'disponible' previo). NO toca inventario_cedis. Luego `aplicar-movimiento` →
'estatus aplicada'. Comprobante: 2 tickets (FACTURA PAGARÉ + DESGLOSE)
client-side (`comprobante_entrega.py`). **Este es EXACTAMENTE el mecanismo
que dotará al concentrador "Mi Cachito App": sin ningún cambio.**

### A.4 Venta (POS Almacén → CEDIS)
`NuevoVentaDialog` → POST `/api/ventas` (actionCrear): items con precio del
CLIENTE (no re-validado), `id_billetero` e `id_tienda` del REQUEST (si falta
tienda resuelve la PRIMERA del CEDIS), folio `V-Ymd-RAND` colisionable.
Transacción: escribe ventas+ventas_detalle, descuenta `inventario_cedis`
('vendida'), marca billetes 'vendido' — camino (a) con id_billete: FOR UPDATE
+ estatus IN ('disponible','asignado') → 'vendido' (los asignados a un
billetero SON vendibles por este camino; exactamente lo que el pool necesita).
Comisión: NO se calcula en la venta; se materializa al CIERRE DE SORTEO
(CierresSorteoController:351-383 — por billetero con `billeteros.comision_porcentaje`,
desde `billetes_loteria.id_billetero_actual` GROUP BY → `cierres_billeteros`
+ `caja_movimientos` 'cobro_billetero'). Ticket de venta: REMISIÓN PAGARÉ
HTML client-side; el backend NO tiene ticket/PDF de venta.

### A.5 Sorteos disponibles
NO existe regla de fechas en backend (`fecha_inicio_ventas`/
`fecha_cierre_ventas` NULL y sin uso). La disponibilidad EMERGE del
inventario/dotación real (sorteo vigente ∩ paquete verificado ∩ stock>0;
sorteos-disponibles-entrega usa `dotacionesConStockPorMovimientos`).
"Próximo + 2 adelantados" es práctica operativa: la lotería llega ~7 días
antes → solo hay inventario del próximo → la restricción física se resuelve
sola. Para Mobile: mostrar SOLO sorteos con dotación asignada al CONCENTRADOR.
Sin regla artificial de fechas (confirmado por usuario: sin bloqueo por
celebrado de momento).

### A.6 Consulta por billetero
`SorteosBilleteroController::actionIndex`: sorteos donde el billetero tiene
billetes (`id_billetero_actual=X` GROUP BY sorteo ORDER BY fecha_sorteo) —
permisos CEDIS (caja.cobros O almacen.entrega), pensada para consultas SOBRE
un billetero. La QUERY es la que Mobile necesita apuntando al concentrador.

### A.7 Alta de billeteros (cómo se crea el concentrador)
`ContCedisClientesController` (permiso contabilidad.clientes, ventana
Clientes de Contabilidad CEDIS): INSERT completo en `billeteros` con
id_cedis, id_zona (del CEDIS), clave, nombre, comision_porcentaje,
retencion_isr, fondo_ahorro, `tipo_config` (varchar(20) LIBRE, default
'NORMAL', solo se escribe aquí — nunca lo lee una regla de negocio; hoy
'existe' un valor 'ESPECIAL' en datos), categoria, estatus. **El
concentrador se crea por esta vía (dato, no código).**

### A.8 Catálogo de productos LN (pantalla 6)
`productos` tipo loteria_nacional = 7 reales: Mayor(600), Superior(800),
Zodiaco(400), Magno(2400), Zodiaco Esp(700), Especial(1200), Gran Especial
(5000). Verificado: precio_unitario/20 = EXACTO el precio del mockup 6
($30/$40/$20/$120/$35/$60/$250). El mock tiene además "GORDITO NAVIDEÑO"
($120) que NO existe en BD (producto estacional por crear cuando exista).
Zodiaco y Zodiaco Esp son productos LN (encabezado LOTERIA NACIONAL).

---

## B. FLUJO ESPERADO MOBILE (modelo concentrador)

```
Desktop: recepción → E-TRASPASO → consigna a "Mi Cachito App" (pool)
Mobile: login expendio (Fase 0) → resolver CONCENTRADOR del CEDIS del
        billetero de la sesión → GET /api/mobile/sorteos (sorteos del pool)
        → GET /api/mobile/ventas/billetes (series + fracciones libres)
        → usuario selecciona CIUDAD (dato operativo, pantalla 8)
        → carrito (9.x/10.x/11) → confirmar
        → POST /api/mobile/ventas {items por fracción (id_billete exacto),
           ciudad} — id_billetero vendedor = sesión; precio server-side
           (precio_fraccion); id_tienda NULL; folio robusto; comisión
           estimada en respuesta
        → billetes 'asignado'→'vendido' (FOR UPDATE = anti-doble-venta)
        → VentaExitosa (folio+resumen+comisión) → ticket Bluetooth
```

Reglas duras (del usuario + auditoría):
- La app NUNCA genera inventario; vende SOLO filas del pool
  (`id_billetero_actual=concentrador`, `estatus='asignado'`).
- Ciudad = atributo de la venta; cualquier ciudad, sin relación con el CEDIS
  receptor. La pantalla 8 SE CONSERVA.
- Unidad visual 9.x: **1 fila = 1 serie (numero_billete+serie[+signo])** con
  sus ≤20 fracciones libres; NO una fila por cachito. Venta de 20/20
  (completo) y parciales permitida.
- id_tienda = NULL en ventas mobile (backend no debe resolver/inventar tienda).
- Comisión estimada calculada por el BACKEND (misma lógica Desktop:
  comision_porcentaje × monto), Mobile solo la presenta.
- id_billetero del vendedor SIEMPRE de la sesión (mobile_sesiones →
  billeteros_expendios → billeteros), jamás del request. Precio jamás del
  cliente. Dotación cerrada (cierres_sorteo) → rechazar venta (409, mismo
  patrón que entrega/devolución).

---

## C. LO QUE YA EXISTE (resumen; detalle y evidencia en v1/skill)

- Backend/BD: todas las tablas del dominio + flujo completo ejercitado con
  datos e2e. Lógica reutilizable: sincronizarPorMovimiento/revertir,
  InventarioCedis::descontar/aumentar, generarFolio secuencial,
  normalizarPrecioFraccion, expandirDetallesVigesimos, actionCrear/cancelar
  (camino (a) por id_billete = el mecanismo del pool), verificar-cachito/
  verificar-serie, query sorteos-por-billetero, alta de billeteros (A.7),
  cierre de sorteo con comisión por id_billetero_actual (A.4).
- Auth Fase 0: token mobile → expendio → billetero (→ su id_cedis). El
  concentrador se resuelve: billeteros WHERE tipo_config=<marcador> AND
  id_cedis=<del billetero de la sesión>.
- Desktop: flujo completo operado; la dotación al concentrador usa la
  operación existente (consigna) — decisión del usuario confirmada (#11).
- Mobile: pantallas 6→7.x→8→9.x→10.x→11 (TODAS conservan; nota: "pestañas" =
  pantalla 6 tipos + lista 7.x con fecha por sorteo; el calendario real es
  la fecha por tarjeta), infraestructura ApiClient/navegación/entidades,
  descubrimiento BT listo (BondedDevices+permisos), TicketPdfService,
  spec NOTAS_TICKETS_VENTA.md. Falta SPP/ESC-POS.

---

## D. LO QUE FALTA

Backend (dev-mobile) — solo agregar + 1 extracción + 1 migración aditiva:
1. `GET /api/mobile/sorteos` — sorteos del POOL del concentrador del CEDIS
   de la sesión (query tipo SorteosBilleteroController::actionIndex apuntada
   al concentrador; con fecha_sorteo, precio_fraccion, numero_sorteo/dotación,
   subcodigo/tipo para colorear la UI).
2. `GET /api/mobile/ventas/billetes?id_sorteo=` — series del pool agrupadas
   por (numero_billete, serie[, signo]) con conteo de fracciones libres
   (estatus='asignado'), precio_fraccion, y la lista de fracciones libres
   para seleccionar 20/20, tiras de 5 o individuales.
3. `VentaService` — extracción de actionCrear (validar ítems, precio
   server-side, totales, folio robusto secuencial, marcar billetes camino
   (a), descontar inventario_cedis del CEDIS del concentrador) con
   paridad exacta; actionCrear delega (MISMO contrato Desktop).
4. `POST /api/mobile/ventas` — sesión→vendedor; ítems por fracción con
   id_billete exacto; ciudad_venta; id_tienda NULL; validar dotación no
   cerrada; respuesta con folio, desglose, comisión estimada y datos de
   ticket (cliente=PÚBLICO EN GENERAL, sin whatsapp — decisiones #8).
5. `POST /api/mobile/ventas/{id}/cancelar` — venta→'cancelada', billetes
   'vendido'→'asignado' **al concentrador** (ubicacion='billetero',
   id_billetero_actual=concentrador — NO 'disponible'/cedis como Desktop,
   ver §J.7), reposición simétrica de inventario.
6. `GET /api/mobile/ventas` + `/{id}` — historial y reimpresión.
7. Migración aditiva: `ventas.ciudad_venta varchar(100) NULL` (única tabla
   Desktop tocada; ver §G/§J.5 — REQUIERE APROBACIÓN).

Mobile (master) — conectar, UI intacta:
8. ApiEndpoints.MobileSorteos/MobileVentas + MobileSorteosService/
   MobileVentasService + DTOs (patrón AuthService; NO tocar SorteosService
   = mock de Gestión).
9. SorteosActivosVM (sorteos del pool), AgregarBoletosVM (series/fracciones
   reales, fila=serie), CarritoComprasVM.VenderAsync → POST real,
   VentaExitosaLotenalPage (patrón Tec + folio/comisión/total reales).
10. Impresión real: SPP + ESC/POS sobre IImpresoraService existente.
11. Historial DetalleVenta con datos reales; saldo real de sesión.

NO falta: dotación al concentrador (consigna Desktop existente), comisión
(lógica de cierre existente), inventario individual (por vigésimo),
precios (precio_fraccion), identidad de usuarios (expendios Fase 0).

---

## E. DATOS/MOCK: sustituir vs NO tocar

Sustituir (exclusivos Vender LN): `TiendasDisponiblesData` (filas 9.x de
prueba → GET billetes del pool), `SorteosLotenalData` como fuente de la
pantalla 7.x (→ GET sorteos del pool; pantalla 6 → productos reales §A.8 —
pendiente #9), saldo "$0.00" hardcodeado.
Se CONSERVA (decisión #2): pantalla 8 + su catálogo de ciudades como PICKER
operativo; `CiudadesCedisData` sobrevive como catálogo de UI v1 (contenido
real de ciudades = pendiente — las 9 del mockup son del territorio Sr.
Billetero; verificar con negocio si son las ciudades reales de operación).
`SignosZodiacoData` queda estático (catálogo inmutable 12 signos).
NO TOCAR: `SorteosTecData`, `TiempoAireData`, `ExpendiosDemoData`, y
CRÍTICO `SorteosActivosLotenalData` COMPARTIDO con Gestión (Sorteos
Celebrados) y Devolución (FechaSorteoCodigo valida QR) — solo se sustituye
el consumo del flujo Vender. Ningún dato de BD se toca desde Mobile (la BD
dev/e2e es de integración, no seed de la app).

---

## F. PROPUESTA TÉCNICA (archivos)

Backend (dev-mobile):
- NUEVO `models/services/VentaService.php`: lógica extraída de actionCrear
  (paridad exacta) + variante mobile (precio server, id_tienda NULL,
  ciudad, cancelación al concentrador). VentasController::actionCrear delega.
- NUEVO `controllers/api/mobile/SorteosController.php` (actionIndex: pool),
  `controllers/api/mobile/VentasController.php` (actionBilletes, actionCrear,
  actionIndex, actionView, actionCancelar).
- `config/web.php`: bloque nuevo /api/mobile/sorteos, /api/mobile/ventas*
  (rutas Desktop intactas).
- MIGRACIÓN aditiva `ventas.ciudad_venta varchar(100) NULL` (aprobación
  previa; ninguna columna existente se modifica; INSERTs Desktop explícitos
  no se rompen — columna queda NULL para ventas Desktop).
- Resolución del concentrador: helper en BaseMobileController (billeteros
  WHERE tipo_config='MICACHITO_APP' AND id_cedis=sesión; fallback claro si
  no existe → 503 "concentrador no configurado").
- CERO otras tablas nuevas: concentrador = fila de datos en billeteros;
  atomicidad = FOR UPDATE existente; ciudad = columna aditiva.

Mobile (master):
- `Api/ApiEndpoints.cs` (+MobileSorteos, +MobileVentas), NUEVOS
  `Services/MobileSorteosService.cs`, `Services/MobileVentasService.cs`,
  DTOs `Models/Responses/`.
- VMs existentes conectados (SorteosActivos, AgregarBoletos, CarritoCompras,
  SeleccionCiudad pasa ciudad al carrito/venta), NUEVA
  `Views/VentaExitosaLotenalPage.xaml`+VM.
- `Platforms/Android/Services/ImpresoraService.cs`: SPP+ESC/POS (fase E).
- Pantalla 6 alimentada por productos LN reales (7; colores/subcódigo
  mapeados como hoy, ver pendiente #9).

---

## G. IMPACTO DESKTOP (riesgo y mitigación)

1. **Migración `ventas.ciudad_venta` (BAJO, requiere tu aprobación)**:
   columna NULL aditiva; ventas Desktop quedan NULL; ninguna consulta
   Desktop hace SELECT * dependiente de columnas nuevas; INSERT Desktop
   enumera columnas explícitas → no se rompe. Es la ÚNICA modificación a
   tabla Desktop del plan.
2. **VentaService (MEDIO)**: único cambio en código Desktop compartido
   (actionCrear). Paridad exacta + regresión Desktop 44/4 + E2E Docker de
   venta/cancelación ANTES de abrir el endpoint mobile. Si la paridad
   falla: DETENERSE.
3. **Cancelación mobile al concentrador (semántica distinta, AISLADA)**:
   Desktop cancel → 'disponible'/cedis; Mobile cancel → 'asignado'/
   concentrador. Vive SOLO en el controller/service mobile; actionCancelar
   Desktop intacto. Documentado para cierre de sorteo: los billetes del
   pool cancelados vuelven a ser vendibles por Mobile (correcto: la
   devolución física al CEDIS la sigue operando Desktop).
4. **Cierre de sorteo (CERO cambio)**: con id_billetero_actual =
   concentrador en los billetes vendidos, el cierre existente agrupa todo
   el pool al concentrador con SU comisión — flujo Desktop intacto
   (ver sub-decidión J.4b).
5. Rutas nuevas aditivas; auth /api/auth/* intacto; aislamiento: sesión→
   vendedor, pool→concentrador, precio server-side, dotación cerrada→409.

---

## H. FLUJO DE IMPRESIÓN (decisión #10: qué existe realmente)

Infraestructura EXISTENTE (verificada): permisos manifest (BLUETOOTH_CONNECT
runtime, BLUETOOTH/ADMIN ≤30; falta BLUETOOTH_SCAN solo para discovery
in-app — innecesario: se usa BondedDevices), `IImpresoraService` (contrato:
ImprimirAsync + ObtenerDispositivosEnlazadosAsync), `ImpresoraService`
Android (BondedDevices + guard permiso; SIN socket SPP, SIN ESC/POS, SIN
escritura — stub documentado), `DispositivosEnlazadosPage` (selección),
`TicketPdfService` (PDF 5.2 carta), botón Imprimir en DetalleVentaVM.
**NO existe: modelo de impresora, formato 58/80, texto vs bitmap, conexión,
reimpresión.** Propuesta: extender IImpresoraService (NO segunda
infraestructura): SPP UUID 00001101-0000-1000-8000-00805F9B34FB + ESC/POS
texto directo (compatible 58/80 con ancho configurable), impresora
guardada en Preferences, spike en impresora física antes de la fase E.
Errores (regla: venta registrada UNA vez): sin impresora/apagada/conexión
perdida/fallo → aviso + "Reintentar impresión" + "Ver ticket" (PDF
compartible ya funciona) + reimpresión desde historial (GET /{id}).

---

## I. PLAN DE IMPLEMENTACIÓN (fases pequeñas y seguras)

- **Fase 0 (datos, sin código)**: crear concentrador por CEDIS vía Clientes
  Desktop (o SQL acordado) con tipo_config='MICACHITO_APP' y comisión/ISR/
  fondo definidos por negocio; dotarle un sorteo de prueba por consigna
  normal; sembrar billetes con 20 fracciones reales para E2E.
- **Fase A (backend lectura)**: GET /api/mobile/sorteos + billetes del pool.
  E2E contra Docker. Commit.
- **Fase B (backend venta)**: migración ciudad_venta (aprobada) →
  VentaService paridad (regresión Desktop 44/4) → POST venta mobile +
  cancelar + historial. Si paridad falla: parar y reportar.
- **Fase C (mobile lectura)**: VMs con datos reales (fila=serie, fracciones
  libres), pantalla 6 con productos reales, ciudad picker intacto →
  emulador → inspección → commit.
- **Fase D (mobile venta)**: VenderAsync→POST, VentaExitosaLotenal (folio,
  total, comisión estimada), historial real → inspección → commit.
- **Fase E (impresión)**: spike impresora física → SPP+ESC/POS + guardado
  + errores + reimpresión.
- **Fase F (limpieza)**: mocks LN sin uso, saldo real, pendientes #7/#9/#12.

---

## MATRIZ (§4, actualizada)

| Funcionalidad | Ya existe | Reutilizable | Falta | Dónde |
|---|---|---|---|---|
| Sorteos LN | Catálogo sorteos+productos (7 LN reales) + query por-billetero | Query sí; permisos no | Endpoint pool del concentrador | mobile/SorteosController |
| Calendario | Mobile: fecha por tarjeta 7.x | UI sí | Datos reales (fecha_sorteo del pool) | Fase C |
| Ciudad (venta) | Pantalla 8 + catálogo UI | UI sí | Persistencia: ventas.ciudad_venta | Migración aprobada + Fase B |
| Precios | sorteos.precio_fraccion + normalizarPrecioFraccion | Sí | Resolver server-side en venta mobile | VentaService |
| Inventario | billetes_loteria 1 fila/vigésimo + inventario_cedis | Sí | Consulta del pool por serie | mobile/VentasController::actionBilletes |
| Dotación concentrador | Consigna Desktop completa + comprobante | Sí (tal cual) | SOLO crear la fila del concentrador (dato) | Fase 0 |
| Disponibilidad venta | verificar-cachito/serie; acción 'asignado' vendible | Sí | Listado series+fracciones del pool | Fase A |
| Venta | actionCrear camino (a) | Lógica sí; contrato no (precio/tienda cliente) | VentaService + endpoint mobile (sesión, precio server, tienda NULL, ciudad) | Fase B |
| Comisión | billeteros.comision_porcentaje + cierre | Sí (misma fórmula) | Exponer estimada en respuesta de venta | Fase B |
| Folio | V-Ymd-RAND (débil) | Patrón generarFolio secuencial | Folio robusto en VentaService | Fase B |
| Ticket | Spec 5.2 + TicketPdfService + PÚBLICO GENERAL/sin whatsapp | Sí | Datos reales + reimpresión | Fases B/D |
| Impresión BT | Descubrimiento (BondedDevices+permisos) | Sí | SPP+ESC/POS+guardado+errores | Fase E |
| Historial | DetalleVenta mock; GET /api/ventas | Patrón sí | Endpoint historial mobile + conectar | Fases B/D |
| Anti doble-venta | FOR UPDATE camino (a) | Sí | NADA (transacción de venta) | — |
| Cancelación mobile | actionCancelar Desktop | Patrón sí | Semántica al concentrador (aislada) | Fase B |

---

## J. RESPUESTAS AL MODELO CONCENTRADOR (las 10 preguntas)

**J.1 Cómo crear "Mi Cachito App"**: vía existente ContCedisClientesController
(permiso contabilidad.clientes; INSERT verificado A.7) o SQL acordado.
UNO POR CEDIS (billeteros.id_cedis NOT NULL; la dotación y el inventario son
por CEDIS; un concentrador nacional rompería el scoping de entrega/consigna).
Campos: id_cedis=<CEDIS>, id_zona=<zona del CEDIS; zonas reales: Centro/
Toluca/Guadalajara/Monterrey>, clave 'MCAPP-<id_cedis>', nombre 'Mi Cachito
App — <CEDIS>', tipo_config='MICACHITO_APP' (marcador; varchar libre nunca
validado — verificado), categoria p.ej. 'CONCENTRADOR APP', estatus 'activo',
comision_porcentaje/retencion_isr/fondo_ahorro = DECISIÓN de negocio (ver
J.4b), tiene_tiempo_aire=0, tiene_prod_digitales=0. Para RECIBIR dotaciones
solo necesita existir y estar activo (la consigna no valida comisión).

**J.2 Relación con la dotación**: CERO código — Entrega a Billeteros
Desktop selecciona "Mi Cachito App" como billetero → consigna →
billetes id_billetero_actual=concentrador, estatus='asignado' (mecanismo
A.3 tal cual). El comprobante pagaré/desglose se imprime igual.

**J.3 Consultar su inventario**: GET /api/mobile/sorteos (sorteos con
filas id_billetero_actual=concentrador AND estatus='asignado', GROUP BY
id_sorteo+numero_sorteo, con fecha_sorteo/precios) y GET
/api/mobile/ventas/billetes (por serie: J.3b). El concentrador se resuelve
desde la sesión: expendio→billetero→id_cedis→concentrador (tipo_config).

**J.4 Reservar/asignar a un usuario + (b) quién queda como dueño tras la
venta**: NO se asigna previamente (MVP sin reservas): el claim es atómico
en la transacción de venta. Recomendación de atribución: los billetes
vendidos MANTIENEN id_billetero_actual=concentrador (el pool es suyo; el
cierre de sorteo existente agrupa y comisiona al concentrador sin cambiar
NADA de Desktop); el VENDEDOR queda en ventas.id_billetero (billetero de la
sesión) + bitácora mobile. Alternativa (reasignar al vendedor) rompería la
atribución del cierre por billetero y repartiría el pool entre usuarios —
no recomendada. SUB-DECISIÓN pendiente: para la COMISIÓN ESTIMADA que se
muestra al usuario (decisión #5), ¿se usa comision_porcentaje del
CONCENTRADOR (margen de la app) o del VENDEDOR de la sesión? La fórmula es
la misma (monto × %/100, calculada en backend); solo cambia la fila fuente.

**J.5 Registrar la ciudad**: propuesta `ventas.ciudad_venta varchar(100)
NULL` (migración aditiva, única tabla Desktop tocada, aprobación previa).
Alternativas descartadas: observaciones de ventas_detalle (hacky), tabla
mobile_ciudades (innecesaria v1 — el picker vive en la app con
CiudadesCedisData; promover a catálogo backend si negocio lo pide).
Pendiente menor: contenido real del catálogo de ciudades (las 9 del mockup
son territorio Sr. Billetero).

**J.6 Evitar doble venta entre usuarios**: SELECT ... FOR UPDATE +
transición 'asignado'→'vendido' dentro de la transacción de la venta
(patrón EXISTENTE camino (a)). El segundo usuario concurrente recibe 409
"fracción ya no disponible" y la UI refresca disponibilidad. Sin tablas de
reserva. (Si más adelante se quiere "apartar" antes de pagar → estatus
'reservada' = migración + diseño = fuera de MVP.)

**J.7 Devolver disponibilidad si falla/cancela**: POST
/api/mobile/ventas/{id}/cancelar: ventas.estatus='cancelada' + billetes de
la venta 'vendido'→'asignado' con id_billetero_actual=CONCENTRADOR y
ubicacion='billetero' (NO 'disponible'/cedis: el dueño del stock es el
pool) + ajuste simétrico de inventario_cedis + guard de doble cancelación.
Semántica DISTINTA de actionCancelar Desktop (que libera a disponible/cedis)
— aislada en el flujo mobile, actionCancelar Desktop intacto (ver G.3).

**J.8 Mantener intacto Desktop**: rutas nuevas aditivas; /api/auth/* sin
tocar; consigna/cierres/reportes Desktop sin cambios (el concentrador es un
billetero más para ellos); VentaService con paridad probada; la única tabla
Desktop modificada es ventas (+ciudad_venta NULL) con aprobación; las
diferencias de semántica (tienda NULL, cancelación al pool) viven solo en
el controller mobile.

**J.9 Tablas/servicios nuevos realmente necesarios**: tablas NUEVAS: ninguna.
Migración aditiva: 1 (ventas.ciudad_venta). Servicios: VentaService
(extracción paritaria) + controllers mobile (Sorteos, Ventas) + helper
resolución de concentrador. Datos: fila del concentrador por CEDIS.

**J.10 Reutilizable sin cambios**: consigna+comprobante (J.2), cierre de
sorteo (J.4), verificar-cachito/serie, normalizarPrecioFraccion,
generarFolio (patrón), expandirDetallesVigesimos (E2E), auth Fase 0
(sesión→expendio→billetero→cedis), ApiClient/SessionService/navegación,
entidades mapeadas, descubrimiento BT, TicketPdfService+ShareFileRequest,
FormatosFecha. Pantallas 6/7.x/8/9.x/10.x/11 íntegras (solo se conectan).

---

## DECISIONES RESUELTAS (por el usuario, 2026-09-15)

1. Concentrador: SÍ existe en BD, uno concentrador (no por usuario); los
   usuarios Mobile venden del pool. → J.1/J.2
2. Ciudad: SÍ se usa en Mobile (dato operativo de la venta, cualquier
   ciudad, independiente del CEDIS); pantalla 8 SE CONSERVA. → J.5
3. 9.x: 1 fila = 1 serie (≤20 fracciones, 4 tiras de 5); disponibilidad real.
4. id_tienda: NULL por ahora; backend no resuelve/inventa tienda.
5. Comisión estimada: SÍ, fuente backend, misma lógica Desktop (sub-decisión
   J.4b: % del concentrador vs del vendedor).
6. Billete completo 20/20: SÍ, y parciales, respetando disponibilidad real.
7. Sorteos celebrados: PENDIENTE — no agregar restricción por fecha todavía.
8. Ticket: cliente PÚBLICO EN GENERAL, SIN whatsapp (ni pedirlo), resto del
   backend/venta real.
9. Pantalla 6: comparar productos+subcódigos reales vs mock vs Desktop —
   hecho (§A.8): 7 productos reales cuyos precios/20 = mockup exacto;
   Gordito Navideño no existe en BD (crear producto cuando el negocio lo
   tenga); decisión restante: mantener colores/subcódigo mapeados en la app
   o mover a datos.
10. Impresora: infra = SOLO descubrimiento (verificado §H); extender, no
    duplicar; ESC/POS texto recomendado; spike con impresora física.
11. Consigna vs entrega: mantener 'consigna' (único valor en datos; código
    las trata igual).
12. Vencimiento: PENDIENTE — documentar propuesta: job/proceso que marque
    'vencido' billetes de sorteos celebrados+cerrados (estatus ya existe en
    el enum; hoy 1 fila seed, ningún job); candidato a reutilizar: el flujo
    de cierre de sorteo (post-cierre) o tarea programada; NO implementar.

## DECISIONES AÚN PENDIENTES (no determinables; no se inventan)

P1. Comisión/ISR/fondo del CONCENTRADOR al crearlo (J.1) — valores de negocio.
P2. Fuente del % para la comisión ESTIMADA al usuario (J.4b): concentrador
    vs vendedor de la sesión.
P3. Contenido del catálogo de ciudades (¿las 9 del mockup? ¿otras?).
P4. Restricción de venta de sorteos celebrados (#7, pendiente deliberada).
P5. Proceso de vencimiento (#12, pendiente deliberada).
P6. Pantalla 6: mapeo colores/subcódigo en app vs backend (menor).
P7. "Plaza" del ticket (¿ciudad del CEDIS?) — campo del PDF 5.2.

## PUNTOS DE DETENCIÓN (antes de implementar, per §5)

D1. Migración `ventas.ciudad_venta` (única tabla Desktop) → requiere tu OK.
D2. VentaService toca actionCrear (Desktop) → paridad + regresión completa
    antes de abrir endpoint mobile; si falla, paro y reporto.
D3. Cancelación mobile con semántica al concentrador (distinta de Desktop)
    → aislada en mobile; confirmar que la quieres así (J.7).
D4. Sub-decisión P2 (comisión estimada) → bloquea solo el campo de la
    respuesta de venta, no el resto de la Fase B.
