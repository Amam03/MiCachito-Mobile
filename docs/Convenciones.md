# Convenciones de MiCachito.Mobile

Guía de estilo y de implementación para el desarrollo de la aplicación móvil **MiCachito.Mobile**.

Está dirigida a desarrolladores y a asistentes de IA. Define **cómo se escribe código en este proyecto**: nombres, organización de carpetas, reglas MVVM, XAML, C#, servicios y reglas de trabajo.

Documentos relacionados:

- `docs/Arquitectura.md` — describe el estado real de la arquitectura (qué existe). Este documento complementa esa guía con las convenciones (cómo se hace).
- `docs/AI_CONTEXT.md` — reglas de trabajo y contexto del proyecto.
- `docs/API.md` — documentación de los endpoints del backend que consume la app. **Debe consultarse antes de integrar cualquier servicio nuevo.**

> Si una convención aquí descrita contradice la arquitectura documentada en `docs/Arquitectura.md`, prevalece la arquitectura real del código: corregir la convención y notificar el cambio.

---

## 1. Convenciones de nombres

### 1.1 Pages / Views

- Nomenclatura: `<Nombre>Page`, en **PascalCase**, en singular.
- Ubicación: carpeta `Views/`, con dos archivos por pantalla: `<Nombre>Page.xaml` y `<Nombre>Page.xaml.cs` (code-behind).
- `x:Class` debe ser `MiCachito.Mobile.Views.<Nombre>Page`.
- `Title` de la página en español, con la primera letra en mayúscula.
- Ejemplos reales: `SplashPage`, `LoginPage`, `HomePage`.

### 1.2 ViewModels

- Nomenclatura: `<Nombre>ViewModel`, en **PascalCase**.
- Ubicación: carpeta `ViewModels/`.
- Todos heredan de `BaseViewModel` (que hereda de `ObservableObject`), salvo justificación.
- Espacio de nombres: `MiCachito.Mobile.ViewModels`.
- Ejemplos reales: `BaseViewModel`, `LoginViewModel`, `HomeViewModel`.

### 1.3 Models

- Nomenclatura: nombre de dominio en **PascalCase**.
- DTOs de entidad provenientes del backend: sufijo `Info` (`CedisInfo`, `TiendaInfo`, `RolInfo`) o nombre de dominio directo (`Usuario`).
- Agrupación por propósito en subcarpetas de `Models/`:
  - `Models/Entities/` — entidades del backend (usuario, cedis, tienda, rol).
  - `Models/Requests/` — cuerpos de peticiones (`LoginRequest`).
  - `Models/Responses/` — respuestas específicas (`LoginResponse`).
  - `Models/Common/` — clases transversales del contrato API (`ApiResponse<T>`, `ApiError`).
  - `Models/` (raíz) — clases de dominio de la app (`SessionInfo`).
- Propiedades en PascalCase. El cliente serializa automáticamente a `snake_case`, por lo que las propiedades deben coincidir en nombre con los campos del backend (`IdUsuario` → `id_usuario`).

### 1.4 Services

- Nomenclatura: interfaz `I<Dominio>Service` + implementación `<Dominio>Service`, en archivos separados dentro de `Services/`.
- Un servicio por dominio de negocio; no crear un servicio por endpoint.
- Ejemplos reales: `IAuthService`/`AuthService`, `ISessionService`/`SessionService`.

### 1.5 Commands

- Método privado marcado con `[RelayCommand]`, `async Task <Accion>Async(...)`.
- El command público resultante se nombra `<Accion>Command` (lo genera `CommunityToolkit.Mvvm`).
- Ejemplos reales: `IngresarAsync` → `IngresarCommand`, `CerrarSesionAsync` → `CerrarSesionCommand`.

### 1.6 Propiedades observables

- Campo privado `[ObservableProperty]` en **camelCase**; la propiedad pública se genera en **PascalCase**.
- Inicializar con `string.Empty` en lugar de `null`.
- Ejemplo real:

```csharp
[ObservableProperty]
private string usuario = string.Empty;

// Genera: public string Usuario { get; set; }
```

### 1.7 Variables y propiedades

- Campos privados de dependencias: `private readonly I<Servicio> _nombre;` (prefijo `_`, camelCase).
- Variables y parámetros locales: **camelCase**.
- Métodos y propiedades públicas: **PascalCase**.
- Interfaces: prefijo `I`.
- Constantes: usar `const`, prefiriendo **PascalCase** para constantes públicas y compartidas (`BaseUrl`, `Session`, `JsonContentType`). No imponer una única nomenclatura si existe una excepción justificada (detalle en §5.5).

