# Módulo Expendios — Notas de interfaz y reglas (mockups 1-6)

Fase: **SOLO INTERFAZ** (2026-09-11). Sin backend, sin endpoints, sin BD —
todo dato vive en `Data/ExpendiosDemoData.cs` (en memoria, se pierde al
reiniciar la app, sustituible por el servicio real sin tocar Views/VMs).

Archivos del módulo:
- `Views/ExpendiosPage.xaml(.cs)` — pantalla principal (2 pestañas + modal)
- `Views/ExpendioFormPage.xaml(.cs)` — Actualizar/Crear (misma estructura)
- `ViewModels/ExpendiosViewModel.cs` — pestañas, modal, consulta, edición
- `ViewModels/ExpendioFormViewModel.cs` — formulario compartido
- `Models/Entities/Expendio.cs` (+ `PermisosVenta`)
- `Data/ExpendiosDemoData.cs` — datos demo

## 1. Qué representa cada pestaña

- **Pestaña 1 (icono lista)**: Consulta de Histórico / Registros. Periodo
  actual (`FechaDesde - FechaHasta`, por defecto hoy–hoy, NO fijo) con dos
  acciones: calendario (abre el modal) e impresora (pendiente, ver §8/§9).
- **Pestaña 2 (icono tienda)**: Administración de Expendios. Tarjetas
  dinámicas (1 o N) con icono, Alias, titular, domicilio y botón editar
  (lápiz) + FAB **+** para crear.

Ambas pestañas viven en `ExpendiosPage` (cambio instantáneo por
`PestanaActiva`, el estado no se rompe al cambiar). El encabezado replica el
de Gestión: banda morada `McLotenalHeader` con `Saldo: $0.00` (saldo verde).

## 2. Consulta por periodo

- Periodo por defecto: fecha actual – fecha actual.
- Formato del texto del periodo: `yyyy-MM-dd - yyyy-MM-dd` (formato de los
  mockups 1/3).
- Validación al Consultar: Desde ≤ Hasta; si no, alerta y no consulta.
- Tras validar: overlay `Procesando...` (~1.5 s fase UI) y resultado.
- Los campos Desde/Hasta del modal abren el selector de fecha nativo
  (Material `MaterialDatePicker`, `DialogoFechaHoraService.PickFechaAsync`
  — mismo componente que Depósitos, NO un selector nuevo).

## 3. Selección de uno o varios expendios

El modal "Seleccionar Fechas" (mockup 2) lista TODOS los expendios del
usuario, cada uno con checkbox (`SeleccionadoConsulta` en la entidad). El
botón Consultar solo se habilita con ≥1 marcado (`PuedeConsultar`). La
consulta puede ser de un solo expendio o de varios — la estructura queda
preparada (`ConsultarAsync` ya recopila `Expendios.Where(SeleccionadoConsulta)`).

## 4. Relación usuario ↔ expendios (1 a N)

Un usuario puede tener **uno o varios** expendios. NO asumir que un
expendio equivale a un usuario. La lista de tarjetas y la del modal son
dinámicas según la relación del usuario que entra. Demo actual: UN expendio
(Centro, usuario `billetero`); las altas de prueba se hacen desde Crear
Expendio en memoria.

## 5. Mismo usuario vs nuevo usuario (escenarios A/B)

- **Escenario A — mismo usuario**: crear un expendio con el MISMO nombre de
  usuario NO crea otro usuario; el usuario queda asociado a 2+ expendios
  (p. ej. juan → Centro y Norte). Al entrar a Expendios ve todos los suyos
  y la consulta permite marcar uno o varios.
- **Escenario B — otro usuario**: crear un expendio con un nombre de usuario
  DIFERENTE (p. ej. maria) considera un nuevo usuario: el expendio queda
  relacionado con el usuario creador (quien lo administra) y maria podrá
  iniciar sesión con sus credenciales viendo SOLO los expendios/permisos de
  su relación.

Regla para el backend (PENDIENTE de confirmación, no inventada): la
distinción A/B se decide por el nombre de usuario capturado en Crear
Expendio. La UI ya recolecta Usuario+Contraseña por expendio para ambos
casos.

## 6. Permisos de Venta (sección del formulario) — ✅ IMPLEMENTADO (2026-09-16)

Los checkboxes Sorteos Tec / Tiempo Aire / Lotenal se persisten por **vía real**: `PUT /api/mobile/expendios/<id>/permisos` escribe `billeteros.tiene_prod_digitales` / `tiene_tiempo_aire` / `tiene_lotenal` (permisos por BILLETERO, compartidos por todo el conjunto de expendios). El formulario funciona en modo solo-permisos: identidad del expendio en solo-lectura (la administra Desktop), sin FAB Crear. `GET /api/mobile/expendios` alimenta las tarjetas de Administración y refresca el `Billetero` de la sesión; tras guardar, `SessionService.UpdateBilleteroAsync` actualiza la sesión y Vender refleja el cambio al volver (OnAppearing → CargarPermisosVenta).

