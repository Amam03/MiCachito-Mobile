# Notas de negocio — Depósitos (mockups 7.x, pestaña Gestión)

Fase: INTEGRADO (2026-09-30) contra `/api/mobile/depositos/*`
(backend `controllers/api/mobile/DepositosController.php`): historial GET,
registro POST con folio servidor `DEP-YYYYMMDD-NNNN` (`generarFolio()`) y
estatus 'pendiente' hasta que Caja lo valida desde Escritorio, catálogo de
cuentas activas GET `.../cuentas`. La entidad mock `Deposito` fue retirada.

## Flujo implementado

1. **Lista "Depósitos"** (mockup 7): header morado + back, pantalla vacía
   al inicio, FAB "+" azul. Registros guardados aparecen como tarjetas
   (mockup 7.3): folio, "Captura: dd-MMMM-yyyy", "BANCO - $X.XX",
   "fecha hh:mm" e icono de nube ámbar (pendiente de sincronizar).
2. **Nuevo Depósito** (mockup 7.1): 7 campos apilados con línea divisoria.
   Banco abre overlay con catálogo; Fecha abre MaterialDatePicker
   (calendario del mockup); Hora abre MaterialTimePicker MODO TECLADO
   (formato 12h con AM/PM); Folio/Movimiento/Autorización y Observaciones
   son Entry con contador de caracteres; Monto abre dial-pad estilo
   teléfono (misma paleta que la pantalla 10, 4 columnas: dígitos,
   "." decimal, ✕ limpiar, Realiz. cierra, Borrar retrocede);
   Comprobante abre el selector nativo de archivos (solo imágenes).
3. **Validaciones** (mockup 7.2): al Aceptar con campos obligatorios
   vacíos, cada campo muestra su hint en rojo (#C40000) y Observaciones
   muestra el aviso "Indica el sorteo del depósito" (#E60000). El usuario
   definió (decisión 3a): TODOS los obligatorios muestran aviso.
   El comprobante es OPCIONAL (decisión por default no confirmada).
4. **Aceptar** registra con POST `api/mobile/depositos` (folio servidor
   `DEP-YYYYMMDD-NNNN`, estatus 'pendiente', id_billetero tomado de la
   sesión) y regresa al listado, que recarga. **Cancelar** regresa sin
   guardar. El selector de banco lista las cuentas activas del backend
   (label corto = banco). Estado vacío/error/overlay al cargar, error de
   catálogo/registro arriba del formulario (patrón Recibos de Pago).

## Decisiones del usuario (2026-09-09)

- 1a: fila 3 del selector de bancos = **BBVA** (ilegible en mockup;
  coincide con el mock de depósitos del desktop).
- 2a: dial-pad completo — "." decimal (col. 4, fila 3), "Borrar"
  (col. 1, fila 4), ✕ limpia todo (col. 4, fila 1), 0 centrado,
  2 celdas beige no interactivas junto al 0.
- 3a: todos los campos obligatorios muestran aviso rojo.
- (sin respuesta) comprobante opcional asumido.

## Longitudes UI vs backend

| Campo | UI (mockup) | Backend (depositos_bancarios) |
|---|---|---|
| Folio/Movimiento/Autorización | 20 | folio_deposito 50 (ÚNICO), referencia 100 |
| Monto | 20 caracteres, 2 decimales | monto number (sin tope en reglas) |
| Observaciones (nombre del sorteo) | 50 | observaciones string (sin tope) |

Resuelto en la integración: `tipo_deposito` fijo 'transferencia'
(sin selector en la UI — decisión 1) y `estatus` inicia 'pendiente';
lo valida Caja desde Escritorio (pendiente → validado|rechazado;
validado → utilizado).

## Catálogos que deben venir del backend

- **Bancos**: RESUELTO — el selector lista las cuentas activas de
  `cuentas_bancarias` vía GET `api/mobile/depositos/cuentas`
  (label = banco, selección por id_cuenta). Ya NO usa el catálogo del
  mockup (BANAMEX, BANCO AZTECA, BBVA, INBURSA, SANTANDER).
- **Folio**: RESUELTO — folio servidor `DEP-YYYYMMDD-NNNN` generado
  por `DepositosBancarios::generarFolio()`; ya NO hay consecutivo
  local.

## Sincronización

- RESUELTO: el registro es POST síncrono al backend (no hay cola local).
  Icono nube ámbar = depósito con estatus 'pendiente' (aún sin validar
  por Caja desde Escritorio); desaparece cuando Caja lo mueve a
  validado/rechazado/utilizado. La imagen del comprobante NO se envía
  ni se guarda (decisión 3: no existe infraestructura de upload en
  ningún canal — 0 filas con imagen en BD); la UI conserva solo el
  nombre del archivo local.
