# API MiCachito.Mobile

## 1. Objetivo

Este documento describe la comunicación entre la aplicación móvil **MiCachito.Mobile** y el backend **PHP Yii2** (rama `dev-mobile`).

Es un documento **vivo**:

- Se actualiza cuando se agregan endpoints.
- No deben crearse integraciones sin documentar primero el contrato.
- La aplicación consume únicamente endpoints existentes o previamente aprobados en el backend.

Todo contrato aquí registrado fue verificado contra el código fuente (frontend y backend). No se documentan endpoints, rutas ni campos inventados.

Documentos relacionados:

- `docs/Arquitectura.md` — describe la arquitectura y el estado del proyecto.
- `docs/Convenciones.md` — reglas de nombres, carpetas, MVVM y C#.
- `docs/AI_CONTEXT.md` — reglas de trabajo (sin datos mock, sin endpoints inventados).

---

## 2. Configuración general

### 2.1 URL base

La URL base del backend se define en `Helpers/AppSettings.cs`:

| Parámetro | Valor |
|---|---|
| `BaseUrl` | `http://10.0.2.2:8080/` (alias del emulador Android al host; configurable con `API_BASE_URL`) |
| `TimeoutSeconds` | `30` |
| `AppName` | `Mi Cachito` |

> La URL base es el **único punto de configuración** del endpoint. Por defecto usa `10.0.2.2:8080` (alias universal del emulador de Android para alcanzar el `localhost` de la máquina host). Se sobreescribe con la variable de entorno `API_BASE_URL` para apuntar a un servidor remoto, otra máquina o un puerto distinto. No se incluyen aquí credenciales ni información sensible.

### 2.2 Cliente HTTP

- Cliente HTTP tipado: `ApiClient` (implementa `IApiClient`).
- Registrado en `MauiProgram.cs` con `AddHttpClient<IApiClient, ApiClient>`, que configura `BaseAddress`, `Timeout` y el header `Accept`.

### 2.3 Headers comunes

| Header | Valor | Cuándo |
|---|---|---|
| `Accept` | `application/json` | Siempre |
| `Content-Type` | `application/json` | Peticiones con cuerpo (`POST`) |
| `Authorization` | `Bearer <token>` | Cuando existe sesión (inyectado automáticamente vía `IAuthTokenProvider`) |

### 2.4 Content-Type y serialización

- Content-Type: `application/json` (`Constants.JsonContentType`).
- Serialización de requests/responses en `snake_case` (`ApiJsonOptions.Default`, `JsonNamingPolicy.SnakeCaseLower`), coincidiendo con la nomenclatura de Yii2.
- El cliente deserializa únicamente el campo `data` del envelope y devuelve el DTO correspondiente.

### 2.5 Autenticación y tokens

- Autenticación por **Bearer token**: `Authorization: Bearer <token>`.
- El token es opaco (string aleatorio de 64 caracteres generado por el backend) y no lo interpreta el cliente.
- `SessionService` persiste la sesión (incluido el token) como un único blob JSON en `SecureStorage` bajo la clave `"session"` y provee el token a `ApiClient` vía `IAuthTokenProvider`.
- Endpoints públicos (sin token): `login`, `logout`, `verify` y `refresh` (definidos en `AuthController::getPublicActions`). El resto de la API requiere Bearer (401) y, según el controlador, permisos (`modulo.accion`, 403).
- El backend registra acciones en bitácora (`login_exitoso`, `login_fallido`, `logout`, etc.).

---

## 3. Arquitectura de consumo API

Flujo de una petición desde la UI hasta el backend:

```
View
  ↓ BindingContext
ViewModel
  ↓
Service (IAuthService, ISessionService)
  ↓
IApiClient
  ↓
ApiClient → Backend Yii2
```

Responsabilidades:

- **View:** solo UI; nunca realiza llamadas HTTP.
- **ViewModel:** estado de pantalla, comandos y orquestación de servicios; maneja errores mostrando mensajes al usuario. **No consume HTTP.**
- **Service:** contiene la lógica de integración por dominio; envuelve llamadas a `IApiClient` indicando la ruta (`ApiEndpoints`) y el modelo request.
- **ApiClient:** maneja la comunicación: añade headers y token Bearer, serializa en `snake_case`, envía la petición, deserializa el envelope y devuelve `data`.
- **Responses → Models:** las respuestas del backend se convierten en modelos (`LoginResponse`, `Usuario`, etc.) que los ViewModels consumen.

---

## 4. Endpoints implementados actualmente

Actualmente solo existe el dominio **Auth**. A medida que se conecten pantallas se agregarán secciones por dominio.

### Auth

#### POST `api/auth/login`

- **Método HTTP:** `POST`
- **Ruta:** `api/auth/login`
- **Descripción:** autentica un usuario con usuario y contraseña. Genera un token de acceso, actualiza `ultimo_acceso` y devuelve los datos completos del usuario más el token.
- **Request:** `LoginRequest` (`Models/Requests/LoginRequest.cs`).