---

## 2. Organización de carpetas

| Carpeta | Contiene | Dónde colocar un archivo nuevo |
|---|---|---|
| `Views/` | Páginas XAML y su code-behind | Una pantalla nueva → `Views/<Nombre>Page.xaml` + `.xaml.cs` |
| `ViewModels/` | ViewModels de pantalla | Un ViewModel nuevo → `ViewModels/<Nombre>ViewModel.cs` |
| `Models/Entities/` | Entidades del backend | Un DTO de entidad → aquí |
| `Models/Requests/` | Cuerpos de peticiones | Un request → aquí |
| `Models/Responses/` | Respuestas específicas | Un response → aquí |
| `Models/Common/` | Contrato transversal de la API | Solo clases compartidas por toda la API |
| `Services/` | Interfaces e implementaciones de servicios | `Services/I<Dominio>Service.cs` + `Services/<Dominio>Service.cs` |
| `Api/` | Cliente HTTP, endpoints, serialización, excepciones | Un endpoint nuevo → extender `Api/ApiEndpoints.cs`; nunca en el servicio |
| `Navigation/` | Servicio de navegación | Solo `INavigationService`/`NavigationService` (no crear duplicados) |
| `Helpers/` | Constantes y utilidades estáticas | Constantes nuevas → `Constants.cs`, `AppSettings.cs`, `StorageKeys.cs` o `Roles.cs` |
| `Controls/` | Componentes reutilizables (hoy vacía) | Un control reutilizable → aquí, solo cuando exista duplicación real |
| `Resources/` | Estilos, fuentes, imágenes, splash | Colores → `Resources/Styles/Colors.xaml`; estilos → `Resources/Styles/Styles.xaml`; imágenes nuevas → `Resources/Images/` (ver §9) |
| `Platforms/` | Código por sistema operativo | No tocar salvo necesidad real de plataforma |

Reglas de organización:

- **Un archivo, una responsabilidad.** No mezclar View, ViewModel o servicios en el mismo archivo.
- **No mover archivos existentes** de carpeta sin justificación documentada.
- **No crear carpetas nuevas** sin justificar; primero agotar las existentes.
- Todo componente que **requiera inyección de dependencias** debe registrarse en `MauiProgram.cs`: ViewModels inyectados por constructor, Services y Navigation. Los Helpers, constantes y clases estáticas **no** se registran.
- Las carpetas vacías o con propósito futuro se declaran en el `.csproj` (`<Folder Include="..." />`).

---

## 3. Reglas MVVM

### 3.1 Qué lógica pertenece al ViewModel

- Estado de la pantalla y propiedades observables.
- Comandos (`[RelayCommand]`) y validación de entrada.
- Orquestación de servicios: llamar a `IAuthService`, `ISessionService`, `INavigationService`.
- Manejo de errores y mensajes para el usuario (`Mensaje`).
- Carga inicial de datos (llamada desde `OnAppearing` de la View).

### 3.2 Qué debe evitarse en el code-behind

- **Prohibido:** llamadas HTTP, lógica de negocio, manipulación del estado de la app.
- **Permitido:** `InitializeComponent()`, asignar `BindingContext = viewModel`, y sobrecargas de ciclo de vida (`OnAppearing`) que delegan en el ViewModel.
- **Excepción documentada actual:** `SplashPage` decide el auto-login en su code-behind usando servicios. No replicar este patrón en pantallas nuevas; está marcado como mejora pendiente en `docs/Arquitectura.md` §10.

