# Arquitectura de MiCachito.Mobile

Documento de referencia técnica oficial para el desarrollo de la aplicación móvil **MiCachito.Mobile**.

Está dirigido a desarrolladores y a asistentes de IA. Describe únicamente el estado real de la arquitectura, tomado del código fuente y de la documentación existente; no describe funcionalidades futuras ni implementaciones inventadas.

---

## Estado del documento

- **Fecha de última actualización:** 2026-09-11
- **Commit del frontend analizado:** `c566350` (Facturación: flujo completo UI-only + correcciones de auditoría en working tree)
- **Commit de la rama `dev-mobile` utilizada como referencia:** `e7ad8f5` (Conexion con app y usuario de prueba)

> Este documento debe actualizarse cada vez que cambie la arquitectura, se agreguen módulos o se analice una versión distinta del código. Siempre registrar la fecha y los commits analizados.

### Cambios desde la última revisión (2026-08-04 → 2026-09-10)

- Fase **SOLO INTERFAZ** en curso: los módulos nuevos usan mock data estático en memoria (patrón `Data/XxxData.cs` para catálogos verbatim de mockups; services con registros temporales en memoria); la app NO está conectada al backend salvo Login (que funciona contra el backend real y persiste sesión).
- Pestaña **Gestión** completa: 8 módulos (Tickets de Venta, Devolución, Premios y Reintegros, Sorteos, Depósitos, Recibos de Pago, Reportes, Tira de Liquidación deshabilitada) + **Tiempo de Aire** ya implementado desde agosto (ver `docs/TIEMPO_AIRE_*.md`).
- **Reportes** completo: Estado de Cuenta + Fondo de Ahorro + Facturación (una página con tab bar interno de 3 pestañas).
- Nueva carpeta `Controls/` con `PieChartDrawable.cs` (gráfica circular con ICanvas nativo, sin paquetes).
- 4 servicios PDF con SkiaSharp (ReciboPago, EstadoDeCuenta, FondoAhorro, Facturacion) + TicketPdfService, y `DescargasService` (MediaStore, Android 10+; guard API<29 con respaldo a AppDataDirectory).
- Escáner QR integrado (CameraX + zxing-cpp nativo) — ver skill `micachito-mobile` / `references/camara-escaner-zxing.md`.
- `Helpers/FormatosFecha.cs`: única fuente de fechas en español (es-MX) — NUNCA CurrentCulture.

---

## 1. Arquitectura general

MiCachito.Mobile es una aplicación móvil desarrollada en **.NET MAUI** (multi-plataforma) que consume un backend **PHP Yii2** expuesto como API REST. Utiliza el patrón **MVVM** con **CommunityToolkit.Mvvm** e inyección de dependencias.

### 1.1 Organización general

La solución `MiCachito.Mobile.slnx` contiene un único proyecto (`.csproj` tipo `Microsoft.NET.Sdk`, `UseMaui=true`) con código compartido y una carpeta `Platforms/` con el arranque específico por sistema operativo.

Plataformas objetivo (`MiCachito.Mobile.csproj`):

- `net10.0-android`
- `net10.0-ios`
- `net10.0-maccatalyst`
- `net10.0-windows10.0.19041.0` (solo en Windows)

### 1.2 Patrón MVVM

El proyecto aplica MVVM con las siguientes reglas de capa (documentadas también en `docs/AI_CONTEXT.md`):

| Capa | Responsabilidad | Prohibido / Reglas |
|---|---|---|
| **Views** | Solo interfaz gráfica (XAML) y su code-behind mínimo | Sin lógica de negocio |
| **ViewModels** | Estado de pantalla, propiedades observables y Commands | Nunca realizan llamadas HTTP directamente |
| **Services** | Única capa que consume la API | Toda comunicación pasa por `IApiClient` |
| **Models** | Entidades y DTOs provenientes del backend | Solo transporte de datos |
| **Api** | Cliente HTTP, endpoints y clases REST | — |
| **Navigation** | Centraliza la navegación | Los ViewModels nunca usan `Shell`/`Page` concretos |
| **Helpers** | Utilidades y constantes reutilizables | Sin estado |

Las Views reciben su ViewModel por constructor (inyección de dependencias) y lo asignan como `BindingContext`.

> Las reglas detalladas de lo que cada capa puede y no puede hacer se resumen en la sección «Arquitectura por capas».

### 1.3 Flujo de comunicación entre capas

```
Views (eventos) ──► ViewModels (Commands) ──► Services ──► IApiClient ──► Backend Yii2 ──► MySQL
       ▲                  │                        │                              │
       └──────────────────┴────────────────────────┘  (respuestas deserializadas)
```

- Un comando del ViewModel invoca un servicio (`IAuthService`, `ISessionService`).
- El servicio envuelve una llamada a `IApiClient` indicando la ruta (`ApiEndpoints`) y el modelo request.
- `ApiClient` añade el token Bearer (vía `IAuthTokenProvider`), serializa en `snake_case` y deserializa el campo `data` de la respuesta.
- El resultado viaja de regreso al ViewModel, que actualiza sus propiedades observables; la View se actualiza por binding.

### 1.4 Dependencias principales

- .NET MAUI (`Microsoft.Maui.Controls`).
- `CommunityToolkit.Mvvm` (8.4.2): MVVM source generators.
- `Microsoft.Extensions.Http` (10.0.0): `HttpClientFactory` para el cliente tipado.
- `Microsoft.Extensions.Logging.Debug` (10.0.0): logging en Debug
- `Microsoft.Maui.Storage.SecureStorage` (parte de MAUI): almacenamiento seguro de la sesión

### Código relacionado

- `MiCachito.Mobile.csproj`
- `MauiProgram.cs`
- `App.xaml.cs`
- `AppShell.xaml` / `AppShell.xaml.cs`
- `docs/AI_CONTEXT.md`

