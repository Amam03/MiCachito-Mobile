# Notas de Negocio — Sorteos (Gestión, mockups 4, 4.1, 4.2)

> Documento para futura integración con backend. Fase actual: SOLO
> INTERFAZ — el listado sale del catálogo local del calendario LOTENAL
> (`Data/SorteosActivosLotenalData`) y, por decisión del usuario
> (2026-09-08), **todas las cifras se muestran en $0.00** con listas de
> detalle vacías: el flujo visual ya fue verificado y aprobado; los
> cálculos reales llegarán con la integración al backend.

## Qué muestra la pantalla

1. **Estado inicial**: solo el selector "Seleccionar Sorteo ▼"; sin
   información hasta seleccionar.
2. **Selector fijado**: al elegir un sorteo (ej. MAYOR 4024 - fecha),
   queda fijo en la parte superior con su flecha — se puede cambiar de
   sorteo en cualquier momento sin salir de la vista.
3. **Resumen**: Ventas | Pagos realizados | Saldo a pagar.
4. **Entregas y Devoluciones** (colapsable ▼/▲): fila Entregas |
   Devolución | Ventas; expandido: tarjetas por folio con Folio,
   Series, Subtotal, ISR, Comisión, FDA, Total.
5. **Pagos Realizados** (colapsable ▼/▲): Total pagado; expandido:
   tabla Fecha | Folio | Monto ($) | Tipo.

## Reglas que deben venir del backend (pendientes)

1. **Listado de sorteos celebrados**: hoy se filtra el catálogo local
   (fecha <= hoy, orden descendente). En producción: endpoint de
   sorteos con `celebrado=1` por CEDIS/tienda (scopeQueryPorSede),
   ordenado por fecha descendente. Cada fila: nombre del tipo,
   número de sorteo y fecha.
2. **Resumen**:
   - Ventas = total vendido del sorteo en el alcance de la tienda.
   - Pagos realizados = suma de pagos aplicados al sorteo.
   - Saldo a pagar = Ventas − Pagos realizados (así se calcula hoy
     en la UI; confirmar si el backend ya devuelve el saldo o hay
     que derivarlo).
3. **Entregas y Devoluciones**:
   - Entregas (fila) = total de los folios de entrega del sorteo.
   - Devolución = monto devuelto (material no vendido).
   - Ventas = misma cifra del resumen.
   - Tarjetas por folio: campos Folio, Series (rango), Subtotal,
     ISR, Comisión, FDA, Total. En el mockup el Total = Subtotal −
     ISR − Comisión − FDA (la UI sintética respeta esa fórmula);
     confirmar la fórmula real y el significado de FDA.
4. **Pagos Realizados**: tabla Fecha | Folio | Monto ($) | Tipo.
   El tipo de los mockups es "CAJA"; confirmar el catálogo de tipos
   (¿CAJA / DEPÓSITO / TRANSFERENCIA?) y si el folio es el mismo que
   el de los recibos de pago (mockups 8.x).
5. **Coherencia esperada**: Pagos realizados (resumen) = Total pagado
   (sección Pagos) — la UI sintética lo garantiza partiendo el monto;
   el backend debe devolver datos ya cuadrados.

## Decisiones de UI tomadas

- Header morado #4125F4 "Sorteos Celebrados" con back (patrón de las
  pantallas de Gestión).
- Dropdown como overlay centrado con scrim (toca fuera para cerrar),
  filas: nombre+numero en negrita y fecha en gris.
- Flechas ▼/▲ como glifos tipográficos (no emojis, regla de la app).
- Al cambiar de sorteo las secciones colapsables se reinician
  (colapsadas).