```csharp
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

### 3.3 Uso correcto de bindings

- Los botones se enlazan con `Command="{Binding <Accion>Command}"`, **nunca** con el evento `Clicked`.
- Las entradas de texto con `Text="{Binding <Propiedad>}"`; contraseñas con `IsPassword="True"`.
- Los mensajes de error/estado con un `Label` enlazado a `{Binding Mensaje}`.
- Proteger cada command con la guarda de `IsBusy` al inicio y restaurarlo en `finally`.
- Las Views **nunca** realizan HTTP; los ViewModels **nunca** usan `HttpClient` ni acceden al backend directamente. Toda comunicación pasa por `IApiClient`.

---

## 4. Convenciones XAML

### 4.1 Organización de elementos

- Un atributo por línea, con indentación de 4 espacios (ver `LoginPage.xaml` como referencia).
- Declarar siempre `xmlns`, `xmlns:x` y `x:Class`.
- Layout principal con `VerticalStackLayout` (o `Grid`/`ScrollView` cuando el contenido lo requiera), con `Padding` y `Spacing` uniformes.
- `Title` en la raíz de la página.

### 4.2 Estilos

- **No hardcodear colores ni fuentes**: usar `{StaticResource ...}` de `Resources/Styles/Colors.xaml` y los estilos implícitos de `Resources/Styles/Styles.xaml`.
- Usar estilos nombrados existentes cuando apliquen (`Headline`, `SubHeadline`).
- Respetar el tema claro/oscuro con `AppThemeBinding` (`Light={StaticResource ...}, Dark={StaticResource ...}`).
- Respetar tamaños táctiles: los estilos implícitos fijan `MinimumHeightRequest`/`MinimumWidthRequest` de 44; no los reducir en controles individuales.

### 4.3 Recursos compartidos

- Colores nuevos → `Resources/Styles/Colors.xaml`.
- Estilos reutilizables → `Resources/Styles/Styles.xaml`.
- Ambos se fusionan en `App.xaml` (`ResourceDictionary.MergedDictionaries`); no definir diccionarios de recursos por página.
- Claves de recursos en **PascalCase** (`Primary`, `Gray100`, `Headline`).

### 4.4 Separación visual

- `Spacing` uniforme dentro de cada stack (el proyecto usa 20) y `Margin` puntual solo donde haga falta.
- Mantener `Padding` consistente entre pantallas del mismo nivel.

### 4.5 Reglas de UI fijadas durante el desarrollo (sep-2026)

- **NUNCA un `BoxView` como espaciador** (aunque lleve `Color="Transparent"`): en Android se pinta NEGRO. Para espacio usar SIEMPRE `Padding` del contenedor. `BoxView` solo con `Color` explícito (líneas de cuadrícula, indicadores).
- **Cero emojis** en pantallas y VMs: todo glifo va como SVG Material-style 24×24 (`Resources/Images/icon_<glifo>_<color>.svg`, `<MauiImage Update ... BaseSize="24,24"/>` en el csproj — sin BaseSize se ven borrosos). Las flechas tipográficas (← ▼ ▲) y el "+" del FAB SÍ se permiten como texto de Label.
- `Shell.NavBarIsVisible="False"` en TODAS las páginas nuevas (la página dibuja su propio header institucional).
- Colores institucionales: header/botones/FABs `#4125F4`; aviso verde `#4CB050`; fondo de contenido `#F5F5F5`; scrim de overlay `#707070` al 50-80%.
- Overlays de carga ("Procesando....", "Descargando....") simulados ~2 s en esta fase, texto blanco centrado.
- Bindings de color hex en `BackgroundColor`/`TextColor` SIEMPRE con `#` inicial.
- En placeholders de pestañas/tarjetas deshabilitadas: `Opacity` ~0.45 + `InputTransparent="True"` (patrón Tira de Liquidación).

### 4.6 Fechas y montos en pantalla

- **Fechas en español: NUNCA `CurrentCulture`** — el dispositivo puede correr en en-US y renderizar "28-October-2025" (bug real Recibos de Pago, auditoría 2026-09-10). Usar `Helpers/FormatosFecha.cs` (`FechaLarga`, `FechaArchivo`, `MesEspanol`, `FechaHoraLarga`), única fuente con es-MX explícito.
- Nombre del archivo PDF según el mockup de su pantalla (EC `estado-cuenta-<dd-MM-yyyy>.pdf`, FA/FAC `<nombre>-<dd-mes-aaaa>.pdf`): cada uno replica su mockup, NO unificar.

---

## 5. Convenciones C#

### 5.1 Formato

- Namespaces con **file-scoped** (`namespace MiCachito.Mobile.ViewModels;`).
- `using` al inicio del archivo, agrupados y en orden.
- Indentación de 4 espacios.
- `Nullable` habilitado en el proyecto; respetar anotaciones `?` y nullabilidad.
- Strings inicializados con `string.Empty`, no con `null`.

### 5.2 Comentarios

- Documentación XML (`/// <summary>`) **en español**, solo donde aporta valor (contratos, clases públicas, decisiones no obvias).
- Comentarios de una línea solo para explicar el *porqué*, no el *qué*.
- Evitar comentarios redundantes que repiten el código.

### 5.3 Async / Await

