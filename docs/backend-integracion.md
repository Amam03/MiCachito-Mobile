# Backend Integration — Mi Cachito Mobile

**Estado:** Fase 0 (auth) implementada, probada E2E y publicada (2026-09-14). **Fase 1 (app MAUI conectada a `/api/mobile/auth/*`) IMPLEMENTADA y probada E2E en emulador contra Docker real (2026-09-14); pendiente de inspección del usuario para commit.** Ventas Lotenal, Tiempo Aire y Devoluciones: pendientes (ver §9).

Este documento es la **guía oficial** de cómo se integra Mi Cachito Mobile al backend. Cualquier desarrollador o agente de IA que toque el backend por trabajo de Mobile debe leerlo completo antes de modificar código.

Repositorio backend: `MiCachito-backend`, rama de trabajo: **`dev-mobile`**.

---

## 1. Arquitectura

```text
Mi Cachito Desktop (PySide6)  ──┐
                                ├──  Backend Mi Cachito (Yii2)  ───  MySQL
Mi Cachito Mobile (MAUI)     ──┘
```

Desktop y Mobile comparten **el mismo backend y la misma base de datos**. La separación está en la API:

| Parte | Compartido / Específico | Detalle |
|---|---|---|
| BD (tablas de negocio) | Compartido | `ventas`, `ventas_electronicas`, `movimientos_almacen`, `billeteros`, etc. Una sola verdad. |
| Modelos Yii2 | Compartido | Mobile usa los mismos ActiveRecord (`Ventas`, `BilletesLoteria`, …). Prohibido duplicarlos. |
| Reglas de negocio | Compartido vía Services | `VentaService`, `DevolucionService` (pendientes); extraídas de los controladores Desktop sin cambiar su comportamiento. |
| API Desktop `/api/*` | Solo Desktop | Auth contra `usuarios.access_token` (HttpBearerAuth). Mobile **no la llama** (salvo endpoints públicos). |
| API Mobile `/api/mobile/*` | Solo Mobile | Namespace propio `controllers/api/mobile/`. Auth contra `mobile_sesiones`. Contratos propios. |
| Autenticación | Separada | Desktop: `usuarios`. Mobile: `billeteros_expendios` + `mobile_sesiones`. Ninguna toca a la otra. |

Estructura en el backend:

```text
app/
├── controllers/
│   └── api/
│       ├── *.php              # Desktop — NO TOCAR por trabajo Mobile
│       └── mobile/            # Mobile — todo lo específico vive aquí
│           ├── BaseMobileController.php
│           └── AuthController.php
├── models/                    # Compartidos + BilleterosExpendios, MobileSesiones
├── services/                  # Lógica de negocio compartida (VentaService, DevolucionService…)
├── migrations/                # m260914_000001_create_mobile_sesiones (aplicada en dev)
└── components/                # BitacoraComponent (mobile registra módulo 'mobile')
```

Rutas: bloque `============ MOBILE ============` al final de `config/web.php`, prefijo `api/mobile/*`.

---

## 2. Reglas de seguridad para Desktop

1. **No modificar endpoints Desktop existentes sin justificación** escrita en el commit y aprobada.
2. **No cambiar contratos/respuestas existentes** de `/api/*` (shape, códigos HTTP, keys).
3. **No modificar `usuarios`** para resolver autenticación Mobile (ni tablas, ni flujo, ni token).
4. **No modificar tablas compartidas** si existe alternativa segura (ver §6). Si no hay alternativa: detenerse y reportar ANTES de escribir la migración.
5. **No duplicar modelos existentes** — si falta un modelo (como faltaba `BilleterosExpendios`), crear el que corresponda a la tabla real, uno solo.
6. **No duplicar reglas de negocio** — extraer a Service compartido (ver §5), nunca copiar-pegar la lógica del controlador Desktop.
7. **No introducir datos de prueba permanentes** — los E2E crean expendios de prueba y los borran al final (patrón de `/tmp/e2e_mobile*.sh`); jamás seeds permanentes de datos falsos.

Regla de oro antes de crear algo nuevo:

1. ¿Ya existe un modelo? → úsalo.
2. ¿Ya existe un endpoint? → verifica si es público (reutilizable directo) o de Desktop (requiere contraparte mobile delgada).
3. ¿Ya existe una regla de negocio? → extráela a Service compartido, no la reimplementes.
4. ¿Puede reutilizarse sin afectar Desktop? → si sí, reutiliza.
5. ¿Realmente necesita implementación Mobile específica? → solo entonces crea endpoint en `controllers/api/mobile/`.

---

## 3. Autenticación Mobile

### Población de credenciales: `billeteros_expendios`

- La **administra Desktop** (`api/cont-cedis/clientes`, permiso `contabilidad.clientes`): al crear/editar un cliente genera `usuario` (UNIQUE) + `password_hash` (bcrypt `PASSWORD_DEFAULT`). El update hace **delete-all + re-insert** de los expendios del cliente.
- Mobile **solo la lee** para autenticar. Jamás escribe en ella.
- Consecuencia del delete-all + re-insert: al regenerar credenciales desde Desktop, las sesiones mueren solas por el **CASCADE** de `mobile_sesiones.id_expendio` → `billeteros_expendios.id_expendio` (revocación implícita por rotación de credenciales — diseñado así a propósito).
- Chequeos en login: credenciales válidas → `autorizado = 1` → `billeteros.estatus = 'activo'`. En cada request (verify/lógica): sesión válida → `autorizado` → `estatus` (defensa en dos capas).

### Sesiones: `mobile_sesiones` (única tabla nueva aprobada)

| Columna | Tipo | Uso |
|---|---|---|
| `id_sesion` | PK | Identificador de sesión/dispositivo |
| `id_expendio` | FK → `billeteros_expendios` (CASCADE) | Quién está autenticado |
| `token_hash` | char(64) UNIQUE | SHA-256 del token; el token plano **nunca** se persiste |
| `dispositivo` | varchar(100) NULL | Etiqueta del dispositivo (pantalla Cuenta) |
| `fecha_creacion` | datetime | Alta de la sesión |
| `fecha_expiracion` | datetime NOT NULL | Expiración (default **30 días**, `MobileSesiones::VALIDEZ_DIAS`) |
| `ultimo_acceso` | datetime NULL | Se actualiza en cada request autenticado |
| `fecha_revocacion` | datetime NULL | No-NULL = sesión muerta (logout o revocación) |

### Tokens

- **Generación:** `Yii::$app->security->generateRandomString(64)` al login; se guarda su SHA-256 y el plano se entrega **una sola vez** en la respuesta del login.
- **Validación:** `MobileSesiones::validarToken($token)` — hash exacto + no revocada + no expirada.
- **Expiración:** 30 días; una sesión expirada es indistinguible de una revocada (401 igual).
- **Revocación:**
  - Individual: `POST /api/mobile/auth/sesiones/revocar` con `{id_sesion}` — solo acepta sesiones **del propio expendio**.
  - Logout: `POST /api/mobile/auth/logout` revoca SOLO la sesión del token con que se llama (los demás dispositivos siguen vivos).
  - Implicit: rotación de credenciales desde Desktop (CASCADE).
- **Multi-dispositivo:** ilimitado por ahora (una fila por login); la revocación por `id_sesion` permite limitar después sin cambio de esquema.
- **Relación con `id_billetero`:** la sesión cuelga del expendio; el billetero se resuelve vía `billeteros_expendios.id_billetero`. En los endpoints de negocio, TODO se acota con `getIdBilleteroActual()` — **nunca** con un id que venga del request.

### Endpoints implementados (probados E2E 2026-09-14)

| Método y ruta | Auth | Body/Params | Respuesta clave |
|---|---|---|---|
| `POST /api/mobile/auth/login` | público | `{username, password, dispositivo?}` | `{token, expira, expendio, billetero}` |
| `GET /api/mobile/auth/verify` | token | — | `{expendio, billetero, sesion}` |
| `POST /api/mobile/auth/logout` | token | — | revoca solo esta sesión |
| `GET /api/mobile/auth/sesiones` | token | — | dispositivos activos, flag `actual` |
| `POST /api/mobile/auth/sesiones/revocar` | token | `{id_sesion}` | 404 si no es del expendio |

Respuestas con el mismo shape que Desktop (`success/data/error/message`), módulo de bitácora: `mobile` (acciones `login_exitoso`, `login_fallido`, `login_bloqueado`, `logout`, `sesion_revocada`).

