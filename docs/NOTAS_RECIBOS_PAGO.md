# Notas de negocio — Recibos de Pago (mockups 8.x, pestaña Gestión)

Fase: SOLO INTERFAZ. Estado en memoria (`Services/RecibosPagoService.cs`)
con UN registro temporal (folio 41868) únicamente para validar el flujo
— sustituir por datos reales del backend. Los datos de los mockups y
del PDF de referencia son referencia visual, no información real.

## Flujo implementado

1. **Lista "Recibos de Pago"** (mockup 8): header morado #4125F4 con
   back, saldo $0.00 y accesos rápidos (patrón Gestión); cards blancos
   con Folio (bold), Fecha (dd-MMMM-yyyy), Total y check verde
   #4CB050 de confirmación a la derecha; orden cronológico inverso.
2. **Detalle de Pago** (mockup 8.1): header azul con Folio/Fecha/Total;
   desglose por fila: Descripción (bold), Referencia (gris) y Monto con
   signo — los negativos se muestran con "-" (devoluciones/notas:
   $ -3,656.50, $ -623.50). FAB azul de descarga abajo a la derecha.
3. **Descarga** (mockup 8.2): overlay oscuro con spinner y
   "Descargando..." (~2 s simulados); el PDF se genera de verdad con
   SkiaSharp; se copia a la carpeta pública Descargas vía MediaStore
   (API 29+, sin permisos); aviso verde #4CB050 "Archivo guardado en la
   carpeta de Descargas" con acción "Abrir" (visor del sistema, fallback
   diálogo de compartir).
4. **Comprobante PDF** (mockup 8.3): carta 612×792, logo Mi Cachito,
   LOTERÍA NACIONAL / para la Asistencia Pública / Agencia Expendedora
   de Primera en Tuxtla Gutiérrez, Chiapas (verbatim del PDF de
   referencia), RECIBO DE CAJA FECHA yyyy-MM-dd FOLIO n (CEDIS ...),
   Cliente, tabla "Documentos Pagados" con encabezado rojo #F44336,
   Total Documentos, tabla "Formas de Pago" con encabezado rojo
   (Efectivo, Depósitos/Transferencias, Premios, Reintegros, Lotería
   Instántanea, Cheques, Notas de Crédito), Total, y pie con fondo rojo
   #E53935: razón social, dirección y teléfono (verbatim).
   Nombre de archivo: `pago-{folio}-{dd-MM-yyyy}.pdf` (mockup 8.3).

## Pendiente de conectar a backend

- **Fuente de recibos**: lista real de recibos de caja por CEDIS/tienda
  (endpoint pendiente; el desktop usa `recibos_pago`/cierre de caja).
  El folio y las fechas del registro temporal son del mockup.
- **Desglose**: cada recibo trae movimientos con descripción,
  referencia y monto con signo. En el mockup: Lotería Nacional
  (+1420), Efectivo (+2860), FACTURA LN (−3656.50 y −623.50, pagos a
  proveedor con folio de factura). El backend debe devolver el desglose
  por documento — la referencia "SUPERIOR 2862 - FOLIO 27373" es el
  folio LN de la factura.
- **Cálculo de formas de pago** (tabla 2 del PDF): hoy Efectivo = suma
  de filas "Efectivo" del desglose y el resto $0.00 (como el PDF de
  referencia). El backend debe calcular cada categoría real
  (Depósitos/Transferencias, Premios, Reintegros, Lotería Instántanea,
  Cheques, Notas de Crédito) por recibo.
- **Documentos Pagados** (tabla 1): hoy = filas FACTURA del desglose
  en valor absoluto (3656.50 + 623.50 = 4,280 = Total Documentos).
  El backend debe devolver la lista real de documentos/facturas
  pagadas con sus importes.
- **Datos de agencia/pie**: "Agencia Expendedora de Primera en Tuxtla
  Gutiérrez, Chiapas", razón social, dirección y teléfono están
  HARDCODEADOS verbatim del PDF de referencia — deben venir de la
  configuración de la agencia/CEDIS en backend.
- **Check verde de confirmación**: hoy constante en la lista; el
  backend debe indicar el estatus de cada recibo (confirmado/validado).
- **Carpeta de descargas**: guardado vía MediaStore en Download
  (Android 10+). En integración real, el PDF podría generarse en el
  backend y descargarse; el flujo UI ya está listo para ambos casos.