- Métodos asíncronos: `async Task` (o `Task<T>`), con sufijo `Async`.
- `ConfigureAwait(false)` en **Services** y en la **capa Api** (no hay SynchronizationContext que preservar).
- **No** usar `ConfigureAwait(false)` en ViewModels/Views (necesitan el contexto de UI).
- Parámetro opcional `CancellationToken cancellationToken = default` al final de las firmas de servicios y `ApiClient`.

### 5.4 Manejo de errores

- Los errores del backend llegan como `ApiException` (de `MiCachito.Mobile.Api`) con `StatusCode`, `ServerMessage` y `Errors`.
- En el ViewModel: `Mensaje = ex.ServerMessage ?? "mensaje por defecto"` para `ApiException`, y un mensaje genérico de conexión para cualquier otra excepción.
- Restaurar `IsBusy = false` en `finally`.
- No tragar excepciones silenciosamente salvo casos documentados (p. ej. el logout remoto que falla sin impedir el logout local).

### 5.5 Uso de constantes

- **No usar strings mágicos.** Centralizar en `Helpers/`:
  - `AppSettings` — configuración de entorno (URL base, timeout).
  - `Constants` — valores generales (content-type, esquema Bearer).
  - `StorageKeys` — claves de SecureStorage/Preferences.
  - `Roles` — nombres de roles del backend.
- Las rutas de la API viven en `Api/ApiEndpoints.cs`, agrupadas por dominio (`ApiEndpoints.Auth.Login`).
- Nomenclatura de constantes:
  - Usar `const` siempre que sea posible.
  - Preferir **PascalCase** para constantes públicas y compartidas (`BaseUrl`, `Session`, `JsonContentType`).
  - No imponer una única nomenclatura cuando exista una excepción justificada y documentada (por ejemplo, valores privados de una clase o nombres que deban coincidir con un contrato externo).

### 5.6 Serialización

- API (requests/responses): `snake_case` automático vía `ApiJsonOptions` (coincide con Yii2).
- Almacenamiento local (`SessionService`): `camelCase`.
- El cliente devuelve solo el campo `data`; los errores se lanzan como `ApiException`.

---

## 6. Reglas para nuevos desarrollos

1. **Antes de crear una pantalla, revisar la arquitectura existente** (`docs/Arquitectura.md`): flujo de autenticación, navegación, servicios y el árbol del proyecto. Reutilizar lo que ya exista.
2. **Reutilizar componentes existentes**: `IApiClient`, `INavigationService`, `BaseViewModel`, `SessionService` y los `Helpers`. No reinventar transporte HTTP, sesión ni navegación.
3. **No duplicar servicios.** Si un dominio ya tiene servicio, extenderlo con el método que falte; crear uno nuevo solo para un dominio nuevo.
4. **Mantener compatibilidad con la arquitectura actual:** respetar MVVM, la navegación raíz por `Window.Page` (Login/Home) y el registro de dependencias en `MauiProgram.cs` (§2).
5. **Flujo pantalla por pantalla** (según `docs/AI_CONTEXT.md`): implementar View y ViewModel, dejar la pantalla preparada para el backend, identificar los endpoints requeridos, verificar que existan en `dev-mobile` (o proponerlos) y recién entonces conectar la pantalla.
6. **Sin datos mock.** Toda información mostrada proviene del backend real; si un dato no existe, identificar qué endpoint se necesita y esperar a que exista.
7. **Verificar que el proyecto compile** después de cada cambio y mantener el estilo consistente con el resto del código.

---

## 7. Reglas específicas para agentes IA

1. **No modificar la estructura del proyecto sin justificarla.** No crear carpetas ni mover archivos; la estructura actual ya está documentada en `docs/Arquitectura.md`. Cualquier cambio estructural debe registrarse ahí antes de ejecutarse.
2. **No crear endpoints inexistentes.** La app consume únicamente lo que existe en la rama `dev-mobile` del backend. No inventar rutas ni suponer respuestas; si falta un endpoint, proponerlo y esperar a que exista antes de conectar la pantalla.
3. **Consultar `docs/API.md` antes de integrar servicios.** Los modelos, requests, responses y errores deben derivarse de esa documentación, no inferirse. Cuando `docs/API.md` no cubra un endpoint, usar `docs/Arquitectura.md` §8 y el código de `Api/ApiEndpoints.cs` como referencia y registrar la brecha.
4. **Mantener la separación MVVM** descrita en §3: Views sin lógica, ViewModels sin HTTP, servicios como única capa de consumo vía `IApiClient`.
5. **No crear modelos ficticios ni inventar respuestas JSON o campos** que no existan en el backend; los DTOs reflejan el contrato real.
6. **Respetar las convenciones de nombres y carpetas** de este documento en todo archivo nuevo.
7. **Verificar que compile y actualizar la documentación** cuando corresponda (estado del proyecto y decisiones en `docs/Arquitectura.md`, fecha y commits en «Estado del documento»).

