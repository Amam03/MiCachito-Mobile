| 500 | `Error al renovar token` | Fallo al guardar el nuevo token |

- **Uso dentro de la app:** `AuthService.RefreshTokenAsync` existe, pero **no está conectado** a ningún flujo (sin reintento automático ante 401). Ver `#8 Endpoints futuros`.

---

## 5. Modelos de intercambio

### Requests

Modelos enviados al backend.

**`LoginRequest`** (`Models/Requests/LoginRequest.cs`) — cuerpo de `POST api/auth/login`:

| Campo | Tipo | Descripción |
|---|---|---|
| `username` | `string` | Usuario (se serializa desde `Username`). |
| `password` | `string` | Contraseña (se serializa desde `Password`). |

### Responses

Modelos de respuesta.

**`LoginResponse`** (`Models/Responses/LoginResponse.cs`) — resultado de `POST api/auth/login`:

| Campo | Tipo | Descripción |
|---|---|---|
| `Usuario` | `Usuario` | Datos del usuario (objeto plano del backend). |
| `Token` | `string` | Token de acceso (separado del objeto plano por `LoginResponseJsonConverter`). |

> Nota: `permisos` y `roles` **no** son campos directos de `LoginResponse`; viven dentro de `Usuario`.

**`ApiResponse<T>`** (`Models/Common/ApiResponse.cs`) — envelope estándar de la API:

| Campo | Tipo | Descripción |
|---|---|---|
| `success` | `bool` | Indica éxito o error. |
| `message` | `string` | Mensaje opcional. |
| `error` | `string` | Mensaje de error (cuando `success=false`). |
| `errors` | `List<ApiError>` | Errores de validación (422). |
| `data` | `T` | Contenido de la respuesta. |

### Entities

Modelos recibidos del backend (definidos por `getApiDataCompleto()` del modelo `User`).

**`Usuario`** (`Models/Entities/Usuario.cs`):

| Campo | Tipo | Descripción |
|---|---|---|
| `id_usuario` | `int` | ID del usuario. |
| `username` | `string` | Nombre de usuario. |
| `tipo_usuario` | `string` | Tipo de usuario (`corporativo`, `almacen`, `caja`, `ventas`, `supervisor`, `billetero`). |
| `id_cedis` | `int?` | CEDIS asignado (si aplica). |
| `id_tienda` | `int?` | Tienda asignada (si aplica). |
| `estatus` | `string` | `activo`, `inactivo` o `bloqueado`. |
| `ultimo_acceso` | `string` | Último acceso (texto, formato SQL). |
| `fecha_creacion` | `string` | Fecha de creación (texto, formato SQL). |
| `cedis` | `CedisInfo?` | Objeto CEDIS, si tiene uno. |
| `tienda` | `TiendaInfo?` | Objeto tienda, si tiene una. |
| `roles` | `List<RolInfo>` | Roles del usuario. |
| `permisos` | `List<string>` | Permisos como `"modulo.accion"`. |

**`CedisInfo`** (`Models/Entities/CedisInfo.cs`): `id_cedis`, `nombre_cedis`.

**`TiendaInfo`** (`Models/Entities/TiendaInfo.cs`): `id_tienda`, `nombre_tienda`.

**`RolInfo`** (`Models/Entities/RolInfo.cs`): `id_rol`, `nombre_rol`, `descripcion`, `estatus`, `fecha_creacion`.

> Notas:
> - `ultimo_acceso` y `fecha_creacion` se mantienen como texto porque el backend las envía en formato SQL `yyyy-MM-dd HH:mm:ss`.
> - Los permisos viajan como arreglo de identificadores `modulo.accion` (`getPermisos()` + `getIdentificador()`).

---

## 6. Manejo de errores

