# NOTAS — Reportes / Estado de Cuenta (mockups 9.x)

Fase: INTEGRACIÓN BACKEND COMPLETA (FASE 4, 2026-09-22). Las 3 pestañas
de Reportes consultan el backend real:

- **Estado de Cuenta (9.1)**: ver más abajo (FASE 4).
- **Fondo de Ahorro (9.2)**: `NOTAS_FONDO_AHORRO.md` (FASE 2 real).
- **Facturación (9.3)**: `NOTAS_FACTURACION.md` (FASE 3 real).

## Qué está implementado

- `Views/ReportesPage.xaml` + `ReportesViewModel`: UNA página con header
  azul "Reportes" y tab bar interno de 3 pestañas. Al entrar siempre abre
  Estado de Cuenta (pestaña 0). Estados: inicial (botón azul "Generar
  Reporte" centrado), generado (resumen + tarjetas de sorteo + totales
  REALES) y descargado (FAB PDF adicional a la izquierda).
- FASE 4 (2026-09-22): "Generar Reporte" consulta GET
  `api/mobile/reportes/estado-cuenta` (sin parámetros — el backend toma
  SIEMPRE el billetero de la sesión). Overlay "Procesando...." mientras
  corre; en error, aviso rojo sobre la pestaña y el botón sigue
  disponible para reintentar (mismo patrón de carga/errores que FA/FAC:
  TaskCanceled → "sin conexión", ApiException → mensaje del backend).
- Resumen de saldo: 6 conceptos reales del bloque `conceptos` del
  endpoint (Fondo de Ahorro, Fideicomiso, Pagarés, Bolsa Electrónica,
  Garantía Total, Capacidad de Crédito).
- Tarjetas de sorteo (mockup 9.1 "Reporte Generado"): BindableLayout
  sobre `SorteosEc` — bloque "Cantidad" + nombre del sorteo + fecha
  larga es-MX, pares Vencido/Consigna y Pagos/Total. **Semántica Fase 1:
  las filas contienen SOLO consignaciones vivas** (ventas solo alimentan
  el concepto Pagarés y al_corriente — incluirlas duplicaría la deuda).
  Estado vacío (billetero sin consignaciones): "Sin registros", sin
  datos inventados.
- Totales: del bloque `totales` del endpoint (el backend es la fuente;
  la app NO recalcula sumas). Total puede ser negativo por sobrepago —
  se muestra tal cual (aprobado ronda 2).
- `al_corriente` se recibe del endpoint pero NO se muestra (decisión
  Fase 1: sin UI nueva; el mockup 9.1 no lo pinta).
- FABs azules 56 px: descarga (derecha, tras generar) y PDF (izquierda,
  tras descargar; abre el visor del sistema con fallback Share).
- PDF: `Services/EstadoDeCuentaPdfService.cs` (SkiaSharp, carta 612×792):
  logo Mi Cachito, título "Estado de Cuenta Informativo", Fecha de
  Emisión REAL del endpoint + Vendedor (nombre_completo) / Plaza
  (nombre_cedis), tabla resumen de 6 conceptos con montos REALES, tabla
  principal de 7 columnas con encabezado rojo #F44336 y filas REALES con
  PAGINACIÓN por fila (encabezado repetido en páginas nuevas), totales
  reales y pie rojo #E53935 institucional.
- Descarga real a Descargas vía MediaStore (`DescargasService`
  reutilizado), nombre `estado-cuenta-<dd-MM-yyyy>.pdf`.
- Fechas en pantalla y nombres de archivo SIEMPRE en español (es-MX)
  vía `Helpers/FormatosFecha.cs`.

## Contrato del endpoint (Fase 1, validado E2E)

GET `api/mobile/reportes/estado-cuenta` (sin parámetros) →

    { billetero: { nombre, cedis }, fecha_emision: "YYYY-MM-DD",
      al_corriente: bool,
      conceptos: { fondo_ahorro, fideicomiso, pagares, bolsa_electronica,
                   garantia_total, capacidad_credito },
      sorteos: [ { sorteo, fecha, cantidad, vencido, consigna, pagos,
                   total } ],
      totales: { vencido, consigna, pagos, total } }

- Filas: SOLO documentos de consignación vivos agrupados por id_sorteo
  (fecha de fila = primer documento del grupo; Cantidad = piezas
  históricamente consignadas; Vencido por FECHA sin descontar pagos;
  Pagos = fuente viva pagos_cartera; Total = Consigna − Pagos).
- FORMATO DEL CAMPO `sorteo` (ajuste 2026-09-22, solo texto):
  "numero_recepcion - nombre_sorteo" (p. ej. "ZOD-0472 - Sorteo Zodiaco
  0472"); el numero es el capturado en RECEPCION de paquetería (fuente
  origen `paquetes_recibidos_detalle.numero_sorteo`, consultando también
  `inventario_cedis`/`billetes_loteria` donde se propaga); sorteos SIN
  numero de recepción (TEC, universitarios, seeds de cartera) muestran
  solo `sorteos.nombre_sorteo`. Varias remesas bajo un mismo sorteo
  listan todos los numeros ("041753, E2EDRAWB - Sorteo 041753").
- Todo acotado al billetero de la sesión; `id_billetero` como parámetro
  se IGNORA (scoping).

## Historial de fases

- Interfaz (2026-09-09): estructura completa en cero sin sorteos.
- FASE 4 (2026-09-22): integración real (esta nota).

## Supuestos declarados (corregibles)

- a. Vendedor/Plaza del PDF = nombre_completo/nombre_cedis del endpoint
  (billetero de la sesión).
- b. FAB descarga visible desde el estado "generado" (spec); el mockup
  "Reporte Generado" no lo muestra.
- c. Pestaña activa: icono+label azul #4125F4; inactivas gris #9E9E9E.
- d. La fecha de emisión del PDF usa la del endpoint (fecha_emision),
  no la hora local del dispositivo.
- e. Tarjetas de sorteo con fondo McPageBg dentro de la tarjeta blanca
  (el mockup 9.1 muestra tarjetas diferenciadas; sin datos de color
  medibles a esa resolución).