**Independencia verificada E2E:** token Desktop contra API Mobile → 401; token Mobile contra API Desktop → 401; login Desktop sigue OK con la API Mobile desplegada.

### Integración en la app (lado MAUI) — ✅ HECHA (Fase 1, 2026-09-14)

La app ya habla exclusivamente con `/api/mobile/auth/*`:

- `ApiEndpoints.MobileAuth` (login/verify/logout/sesiones/revocar); los endpoints Desktop `ApiEndpoints.Auth` se eliminaron junto con el contrato viejo (`LoginResponse`, `LoginRequest`, `Usuario`/`CedisInfo`/`TiendaInfo`/`RolInfo`, `LoginResponseJsonConverter`, `RefreshTokenAsync` — grep de cero usos antes de borrar).
- `IAuthService`/`AuthService` → contrato mobile; `SessionService` persiste `SessionInfo` con expendio/billetero/id_sesion/dispositivo.
- Login envía `dispositivo = DeviceInfo.Current.Name` (aparece así en la lista de sesiones del expendio).
- Splash: token → `verify` → Home; ApiException (expirada/revocada/rotada) → Clear + Login. Sesión vieja Desktop: verify la rechaza → Login limpio.
- Cuenta: identidad = `Billetero.NombreCompleto`. Home/Vender: `CargarPermisosVenta()` desde flags del billetero — **mapeo P1 (decisión del usuario 2026-09-14): Tiempo Aire→`tiene_tiempo_aire`, Sorteos Tec→`tiene_prod_digitales`, Lotenal→habilitado por ahora** (el backend NO tiene flag propio para Lotenal; si se necesita per-expendio, cambio separado — sin tocar tablas compartidas).
- E2E en emulador contra Docker real: login→Home, restauración por verify, logout (revoca SOLO esa sesión), re-login, credenciales malas → "Credenciales incorrectas" sin navegar, permisos Vender correctos por flags, identidad real en Cuenta. Pendiente: inspección del usuario → commit.

---

## 4. Organización de endpoints

Principio: **Mobile no llama endpoints Desktop autenticados** (el Bearer de `mobile_sesiones` no pasa `HttpBearerAuth` de Desktop — por diseño). Reutilización directa solo para endpoints **públicos**; el resto son endpoints mobile **delgados** que reutilizan modelos/lógica.

| Módulo | Endpoint | Tipo | Reutiliza existente | Específico Mobile |
|---|---|---|---|---|
| auth | `POST /api/mobile/auth/login` | ✅ hecho | `billeteros_expendios` (tabla), bcrypt | flujo+token propios |
| auth | `GET /api/mobile/auth/verify` | ✅ hecho | — | sesión mobile |
| auth | `POST /api/mobile/auth/logout` | ✅ hecho | — | revocación individual |
| auth | `GET /api/mobile/auth/sesiones` | ✅ hecho | — | pantalla Cuenta |
| auth | `POST /api/mobile/auth/sesiones/revocar` | ✅ hecho | — | pantalla Cuenta |
| catálogo | `GET /api/mobile/sorteos` | pendiente | modelo `Sorteos` (misma query que `SorteosController::actionIndex`) | thin: sin permisos Desktop, sin `?id_cedis` |
| catálogo | `GET /api/mobile/sorteos/{id}` | pendiente | modelo `Sorteos` | thin |
| premios | `POST /api/premios/consultar` | **reuso directo** | endpoint público (sin auth, como Sr. Billetero) | nada — Mobile ya lo puede llamar |
| ventas LN | `GET /api/mobile/ventas/billetes` | pendiente | `BilletesLoteria` (asignados al billetero) | thin |
| ventas LN | `POST /api/mobile/ventas` | pendiente | `VentaService` + `ventas`/`ventas_detalle` | precios server-side, fracciones |
| ventas LN | `POST /api/mobile/ventas/{id}/cancelar` | pendiente | lógica de `actionCancelar` (vía service) | scope del expendio |
| tiempo aire | `POST /api/mobile/tae` | pendiente | `ventas_electronicas` + reglas de `billeteros` | validaciones/cálculo server-side |
| tiempo aire | `GET /api/mobile/tae/historial` | pendiente | modelo `VentasElectronicas` | thin por id_billetero |
| devoluciones | `POST /api/mobile/devoluciones` | pendiente | `DevolucionService` (extraído de `DevolucionesTiendaController::actionCreate`) | `id_tienda_origen = NULL` |
| reportes | `GET /api/mobile/reportes/*` | pendiente | `LiquidacionesBilleteros`, `VentasElectronicas`, `Ventas` | thin, scope id_billetero |
| cuenta | cubierto por `auth/sesiones` | ✅ hecho | — | — |