---

## 2. Estructura del proyecto

| Carpeta | Propósito |
|---|---|
| `Api/` | Cliente HTTP tipado (`ApiClient`/`IApiClient`), rutas (`ApiEndpoints`), opciones JSON (`ApiJsonOptions`), excepción (`ApiException`), convertidores (`LoginResponseJsonConverter`, `ApiErrorConverter`) y contrato de token (`IAuthTokenProvider`). |
| `Services/` | Servicios de dominio que consumen la API. Hoy: `AuthService` (login/verify/logout/refresh) y `SessionService` (persistencia y proveedor de token). |
| `Models/` | DTOs. Subcarpetas: `Common/` (envelope de API), `Entities/` (usuario, cedis, tienda, rol), `Requests/` (login) y `Responses/` (login). También `SessionInfo.cs` en la raíz de la carpeta. |
| `ViewModels/` | ViewModels de las pantallas: `BaseViewModel` (base con `IsBusy`/`Title`), `LoginViewModel`, `HomeViewModel`. |
| `Views/` | Páginas XAML: `SplashPage`, `LoginPage`, `HomePage`. |
| `Navigation/` | `INavigationService`/`NavigationService`: reemplaza la página raíz de la ventana. |
| `Helpers/` | Constantes y utilidades: `AppSettings`, `Constants`, `Roles`, `StorageKeys`. |
| `Resources/` | Estilos (`Styles/`), imágenes, fuentes, ícono y splash de la app. |
| `Platforms/` | Código de arranque por plataforma (Android, iOS, MacCatalyst, Windows). |
| `Properties/` | `launchSettings.json` (perfiles de lanzamiento). |
| `docs/` | Documentación del proyecto (`AI_CONTEXT.md`, este documento). |

### 2.1 Notas sobre carpetas

- `Controls/` contiene `PieChartDrawable.cs` (gráfica circular de Facturación, ICanvas nativo).
- ~~`MainPage.xaml` / `MainPage.xaml.cs`~~ **eliminados** (2026-09-10, auditoría): eran restos de la plantilla "Hello World", sin registro en DI ni navegación. También se retiró `dotnet_bot.png` y su entrada en el csproj.
- Las subcarpetas `Models/Common`, `Models/Entities`, `Models/Requests`, `Models/Responses` existen y se usan.
- `Helpers/FormatosFecha.cs` (nuevo, 2026-09-10): única fuente de formato de fechas en español (es-MX) para pantalla y nombres de archivo PDF.
- `Data/` catálogos estáticos verbatim de mockups (fase solo-interfaz) y `Natives/` + `Interop/` (zxing-cpp del escáner QR). Incluye `Data/ExpendiosDemoData.cs` (módulo Expendios, datos demo en memoria de la relación usuario↔expendios).

---

## 3. Flujo de autenticación

### 3.1 Cómo inicia la aplicación

1. `MauiProgram.CreateMauiApp()` configura fonts, logging, DI y registra todos los servicios y páginas.
2. `App.CreateWindow()` crea la ventana inicial con **`SplashPage`** (obtenida del contenedor DI).
3. `SplashPage.OnAppearing()` (con guarda `_started`) decide el destino inicial.

### 3.2 Decisión inicial (auto-login)

`SplashPage` es una `ContentPage` sin ViewModel; la decisión de ruta vive en su code-behind:

```
OnAppearing()
  │
  ├─ Cargar sesión (ISessionService.LoadAsync)
  │     │
  │     ├─ Sin token ────────────────► NavigateToLoginAsync
  │     │
  │   └─ Con token ──► IAuthService.VerifyAsync() (GET api/auth/verify)
  │            │
  │            ├─ Usuario null ─► ClearAsync ─► NavigateToLoginAsync
  │            ├─ ApiException (401 token inválido/expirado)
  │            │        ─► ClearAsync ─► NavigateToLoginAsync
  │            └─ Otra excepción (sin conexión)
  │                     ─► NavigateToHomeAsync (sesión cacheada en memoria)
```

### 3.3 Login

`LoginViewModel.IngresarAsync` (`[RelayCommand]`):

1. Valida que `Usuario` y `Password` no estén vacíos; si no, muestra `Mensaje = "Ingrese usuario y contraseña"`.
2. Llama a `IAuthService.LoginAsync(usuario, password)` → `POST api/auth/login` con `LoginRequest {username, password}`.
3. Si la respuesta no trae `Usuario` o `Token`, muestra error genérico.
4. Guarda la sesión con `ISessionService.SaveAsync(response, rememberMe: false)`.
5. Navega al Home con `NavigateToHomeAsync()`.
6. `ApiException` → `Mensaje = ex.ServerMessage ?? "Error de autenticación. Intente de nuevo."`; cualquier otra excepción → "No se pudo conectar con el servidor. Intente más tarde."

### 3.4 Almacenamiento del token

`SessionService` implementa `ISessionService` e `IAuthTokenProvider`:

- Persiste la sesión completa como **un único blob JSON** (`SessionInfo`) en **`SecureStorage`** bajo la clave `"session"` (ver `StorageKeys.Session`), serializado en `camelCase`.
- `SessionInfo` contiene: `AccessToken`, `RefreshToken` (reservado, hoy siempre `null`), `Usuario`, `LoginAt`, `ExpiresAt` (hoy siempre `null`) y `RememberMe`.
- `LoadAsync()` cachea la sesión en memoria (`_currentSession`); si el JSON está corrupto, la descarta (`ClearAsync`).
- `GetTokenAsync()` (implementación de `IAuthTokenProvider`) devuelve el `AccessToken` actual. `ApiClient` lo inyecta como `Authorization: Bearer <token>` sin conocer los detalles de almacenamiento.

> Otras claves en `StorageKeys` (`access_token`, `refresh_token`, `usuario`, `login_at`, `expires_at`, `remember_me`) están **definidas pero no se utilizan**: el almacenamiento real es el blob único `"session"`.