```json
{
  "username": "billetero",
  "password": "billetero123"
}
```

- **Response:** `LoginResponse` (`Models/Responses/LoginResponse.cs`) con `Usuario` (de `getApiDataCompleto()`) y `Token`.

```json
{
  "success": true,
  "data": {
    "id_usuario": 1,
    "username": "billetero",
    "tipo_usuario": "billetero",
    "id_cedis": null,
    "id_tienda": null,
    "estatus": "activo",
    "ultimo_acceso": "2026-08-04 12:00:00",
    "fecha_creacion": "2026-08-03 10:00:00",
    "cedis": { "id_cedis": 1, "nombre_cedis": "CEDIS Centro" },
    "tienda": { "id_tienda": 5, "nombre_tienda": "Tienda Centro" },
    "roles": [ { "id_rol": 1, "nombre_rol": "Billetero", "descripcion": "..." } ],
    "permisos": ["ventas.crear", "ventas.consultar"],
    "token": "<string aleatorio de 64 caracteres>"
  }
}
```

> Nota: el backend responde login con un **objeto plano** (campos del usuario + `token`); `LoginResponseJsonConverter` separa el objeto plano en `Usuario` y `Token`.

- **Errores conocidos:**

| HTTP | `error` | Condición |
|---|---|---|
| 400 | `Usuario y contrasena son requeridos` | Faltan `username` o `password` |
| 401 | `Usuario o contrasena incorrectos` | Usuario no existe o contraseña incorrecta |
| 403 | `Usuario bloqueado. Contacte al administrador` | `estatus = bloqueado` |
| 403 | `Usuario inactivo` | `estatus = inactivo` |
| 500 | `Error interno del servidor` | Fallo al guardar el token |

- **Uso dentro de la app:** `AuthService.LoginAsync` → `LoginViewModel.IngresarCommand`.

#### POST `api/auth/logout`

- **Método HTTP:** `POST`
- **Ruta:** `api/auth/logout`
- **Descripción:** cierra la sesión en el servidor. Invalida el `access_token` del usuario si el header Bearer es válido. No es estrictamente requerido el token (responde éxito igualmente).
- **Request:** sin cuerpo.
- **Response:** sin `data`.

```json
{
  "success": true,
  "message": "Sesion cerrada exitosamente"
}
```

- **Errores conocidos:** no definidos (siempre responde éxito).
- **Uso dentro de la app:** `AuthService.LogoutAsync` → `HomeViewModel.CerrarSesionCommand` (tolerante a fallo: si el logout remoto falla, la sesión local se borra igualmente).

#### GET `api/auth/verify`

- **Método HTTP:** `GET`
- **Ruta:** `api/auth/verify`
- **Descripción:** valida que el token sea vigente y devuelve los datos completos del usuario. Actualiza `ultimo_acceso`.
- **Request:** sin cuerpo; requiere `Authorization: Bearer <token>`.
- **Response:** `Usuario` (`Models/Entities/Usuario.cs`) con `getApiDataCompleto()` (misma estructura de `data` que el login, sin `token`).

```json
{
  "success": true,
  "data": {
    "id_usuario": 1,
    "username": "billetero",
    "tipo_usuario": "billetero",
    "id_cedis": null,
    "id_tienda": null,
    "estatus": "activo",
    "ultimo_acceso": "2026-08-04 12:05:00",
    "fecha_creacion": "2026-08-03 10:00:00",
    "cedis": { "id_cedis": 1, "nombre_cedis": "CEDIS Centro" },
    "tienda": { "id_tienda": 5, "nombre_tienda": "Tienda Centro" },
    "roles": [ { "id_rol": 1, "nombre_rol": "Billetero", "descripcion": "..." } ],
    "permisos": ["ventas.crear", "ventas.consultar"]
  }
}
```

- **Errores conocidos:**

| HTTP | `error` | Condición |
|---|---|---|
| 401 | `Token no proporcionado` | Falta el header Bearer |
| 401 | `Token invalido o expirado` | Token no coincide con un usuario activo |

- **Uso dentro de la app:** `AuthService.VerifyAsync` → `SplashPage` (auto-login).

#### POST `api/auth/refresh`

- **Método HTTP:** `POST`
- **Ruta:** `api/auth/refresh`
- **Descripción:** renueva el token de acceso generando uno nuevo para el usuario autenticado.
- **Request:** sin cuerpo; requiere `Authorization: Bearer <token>`.
- **Response:** nuevo token en `data`.

```json
{
  "success": true,
  "data": {
    "token": "<string aleatorio de 64 caracteres>"
  }
}
```

- **Errores conocidos:**

| HTTP | `error` | Condición |
|---|---|---|
| 401 | `Token no proporcionado` | Falta el header Bearer |
| 401 | `Token invalido o expirado` | Token no coincide con un usuario activo |
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
