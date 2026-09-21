# Notas de negocio — Recibos de Pago (mockups 8.x, pestaña Gestión)

Fase: INTEGRADA AL BACKEND, formato mockup 8.1 restituido, datos por el
flujo real de Caja, crédito retirado y limpieza de datos ejecutada
(2026-09-21 pm). Fuente real: `fichas_pago` con estatus 'aplicado' — las
aplica Caja desde Escritorio en **Usuario Caja → Punto de Cobro → B.
Recepción de Pagos** — vía GET `/api/mobile/pagos/*` (controller mobile
dedicado, solo lectura).

## De dónde vienen los pagos (regla de negocio)

Caja registra el cobro en el diálogo **AplicarPagosDialogNuevo**
(Escritorio): montos por forma de pago + documentos de cartera a
pagar. `pagos_service.guardar_movimiento` crea **una ficha por forma
de pago con monto > 0** (POST `api/caja/fichas/crear`) con
observaciones JSON compartidas: `{folio_grupo (UUID), desglose{...},
cheques_detalle, depositos_transferencias_detalle,
loteria_nacional_detalle, loteria_instantanea_detalle,
notas_credito_detalle, sobrante, sobrante_motivo,
documentos_seleccionados[{tab, id_cartera, monto}], numero_turno}`.
Al aplicar (POST `api/caja/fichas/aplicar-pago` →
`CajaController::actionAplicarPago`): estatus='aplicado' +
`caja_movimientos` (cobro_billetero) + `pagos_cartera` por cada
documento seleccionado.

Un **"recibo"** para Mobile es el MOVIMIENTO aplicado (grupo
folio_grupo), no la ficha individual: la lista agrupa (mockup 8) y el
detalle (mockup 8.1) muestra el desglose completo del movimiento.

## Crédito: retirado de la pantalla

El bloque "Crédito utilizado / Crédito disponible" se RETIRÓ de la
lista y del detalle (2026-09-21 pm). La referencia visual 8.x no lo
muestra — verificado por OCR posicional contra las 5 imágenes
(`8. Recibos de pagos.png`, `8.1 …Detalle de pago….png`, `8.2
…Descargar.png`, `8.2 …Descargando.png`, `8.3 …Comprobante.png/pdf`):
la lista muestra solo cards Folio/Fecha/Total; el detalle, header +
filas del desglose (región inferior vacía); el comprobante, las dos
tablas + pie. Se había integrado porque la lista de requisitos del
usuario ("Qué debe mostrar") lo incluía; el usuario lo confirmó como
dato no solicitado. El endpoint `GET /api/mobile/pagos/credito` se
retiró del controller y de las rutas; `CreditoApi` y
`ObtenerCreditoAsync` se eliminaron de Mobile. El cálculo oficial
sigue disponible en `ReportesController::actionEstadoCuentaCredito`.

## Flujo implementado

1. **Lista "Recibos de Pago"** (mockup 8): una card por MOVIMIENTO
   aplicado con Folio (de la ficha representativa), Fecha
   (aplicación; si no, fecha de pago — dd-MMMM-yyyy es-MX), Total =
   suma de TODAS las formas de pago del grupo, y check verde;
   cronológico inverso. Regla: un grupo se lista SOLO si TODAS sus
   fichas están aplicadas (movimiento con fichas pendientes no es
   recibo). Sin bloque de crédito (ver arriba).
2. **Detalle de Pago** (mockup 8.1, formato restituido): header
   morado con Folio/Fecha/Total del movimiento; desglose con filas de
   TRES líneas con labels — `Descripción: X` / `Referencia: Y` /
   `Monto: $ Z` (monto abajo-derecha; rojo para documentos). Filas
   positivas = formas de pago del movimiento (Lotería Nacional,
   Efectivo, Transferencia con su referencia, Cheques, Depósitos,
   Notas de Crédito); filas negativas = `FACTURA LN` con referencia
   `NUMSORTEO --> FOLIO folio_documento` (ej. `MAYOR-2025 --> FOLIO
   CONS-20260225-001`). Fichas sin desglose degradan a forma de
   pago simple (tipo + monto). Sin bloque de crédito (ver arriba).
3. **Descarga** (mockup 8.2): overlay "Descargando..." durante la
   generación real; PDF copiado a Descargas vía MediaStore (API 29+,
   sin permisos); aviso verde #4CB050 con acción "Abrir" (visor del
   sistema, fallback diálogo de compartir).
