# Notas de Negocio — Tickets de Venta (Gestión, mockups 5, 5.1, 5.2)

> **INTEGRADO AL BACKEND (2026-10-13).** Historial y detalle consultan
> las VENTAS REALES del billetero de la sesión: GET `api/mobile/ventas`
> (todas, cualquier estatus, fecha descendente) y GET
> `api/mobile/ventas/{id}` (detalle con partidas; 404 si la venta es
> ajena). Los 3 movimientos TEMPORALES en memoria fueron ELIMINADOS.
> Decisiones D1–D5 cerradas abajo. TAE no aparece (0 filas en
> `ventas_electronicas`; módulo en espera de TAECEL — el filtro
> "Tiempo Aire" muestra la lista vacía).

## Decisiones D1–D5 (cerradas 2026-10-13)

1. **D1 Plaza**: NO existe ciudad del CEDIS en la BD → el PDF muestra
   SOLO el CEDIS emisor real (`cedis.nombre_cedis` vía
   `billeteros.id_cedis`). Sin datos inventados.
2. **D2 Multi-partida**: el detalle une los boletos por coma
   ("0101, 0102"); el PDF dibuja UNA FILA POR PARTIDA en la tabla de
   desglose. Partidas legacy sin `id_billete` → boleto "—".
3. **D3 Estatus**: el historial trae TODAS las ventas (D5) y la fila
   marca discretamente CANCELADA/DEVUELTA en gris junto al folio.
4. **D4 Folio de compra Tec**: NO existe ni en BD ni en backend
   (grep = 0) → línea y leyenda de certificados OMITIDAS del PDF.
   Cliente: nombre capturado en DatosClienteTecPage o
   "PÚBLICO EN GENERAL" (nunca inventado).
5. **D5 Historial completo**: TODAS las ventas del billetero,
   cualquier estatus (activa/pagada/cancelada/devuelta).

Contrato (backend `dev`, `controllers/api/mobile/VentasController.php`
`actionIndex`/`actionDetalle` + rutas en `config/web.php`):
- Fila: `id_venta, folio, tipo ('lotenal'|'sorteos_tec'), estatus,
  fecha_venta, total, id_sorteo, descripcion, cliente{nombre,
  telefono, correo}|null`. Navegación por `id_venta` (NO por folio:
  es display, "1103" o legacy "V-20260831-67707").
- Detalle: encabezado + `vendedor` (billetero de la sesión), `cedis`,
  `partidas[{numero_billete, serie, signo, cantidad, precio_unitario,
  subtotal, numero_sorteo, nombre_sorteo}]`.
- App: `TicketsVentaService` (IApiClient) + `MovimientoVenta` con
  `DesdeHistorial`/`DesdeDetalle`; `MovimientoVenta.Folio` pasó de
  int a string. E2E: `/home/sistemas/e2e_mobile_ventas_historial.py`
  (40 checks, TODO OK 2026-10-13).

## Qué muestra el flujo

1. **Lista "Ventas" (mockup 5)**: header morado "Ventas" + icono
   filtro; filas con chip de color por tipo (LOTENAL #FF0080,
   Sorteos Tec #1A3688, Tiempo Aire #2879FE), descripción, cliente,
   "Folio: N fecha" e importe. Filtro por tipo en overlay.
2. **Detalle de Venta (mockup 5.1)**: tarjeta cliente + importe
   grande + sección "Detalles" con campos según tipo:
   - LOTENAL / Sorteos Tec: Cliente, Producto/Sorteo, Boleto, Valor,
     Fecha, Folio, Importe total.
   - Tiempo Aire: Compañía, Teléfono, Monto/Paquete, Fecha, Folio,
     Importe total.
   - Botones circulares: **Imprimir** (Bluetooth) y **Compartir**
     (PDF + diálogo nativo).
3. **PDF "Ticket de Venta" (mockup 5.2)**: generado con SkiaSharp
   (página carta 612×792pt) replicando el PDF de referencia: logo,
   título, Plaza | CEDIS, vendedor, Fecha | Folio, Datos del Cliente,
   tabla desglose (Sorteo|Boleto|Precio o Compañía|Teléfono|Precio),
   Total, Folio de Compra de Sorteos Tec + leyenda de certificados
   (solo Tec), GRACIAS POR SU COMPRA, Fecha y Hora del Movimiento y
   whatsapp de soporte. Archivo: `ticket-{st|lot|ta}-{folio}.pdf` en
   AppDataDirectory.

## Reglas que deben venir del backend (pendientes)

1. **Fuente real de las ventas**: hoy los 3 movimientos son
   temporales. En producción, cada módulo de Vender debe registrar la
   venta al confirmarla (endpoint de ventas con tipo, folio, partidas
   y datos del cliente) y esta pantalla consulta ese registro.
2. **Folio de la venta**: los mockups usan folios sueltos (5369, 5569,
   961). Definir si es el mismo folio de la tabla `ventas` del
   backend o uno por tipo de operación.
3. **Plaza / CEDIS / Vendedor**: hoy verbatim del mockup (PUEBLA |
   CEDIS PUEBLA, MAURICIO LOPEZ GONZALEZ). En producción salen de la
   sesión del usuario (`SessionInfo.Usuario.Cedis.NombreCedis`,
   tienda) — el campo "Plaza" aparte de "CEDIS" debe confirmarse con
   el backend (posiblemente ciudad del CEDIS o catálogo de plazas).
4. **Datos del cliente**: solo el flujo Tec captura cliente (nombre,
   teléfono, correo). LOTENAL y Tiempo Aire NO capturan cliente hoy —
   confirmar si la venta externa guardará "PÚBLICO EN GENERAL" o el
   teléfono recargado como identificador.
5. **Folio de Compra de Sorteos Tec** (7448622 en el mockup): número
   devuelto por Sorteos Tec al confirmar la compra; pedirlo a su API
   en la integración.
6. **Impresión Bluetooth**: el botón valida permiso
   BLUETOOTH_CONNECT y avisó si no hay impresora vinculada. Falta la
   impresión real: conexión SPP (`00001101-0000-1000-8000-00805F9B34FB`),
   selección de impresora vinculada (¿filtrar por nombre?, ¿guardar
   la última usada?) y formato del ticket (¿ESC/POS de texto directo
   o render del PDF?). Decidir con el modelo de impresora real.
7. **Compartir**: `ShareFileRequest` nativo ya funciona; el nombre
   del archivo usa el patrón del mockup (`ticket-st-5369`).
8. **WhatsApp de soporte** (961 103 5921): verbatim del PDF de
   referencia; confirmar si es el número final o viene de
   configuración del backend.
9. **Fechas**: la venta Tec del mockup es 01/09/2023 (fecha vieja) —
   en producción la fecha del movimiento es la real del día.

## Decisiones de UI tomadas

- La lista NO tiene botón back (pestaña Gestión → es una pantalla
  hija con back en el detalle, patrón de Sorteos/Premios).
  NOTA: mockup 5 no muestra back; se dejó header simple con título +
  filtro. El detalle SÍ tiene back.
- Chips de color por tipo usando los colores de marca existentes
  (McLotenal/McSorteosTec/McTiempoAire).
- Filtro como overlay centrado con scrim (patrón pantalla 8), opción
  "Todos" + 3 tipos, check verde en la activa.
- Botones Imprimir/Compartir circulares morados con SVG blanco
  (Material 24×24, misma convención de iconos de la app).
- Logo del PDF extraído VERBATIM del PDF de referencia (imagen
  378×309 con alpha) como `logo_ticket_pdf.png`.