---

## 8. Convenciones Git

Reglas para mantener el historial del repositorio ordenado y legible.

### 8.1 Reglas de commits

- **Un cambio funcional debe tener un commit independiente.** No agrupar funcionalidades no relacionadas en un mismo commit.
- **No mezclar implementación con refactors grandes.** Un refactor amplio va en su propio commit para no ensombrecer el cambio funcional.
- **No mezclar cambios de código con documentación**, salvo que estén directamente relacionados (p. ej. documentar la pantalla que se acaba de implementar).

### 8.2 Formato de mensajes

Prefijo por tipo de cambio:

| Prefijo | Uso | Ejemplo |
|---|---|---|
| `Add` | Nueva funcionalidad | `Add Login validation` |
| `Fix` | Corrección de errores | `Fix API authentication error` |
| `Refactor` | Mejora interna sin cambio funcional | `Refactor session storage` |
| `Docs` | Cambios de documentación | `Docs update architecture` |

- Mensaje corto, en inglés y en imperativo, describiendo el cambio concreto.
- Un prefijo por commit; si el cambio mezcla tipos, separarlo en commits distintos.

---

## 9. Convenciones de recursos visuales

Reglas para imágenes y recursos de la aplicación.

### 9.1 Imágenes

- **Imágenes nuevas en `Resources/Images/`**; la carpeta ya se incluye en el paquete vía `<MauiImage Include="Resources\Images\*" />` en `MiCachito.Mobile.csproj`.
- **Nombres en minúsculas con guion bajo**, descriptivos del contenido o de su uso: `login_logo.png`, `empty_inventory.png`.
- **Evitar duplicar recursos existentes:** antes de agregar una imagen, verificar si ya existe una equivalente en `Resources/` (hoy solo vive el `dotnet_bot.png` de la plantilla).
- No usar nombres genéricos (`imagen1.png`, `logo.png`) que no indiquen qué muestran o dónde se usan.

### 9.2 Otros recursos

- Fuentes → `Resources/Fonts/` (`<MauiFont Include="Resources\Fonts\*" />`).
- Iconos de app y splash → se declaran en el `.csproj` (`<MauiIcon>`, `<MauiSplashScreen>`).
- Assets crudos → `Resources/Raw/` (`<MauiAsset>`).
- Si un recurso se coloca fuera de estas carpetas, **declararlo en el `.csproj`** según la configuración MAUI correspondiente para que se incluya en el paquete.

---

## Checklist de verificación

Antes de dar por bueno un cambio nuevo:

- [ ] La pantalla/componente sigue MVVM (sin HTTP en Views ni ViewModels).
- [ ] Nombres correctos (`*Page`, `*ViewModel`, `I*Service`/`*Service`, `<Accion>Command`).
- [ ] Archivos colocados en la carpeta correspondiente; registrados en `MauiProgram.cs` solo si requieren inyección de dependencias.
- [ ] Sin strings mágicos; constantes en `Helpers/` y rutas en `ApiEndpoints`.
- [ ] Sin datos mock ni endpoints inventados; todo validado contra `dev-mobile`.
- [ ] Commit independiente por cambio funcional, con prefijo `Add`/`Fix`/`Refactor`/`Docs` (§8).
- [ ] Imágenes nuevas en `Resources/Images/`, con nombre descriptivo en minúsculas y guion bajo, sin duplicados (§9).
- [ ] Manejo de errores con `ApiException.ServerMessage` y mensaje por defecto.
- [ ] `IsBusy` con guarda y restaurado en `finally`.
- [ ] Colores y estilos vía `StaticResource`/`AppThemeBinding`.
- [ ] XML docs en español solo donde aportan; sin comentarios triviales.
- [ ] El proyecto compila sin errores.

---

## Estado del documento

- **Fecha:** 04 agosto 2026
- **Estado:** Documento completado.
- **Propósito:** Guía oficial de convenciones para el desarrollo de MiCachito.Mobile.

> Este documento debe actualizarse cada vez que cambien las convenciones del proyecto; siempre registrar la fecha en este apartado.
