# CLAUDE.md — MI CACHITO MOBILE

# 1. CONTEXTO GENERAL DEL PROYECTO

Trabaja exclusivamente en el proyecto **Mi Cachito Mobile**.

- **Proyecto:** Mi Cachito Mobile.
- **Repositorio:** `Amam03/MiCachito-Mobile`.
- **Ruta local:** `C:\Users\amati\Documents\MiCachito.Mobile`.
- **Tecnología:** .NET MAUI, C#, CommunityToolkit.Mvvm.
- **Arquitectura:** MVVM.
- **Comunicación:** API REST con respuestas JSON.
- **Backend:** Yii2 / PHP.
- **Base de datos:** MySQL compartida con Mi Cachito Desktop.

Mobile y Desktop comparten backend y base de datos, pero tienen interfaces, flujos y mecanismos de autenticación diferentes.

**Regla principal:** cualquier cambio debe respetar la arquitectura móvil y evaluar su impacto sobre el backend y la aplicación de escritorio.

# 2. REPOSITORIOS Y RAMAS

## Aplicación móvil

- Repositorio: `Amam03/MiCachito-Mobile`.
- Rama de trabajo actual: `master`.
- No cambiar de rama durante las tareas, salvo instrucción expresa.

## Backend compartido

- Repositorio: `globaloxs-sys/MiCachito-backend`.
- Rama de trabajo: `dev`.
- Mobile consume principalmente los controladores y endpoints `/api/mobile/*`.

**Importante:**

- No crear un backend independiente para Mobile.
- No asumir que existe una rama `dev-mobile`.
- No modificar endpoints compartidos sin evaluar su impacto en Desktop.
- No cambiar de rama ni realizar merges sin autorización.
- Antes de trabajar en el backend, verificar que se encuentre en la rama `dev`.

# 3. ARQUITECTURA DEL PROYECTO

La aplicación utiliza arquitectura MVVM.

Flujo general:

`View → ViewModel → Service → IApiClient → Backend Yii2 → MySQL`

## Responsabilidades

**Views**
- Contienen interfaz XAML.
- Mantienen code-behind mínimo.
- No deben contener lógica de negocio.
- No deben realizar solicitudes HTTP directamente.

**ViewModels**
- Administran el estado de la pantalla.
- Contienen propiedades observables.
- Implementan Commands.
- Coordinan la interacción con los servicios.
- No deben utilizar directamente `HttpClient`.
- No deben depender de páginas concretas.

**Services**
- Implementan la lógica de comunicación con la API.
- Utilizan `IApiClient`.
- No deben manejar interfaces gráficas ni navegación.
- Deben reutilizar servicios existentes.

**Models**
- Contienen entidades, DTOs, Requests y Responses.
- Representan los datos intercambiados con backend.

**Api**
- Centraliza el cliente HTTP.
- Administra endpoints.
- Maneja serialización.
- Gestiona errores y excepciones.

**Navigation**
- Centraliza la navegación mediante `INavigationService`.

**Dependency Injection**
- Se configura centralmente en `MauiProgram.cs`.

## Reglas obligatorias de MVVM

- Mantener separadas las responsabilidades.
- Utilizar interfaces para las dependencias cuando corresponda.
- No duplicar lógica del backend.
- No colocar lógica de negocio en las Views.
- No utilizar HTTP directamente desde ViewModels.
- No acceder a páginas concretas desde ViewModels.
- Respetar el registro de dependencias existente.
- Reutilizar componentes y servicios antes de crear otros nuevos.

# 4. ESTRUCTURA DE CARPETAS

Estructura identificada:

- `Api/`: ApiClient, IApiClient, ApiEndpoints, ApiException, ApiJsonOptions y converters.
- `Services/`: servicios de autenticación, sesiones, ventas, expendios, sorteos, devoluciones, depósitos, recibos, reportes y otros.
- `Models/`: Entities, Requests, Responses y Common.
- `ViewModels/`: ViewModels de las pantallas.
- `Views/`: páginas XAML.
- `Navigation/`: INavigationService y NavigationService.
- `Helpers/`: AppSettings, Constants, Roles, StorageKeys y FormatosFecha.
- `Controls/`: controles reutilizables.
- `Data/`: componentes de datos.
- `Interop/`: interoperabilidad.
- `Natives/`: funcionalidades nativas.
- `Platforms/`: configuraciones específicas por plataforma.

