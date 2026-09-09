# Notas — Fondo de Ahorro (pestaña 2 de Reportes, mockups 9.2)

Fase: SOLO INTERFAZ (2026-09-09). Flujo completo local sin backend.

## Qué está implementado

- Campo Periodo ("Indicar Periodo" azul + icono calendario) → modal
  "Seleccionar Fechas" (overlay #707070 + card blanca, campos Desde/Hasta
  con MaterialDatePicker nativo, botón Consultar habilitado solo con ambas
  fechas).
- Tras consultar: periodo real del usuario ("01-octubre-2025 /
  29-octubre-2025"), gráfica en estado SIN datos (cuadrícula tenue +
  línea base azul — sin puntos inventados) y resumen Saldo inicial /
  Depósitos / Retiros / Saldo final en $0.00.
- FAB descarga (derecha) → overlay "Descargando...." → PDF real
  fondo-ahorro-<fin>.pdf en Descargas (MediaStore) → aviso verde
  "Fondo de Ahorro guardado en la carpeta de Descargas" + FAB PDF
  (izquierda) que abre el visor del sistema.
- PDF: logo, título "Reporte de Fondo de Ahorro", subtítulo "Del
  [inicio] al [fin]" con las fechas reales, Titular "—" (estado vacío),
  tabla Fecha|Folio|Origen|Monto con encabezado rojo #F44336 VACÍA,
  barra roja de Total $0.00 y pie institucional #E53935.

## Reglas de esta fase (directrices del usuario)

- Sin movimientos, mocks, seeds ni registros ficticios — la tabla de
  movimientos y la gráfica están VACÍAS (todo $0.00).
- Las fechas de los mockups (01/10/2025, 29/10/2025) son solo
  referencia visual; en la app salen del MaterialDatePicker.
- PDF completo aunque no existan movimientos.
- Desde debe ser ≤ Hasta (Consultar ignora rangos inválidos).

## Pendientes para conectar backend (NO implementar aún)

1. Endpoint de movimientos de Fondo de Ahorro por rango de fechas
   (Fecha, Folio, Origen, Monto) → llenar FondoAhorro.Movimientos.
2. Saldos del periodo (inicial, depósitos, retiros, final) →
   FondoAhorroService.ObtenerAsync(inicio, fin) es el punto único.
3. Titular real (nombre del cliente/agente de la sesión) → campo
   Titular (hoy "—" en el PDF).
4. Serie de la gráfica de rendimiento (evolución del saldo por fecha
   del rango) → sustituir la línea base en ReportesPage.xaml.
5. Validación de rango máximo permitido (si aplica negocio).
6. Paginación/desbordamiento si el periodo trae muchos movimientos
   (el PDF actual es de una página).

## Supuestos de diseño (aprobados en el plan)

- Azul de marca #4125F4 en botones/iconos (mockup mide #3F51B6).
- Gráfica sin card blanco, flat sobre el fondo #F5F5F5.
- Aviso verde persistente tras descargar; FABs sobre él.
- Nombre de archivo con mes español: fondo-ahorro-29-octubre-2025.pdf.