Dentro de Actualizar/Crear Expendio, sección **Permisos de Venta** con tres
checkboxes: **Sorteos Tec** (icono dado), **Tiempo Aire** (icono chip/SIM),
**Lotenal** (icono boleto). Los iconos usan el MISMO color que su botón en
Vender (McSorteosTec #1A3688, McTiempoAire #2879FE, McLotenal #FF0080 —
recursos existentes, no colores nuevos). Los tres togglean la propiedad
correspondiente de `PermisosVenta` del expendio en edición.

## 7. Relación Permisos de Venta ↔ pestaña Vender (REGLA CLAVE — IMPLEMENTADA)

**La fuente de verdad de qué productos se venden es el expendio** (`PermisosVenta`),
NO la pestaña Vender. Implementación (2026-09-11):

- `HomeViewModel.CargarPermisosVenta()` lee los permisos del expendio (fase
  UI: `ExpendiosDemoData`; con backend: expendio activo de la sesión) y fija
  `MuestraSorteosTec` / `MuestraTiempoAire` / `MuestraLotenal`.
- `HomePage.xaml` enlaza `IsVisible` de cada botón a su flag — Vender NO
  duplica la configuración, SOLO la consulta.
- Regla aplicada: solo Sorteos Tec → solo ese botón; solo Tiempo Aire → solo
  ese; solo Lotenal → solo ese; combinaciones → únicamente los marcados.
- Si no hay expendio (sin datos), los tres botones se mantienen visibles
  (comportamiento previo, sin romper Vender).
- Al cambiar permisos en Expendios y volver a Vender, `LoadAsync` (OnAppearing)
  recarga y los botones reflejan el cambio al instante.

**Actualización Fase 1 (2026-09-14, integración backend):** la pestaña Vender
ya NO lee `ExpendiosDemoData`; `CargarPermisosVenta()` toma los flags del
billetero de la sesión mobile (`datosBilletero()` del login/verify). Mapeo
decidido con el usuario:

- Tiempo Aire → `tiene_tiempo_aire`
- Sorteos Tec → `tiene_prod_digitales`
- Lotenal → `tiene_lotenal` (columna nueva m260916_000001, aprobada
  2026-09-16, DEFAULT 1: los billeteros existentes conservan el botón;
  editable desde el formulario Expendios junto a los otros dos).

La pestaña **Expendios** (consulta + administración) sigue con demo data
hasta su propia integración; el formulario Expendios sigue escribiendo
`ExpendiosDemoData` local.

## 8. Pendientes por falta de capturas de referencia

NO se implementaron (ni se inventaron) por falta de mockups:

1. **Consulta con registros**: formato de la lista de resultados, información
   exacta de cada registro y posible detalle.
2. **Resultado con múltiples expendios**: cómo se agrupa/distingue por expendio.
3. **Impresión**: formato, contenido y flujo completo de impresión del
   resultado filtrado (el icono existe; `ImprimirCommand` solo avisa que está
   pendiente).
4. Filtros o datos adicionales del resultado (orden, totales, etc.).

Al llegar las capturas: extender `ExpendiosViewModel.ConsultarAsync` (ya
estructura rango + expendios seleccionados) y añadir el template de la
colección de resultados.

## 9. Impresión pendiente de definición

El icono de impresora existe y tiene comando enlazado, pero NO existe un
formato de impresión definido (ni PDF ni ticket). No implementar hasta tener
referencia. Reutilizar cuando llegue: `Services/TicketPdfService` /
`ImpresoraService` ya existentes para otros módulos.

## Datos temporales

`Data/ExpendiosDemoData.cs` — **UN SOLO expendio de prueba** (directriz
2026-09-11): alias Centro, titular/domicilio verbatim del mockup 4, usuario
`billetero`, permisos iniciales con los tres productos. Las altas desde Crear
Expendio viven SOLO en memoria. NO es seed permanente, NO toca BD. Sustituir
por el servicio del backend cuando exista contrato (misma forma de
`ObtenerExpendios()`).

## Navegación verificada (emulador, 2026-09-11)

- Tab bar → Expendios (entrada desde Gestión OK: es tab del Shell, igual que
  Vender/Gestión/Cuenta).
- Pestaña 1 ↔ pestaña 2 (cambio fluido, ambas conservan estado).
- Calendario e Impresora visibles junto al periodo (iconos morados #4125F4).
- Calendario → modal abre/cierra (tap fuera cierra).
- Consultar → Procesando... → Sin Registros.
- Lápiz → Actualizar Expendio carga datos del expendio (verificado con
  Centro: alias/titular/usuario/contraseña oculta/domicilio).
- Ojo → contraseña visible/oculta (•••• ↔ demo1234).
- Permisos de Venta: selección/deselección correcta en los 3 checkboxes
  (bug de instancia corregido: notificar Permisos tras reasignar _edicion).
- Actualizar → regresa a la lista; permisos conservados.
- FAB + → Crear Expendio → Guardar (con campos) → tarjeta nueva visible;
  Guardar vacío → avisos y NO navega.
- Back del header y back de sistema → regreso a lista (sin pantallas huérfanas).
- Vender: botones según permisos del expendio (verificado: 3 → 3; solo Tec →
  solo Tec).