Antes de crear archivos o carpetas, revisar la estructura existente y respetar sus convenciones.

# 5. CONFIGURACIÓN DE API Y ENTORNOS

Archivo principal identificado:

`Helpers/AppSettings.cs`

Configuración observada:

- `BaseUrl` utiliza actualmente una IP LAN fija: `http://192.168.100.56:8080/`.
- `TimeoutSeconds = 30`.

Hallazgo importante:

La aplicación utiliza una dirección IP fija y no implementa actualmente el mecanismo de configuración por entorno esperado.

Para Android Emulator, el fallback local debe ser:

`http://10.0.2.2:8080/`

## Reglas

- No introducir otra IP LAN fija como predeterminada.
- No hardcodear URLs en Views, ViewModels o Services.
- Centralizar la configuración de API.
- Permitir configurar el entorno sin modificar múltiples archivos.
- Diferenciar desarrollo y producción.
- Verificar la configuración específica de Android, Windows y demás plataformas.
- No modificar la URL actual sin revisar primero cómo se utiliza en todos los entornos.

Cualquier corrección de `BaseUrl` debe incluir pruebas de conectividad con el backend.

# 6. AUTENTICACIÓN Y SESIONES

Mobile tiene autenticación propia.

**No utiliza los roles de escritorio.**

La autenticación se realiza mediante:

- `billeteros_expendios`.
- `mobile_sesiones`.

## Login

Endpoint:

`POST api/mobile/auth/login`

Parámetros identificados:

- `username`
- `password`
- `dispositivo`

El backend valida que el expendio esté autorizado y que el billetero se encuentre activo.

## Manejo del token

- El token se entrega en el inicio de sesión.
- La base de datos almacena el hash del token.
- El token se envía mediante:

`Authorization: Bearer <token>`

- `ApiClient` agrega automáticamente el encabezado de autorización.
- `SessionService` persiste la sesión mediante `SecureStorage`.
- La clave de almacenamiento identificada es `session`.

Nunca guardar tokens en texto plano en archivos de configuración, logs o documentación.

## Endpoints de sesión

- `POST api/mobile/auth/login`: inicio de sesión.
- `GET api/mobile/auth/verify`: verificar sesión.
- `POST api/mobile/auth/logout`: cerrar sesión actual.
- `GET api/mobile/auth/sesiones`: consultar sesiones activas.
- `POST api/mobile/auth/sesiones/revocar`: revocar una sesión.

El logout revoca únicamente la sesión actual.

## Renovación de sesión

Existe `RefreshTokenAsync` en `AuthService`, pero no está conectado al flujo automático.

No asumir que existe renovación automática.

Antes de modificar autenticación:

1. Revisar `AuthService`.
2. Revisar `SessionService`.
3. Revisar `ApiClient`.
4. Revisar almacenamiento seguro.
5. Revisar el comportamiento del backend.
6. Comprobar expiración y revocación.

## Seguridad

- Manejar correctamente respuestas 401 y 403.
- Evitar sesiones inválidas.
- Limpiar los datos de sesión al cerrar sesión.
- No exponer información sensible.
- No almacenar credenciales innecesariamente.

# 7. ROLES Y PERMISOS MÓVILES

La aplicación móvil no utiliza los roles de escritorio.

La identidad móvil se determina mediante el expendio y el billetero autenticado.

Los permisos de venta se administran por expendio.

Endpoints identificados:

- `GET api/mobile/expendios`
- `PUT api/mobile/expendios/{id}/permisos`

El backend determina el alcance de los datos a partir de la sesión autenticada.

## Reglas obligatorias