Regla de la tabla: al implementar cada endpoint, mover su fila de "pendiente" a "✅ hecho" y anotar en el commit.

**Módulos que NO crean controlador propio:** expendios (la alta/baja de credenciales sigue siendo Desktop `cont-cedis/clientes`), fondo de ahorro y depósitos (se definirá su endpoint mobile delgado cuando se implemente la pantalla, reutilizando los modelos de caja existentes).

---

## 5. Servicios

> El Controller recibe/valida la petición y coordina la respuesta. La lógica de negocio que deba compartirse entre Desktop y Mobile debe vivir en una capa reutilizable, evitando duplicarla.

Cuándo usar un Service:
- La misma regla de negocio se necesita desde Desktop **y** Mobile (o se prevé a corto plazo).
- Hay una transacción de BD multi-tabla que no debe fragmentarse (venta: cabecera+detalle+inventario+billetes; devolución: movimiento+detalle+inventario+billetes).

Cuándo NO:
- Queries de lectura simples → directo al modelo desde el controller mobile.
- Validación de entrada/shape de respuesta → eso es del controller, no del service.
- Lógica que solo Desktop usa → se queda donde está; no extraer "por si acaso".

Contrato de un Service:
- Ubicación: `services/`, namespace `app\services`.
- Sin conocer request/response/Yii web layer: reciben arrays/escalars y modelos, devuelven modelos/arrays o lanzan excepciones de dominio.
- Reutiliza modelos ActiveRecord; abre sus propias transacciones (`$tx = Yii::$app->db->beginTransaction()`).
- Los controladores Desktop existentes **no cambian de comportamiento** al refactorsarse a llamar al service (mismo resultado, mismo orden de escritura).

Aprobados: `VentaService`, `DevolucionService`, y un service de TAE **solo si** su lógica también la usa Desktop (si es solo Mobile, puede vivir como método privado del controller mobile). Precedente en el repo: `services/SabanaPremiosService.php`.

---

## 6. Base de datos

### Tablas que Mobile REUTILIZA (lectura)

`billeteros` (perfil/permisos de venta del expendio), `billeteros_expendios` (auth), `sorteos`, `productos`, `billetes_loteria`, `ventas`, `ventas_detalle`, `ventas_electronicas`, `liquidaciones_billeteros`, `movimientos_almacen` + detalle, `inventario_cedis`, depósitos/fondo de ahorro (modelos de caja), `bitacora_sistema` (solo vía `registrarBitacora`).

### Tablas que Mobile ESCRIBE (vía endpoints, siempre acotadas a `id_billetero`)

| Tabla | Endpoint futuro | Motivo |
|---|---|---|
| `mobile_sesiones` | auth (ya activo) | única tabla nueva aprobada |
| `ventas` + `ventas_detalle` | `POST /api/mobile/ventas` | venta del expendio, mismo dominio que Desktop |
| `ventas_electronicas` | `POST /api/mobile/tae` | transacción TAE del expendio |
| `movimientos_almacen` + detalle | `POST /api/mobile/devoluciones` | devolución estatus `pendiente`, la procesa Almacén |

### Tablas que NO se tocan desde Mobile (ni lectura ni escritura directa)

`usuarios`, `usuarios_roles`, `permisos` (Mobile no tiene sistema de permisos: su autorización es `autorizado` + `estatus` + flags del billetero), `cedis`, `tiendas_sucursales`, `turnos`, `cierres_*`, y cualquier tabla de administración Desktop.

### Migraciones

| Migración | Estado | Motivo |
|---|---|---|
| `m260914_000001_create_mobile_sesiones` | aplicada (dev) | sesiones mobile: token hash, dispositivo, expiración, revocación, multi-dispositivo, CASCADE a credenciales |