### 3.5 Verificación de sesión

- Al arrancar: `GET api/auth/verify` (detallado en 3.2). En éxito, `UpdateUsuarioAsync` refresca los datos del usuario en la sesión persistida.
- El Home también puede re-leer la sesión cacheada (`SessionService.CurrentSession`).

### 3.6 Logout

`HomeViewModel.CerrarSesionAsync`:

1. `IAuthService.LogoutAsync()` → `POST api/auth/logout` (si falla, se ignora el error: la sesión local se borra igualmente).
2. `ISessionService.ClearAsync()` (borra SecureStorage y la caché en memoria).
3. `NavigateToLoginAsync()`.

### 3.7 Renovación del token

- `IAuthService.RefreshTokenAsync()` → `POST api/auth/refresh` existe en el cliente y en `ApiEndpoints.Auth.Refresh`.
- **Actualmente no está conectado a ningún ViewModel ni al `ApiClient`**: no hay reintento automático ante un 401.
- `SessionInfo.RefreshToken` y `SessionInfo.ExpiresAt` son campos reservados para cuando el backend lo implemente.

### 3.8 Servicios que participan

| Servicio | Rol |
|---|---|
| `AuthService` (`IAuthService`) | Envuelve los endpoints públicos de auth. |
| `SessionService` (`ISessionService`, `IAuthTokenProvider`) | Persiste/carga/limpia la sesión y provee el token Bearer. |
| `NavigationService` (`INavigationService`) | Cambia la página raíz entre Login y Home. |
| `ApiClient` (`IApiClient`) | Transporte HTTP con envelope y autenticación. |

### Código relacionado

- `MauiProgram.cs`
- `App.xaml.cs`
- `Views/SplashPage.xaml.cs`
- `ViewModels/LoginViewModel.cs`
- `ViewModels/HomeViewModel.cs`
- `Services/AuthService.cs`
- `Services/SessionService.cs`
- `Navigation/NavigationService.cs`
- `Api/ApiClient.cs`
- `Models/SessionInfo.cs`

---

## 4. Comunicación con el backend

### 4.1 ApiClient

`ApiClient` es un **cliente HTTP tipado** registrado con `AddHttpClient<IApiClient, ApiClient>` en `MauiProgram`:

- `BaseAddress` = `AppSettings.BaseUrl`.
- `Timeout` = `AppSettings.TimeoutSeconds` (30 segundos).
- `Accept: application/json` por defecto.

Expone `GetAsync<T>` y `PostAsync<T>`. En `SendAsync`:

1. Añade `Accept: application/json`.
2. Obtiene el token vía `IAuthTokenProvider` y, si existe, añade `Authorization: Bearer <token>`.
3. Serializa el cuerpo en `snake_case` con `ApiJsonOptions.Default`.
4. Envía la petición y lee el cuerpo de la respuesta.
5. **HTTP no 2xx** → deserializa el envelope y lanza `ApiException(StatusCode, error ?? message, errors)`.
6. **2xx con `success=false`** → lanza `ApiException` con los mismos campos.
7. En éxito, devuelve **`apiResponse.Data`** deserializado en `T`.

### 4.2 Interfaces utilizadas

- `IApiClient`: `GetAsync<T>`, `PostAsync<T>`.
- `IAuthTokenProvider`: `GetTokenAsync()` (implementado por `SessionService`).
- `IAuthService`: `LoginAsync`, `VerifyAsync`, `LogoutAsync`, `RefreshTokenAsync`.
- `ISessionService`: `CurrentSession`, `SaveAsync`, `LoadAsync`, `UpdateUsuarioAsync`, `ClearAsync`.

### 4.3 Servicios existentes

- `AuthService` (login, verify, logout, refresh).
- `SessionService` (sesión + token).

### 4.4 Endpoints consumidos

`ApiEndpoints.Auth` (todas las rutas relativas a la URL base):

| Método | Ruta | Uso |
|---|---|---|
| `POST` | `api/auth/login` | `AuthService.LoginAsync` |
| `POST` | `api/auth/logout` | `AuthService.LogoutAsync` |
| `GET` | `api/auth/verify` | `AuthService.VerifyAsync` |
| `POST` | `api/auth/refresh` | `AuthService.RefreshTokenAsync` (sin conectar) |

### 4.5 Manejo de errores

- **`ApiException`**: expone `StatusCode` (`HttpStatusCode`), `ServerMessage` (campo `error` del backend) y `Errors` (`IReadOnlyList<ApiError>`). El mensaje base cae a un texto por defecto si el backend no envía `error`.
- **`ApiErrorConverter`**: normaliza el campo `errors` que Yii2 puede enviar como objeto (`{campo: ["msg"]}`) o arreglo (`["msg"]`), produciendo una lista de `ApiError {Field, Message}`.
- **`ApiError`**: `Field`, `Message`, `ToString()` legible.

### 4.6 Serialización JSON

`ApiJsonOptions` (interno):

- `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower` (coincide con la nomenclatura del backend).
- `PropertyNameCaseInsensitive = true`.
- `DefaultIgnoreCondition = WhenWritingNull`.
- Converters registrados: `ApiErrorConverter` y `LoginResponseJsonConverter`.

`LoginResponseJsonConverter`: el backend responde login con un objeto plano (campos del usuario + `token`). El converter deserializa el resto como `Usuario` (los campos desconocidos, incluido `token`, se ignoran) y extrae `token` por separado en `LoginResponse {Usuario, Token}`.

### 4.7 Envelope de respuesta (`ApiResponse<T>`)

```
Éxito: {"success": true, "data": {...}, "message": "opcional"}
Error: {"success": false, "error": "mensaje", "errors": [...]}   (errors solo en validaciones 422)
```