4. **Comprobante PDF** (mockup 8.3, verificado contra la imagen): carta
   612×792, logo Mi Cachito, leyendas verbatim del PDF de referencia,
   RECIBO DE CAJA FECHA FOLIO (CEDIS ...) — `nombre_cedis` YA trae el
   prefijo "CEDIS " desde la BD, no se antepone de nuevo (auditoría
   2026-09-21). Tabla "Documentos Pagados" = documentos del desglose
   (FACTURA LN + referencia); tabla "Formas de Pago" = **7 filas
   fijas de la referencia** (Efectivo, Depósitos/Transferencias,
   Premios, Reintegros, Lotería Instántanea, Cheques, Notas de
   Crédito — no hay fila "Lotería Nacional"): el
   `loteria_nacional_total` se suma a "Depósitos/Transferencias"
   porque así lo registra Escritorio (la ficha de LN se crea con
   tipo_pago 'deposito') y así el Total cuadra. Total = total del
   MOVIMIENTO. Pie verificado contra el PDF de referencia (razón
   social/dirección/teléfono coinciden exacto — sigue hardcodeado
   porque no existe tabla de datos fiscales de la empresa).
5. **API backend** (`controllers/api/mobile/PagosController.php`):
   - `GET /api/mobile/pagos/recibos` — movimientos aplicados
     agrupados por folio_grupo.
   - `GET /api/mobile/pagos/recibo/{id}` — ficha + `formas` (filas
     positivas), `documentos` (con `referencia` formateada e
     `id_cartera` real), `desglose` crudo, `*_detalle`,
     `documentos_seleccionados` y `total` del movimiento.
   - Scoping: TODO acotado a `getIdBilleteroActual()`; los
     documentos se filtran a la cartera del PROPIO billetero (un
     `documentos_seleccionados` que apunte a cartera ajena se
     ignora).

## Fix en el flujo de Caja (Backend Desktop, 2026-09-21 pm)

`CajaController::actionAplicarPago` duplicaba `pagos_cartera` en
movimientos compuestos: Escritorio crea una ficha por forma de pago
con observaciones COMPARTIDAS (mismo folio_grupo y
documentos_seleccionados) y las aplica una a la vez → cada ficha del
grupo volvía a crear el pago de cada documento. Se agregó guardia
`pagoCarteraYaRegistrado(idCartera, folioGrupo, idBilletero)`: si ya
existe un pagos_cartera de ese documento en una ficha aplicada del
mismo movimiento, se omite. Fichas sueltas (sin folio_grupo) NO se
deduplican (pagos independientes legítimos). **Verificado con flujo
real**: movimiento de 2 fichas → 1 pagos_cartera por documento
(antes 2). Los duplicados históricos de la BD (pagos_cartera 6/7 de
la ficha 55) se eliminaron en la limpieza.

## Infraestructura para recepción de mercancía (pendiente)

El flujo de recepción de mercancía NO existe todavía. NO se inventó
ninguna relación: los documentos del desglose viajan con sus
identificadores reales (`id_cartera`, `folio_documento`,
`tipo_documento`, `id_sorteo`) para que el flujo futuro pueda
relacionarse con los pagos. La vinculación existente es
`fichas_pago → pagos_cartera → cartera_billeteros` (la que crea
`actionAplicarPago` al aplicar la ficha).

## Datos de prueba (billetero 1, usuario mobile_dev)

Creados por el FLUJO REAL (`seed_pagos_flujo_real.py`, login usuario
`caja` → crear fichas → aplicar):
- **Movimiento 1** (espejo del mockup 8.1): Lotería Nacional $1,420
  + Efectivo $2,860 = **$4,280**, pagando CONS-20260225-001
  $3,656.50 y CONS-20260226-003 $623.50 (fichas 54/55, grupo 2f5e…).
- **Movimiento 2**: Transferencia $5,000 (ref TRANSF-20260921-001)
  pagando $5,000 de CONS-20260225-001 (ficha 56, grupo e590…).

Limpieza ejecutada (2026-09-21 pm, autorizada): fichas 44-47 (grupo
'test-uuid-1234', sembradas a mano con movimiento incompleto),
CC-TEST 48/49/51/53 del billetero 1 (sembradas por la migración
m260525_000001 con documentos_seleccionados apuntando a cartera de
otros billeteros), sus caja_movimientos 53/54 y los pagos_cartera
duplicados 6/7. Quedan del billetero 1: semillas oficiales de
migración 1/9 (aplicadas, listan) y 7/18 (pendientes, no listan) +
los 2 movimientos reales. La lista queda en **4 recibos**.

## Pendientes conocidos

- Pie de página del PDF hardcodeado (razón social/dirección/tel):
  coincide exacto con el PDF de referencia (verificado por OCR);
  no existe tabla de datos fiscales de la empresa en el backend.
- Aplicar un pago NO descuenta `saldo_pendiente` en
  `cartera_billeteros`: gap de sistema completo (ningún código en el
  backend escribe esa columna; los reportes oficiales la leen).
  Documentado, no corregido aquí (no inventar reglas de negocio).
- Sin paginación en `actionRecibos` (nota de escala).
- Integración con recepción de mercancía (ver arriba).
