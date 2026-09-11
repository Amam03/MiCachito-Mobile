# Módulo Cuenta (docs/UI/Cuenta 1-6)

Pestaña final del menú principal. Implementada 2026-09-11, fase SOLO INTERFAZ
(excepto Login/Logout reales contra backend Yii2, que ya existían).

## 1. Pantalla principal (mockup 1)

- `Views/CuentaPage.xaml` — ContentPage tab del Shell (ya existía como placeholder).
- Header morado #4125F4 (`McPrimaryMorado`/`McLotenalHeader`) con **Saldo: $0.00**
  (verde `McGesSaldoVerde`, patrón Gestion/Home).
- Identidad de la app: `logo_micachito.svg` (billete verde existente) + "Mi Cachito"
  + versión REAL `v{VersionTracking.CurrentVersion}` (= "v1.0" del csproj, no inventada).
- Usuario autenticado REAL: `ISessionService.CurrentSession?.Usuario.Username`
  (= "billetero" en emulador).
- Card blanca con 3 filas (icono + label 15 + chevron; colores MEDIDOS del mockup 1):
  - **Notificaciones** (`icon_campana_azul.svg` #5DB7F3 azul claro) → navega a NotificacionesPage.
  - **Impresora** (`icon_impresora_gris.svg` #A9A9A9, desplegable; chevron ▼/› conmuta).
  - **Cerrar Sesión** (`icon_power_rojo.svg` #D7493D) → modal de confirmación.

## 2. Notificaciones (mockup 3)

- `Views/NotificacionesPage.xaml` + VM — página pusheada (ruta relativa `NotificacionesPage`).
- Header morado + flecha atrás (patrón DetalleVenta/Tickets) + título blanco.
- Cuerpo VACÍO: no existen datos reales de notificaciones (regla: no inventar
  registros). El listado llegará con la integración del backend.

## 3. Impresora desplegable (mockup 4)

- Al expandir: consulta dispositivos Bluetooth VINCULADOS reales vía
  `IImpresoraService.ObtenerDispositivosEnlazadosAsync()` (NUEVO método en la
  interfaz; implementación Android consulta `BluetoothAdapter.BondedDevices`
  con guard de permiso BLUETOOTH_CONNECT API 31+).
- Sub-fila "Seleccionar Dispositivo": **texto azul** (#3F51B5, familia Indigo del mockup,
  misma que "Consultar" de Expendios) + **icono Bluetooth azul a la derecha**
  (`icon_bluetooth_azul.svg` #0087E5) SIN chevron — estructura medida del mockup 4
  (texto x=26-112, icono x=143-149). → DispositivosEnlazadosPage.
- Si NO hay dispositivos vinculados: diálogo Atención SOLO LA PRIMERA VEZ
  (`_avisoSinImpresoraMostrado`); re-expansiones quedan sin diálogo (mockup 4).

## 4. Diálogo Atención (mockup 2)

- Overlay scrim `McScrimBlack55` + card blanca centrada ~280px:
  `icon_atencion_rojo.svg` (triángulo) + "Atención" bold + "No se ha enlazado
  ninguna impresora" + pastilla ACEPTAR roja #FF5455 (≈McGesRojo).
- Se cierra con ACEPTAR o tocando el scrim. No se muestra si hay impresora
  vinculada (condición `SinDispositivos` real del adaptador BT).

## 5. Dispositivos Enlazados (mockup 5)

- `Views/DispositivosEnlazadosPage.xaml` + VM — página pusheada.
- Lista REAL de dispositivos vinculados (BindableLayout, filas blancas con
  chevron). En emulador sin vínculos → cuerpo vacío tal como el mockup 5.
- El emparejamiento/selección real (discovery + SPP + ESC/POS) queda para la
  fase de integración — `IImpresoraService.ImprimirAsync` ya documenta el aviso.

## 6. Cerrar Sesión (mockup 6)

- Modal in-page: scrim atenuado + card "Cerrar Sesión" (título ROJO #FF5455) +
  "¿Está seguro de cerrar su sesión?" + Cancelar (texto gris) + Aceptar (pastilla
  roja). Tap en scrim = Cancelar; tap en card no se propaga (CardModal_Tapped).
- **Aceptar** = mismo flujo que `HomeViewModel.CerrarSesionAsync`:
  `_authService.LogoutAsync()` (best-effort) → `_sessionService.ClearAsync()` →
  `NavigateToLoginAsync()` (reemplaza Window.Page → NO hay pila de regreso).

## 7. Fix de plataforma: back press (MAUI 10) y BoxView negro

- Espacio inferior de la pestaña: `Padding="0,0,0,24"` del VerticalStackLayout —
  NUNCA BoxView Color="Transparent" (se pinta NEGRO en Android; reporte del
  usuario 2026-09-11, mismo pitfall ya documentado del proyecto).

- Bug dotnet/maui#32750: back en raíz destruye la Activity y
  `ShellFragmentContainer.OnDestroy` crashea con `ObjectDisposedException`
  (IServiceProvider disposed). Reproducido 3 veces (09-10, 11:27, 13:35).
- Fix doble:
  1. `AndroidManifest.xml`: `android:enableOnBackInvokedCallback="false"`
     (opt-out del back predictivo, ON por default con targetSdk 36; sin esto
     el sistema NUNCA llama a OnBackPressed).
  2. `MainActivity.OnBackPressed` (override public): si hay pila Shell → pop
     normal; si es raíz (Login/tab) → `MoveTaskToBack(true)` (segundo plano,
     estándar Android) en vez de finish() → sin crash, y no regresa a
     pantallas protegidas.

## 8. Wiring

- `AppShell.xaml.cs`: RegisterRoute NotificacionesPage, DispositivosEnlazadosPage.
- `MauiProgram.cs`: DI Transient (CuentaViewModel+CuentaPage, NotificacionesVM+Page,
  DispositivosEnlazadosVM+Page). CuentaPage ya era ShellContent tab.
- 7 SVGs en Resources/Images (campana azul #5DB7F3, impresora gris #A9A9A9, power rojo
  #D7493D, bluetooth azul #0087E5, chevron derecha/abajo gris, triángulo atención rojo)
  — convención Material 24x24 un-path, TODOS declarados en el csproj con BaseSize 24,24
  (los 6 originales habían quedado fuera — corregido junto con los colores).

## 9. Datos temporales

- NINGUNO inventado: usuario y versión salen de fuentes reales (session
  service / VersionTracking); dispositivos Bluetooth del adaptador real;
  notificaciones = estado vacío documentado.

## 10. Pendientes por integración real (NO implementados a propósito)

- Notificaciones: listado real (fuente: backend, aún sin contrato).
- Impresora: selección/vínculo real (discovery BT + SPP + ESC/POS) — la
  interfaz queda lista; `ObtenerDispositivosEnlazadosAsync` expone los
  vinculados reales.
- Diálogo Atención: solo sale con adaptador sin vínculos (emulador). En
  físico con impresora vinculada no debe aparecer (spec punto 4).
- Saldo: $0.00 estático como las demás pestañas (llegará con backend).