`ApiClient` devuelve únicamente `data`; los errores se lanzan como `ApiException`.

### 4.8 Configuración de la URL base

La URL base se obtiene desde `AppSettings.BaseUrl` (`Helpers/AppSettings.cs`), punto único de configuración del endpoint del backend. El valor corresponde únicamente al entorno de desarrollo analizado y puede cambiar según el ambiente (QA, producción). También define `TimeoutSeconds` y `AppName`.

### Código relacionado

- `Api/ApiClient.cs`
- `Api/IApiClient.cs`
- `Api/ApiEndpoints.cs`
- `Api/ApiJsonOptions.cs`
- `Api/ApiException.cs`
- `Api/ApiErrorConverter.cs`
- `Api/LoginResponseJsonConverter.cs`
- `Api/IAuthTokenProvider.cs`
- `Services/AuthService.cs`
- `Helpers/AppSettings.cs`

---

## 5. Navegación

### 5.1 Enfoque

`NavigationService` **no usa `Shell.GoToAsync`** para el cambio Login ↔ Home: como Home vive dentro de un `AppShell` y Login es una `ContentPage` independiente, la navegación se hace **reemplazando la página raíz de la ventana activa** (`Application.Current.Windows[0].Page`).

- `NavigateToHomeAsync()` → `Window.Page = AppShell`.
- `NavigateToLoginAsync()` → `Window.Page = LoginPage`.

Los ViewModels dependen solo de `INavigationService`, nunca de `Shell`/`Page` concretos.

### 5.2 Shell

`AppShell` (`FlyoutBehavior="Disabled"`) contiene **un único `ShellContent`**:

- Título: "Inicio".
- Ruta: `HomePage`.
- Contenido: `HomePage` (inyectada por constructor).

No hay más rutas registradas en el Shell.

### 5.3 Rutas registradas

- Shell: `HomePage` (única).
- Páginas raíz manejadas por `NavigationService`: `SplashPage` (arranque), `LoginPage`, `AppShell`.

### 5.4 Navegación pública (sin sesión)

```
SplashPage ──► LoginPage
HomePage (logout) ──► LoginPage
```

### 5.5 Navegación autenticada

```
SplashPage (token válido / sin conexión con sesión cacheada) ──► AppShell → HomePage
LoginPage (login exitoso) ──► AppShell → HomePage
```

### Código relacionado

- `AppShell.xaml` / `AppShell.xaml.cs`
- `Navigation/INavigationService.cs`
- `Navigation/NavigationService.cs`
- `App.xaml.cs`

---

## 6. Estado actual

### 6.1 Pantallas

| Pantalla | ViewModel | Estado |
|---|---|---|
| `Views/SplashPage` | Sin ViewModel (lógica en code-behind) | Activa: decisión de auto-login |
| `Views/LoginPage` | `LoginViewModel` | Activa: login completo |
| `Views/HomePage` | `HomeViewModel` | Activa: muestra usuario/tipo/sede y logout |
| `MainPage` | — | Resto de plantilla, sin usar |

### 6.2 ViewModels

- `BaseViewModel` (`IsBusy`, `Title`).
- `LoginViewModel` (`Usuario`, `Password`, `Mensaje`, `IngresarCommand`).
- `HomeViewModel` (`Username`, `TipoUsuario`, `Sede`, `LoadAsync`, `CerrarSesionCommand`).

### 6.3 Servicios

- `AuthService` / `IAuthService`.
- `SessionService` / `ISessionService` / `IAuthTokenProvider`.
- `NavigationService` / `INavigationService`.

### 6.4 Funcionalidades completas

- Login con usuario/contraseña contra el backend.
- Persistencia de sesión en `SecureStorage`.
- Auto-login al arrancar (verify) y entrada offline con sesión cacheada.
- Logout (remoto tolerante a fallo + limpieza local).
- Home básico con datos del usuario (username, tipo, sede CEDIS/tienda).
- Cliente HTTP con envelope, errores normalizados y token Bearer automático.

### 6.5 Partes que son únicamente estructura

- `Controls/` vacía.
- `api/auth/refresh` implementado en cliente pero **sin conectar** a ningún flujo.
- `SessionInfo.RefreshToken` / `ExpiresAt` reservados (siempre `null`).
- Claves `StorageKeys` no utilizadas (`access_token`, `refresh_token`, `usuario`, `login_at`, `expires_at`, `remember_me`).
- `MainPage` (plantilla).
- `Helpers/Roles` con nombres de roles definidos pero sin lógica de UI por rol aún.

### Código relacionado

- `Views/SplashPage.xaml`, `Views/LoginPage.xaml`, `Views/HomePage.xaml`
- `ViewModels/BaseViewModel.cs`, `ViewModels/LoginViewModel.cs`, `ViewModels/HomeViewModel.cs`
- `Services/AuthService.cs`, `Services/SessionService.cs`
- `MainPage.xaml` / `MainPage.xaml.cs`

---

## 7. Dependencias

Referencias de `MiCachito.Mobile.csproj`:

| Paquete | Versión | Uso |
|---|---|---|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` (source generators). Base de todos los ViewModels. |
| `Microsoft.Extensions.Http` | 10.0.0 | `AddHttpClient<IApiClient, ApiClient>`: ciclo de vida y configuración del `HttpClient` tipado. |
| `Microsoft.Maui.Controls` | `$(MauiVersion)` | Framework MAUI (UI, Shell, páginas, estilos). |
| `Microsoft.Extensions.Logging.Debug` | 10.0.0 | Logging a la consola de Debug (`builder.Logging.AddDebug()`). |

Además (parte de MAUI, sin referencia explícita en el csproj):

- `Microsoft.Maui.Storage.SecureStorage` (uso en `SessionService`)
- `Microsoft.Maui.Controls.Application` (`Application.Current`, usado en `NavigationService`)

### Código relacionado

- `MiCachito.Mobile.csproj`
- `MauiProgram.cs`

---

## 8. Backend (`dev-mobile`)

### 8.1 Repositorio y rama de referencia

- Repositorio: `globaloxs-sys/MiCachito-backend.git`.
- Rama analizada: **`dev-mobile`**.
- Commit de referencia: `e7ad8f5` (Conexion con app y usuario de prueba).
- Estructura: aplicación Yii2 (`config/web.php` id `"basic"`), API bajo `controllers/api/`, rutas definidas en `config/web.php`, base de datos MySQL.

> El backend NO es una copia del módulo web: la API es la fuente de datos única para la app móvil. Toda funcionalidad móvil debe existir primero en esta rama.

### 8.2 Autenticación

Los cuatro endpoints públicos (sin Bearer). Definidos en `AuthController` y en las rutas de `config/web.php`:

#### POST `api/auth/login`

- Cuerpo: `{"username": "...", "password": "..."}` (parser JSON de Yii).
- Valida campos requeridos (400), usuario/contraseña (401), estatus bloqueado/inactivo (403).
- Éxito: genera token de acceso, actualiza `ultimo_acceso` y responde con `data` = `getApiDataCompleto()` + `token`.

#### POST `api/auth/logout`

- Requiere header Bearer (opcional: si no hay token responde éxito igualmente).
- Invalida el `access_token` del usuario y responde `{"success": true, "message": "Sesion cerrada exitosamente"}`.

#### GET `api/auth/verify`

- Requiere `Authorization: Bearer <token>`; si falta o es inválido → 401.
- Actualiza `ultimo_acceso` y responde `data` = `getApiDataCompleto()`.

#### POST `api/auth/refresh`

- Requiere Bearer; inválido → 401.
- Genera un nuevo token y responde `{"success": true, "data": {"token": "<nuevo>"}}`.

### 8.3 Infraestructura de la API (`BaseApiController`)

Todos los controladores de `controllers/api` heredan de `BaseApiController` (`yii\rest\Controller`):

- **Formato**: siempre `application/json`.
- **CORS**: origen `*`, métodos GET/POST/PUT/PATCH/DELETE/HEAD/OPTIONS, headers `*`, credenciales permitidas.
- **Autenticación**: `HttpBearerAuth` para todas las acciones **excepto** `getPublicActions()` (por defecto `options`; `AuthController` agrega `login`, `logout`, `verify`, `refresh`).
- **Helpers de respuesta**: `success($data, $message, $status)`, `error($message, $status, $errors)`, `validationError($errors)` (422), `notFound($recurso)` (404).
- **Permisos**: `tienePermiso($permiso)` (formato `modulo.accion`), `requirePermiso`, `requireAnyPermiso`.
- **Scoping por sede**: `scopeQueryPorSede($query, ...)` filtra según `tipo_usuario`/`id_cedis`/`id_tienda` del usuario autenticado (Corporativo ve todo; CEDIS ve su CEDIS + tiendas; tienda solo su tienda). Soporta filtro adicional por `?id_zona=X` y la zona lógica CORPORATIVO.
- **Otros**: `requireSede()`, `getUsuarioId()`, `registrarBitacora(...)` (componente `BitacoraComponent`), `resolveTurnoAlmacen(...)`, `actionOptions()` para preflight CORS.

### 8.4 Modelos relevantes para la autenticación

- `User` (tabla `usuarios`): `id_usuario`, `username`, `password_hash`, `access_token`, `tipo_usuario`, `id_cedis`, `id_tienda`, `estatus` (activo/inactivo/bloqueado), `ultimo_acceso`, `fecha_creacion`, `fecha_modificacion`. Implementa `IdentityInterface`. Métodos clave: `findByUsername`, `findIdentityByAccessToken`, `validatePassword`, `generateAccessToken` (string aleatorio de 64 caracteres), `invalidateAccessToken`, `tieneRol`, `tienePermiso`, `getApiData`, `getApiDataCompleto` (agrega `cedis`, `tienda` y `roles` como objetos, y `permisos` como identificadores `modulo.accion`).
- `Rol` (tabla `roles`) y `RolPermiso` (tabla `roles_permisos`): roles y permisos.
- `UsuarioRol` (tabla `usuarios_roles`): relación usuario ↔ rol.
- `Cedis` (tabla `cedis`) y `TiendasSucursales` (tabla `tiendas_sucursales`): sedes del usuario.
- `LoginForm`: existe en `models/`, pero `AuthController` **no lo usa** (valida manualmente `username`/`password`).

### 8.5 Respuestas estándar y manejo de errores

Envelope común (mismo que espera la app móvil):

```
Éxito:  {"success": true, "data": {...}, "message": "opcional"}
Error:  {"success": false, "error": "mensaje"}            → 400/401/403/404/500
        {"success": false, "error": "Error de validacion",
         "errors": {campo: ["mensaje"]}}                  → 422