**Regla:** no crear más tablas (ni ALTERs a tablas compartidas) sin justificar la necesidad primero con el usuario. Ejemplo conocido y diferido: fracciones vendidas por billete — se DERIVARÁ de `ventas_detalle`; si en implementación resulta insuficiente, **detenerse y documentar** antes de tocar `billetes_loteria`.

---

## 7. Flujo de desarrollo

```text
Analizar BD/código existente
        ↓
Identificar reutilización (regla de oro §2)
        ↓
Definir endpoint/servicio (tabla §4)
        ↓
Evaluar impacto Desktop
        ↓
Implementar en dev-mobile
        ↓
Probar Mobile (E2E contra Docker)
        ↓
Probar que Desktop no se rompió
        ↓
Documentar (actualizar §4 y este doc)
        ↓
Commit
```

Notas operativas:
- Backend Docker: contenedor `micachito-php`, repo montado en `/app`, API en `http://localhost:8080`. `php yii migrate` dentro del contenedor.
- Credenciales de BD: siempre de `/app/config/db.php` dentro del contenedor; nunca hardcodear.
- **Baseline de tests:** `vendor/bin/codecept run unit` tiene 4 fallos PRE-EXISTENTES (LoginForm + User×3, por datos del entorno). Un cambio Mobile es verde si deja el mismo resultado 44/4 — no si "parece ok".
- E2E Mobile: crear expendio de prueba con hash bcrypt, ejercitar el flujo con curl, verificar independencia (token Desktop ↔ token Mobile se rechazan mutuamente), y **borrar el expendio de prueba al final** (las sesiones mueren por CASCADE). Patrón en `/tmp/e2e_mobile*.sh`.
- Verificar bitácora: `SELECT ... FROM bitacora_sistema WHERE modulo='mobile'`.

---

## 8. Reglas para agentes de IA

Cualquier agente que trabaje en el backend por Mobile:

1. **Leer este documento completo antes de modificar backend.**
2. **Trabajar sobre `dev-mobile`** (mantenerla fusionada con `dev`).
3. **Inspeccionar primero la implementación existente** (controlador/modelo/migración real) — no asumir.
4. **No asumir que una funcionalidad necesita endpoint nuevo**: aplicar la regla de oro de §2; los endpoints públicos se reutilizan directo, el resto son contrapartes delgadas sobre modelos existentes.
5. **No crear tablas/modelos duplicados** ni models paralelos "mobile_*" de tablas de negocio.
6. **No modificar Desktop sin autorización** explícita: endpoints, contratos y tablas de §2/§6.
7. **Explicar primero cualquier cambio estructural importante** (nuevo namespace, service, migración) y esperar aprobación.
8. **Detenerse y reportar** cuando encuentre una decisión que pueda afectar BD o Desktop (ejemplo: fracciones vendidas que no quepa derivar de `ventas_detalle`) — documentar el problema y preguntar, no improvisar migraciones.
9. Toda sesión/acción queda en bitácora módulo `mobile`; usar `registrarBitacora` en cada endpoint.
10. Al terminar: actualizar la tabla de §4 y dejar E2E verificado + regresión de tests en 44/4 (baseline §7).

---

## 9. Pendiente para la implementación funcional

1. ~~**Conectar la app MAUI al auth mobile**~~ ✅ Fase 1 HECHA (2026-09-14, ver §3 "Integración en la app"); pendiente solo inspección + commit.
2. **`VentaService`** — extraer de `VentasController::actionCrear` (precios server-side desde `sorteos.precio_fraccion`, folio robusto) + **`MobileVentasController`** (fracciones sobre billetes asignados; derivar fracciones vendidas de `ventas_detalle`; si no cabe → parar y documentar).
3. **`DevolucionService`** — extraer de `DevolucionesTiendaController::actionCreate`; **`MobileDevolucionController`** con `id_tienda_origen = NULL` (verificar nulidad en modelo/procesamiento antes; documentar si algo la exige).
4. **TAE** — `POST /api/mobile/tae` sobre `ventas_electronicas`: validaciones y cálculos (comisión/ISR/fondo ahorro con las reglas del billetero) 100% server-side.
5. **Catálogo sorteos mobile delgado** (`GET /api/mobile/sorteos`) — primer endpoint de lectura post-auth.
6. Reportes / Tira de Liquidación / Depósitos / Fondo de Ahorro: thin endpoints sobre modelos existentes, cada uno con su análisis puntual antes de implementar.
