# Notas — Facturación (pestaña 3 de Reportes, mockups 9.3)

Fase: SOLO INTERFAZ/LOCAL (2026-09-09). Sin backend, API ni BD.

## Qué está implementado

- Campo Periodo ("Indicar Periodo" + calendario) → modal "Seleccionar
  Fechas" COMPARTIDO con Fondo de Ahorro (bifurca por pestaña activa;
  cada pestaña conserva sus propias fechas).
- Tras Consultar: periodo real ("08-septiembre-2026 /
  09-septiembre-2026"), bloque Total facturado + % principal, pie chart
  (GraphicsView + ICanvas nativo, sin paquetes) con % dentro de cada
  segmento, y leyenda (indicador de color + nombre + monto + %).
- FAB descarga → overlay "Descargando...." → PDF real
  facturacion-<fecha-fin>.pdf en Descargas (MediaStore) → aviso verde
  "Facturación guardada en la carpeta de Descargas" + FAB PDF (visor).
- PDF: logo, "Reporte de Facturación", "Del [inicio] al [fin]" real,
  Vendedor "—", una sección por categoría (barra roja con nombre +
  encabezado rojo Fecha|Sorteo|Entrega|Devolución|Venta|Ganancia +
  filas + subtotal dinámico), barra Total general, pie institucional
  con "Fecha de Impresión" (fecha/hora real de generación).

## Datos TEMPORALES de prueba (sustituir por backend)

FacturacionService mantiene en MEMORIA 2 registros para validar el
flujo (directriz del usuario, spec 9.3 §3/§13):

- Mayor — 08/09/2026 — Entrega $1,500.00 / Devolución $264.50 /
  Venta $1,235.50 / Ganancia $92.66
- Superior — 09/09/2026 — Entrega $900.00 / Devolución $135.80 /
  Venta $764.20 / Ganancia $57.32

Estos registros NO son seeds, NO van a BD ni backend y NO son mocks
permanentes: existen solo para comprobar pie/porcentajes/leyenda/
detalle/PDF con más de una categoría y validar el FILTRO REAL por
rango (p. ej. consultando solo 09/09: Mayor desaparece, el pie queda
100% Superior, total $764.20 y el PDF trae una sola sección). Al
conectar el backend se sustituye ObtenerAsync por la consulta real.

## Reglas de esta fase (directrices del usuario)

- Totales, subtotales, porcentajes y proporciones del pie CALCULADOS
  dinámicamente desde los registros filtrados — nunca escritos a mano
  ni copiados de la imagen.
- Los montos de prueba son propios, NO los de la imagen ($10,339.13
  etc. son solo referencia visual).
- "Monto" de categoría/total = columna Venta (la ganancia solo va en
  el PDF); % principal = categoría con mayor venta.
- Colores del pie por categoría (Mayor #FFB600, Superior #4D9D2E,
  Zodiaco #ED40A9, Especial #0278D7) tomados del mockup como ESTILO.
- Desde ≤ Hasta (Consultar ignora rangos inválidos).

## Pendientes para conectar backend (NO implementar aún)

1. Endpoint de facturación por rango: registros (Fecha, Sorteo,
   Entrega, Devolución, Venta, Ganancia) → FacturacionService.
   ObtenerAsync(inicio, fin) es el punto único.
2. Vendedor real (sesión) → campo Vendedor (hoy "—" en el PDF).
3. Catálogo real de categorías/tipos de sorteo y sus colores de
   presentación (el mapa actual cubre Mayor/Superior/Zodiaco/Especial).
4. Regla de negocio de Ganancia (el 7.5% de los datos de prueba es
   solo coherencia interna de los datos temporales).
5. Paginación del PDF cuando el periodo traiga muchas categorías/filas
   (hoy una página).