```

Toda la API registra acciones en bitácora (`BitacoraComponent`) y protege los endpoints restantes con Bearer (401) y permisos (403).

### 8.6 Controladores y endpoints existentes (estado actual)

Existen **78 controladores** bajo `controllers/api/`. Los dominios actuales incluyen:

- **Auth**: `AuthController` (login, logout, verify, refresh).
- **Usuarios/Administración**: `UsuariosController`, `AdministracionController`, `AdministracionMovimientosController`.
- **Almacén**: `AlmacenController`, `AlmacenCierreTurnoController`, `MovimientosAlmacenController`, `DotacionNumerosController`, `PaquetesRecibidosController`, `PedidosController`, `ProcesamientoDevolucionController`.
- **Caja**: `CajaController`, `CajaCierreTurnoController`, `CajaTransferenciasController`.
- **Ventas**: `VentasController`, `VentasCierreTurnoController`, `VentasCorteController`, `VentasElectronicasController`, `VentasTransferenciasController`, `DevolucionesTiendaController`.
- **Premios / Billetes / Sorteos**: `PremiosController`, `BilletesController`, `SorteosController`, `SorteosBilleteroController`, `SorteosTecController`, `CierresSorteoController`, `ConciliacionesController`, `ConciliacionPronosticosController`.
- **Catálogos**: `CedisController`, `TiendasController`, `ZonasController`, `BilleterosController`, `ProductosController`, `FamiliasProductoController`, `ClientesController`, `ProveedoresController`, `ColaboradoresController`, `CuentasBancariasController`, `FichasBancariasController`.
- **Reportes**: `ReportesController` y ~20 controladores `Reportes*Controller`.
- **Contabilidad / Gastos / Arqueos / Monitores / Otros**: `GastosController`, `GastosProveedoresController`, `ComprobacionesPagosController`, `ComprobacionPronosticosController`, `ArqueosController`, `MonitoresController`, `ContCedisClientesController`, `GuiasController`, `GuiasPersonasController`, `VolantesInstantaneaController`, `VolantesNacionalController`, `TraspasoLnZonasController`, `UbicacionNumerosController`, `UniversitariosController`, `SolicitudesEliminacionController`.

El catálogo completo de rutas se define en `config/web.php` (URLs limpias, `showScriptName=false`) y existe documentación adicional en `back/docs/CAMBIOS_ENDPOINTS.md` y la colección Postman `back/docs/MiCachito_API.postman_collection.json`.

### 8.7 Cambios específicos de `dev-mobile` vs `dev`

| Cambio | Descripción |
|---|---|
| Migración `m260802_000001_convert_tipo_usuario_to_varchar` | Convierte `usuarios.tipo_usuario` de ENUM a VARCHAR(50) para permitir tipos nuevos sin alterar el esquema. |
| Migración `m260803_000003_seed_usuario_billetero` | Crea el usuario de prueba `billetero` / `billetero123`, `tipo_usuario='billetero'`, `estatus='activo'`, para que la app móvil inicie sesión durante el desarrollo. |
| `models/User.php` | Elimina la constante `TIPO_BILLETERO`, la regla de validación `in` y su entrada en `getTiposUsuario()`. |

> Nota: `User.php` ya no incluye `'billetero'` en la regla `in` de `tipo_usuario`, mientras que la migración de seed inserta el usuario con ese tipo. El seed se aplica por SQL (sin validación del modelo), por lo que funciona, pero es una inconsistencia a vigilar.

### 8.8 Actualización del documento

Este apartado documenta el estado del backend en el commit de referencia. **Debe actualizarse conforme se agreguen nuevos módulos al backend**: cada nueva funcionalidad móvil requiere que su endpoint exista en `dev-mobile` antes de conectar la pantalla, y debe quedar registrado aquí (método, ruta, request, response y errores).

### Código relacionado

- `controllers/api/AuthController.php`
- `controllers/api/BaseApiController.php`
- `models/User.php`
- `config/web.php`
- `components/BitacoraComponent.php`
- `migrations/m260802_000001_convert_tipo_usuario_to_varchar.php`
- `migrations/m260803_000003_seed_usuario_billetero.php`

---

## 9. Diagrama de arquitectura

```
┌──────────────────────────────────────────────────────────────────┐
│                    MiCachito.Mobile (.NET MAUI)                  │
│                                                                  │
│   App (App.xaml.cs)  ──►  CreateWindow → SplashPage              │
│        │                                                         │
│        ▼                                                         │
│   Views (XAML)                                                   │
│     SplashPage · LoginPage · HomePage                            │
│        ▲  BindingContext                                         │
│        │                                                         │
│        ▼                                                         │
│   ViewModels                                                     │
│     BaseViewModel · LoginViewModel · HomeViewModel               │
│        │  Commands (nunca HTTP directo)                          │
│        ▼                                                         │
│   Services                                                       │
│     AuthService (IAuthService) · SessionService (ISessionService)│
│        │                                                         │
│        ▼                                                         │
│   ApiClient (IApiClient)  ── Bearer token (IAuthTokenProvider)   │
│     ApiEndpoints · ApiJsonOptions · ApiException                 │
│        │  HTTP JSON (snake_case)                                 │
└────────┼─────────────────────────────────────────────────────────┘
         ▼
   Backend PHP Yii2 — rama dev-mobile
     controllers/api (BaseApiController: Bearer + CORS + envelope)
     AuthController: login · logout · verify · refresh
     config/web.php (URL rules)
     models (User, Rol, ...)
         │
         ▼
      MySQL
```

**Flujo típico:**

```
Arranque → SplashPage
  │
  ├─ Sin sesión ──────────────► LoginPage ──(login)──► AppShell → HomePage
  │
  └─ Con sesión ── verify ──► AppShell → HomePage ──(logout)──► LoginPage