- **`ApiException`** (`Api/ApiException.cs`): excepción lanzada cuando el backend responde con error (HTTP no 2xx o respuesta 2xx con `success=false`). Expone:
  - `StatusCode` — código HTTP (`HttpStatusCode`).
  - `ServerMessage` — campo `error` del backend (o `message`), o `null` si no lo envió.
  - `Errors` — lista `IReadOnlyList<ApiError>`, normalizada por `ApiErrorConverter`.
- **`ApiError`** (`Models/Common/ApiError.cs`): `Field` y `Message`; `ToString()` legible.
- **`ApiErrorConverter`** (`Api/ApiErrorConverter.cs`): normaliza el campo `errors` que Yii2 envía como objeto (`{campo: ["msg"]}`) o como arreglo (`["msg"]`).
- **Errores de conexión:** cualquier otra excepción (sin conexión, timeout, JSON inválido) no es `ApiException`; se captura de forma genérica.
- **Mensajes mostrados al usuario** (patrón en los ViewModels):
  - `ApiException` → `Mensaje = ex.ServerMessage ?? "mensaje por defecto"`.
  - Otra excepción → mensaje genérico de conexión ("No se pudo conectar con el servidor. Intente más tarde.").
  - `SplashPage` distingue `ApiException` (token inválido → limpiar sesión y login) de otra excepción (sin conexión → entrar al Home con sesión cacheada).
- `IsBusy` se restaura en `finally`.

---

## 7. Convenciones para nuevos endpoints

Antes de crear una integración nueva:

1. **Confirmar que el endpoint existe en el backend** (rama `dev-mobile`). Si no existe, no inventarlo: proponerlo y esperar a que se implemente.
2. **Documentar el contrato** aquí en `docs/API.md`: método, ruta, request, response, errores y ejemplo JSON real.
3. **Crear Request/Response si corresponde** en `Models/Requests/` y `Models/Responses/` (o reutilizar DTOs existentes).
4. **Agregar la ruta en `Api/ApiEndpoints.cs`**, agrupada por dominio (`ApiEndpoints.<Dominio>.<Accion>`).
5. **Crear o extender el Service existente** en `Services/` (un servicio por dominio; extender antes de duplicar).
6. **Conectar el ViewModel** usando el servicio; sin HTTP directo.
7. **Probar el flujo completo** contra el backend y verificar que compile.

---

## 8. Endpoints futuros

Sección para funcionalidades pendientes. Solo se registran **necesidades reales**; las rutas se agregan cuando existan o se aprueben en el backend.

| Funcionalidad | Endpoint requerido | Estado |
|---|---|---|
| Renovación automática de token ante 401 (reintento con token nuevo) | `api/auth/refresh` (ya existe) | Parcial — implementado en cliente pero sin conectar |

> Las futuras pantallas (ventas, almacén, caja, reportes, etc.) requerirán endpoints del backend ya existentes en `dev-mobile` (78 controladores bajo `controllers/api/`). A medida que se conecten, se documentarán en `#4` y se registrarán aquí los pendientes.

---

## 9. Reglas para agentes IA

- **Consultar `docs/API.md` antes de crear servicios.** Los modelos, requests, responses y errores deben derivarse de este documento, no inferirse.
- **No asumir la estructura JSON.** Cada contrato se verifica contra este documento y/o el código del backend; nunca suponer campos.
- **No inventar endpoints.** La app consume únicamente lo que existe en `dev-mobile`; si falta un endpoint, reportarlo como pendiente en `#8`.
- **No crear modelos basándose únicamente en nombres.** Verificar la estructura real antes de definir DTOs.
- **Si falta un endpoint, reportarlo como pendiente** en `#8 Endpoints futuros` y no conectar la pantalla hasta que exista.
- **Mantener sincronización entre `docs/API.md` y el código**: cada endpoint, modelo o error nuevo debe reflejarse aquí, y cada cambio en este documento debe corresponder a código real.

---

## Estado del documento

- **Fecha:** 04 agosto 2026
- **Estado:** Base inicial creada.
- **Nota:** Este documento crecerá conforme se implementen nuevas pantallas y endpoints del backend.
