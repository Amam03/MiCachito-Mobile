# Notas de negocio — Depósitos (mockups 7.x, pestaña Gestión)

Fase: SOLO INTERFAZ. Estado en memoria (`Services/DepositoService.cs`), sin
backend, sin BD, sin seeds. Los datos de los mockups (bancos, fechas,
montos) son referencias visuales, no datos reales.

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
4. **Aceptar** guarda en memoria con folio consecutivo local desde 1 y
   regresa al listado. **Cancelar** regresa sin guardar.

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

El backend exige `tipo_deposito` (efectivo|cheque|transferencia) y
`estatus` (pendiente|validado|rechazado|utilizado) que la UI aún no
captura — al integrar, definir de dónde salen (¿tipo desde el banco?
estatus inicia "pendiente" como la nube ámbar).

## Catálogos que deben venir del backend

- **Bancos**: la fuente real es `cuentas_bancarias.banco` (backend
  Yii2), no una lista fija. El selector actual usa el catálogo del
  mockup (BANAMEX, BANCO AZTECA, BBVA, INBURSA, SANTANDER).
- **Folio**: consecutivo local solo para UI; el real es
  `folio_deposito` único generado por el backend (regla `unique`).

## Sincronización

- Icono nube ámbar = registro local pendiente de sincronizar. Al
  integrar: POST del depósito + imagen (`imagen_deposito` en backend,
  nombre de archivo/ruta), y la nube cambia a "sincronizado" (verde o
  check) al confirmar el backend. El estatus del depósito en backend
  ("pendiente") lo valida alguien más ("validado_por",
  "fecha_validacion").