```

---

## 10. Recomendaciones

### 10.1 Mejoras de arquitectura

- **Conectar `api/auth/refresh`**: usar el token de refresco ante un 401 (reintentar la petición una vez con token nuevo) en lugar de forzar logout. Hoy el flujo solo borra la sesión.
- **Mover la decisión de auto-login fuera del code-behind**: `SplashPage` contiene lógica de flujo; puede migrarse a un ViewModel o a un servicio de arranque para mantener MVVM.
- **Registrar rutas de Shell para el futuro**: `NavigationService` reemplaza `Window.Page`; si crece el número de pantallas autenticadas, conviene definir las rutas en `AppShell` y navegar con `Shell.GoToAsync`.
- **Configuración por ambiente**: `AppSettings.BaseUrl` es una constante; conviene separarla por configuración (Debug/Release/QA/Producción).
- **Validación de entrada**: el login valida campos vacíos a mano; se puede usar el validador de CommunityToolkit.Mvvm (`[ObservableProperty]` + reglas).
- **Quitar el código muerto**: eliminar o migrar `MainPage.*` (plantilla) y definir el destino de `Controls/`.

### 10.2 Riesgos técnicos

- **Transporte sin TLS**: la URL base es HTTP (desarrollo en LAN). En Android 9+ el tráfico cleartext está deshabilitado por defecto; el `AndroidManifest.xml` no declara `usesCleartextTraffic`, por lo que el despliegue real deberá usar HTTPS o una config de red explícita.
- **Token sin expiración y sin renovación automática**: el token se invalida solo con logout; si se filtra, tiene validez indefinida.
- **Sesión cacheada offline**: al no haber conexión, la app entra al Home con la sesión en memoria; hay que definir qué hacer cuando expira realmente.
- **Fechas como texto**: `ultimo_acceso`/`fecha_creacion` se reciben como string SQL; cualquier comparación de fechas requiere parseo.
- **Inconsistencia `billetero`**: el modelo `User` ya no valida `'billetero'` en la regla `in`, pero el seed lo usa (ver 8.7).

### 10.3 Código duplicado / componentes faltantes

- `StorageKeys` define claves que no se usan (el almacenamiento es un blob único) → limpiar o documentar su propósito.
- No hay servicio de manejo de errores centralizado (toast/alerta); cada ViewModel muestra mensaje propio.
- No hay `ApiEndpoints` más allá de Auth; los futuros módulos deben agruparse ahí (Ventas, Premios, etc.), tal como indica el comentario de la clase.
- `Controls/` está vacía: definir los primeros componentes reutilizables cuando aparezca duplicación real de UI.

### 10.4 Buenas prácticas

- Mantener la regla: **Views sin lógica, ViewModels sin HTTP, toda comunicación vía `IApiClient`**.
- **No implementar nuevos patrones arquitectónicos sin documentarlos previamente en este archivo** (Repository, Mediator, CQRS, etc.). Antes de introducir cualquier patrón nuevo, registrar aquí el criterio, el problema que resuelve y el alcance.
- Toda funcionalidad nueva debe validarse contra `dev-mobile` antes de conectarse; no inventar endpoints ni datos mock.
- Actualizar el apartado **Estado del documento** (fecha y commits) en cada revisión.
- Registrar en **Decisiones arquitectónicas** cada decisión de diseño que se tome a lo largo del proyecto.

---

## Dependencias entre componentes

### Dependencias de inyección (registradas en `MauiProgram`)

| Componente | Registro |
|---|---|
| `IApiClient` → `ApiClient` | `AddHttpClient` (transient) |
| `IAuthService` → `AuthService` | Transient |
| `ISessionService` → `SessionService` | Singleton |
| `IAuthTokenProvider` → `SessionService` | Singleton |
| `INavigationService` → `NavigationService` | Singleton |
| `LoginPage` → `LoginViewModel` | Transient |
| `HomePage` → `HomeViewModel` | Transient |
| `SplashPage`, `AppShell` | Transient |

### Flujo de inicio y auto-login

```
App (App.xaml.cs)
  ↓ DI
SplashPage
  ↓
ISessionService → SessionService (lee SecureStorage)
  ↓
IAuthService → AuthService
  ↓
IApiClient → ApiClient
  ↓
Backend Yii2 (dev-mobile)
```

### Flujo de login

```
LoginPage
  ↓ BindingContext
LoginViewModel
  ↓
IAuthService → AuthService
  ↓
IApiClient → ApiClient (POST api/auth/login)
  ↓
ISessionService → SessionService (guarda sesión)
  ↓
INavigationService → NavigationService (Window.Page = AppShell)
```

### Flujo de logout

```
HomePage
  ↓ BindingContext
HomeViewModel
  ↓
IAuthService → AuthService (POST api/auth/logout)
  ↓
ISessionService → SessionService (ClearAsync)
  ↓
INavigationService → NavigationService (Window.Page = LoginPage)
```

### Peticiones autenticadas (token)

```
ApiClient
  ↓
IAuthTokenProvider → SessionService → SecureStorage (lee "session")
  ↓