- No copiar la matriz de roles de Desktop a Mobile.
- No utilizar roles de escritorio para habilitar funcionalidades móviles.
- No asumir permisos únicamente por ocultar o mostrar botones.
- Validar la autorización en backend.
- Respetar el expendio y billetero autenticados.
- No permitir consultar o modificar expendios ajenos mediante parámetros manipulados.
- Mantener consistencia entre la interfaz y los permisos reales del servidor.

# 8. ENDPOINTS MÓVILES IDENTIFICADOS

Mobile consume los siguientes endpoints del backend compartido.

## Autenticación

Controlador: `Mobile/AuthController`

| Método | Endpoint |
|---|---|
| POST | `api/mobile/auth/login` |
| GET | `api/mobile/auth/verify` |
| POST | `api/mobile/auth/logout` |
| GET | `api/mobile/auth/sesiones` |
| POST | `api/mobile/auth/sesiones/revocar` |

## Expendios

Controlador: `Mobile/ExpendiosController`

| Método | Endpoint |
|---|---|
| GET | `api/mobile/expendios` |
| PUT | `api/mobile/expendios/{id}/permisos` |

## Ventas

Controlador: `Mobile/VentasController`

| Método | Endpoint |
|---|---|
| GET | `api/mobile/ventas` |
| GET | `api/mobile/ventas/{id}` |
| GET | `api/mobile/ventas/sorteos` |
| GET | `api/mobile/ventas/billetes` |
| POST | `api/mobile/ventas/crear` |
| POST | `api/mobile/ventas/devolver` |
| GET | `api/mobile/devoluciones` |

## Sorteos TEC

Controladores identificados: `Mobile/VentasController` y `Mobile/VentasTecController`.

| Método | Endpoint |
|---|---|
| GET | `api/mobile/ventas/sorteos-tec` |
| GET | `api/mobile/ventas/billetes-tec` |

## Premios y reintegros

Controlador: `Mobile/PremiosController` y `Mobile/PremiosReintegrosController`.

| Método | Endpoint |
|---|---|
| POST | `api/mobile/premios/consultar` |
| GET/POST | `api/mobile/premios-reintegros` |
| GET | `api/mobile/premios-reintegros/{id}` |

## Sorteos

Controlador: `Mobile/SorteosMobileController`.

| Método | Endpoint |
|---|---|
| GET | `api/mobile/sorteos` |
| GET | `api/mobile/sorteos/{id}` |

## Depósitos

Controlador: `Mobile/DepositosController`.

| Método | Endpoint |
|---|---|
| GET/POST | `api/mobile/depositos` |
| GET | `api/mobile/depositos/cuentas` |

## Pagos

Controlador: `Mobile/PagosController`.

| Método | Endpoint |
|---|---|
| GET | `api/mobile/pagos/recibos` |
| GET | `api/mobile/pagos/recibo/{id}` |

## Reportes

Controlador: `Mobile/ReportesController`.

| Método | Endpoint |
|---|---|
| GET | `api/mobile/reportes/estado-cuenta` |
| GET | `api/mobile/reportes/fondo-ahorro` |
| GET | `api/mobile/reportes/facturacion` |

## Tiempo Aire

Controlador: `Mobile/TiempoAireController`.

| Método | Endpoint |
|---|---|
| GET | `api/mobile/tiempo-aire/proveedores` |
| GET | `api/mobile/tiempo-aire/proveedores/{codigo}/montos` |
| POST | `api/mobile/tiempo-aire/venta` |

**Regla:** esta tabla representa endpoints identificados durante la auditoría. Antes de modificarlos, verificar su implementación y contrato actual en el backend.

No asumir que todos tienen el mismo formato de respuesta.

# 9. REGLAS DE NEGOCIO DE PRODUCTOS E INVENTARIO

Esta sección es obligatoria para cualquier cambio relacionado con ventas, inventarios, devoluciones, productos, sorteos y movimientos.

No todos los productos se manejan de la misma forma.

Antes de implementar una funcionalidad, identificar su categoría y unidad de manejo.

## 9.1. Lotería Nacional

Los sorteos de Lotería Nacional se manejan por fracciones o cachitos.

Equivalencias:

- 1 serie completa = 20 cachitos.
- 1 tira = 5 cachitos.
- 1 cachito = 1 unidad.
- 1 serie = 4 tiras.

Un mismo número de billete puede tener varias series independientes.

Ejemplo:

- Billete 45678, serie 1 = 20 cachitos.
- Billete 45678, serie 2 = otros 20 cachitos.

Total: 40 cachitos distribuidos en dos series distintas.

## 9.2. Identificación y trazabilidad

Mantener diferenciados:

- Número de sorteo.
- Número de billete.
- Número de serie.
- Cantidad de cachitos.
- Unidad de operación.

Reglas:

- Un número de billete no representa necesariamente una única serie.
- No mezclar existencias de series diferentes.
- No duplicar series.
- No descontar unidades de una serie distinta a la seleccionada.
- Conservar la trazabilidad de cada operación.
- No registrar una tira como una serie completa.
- No modificar equivalencias sin autorización funcional.

Las conversiones deben ser consistentes entre venta, devolución, inventario y backend.

## 9.3. Otros productos

Existen categorías adicionales, entre ellas:

- Sorteos TEC.
- Universitarios.
- Rascas.
- Otros productos registrados.

Sorteos TEC cuenta con un flujo propio y componentes identificados como `SorteosTec` y `CarritoTec`.

Las reglas de Rascas y Universitarios requieren verificación adicional en código.

No aplicar automáticamente las equivalencias de Lotería Nacional a otras categorías.

## 9.4. Precios

- No utilizar precios hardcodeados.
- Obtener los precios de la configuración o respuesta del backend.
- Respetar las reglas particulares de cada categoría.
- No duplicar cálculos que ya estén centralizados en backend.
- Mantener consistencia entre precio, cantidad y unidad de venta.

Para Lotería Nacional de 8 sorteos, cuando aplique la regla de precio por cachito y serie, una serie equivale a 20 veces el precio del cachito.

Para otros productos, respetar su cálculo correspondiente por billete u otra unidad configurada.

No generalizar esta regla sin comprobar el tipo de producto.

## 9.5. Integridad del inventario

Toda operación debe:

- Validar disponibilidad.
- Evitar cantidades negativas.
- Evitar ventas sin existencia.
- Evitar movimientos duplicados.
- Validar el producto y sorteo.
- Mantener origen y destino.
- Respetar la unidad de manejo.
- Conservar trazabilidad.
- Utilizar las validaciones centrales del backend.

Mobile debe proporcionar validaciones de experiencia de usuario, pero el backend es responsable de las validaciones críticas.

# 10. MÓDULO VENDER

Pantallas identificadas:

## Lotería Nacional

- `SorteosLotenalPage`
- `SorteosLotenalViewModel`
- `SorteosActivosPage`
- `SeleccionCiudadPage`
- `AgregarBoletosPage`
- `CarritoComprasPage`
- `VentaExitosaLotenalPage`

## Sorteos TEC

- `SorteosTecPage`
- `SeleccionarBilletePage`
- `CarritoTecPage`
- `DatosClienteTecPage`
- `VentaExitosaTecPage`

## Tiempo Aire

- `TiempoAirePage`
- `MontosTiempoAirePage`
- `NumeroTelefonoTiempoAirePage`

Servicios identificados:

- `MobileVentasService`.
- `CarritoTecService`.
- Servicios de Tiempo Aire según su implementación actual.

## Reglas para modificar ventas

Antes de realizar cambios, analizar:

1. Selección del sorteo.
2. Selección de billete o serie.
3. Escaneo o captura manual.
4. Disponibilidad.
5. Cantidad.
6. Precio.
7. Carrito.
8. Confirmación.
9. Registro de venta.
10. Respuesta del backend.
11. Actualización de inventario.
12. Pantalla de resultado.

No modificar únicamente la pantalla de confirmación si el problema se origina en el servicio o backend.

Las operaciones deben ser consistentes con el inventario compartido.

# 11. MÓDULO GESTIÓN

Pantallas identificadas:

- `TicketsVentaPage`
- `DetalleVentaPage`
- `DevolucionesPage`
- `ListaSorteosDevolucionPage`
- `NuevaDevolucionPage`
- `DesgloseDevolucionPage`
- `EscanearSeriesPage`
- `PremiosReintegrosPage`
- `DetallePremiosReintegrosPage`
- `CapturaPremiosReintegrosPage`
- `EscanearBoletosPage`
- `SorteosPage`
- `DepositosPage`
- `NuevoDepositoPage`
- `RecibosPagoPage`
- `DetallePagoPage`
- `ReportesPage`

Servicios identificados:

- `TicketsVentaService`
- `DevolucionService`
- `PremiosReintegrosService`
- `SorteosService`
- `DepositoService`
- `RecibosPagoService`
- `EstadoCuentaService`
- `FondoAhorroService`
- `FacturacionService`

## Reglas para Gestión

Antes de modificar una funcionalidad, revisar su relación con:

- Ventas.
- Inventario.
- Devoluciones.
- Premios.
- Reintegros.
- Depósitos.
- Recibos de pago.
- Reportes.

Las devoluciones deben conservar la relación con la venta y los productos correspondientes.

No permitir que una devolución genere existencias incorrectas o duplique movimientos.

Los depósitos y pagos deben conservar sus referencias y estados.

# 12. MÓDULO EXPENDIOS

Pantallas identificadas:

- `ExpendiosPage`
- `ExpendioFormPage`

Servicio:

- `ExpendiosService`

Funciones identificadas:

- Consultar expendios asociados al billetero.
- Administrar información permitida.
- Actualizar permisos de venta por expendio.

Reglas:

- El backend debe limitar los resultados al billetero autenticado.
- No permitir consultar expendios ajenos mediante manipulación de identificadores.
- Mantener la autorización en servidor.
- No confundir la identidad del expendio con los roles de escritorio.

# 13. MÓDULO CUENTA

Pantallas identificadas:

- `CuentaPage`
- `NotificacionesPage`
- `ImpresoraPage`
- `DispositivosEnlazadosPage`

Servicios y componentes:

- `SessionService`
- `IImpresoraService`

Funciones identificadas:

- Información de cuenta.
- Identidad del usuario autenticado.
- Notificaciones.
- Configuración de impresora Bluetooth.
- Consulta de dispositivos enlazados.
- Cierre de sesión.

## Reglas

- Mantener segura la información de sesión.
- No mostrar datos de otros usuarios.
- Respetar permisos del dispositivo.
- Gestionar correctamente la conexión Bluetooth.
- Al cerrar sesión, revocar la sesión correspondiente y limpiar el almacenamiento local.

# 14. TIEMPO AIRE

El módulo cuenta con:

- Catálogo visual de proveedores.
- Consulta de montos.
- Captura de número telefónico.
- Endpoint de venta.

Endpoints:

- `GET api/mobile/tiempo-aire/proveedores`
- `GET api/mobile/tiempo-aire/proveedores/{codigo}/montos`
- `POST api/mobile/tiempo-aire/venta`

Se tiene identificado un catálogo visual de 34 proveedores.

La integración TAECEL requiere verificación de su estado real.

## Reglas

- No declarar que una recarga funciona únicamente porque exista la interfaz.
- Verificar el flujo completo con backend.
- Validar proveedor y monto.
- Validar número telefónico.
- Evitar registrar transacciones duplicadas.
- Revisar la respuesta del proveedor.
- Mantener trazabilidad de la operación.
- No simular respuestas exitosas en producción.
- Distinguir claramente simulaciones de transacciones reales.

Las tablas `ventas_electronicas` y los campos de TAE relacionados con billeteros deben revisarse en backend cuando se modifique esta funcionalidad.

# 15. PRUEBAS Y COMPILACIÓN

Comandos identificados:

Restaurar paquetes:

```powershell
dotnet restore
```

Compilar Android:

```powershell
dotnet build -f net10.0-android
```

Ejecutar en Android, según configuración:

```powershell
dotnet run -f net10.0-android
```

Antes de utilizar otros comandos:

- Revisar el `.csproj`.
- Verificar los frameworks instalados.
- Revisar las configuraciones de compilación.
- Confirmar las plataformas disponibles.

No asumir que una compilación exitosa significa que todas las funcionalidades trabajan correctamente.

## Estado de pruebas

La auditoría no encontró evidencia suficiente de pruebas automatizadas en el repositorio móvil.

Por lo tanto:

- No afirmar que existen pruebas unitarias si no se localizan.
- Revisar si hay proyectos de pruebas.
- Identificar las pruebas manuales disponibles.
- Documentar las áreas sin cobertura.
- Incorporar pruebas cuando el requerimiento lo amerite.

## Validaciones obligatorias

Después de modificar código:

1. Restaurar dependencias si corresponde.
2. Compilar la aplicación.
3. Ejecutar pruebas disponibles.
4. Validar el flujo afectado.
5. Revisar errores y logs.
6. Comprobar comunicación con backend.
7. Verificar que no se hayan afectado otras pantallas.

Cuando sea posible, probar en emulador o dispositivo físico.

# 16. SEGURIDAD

- Utilizar `SecureStorage` para los tokens.
- No guardar contraseñas en texto plano.
- No incluir credenciales en el código.
- No incluir API keys en documentación.
- No registrar tokens en logs.
- No exponer información sensible.
- Utilizar HTTPS en producción.
- Validar respuestas del backend.
- Manejar correctamente errores HTTP.
- No confiar en validaciones exclusivamente visuales.
- Respetar los permisos de plataforma.
- No modificar mecanismos de autenticación sin analizar Mobile y backend.

# 17. RELACIÓN ENTRE MOBILE Y DESKTOP

Ambas aplicaciones comparten:

- Backend Yii2.
- Base de datos MySQL.
- Información de inventarios.
- Información de productos.
- Operaciones de venta.
- Información relacionada con movimientos.

Diferencias:

- Mobile utiliza principalmente `/api/mobile/*`.
- Desktop utiliza sus propios controladores y endpoints.
- Mobile tiene autenticación mediante billeteros y expendios.
- Desktop utiliza roles de escritorio.

## Regla fundamental

Antes de modificar un endpoint compartido:

1. Identificar los consumidores.
2. Revisar si Desktop utiliza el mismo método o servicio.
3. Analizar el impacto en la base de datos.
4. Mantener compatibilidad.
5. Probar Mobile.
6. Probar Desktop cuando corresponda.

No duplicar lógica de negocio en Mobile si ya existe una implementación centralizada en backend.

# 18. REGLAS OBLIGATORIAS ANTES DE IMPLEMENTAR

Antes de modificar cualquier funcionalidad:

1. Comprender el requerimiento.
2. Identificar el módulo.
3. Revisar la pantalla.
4. Revisar el ViewModel.
5. Revisar el servicio.
6. Revisar los modelos y DTOs.
7. Revisar los endpoints utilizados.
8. Revisar la implementación del backend.
9. Identificar las reglas de negocio.
10. Identificar los posibles efectos en Desktop.
11. Revisar permisos y seguridad.
12. Presentar un plan breve.
13. Implementar después del análisis.

No modificar archivos sin comprender su responsabilidad.

Si falta información funcional, solicitar únicamente las aclaraciones necesarias.

# 19. REGLAS DE IMPLEMENTACIÓN

- Respetar MVVM.
- Reutilizar servicios e interfaces.
- Evitar código duplicado.
- No colocar HTTP directamente en ViewModels.
- No colocar lógica de negocio en Views.
- No introducir dependencias innecesarias.
- No utilizar valores hardcodeados.
- No modificar contratos de API sin analizar consumidores.
- No duplicar lógica del backend.
- No eliminar validaciones existentes para hacer funcionar una pantalla.
- No modificar funcionalidades ajenas al requerimiento.
- Mantener compatibilidad con las plataformas existentes.
- Mantener consistencia con la base de datos compartida.
- No modificar el backend sin revisar el impacto en Desktop.

# 20. RESOLUCIÓN AUTÓNOMA DE ERRORES

Cuando aparezca un error:

1. Analizar el mensaje.
2. Revisar logs.
3. Identificar la causa.
4. Inspeccionar el código relacionado.
5. Revisar servicios y contratos de API.
6. Implementar una solución compatible.
7. Compilar nuevamente.
8. Ejecutar pruebas.
9. Verificar que no existan regresiones.

No detener el trabajo por errores menores que puedan resolverse razonablemente.

Sin embargo, no ejecutar acciones destructivas, modificar datos productivos o alterar permisos sin autorización.

Si el problema requiere una decisión de negocio, detener esa parte y solicitar confirmación.

# 21. GIT Y CONTROL DE CAMBIOS

Antes de trabajar:

```bash
git status
git branch --show-current
git log -1 --oneline
```

Reglas:

- Trabajar en la rama móvil autorizada.
- La rama actual identificada es `master`.
- No cambiar de rama durante auditorías.
- No sobrescribir cambios ajenos.
- No descartar modificaciones existentes.
- No incluir credenciales ni archivos locales.
- No utilizar `git add .` indiscriminadamente.
- Revisar el diff antes de preparar cambios.

No ejecutar:

```bash
git reset --hard
git clean -fd
```

ni otros comandos destructivos sin autorización expresa.

Realizar commit o push únicamente cuando esté autorizado y después de validar los cambios.

No hacer merge a ramas principales sin autorización.

# 22. FORMATO OBLIGATORIO DE ENTREGA

Al finalizar cada tarea, entregar un informe con este formato:

## Resumen

Descripción breve del requerimiento y de la solución.

## Archivos modificados

Lista de archivos y propósito de cada cambio.

## Lógica implementada

Explicación de cómo se resolvió el problema.

## Backend

Indicar si fue necesario modificarlo, qué endpoints se utilizaron y qué impacto puede existir en Desktop.

## Validaciones ejecutadas

- Comandos utilizados.
- Compilación.
- Pruebas realizadas.
- Pantallas probadas.
- Endpoints probados.
- Resultados obtenidos.

## Evidencia

Incluir resultados de pruebas, respuestas HTTP, logs o capturas cuando estén disponibles.

## Estado final

Clasificar el resultado como:

- Validado correctamente.
- Implementado, pendiente de validación.
- Parcialmente validado.
- Error encontrado.
- Pendiente de información funcional.

## Pendientes y riesgos

Indicar las limitaciones, pruebas faltantes y decisiones que requieran confirmación.

**Nunca afirmar que una funcionalidad fue probada si únicamente se revisó el código.**

# 23. HALLAZGOS Y PENDIENTES CONOCIDOS

Los siguientes puntos provienen de la auditoría y deben verificarse antes de intervenirlos:

| ID | Tema | Hallazgo |
|---|---|---|
| M-01 | BaseUrl | Existe una IP LAN fija en `AppSettings`. |
| B-02 | Caja | Revisar alcance y permisos del backend compartido. |
| M-07 | Pruebas | No hay evidencia suficiente de pruebas automatizadas. |
| B-06 | Codeception | Revisar cobertura de pruebas del backend. |
| M-02 | Saldo | Se identificó saldo mostrado como `$0.00`; verificar origen de datos. |
| M-03 | Tiempo Aire | Verificar integración TAECEL. |
| M-04 | Consulta | Verificar si existe simulación de consulta y su alcance. |
| M-06 | Sesiones | El endpoint de sesiones existe; revisar su utilización en Mobile. |

Otros puntos:

- `RefreshTokenAsync` existe, pero no está conectado al flujo automático.
- Rascas y Universitarios requieren revisión de reglas.
- La integración real de TAECEL debe comprobarse.
- La cobertura de pruebas móviles debe documentarse.
- La configuración de API debe revisarse para los distintos entornos.

No considerar estos hallazgos resueltos sin evidencia.

# 24. PRINCIPIO FUNDAMENTAL

Mi Cachito Mobile comparte información y procesos con Mi Cachito Desktop.

Una modificación en Mobile puede afectar inventarios, ventas, sesiones, expendios y operaciones del backend