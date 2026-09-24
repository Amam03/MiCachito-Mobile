# Notas — Fondo de Ahorro (pestaña 2 de Reportes, mockups 9.2)

Fase: INTEGRACIÓN CON BACKEND — Fase 2 (2026-09-22). La pantalla consulta
el endpoint real; la fase solo-interfaz quedó atrás.

## Qué está implementado

- Campo Periodo ("Indicar Periodo" azul + icono calendario) → modal
  "Seleccionar Fechas" (overlay + card blanca, campos Desde/Hasta con
  MaterialDatePicker nativo, botón Consultar habilitado solo con ambas
  fechas).
- Tras consultar (FASE 2): overlay "Procesando...." mientras corre el
  GET `api/mobile/reportes/fondo-ahorro?fecha_inicio&fecha_fin`
  (FondoAhorroService vía IApiClient; acotado al billetero de la sesión,
  sin ids del cliente); periodo real del usuario en 2 líneas azules;
  gráfica de rendimiento con la SERIE REAL de saldo acumulado (línea
  #3A4DBC + área #CACEE7 + cuadrícula #E3E3E3 — LineaSaldoDrawable);
  resumen Saldo inicial / Depósitos / Retiros / Saldo final del backend.
- Manejo de errores (patrón DetallePago): modal NO se cierra en error;
  aviso rojo McError con el mensaje (sin conexión / mensaje del backend);
  reintentable con el mismo botón Consultar.
- Respuesta vacía: movimientos [] → gráfica en estado sin datos (solo
  cuadrícula) + saldos del resumen tal cual (0/valores reales).
- FAB descarga (derecha) → overlay "Descargando...." → PDF real
  fondo-ahorro-<fin con mes español>.pdf en Descargas (MediaStore) →
  aviso verde + FAB PDF (izquierda) que abre el visor del sistema.
- PDF: logo, título, subtítulo "Del [inicio] al [fin]" real, TITULAR
  real del backend, tabla de movimientos (Fecha | Folio | Origen |
  Descripción | Monto — 5 columnas, monto con signo, descripción con
  elipsis si excede) y barra roja de Total = SALDO FINAL del periodo
  (mockup 9.2: Total $15,911 == Saldo final).

## Reglas de esta fase

- Serie de la gráfica = saldo acumulado por fecha (punto inicial =
  saldo_inicial; aportación sube, retiro baja) — interpretación
  declarada y aprobada en el plan de Reportes.
- Montos de movimientos llegan POSITIVOS del backend; el signo lo
  deriva la app del origen (aportacion | retiro) en FondoAhorroService.
- Sin mocks ni seeds: los valores visibles salen del endpoint o son 0.
- PDF completo aunque no existan movimientos (solo encabezado + Total).
- Desde debe ser ≤ Hasta (Consultar ignora rangos inválidos; el backend
  además responde 400).

## Pendientes (NO implementar sin pedir)

1. Paginación/desbordamiento si el periodo trae muchos movimientos
   (el PDF actual es de una página).
2. Validación de rango máximo permitido (si aplica negocio).

## Supuestos de diseño (aprobados)

- Azul de marca #4125F4 en botones/iconos (mockup mide #3F51B6 casi igual).
- Gráfica sin card blanco, flat sobre el fondo #F5F5F5.
- Aviso verde persistente tras descargar; FABs sobre él.
- Nombre de archivo con mes español: fondo-ahorro-29-octubre-2025.pdf.
- La pantalla NO muestra lista de movimientos (mockup 9.2 detalle
  verificado por OCR: hueco entre resumen y FABs vacío) — los
  movimientos viven en la tabla del PDF.