Authorization: Bearer <token>
```

---

## Arquitectura por capas

Lo permitido y prohibido en cada capa, según el código actual.

### Views

- ✔ Mostrar UI y bindear propiedades/Commands del ViewModel
- ✔ Recibir su ViewModel por constructor (DI) y asignarlo como `BindingContext`
- ✘ Realizar llamadas HTTP directas
- ✘ Contener lógica de negocio (excepción actual: `SplashPage` decide el auto-login en su code-behind usando servicios)

### ViewModels

- ✔ Usar Services (`IAuthService`, `ISessionService`, `INavigationService`)
- ✔ Usar Models (`Usuario`, `LoginResponse`, `SessionInfo`)
- ✘ Usar `HttpClient` o realizar llamadas HTTP
- ✘ Acceder directamente al backend
- ✘ Referenciar páginas/Shell concretos

### Services

- ✔ Consumir la API vía `IApiClient`
- ✔ Manejar el estado de sesión (`SessionService`)
- ✘ Ejecutar lógica de UI
- ✘ Manejar la navegación directamente

### Api

- ✔ Definir endpoints (`ApiEndpoints`) y serialización (`ApiJsonOptions`)
- ✔ Normalizar errores (`ApiException`, `ApiErrorConverter`)
- ✘ Contener lógica de negocio
- ✘ Manejar estado de sesión

### Models

- ✔ Representar DTOs de entrada/salida del backend
- ✘ Contener lógica de negocio
- ✘ Realizar llamadas HTTP

### Navigation

- ✔ Cambiar la página raíz de la ventana
- ✘ Contener lógica de negocio

### Helpers

- ✔ Proveer constantes y utilidades reutilizables
- ✘ Mantener estado mutable

---

## Estado del proyecto

| Componente | Estado | Observaciones |
|---|---|---|
| Login (`POST api/auth/login`) | Completo | `LoginViewModel` → `AuthService` → `ApiClient` |
| Persistencia de sesión | Completo | Blob JSON (`SessionInfo`) en `SecureStorage`, clave `"session"` |
| Auto-login / verificación | Completo | `SplashPage` valida el token con `api/auth/verify` |
| Entrada offline | Completo | Sin conexión entra al Home con la sesión cacheada |
| Logout | Completo | Remoto tolerante a fallo + limpieza local |
| Home con datos del usuario | Completo | `Username`, `TipoUsuario`, `Sede` (CEDIS/Tienda) |
| Cliente HTTP (`ApiClient`) | Completo | Envelope, snake_case, Bearer automático |
| Backend auth (`dev-mobile`) | Completo | `login`, `logout`, `verify`, `refresh` en `AuthController` |
| Renovación de token | Parcial | `RefreshTokenAsync` y endpoint existen; sin conectar a ningún flujo |
| Navegación Shell | Parcial | Un solo `ShellContent` ("Inicio"); el resto vía `Window.Page` |
| Roles para UI | Parcial | Constantes en `Helpers/Roles`; sin lógica de UI por rol |
| Otros módulos del backend | Pendiente | 78 controladores existentes; se documentan conforme avance el desarrollo móvil |
| `Controls/` | Pendiente | Carpeta vacía |
| `MainPage` (plantilla) | Pendiente | Sin usar, no registrada en DI |

---

## Árbol del proyecto

```
MiCachito.Mobile
├── Api
│   ├── ApiClient.cs
│   ├── ApiEndpoints.cs
│   ├── ApiErrorConverter.cs
│   ├── ApiException.cs
│   ├── ApiJsonOptions.cs
│   ├── IApiClient.cs
│   ├── IAuthTokenProvider.cs
│   └── LoginResponseJsonConverter.cs
├── Services
│   ├── AuthService.cs
│   ├── IAuthService.cs
│   ├── ISessionService.cs
│   └── SessionService.cs
├── Models
│   ├── SessionInfo.cs
│   ├── Common
│   │   ├── ApiError.cs
│   │   └── ApiResponse.cs
│   ├── Entities
│   │   ├── CedisInfo.cs
│   │   ├── RolInfo.cs
│   │   ├── TiendaInfo.cs
│   │   └── Usuario.cs
│   ├── Requests
│   │   └── LoginRequest.cs
│   └── Responses
│       └── LoginResponse.cs
├── ViewModels
│   ├── BaseViewModel.cs
│   ├── HomeViewModel.cs
│   └── LoginViewModel.cs
├── Views
│   ├── HomePage.xaml (+ .cs)
│   ├── LoginPage.xaml (+ .cs)
│   └── SplashPage.xaml (+ .cs)
├── Navigation
│   ├── INavigationService.cs
│   └── NavigationService.cs
├── Helpers
│   ├── AppSettings.cs
│   ├── Constants.cs
│   ├── Roles.cs
│   └── StorageKeys.cs
├── Resources
│   └── Styles
│       ├── Colors.xaml
│       └── Styles.xaml
├── Platforms (Android, iOS, MacCatalyst, Windows)
├── App.xaml (+ .cs)
├── AppShell.xaml (+ .cs)
├── MainPage.xaml (+ .cs)    ← plantilla sin usar
├── MauiProgram.cs
└── docs
    ├── AI_CONTEXT.md
    └── Arquitectura.md
```

---

## Decisiones arquitectónicas

Decisiones ya tomadas y adoptadas en el código. Esta sección documenta únicamente decisiones reales y debe ampliarse conforme el proyecto crezca.

- **MVVM estricto**: las Views solo contienen UI; los ViewModels manejan estado y comandos.
- **No se realizan llamadas HTTP desde las Views**: la única capa de consumo de API son los Services.
- **Toda comunicación pasa por `IApiClient`**: ningún servicio o ViewModel crea `HttpClient` propio ni llama a la API por otro medio.
- **SessionService centraliza el manejo de la sesión**: implementa `ISessionService` e `IAuthTokenProvider`; ni los ViewModels ni `ApiClient` conocen los detalles de `SecureStorage`.
- **Los ViewModels dependen de interfaces** (`IAuthService`, `ISessionService`, `INavigationService`, `IApiClient`), nunca de implementaciones o páginas concretas.
- **El backend de referencia es exclusivamente la rama `dev-mobile`** del repositorio `globaloxs-sys/MiCachito-backend`; no se usan datos de otras ramas.
- **La serialización con el backend es `snake_case`** y la respuesta se envuelve en el envelope `{success, data, error, errors}`.
- **El token se envía como `Authorization: Bearer <token>`** inyectado automáticamente por `ApiClient`.
- **La navegación raíz se hace reemplazando `Window.Page`** (no `Shell.GoToAsync`), porque Login y Home son raíces distintas (ContentPage vs Shell).
- **Los errores del backend se normalizan en `ApiException`** con `StatusCode`, `ServerMessage` y `Errors` listos para la UI.
- **Inyección de dependencias por constructor**: Views y ViewModels reciben sus dependencias desde el contenedor registrado en `MauiProgram`.
- **La sesión se persiste como un único blob JSON** (`SessionInfo`) en `SecureStorage` bajo la clave `"session"`; no se usan claves separadas por campo.
- **El cliente devuelve únicamente el campo `data`**: los errores HTTP o `success=false` se lanzan como `ApiException`.
- **El token de acceso es opaco**: lo genera el backend (64 caracteres aleatorios) y se transporta como `Authorization: Bearer`; el cliente no lo interpreta.
- **La app móvil no utiliza datos mock**: toda la información mostrada proviene del backend `dev-mobile` (regla establecida en `docs/AI_CONTEXT.md`).
