# NOTAS — Devolución (mockups 6.x) — reglas para la futura integración

Fase solo-interfaz: todo el estado vive en memoria (`DevolucionService`,
singleton). Nada se persiste; sin seeds ni mocks. Reglas que el backend
deberá resolver en la integración real:

## Flujo implementado (UI)

1. **Lista de Devoluciones** (6): registros GUARDADOS en memoria + estado
   vacío "No se encontraron Devoluciones" + FAB "+".
2. **Lista de Sorteos** (6.1): sorteos activos desde
   `SorteosActivosLotenalData.ObtenerTodos()` (misma fuente que LOTENAL,
   filtrada por `EstaDisponible`). En la integración real: endpoint de
   sorteos activos del CEDIS/billetero.
3. **Nueva Devolución** (6.2): folio CONSECUTIVO LOCAL desde 1 (decisión
   aprobada; el backend asignará el folio real).
4. **Escaneo** (6.2 escaneo): componente `CameraScannerView` reutilizado de
   Premios y Reintegros + entrada manual. Cada código se registra en el
   MODO ACTIVO (Series/Tiras/Cachitos).

## Equivalencias (regla de negocio confirmada en el spec)

- Cachito = 1
- Tira = 5
- Serie = 20

Un mismo código escaneado aporta su equivalencia según el modo activo al
momento de la captura. La "Cantidad" de la fila del desglose es esa
equivalencia (1/5/20).

## Validaciones implementadas (UI) — el backend debe re-validar

- Código parseado con `BilleteParser` (34/35 dígitos, Lotenal/Zodiaco).
  En la integración: validar contra el inventario real del CEDIS.
- Dedupe por código completo dentro de la devolución en curso.
- **Fecha del cachito vs sorteo seleccionado**: si la fecha del código
  (`BilleteParser.FechaSorteo`, formato "yyyy-M-d") difiere de la fecha de
  celebración del sorteo elegido, se rechaza con el banner del mockup:
  "La fecha del cachito no corresponde con la fecha del sorteo
  seleccionado". En la integración: esta validación debe venir del
  backend con la fecha oficial del sorteo.

## Pendientes de negocio (documentar con el equipo)

- ¿La devolución debe descontar del inventario del CEDIS al GUARDAR?
  (Regla de dominio: boletos devueltos se mantienen aparte, se reúnen
  para devolución, solo vuelven a disponible vía cancelación válida.)
- ¿Se permite mezclar modos (p. ej. 2 series + 3 tiras) en una misma
  devolución? La UI lo permite; confirmar regla.
- ¿Tiene tope máximo por sorteo (inventario disponible)?
- ¿El folio real es asignado por el backend al GUARDAR (como el "Folio de
  Compra" de Sorteos Tec)?
- Estado inicial de una devolución guardada (pendiente de autorización,
  aplicada, etc.) — la lista actual no muestra estado.
- GUARDAR con 0 capturas está permitido en UI (decisión aprobada);
  el backend debe decidir si lo rechaza.
