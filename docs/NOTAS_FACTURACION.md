# Notas — Facturación (pestaña 3 de Reportes, mockups 9.3)

Fase: INTEGRACIÓN CON BACKEND (FASE 3, 2026-09-22). Endpoint real:
GET `api/mobile/reportes/facturacion?fecha_inicio&fecha_fin` (contrato
Fase 1 backend, mockup 9.3 en DINERO).

## Qué está implementado

- Campo Periodo ("Indicar Periodo" + calendario) → modal "Seleccionar
  Fechas" COMPARTIDO con Fondo de Ahorro (bifurca por pestaña activa;
  cada pestaña conserva sus propias fechas).
- Tras Consultar: periodo real, bloque Total facturado + % principal,
  pie chart (GraphicsView + ICanvas) con % dentro de cada segmento, y
  leyenda (BindableLayout) — AGRUPACIÓN POR CATEGORÍA (producto real:
  Mayor/Zodiaco/Mi Sueño...), monto = columna Venta, % calculado.
- Overlay "Procesando...." durante la consulta (ConsultandoFac) y
  manejo de errores igual que Fondo de Ahorro: TaskCanceled/
  HttpRequest → "sin conexión"; ApiException → mensaje del backend;
  el modal NO cierra en error (aviso rojo reintentable, bifurcado por
  pestaña: ModalHayError/ModalMensajeError).
- Respuesta vacía (rango sin registros): consulta exitosa, total
  $0.00, pie sin segmentos (GraphicsView vacío) y leyenda vacía —
  sin inventar datos.
- FAB descarga → overlay "Descargando...." → PDF real
  facturacion-<fecha-fin>.pdf en Descargas (MediaStore) → aviso verde
  + FAB PDF (visor).
- PDF: logo, "Reporte de Facturación", "Del [inicio] al [fin]" real,
  Vendedor REAL del endpoint + línea de Comisión (comision_pct), una
  sección por categoría (barra roja nombre + encabezado rojo
  Fecha|Sorteo|Entrega|Devolución|Venta|Ganancia + filas + subtotal
  dinámico), barra Total general, pie institucional con "Fecha de
  Impresión" — con PAGINACIÓN real (secciones largas saltan a página
  nueva por sección y por fila; el pie institucional va solo en la
  última página).

## Datos reales (endpoints Fase 1, billetero de la sesión)

- `vendedor`: nombre_completo del billetero (PDF); la pantalla 9.3 NO
  muestra vendedor (solo el PDF), igual que FA con su titular.
- `comision_pct`: porcentaje del billetero (solo informativo en el
  PDF; la ganancia YA viene calculada del backend — la app NO
  recalcula lógica de negocio).
- `registros`: una fila por (fecha, sorteo) en dinero — SORTEO es la
  ETIQUETA de la fila (formato "numero_recepcion - nombre_sorteo", p.
  ej. "041753 - Sorteo 041753"; TEC sin numero → solo el nombre) y
  CATEGORÍA el
  PRODUCTO que agrupa pie/leyenda/secciones del PDF (el mock de fase
  interfaz agrupaba por Sorteo; el contrato real los separa).
- Registros sin fecha se descartan (no se inventa fila); categoría
  vacía → "N/A" (convención del propio backend).

## Reglas de esta fase (directrices del usuario)

- Totales, subtotales, porcentajes y proporciones del pie CALCULADOS
  dinámicamente desde los registros — nunca escritos a mano.
- "Monto" de categoría/total = columna Venta; % principal = categoría
  con mayor venta.
- Colores del pie por categoría (Mayor #FFB600, Superior #4D9D2E,
  Zodiaco #ED40A9, Especial #0278D7) del mockup como ESTILO; categorías
  fuera del mapa (p. ej. "Mi Sueño") usan el gris de fallback
  preexistente — no se inventan colores.
- Desde ≤ Hasta (Consultar ignora rangos inválidos).

## Pendientes

- Catálogo real de colores por categoría si el usuario define más
  (hoy: 4 del mockup + fallback gris).
- F5 (pruebas y regresión) al cierre de las fases.
