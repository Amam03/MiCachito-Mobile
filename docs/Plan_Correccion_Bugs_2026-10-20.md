# Plan de Corrección de Bugs — Auditoría Integral 2026-10-20
**Estado:** PENDIENTE DE AUTORIZACIÓN — sin implementar. Clasificación: bug confirmado / riesgo / funcionalidad incompleta / pendiente de validación. Prioridad: crítico / alto / medio / bajo.
Convención: IDs M-xx (Mobile), B-xx (Backend). Evidencia = archivo+método/línea verificado durante la auditoría.

---

## M-01 · BaseUrl regresión: IP fija pisó la configuración por API_BASE_URL
- **Categoría:** BUG CONFIRMADO · **Prioridad:** ALTA · **Plataforma:** Mobile
- **Evidencia:** `Helpers/AppSettings.cs:31` — `public static readonly string BaseUrl = "http://192.168.100.56:8080/";`
- **Causa raíz (comprobada):** el commit 2fc3057 la hizo configurable (`Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://10.0.2.2:8080/"`); el commit 01fe021 (14-oct, "integracion de boton premios y reintegros") la revirtió a la IP fija de LAN al integrar PyR.
- **Impacto:** fuera de esa LAN (emulador default, QA, otro equipo, producción) la app no conecta; el comentario del propio archivo (líneas 12-27) documenta el mecanismo que YA NO está activo — doc que miente.
- **Solución propuesta:** restaurar la línea de 2fc3057 (1 línea). Alternativa de bajo costo: mantener IP fija SOLO si el usuario decide que producción será esa LAN (declarar la decisión).
- **Archivos afectados:** `Helpers/AppSettings.cs`
- **Pruebas necesarias:** build + login en emulador sin API_BASE_URL (debe apuntar a 10.0.2.2), y con `API_BASE_URL` exportada (debe respetarla).
- **Criterio de aceptación:** emulador default conecta a Docker sin tocar código; grep de "192.168.100.56" = 0 en Helpers.

## M-02 · Saldo "$0.00" hardcoded en 4 headers (pendiente conocido)
- **Categoría:** FUNC. INCOMPLETA · **Prioridad:** MEDIA · **Plataforma:** Mobile+Backend
- **Evidencia:** `ViewModels/HomeViewModel.cs:36`, `ViewModels/GestionViewModel.cs:18`, `ViewModels/ExpendiosViewModel.cs:41`, `ViewModels/CuentaViewModel.cs:28` — 4 propiedades independientes `saldo = "$0.00"`.
- **Causa raíz:** fase solo-interfaz; nunca se decidió la cifra de negocio del header.
- **Impacto:** dato visible al billetero siempre falso; duplicación de estado (4 fuentes para un mismo número).
- **Solución propuesta:** el backend YA expone la cifra: `GET api/mobile/reportes/estado-cuenta` → `conceptos` (verificado en vivo: fondo_ahorro/fideicomiso/pagares/garantia_total/capacidad_credito) + `al_corriente`. Fases: (1) decisión de negocio — ¿qué concepto va en el header?; (2) servicio `SaldoService` único (o extensión de SessionService al refrescar por `verify` en Home — ya existe el mecanismo de 799bfdc); (3) los 4 VMs leen del mismo servicio.
- **Archivos afectados:** 4 VMs + nuevo/extendido servicio; opcional `ReportesController::actionEstadoCuenta` si se prefiere un campo agregado.
- **Pruebas:** E2E login → header refleja la cifra del endpoint; offline conserva el último valor.
- **Criterio de aceptación:** 0 hardcoded "$0.00" de saldo en VMs (grep); un solo origen de datos.

## M-03 · Tiempo Aire sin integración (HOLD — NO corregir aún)
- **Categoría:** FUNC. INCOMPLETA · **Prioridad:** MEDIA · **Plataforma:** Mobile+Backend
- **Evidencia:** `Data/TiempoAireData.cs` (34 proveedores estáticos); `ViewModels/NumeroTelefonoTiempoAireViewModel.cs:192` — `// TODO: Implementar recarga real cuando exista el endpoint del backend.`
- **Impacto:** flujo TAE navegable hasta "recarga" sin efecto real (no persiste nada: el POST real NO se llama desde la app).
- **Solución propuesta:** NINGUNA por ahora — decisión del usuario 2026-10-13: no integrar hasta comenzar con TAECEL. El módulo backend existe (TiempoAireController + ProveedorTiempoAireSimulado + `ventas_electronicas.estado_operacion` real/simulada). Documentado para el arranque de ese trabajo.
- **Pruebas/criterio:** cuando se retome: consumir `tiempo-aire/proveedores`/`montos`/`venta`, sustituir TiempoAireData, y reportes Desktop ya excluyen 'simulada'.

## M-04 · Expendios "Consulta de registros" simulada
- **Categoría:** FUNC. INCOMPLETA · **Prioridad:** MEDIA · **Plataforma:** Mobile (backend pendiente de contrato)
- **Evidencia:** `ViewModels/ExpendiosViewModel.cs:273-303` — `ConsultarAsync` = `await Task.Delay(1500)` + estado "Sin Registros"; `:311` — `ImprimirAsync` = alerta "pendiente de definición".
- **Impacto:** botón Consultar aparenta funcionar (overlay Procesando) pero nunca trae datos; impresión informativa.
- **Solución propuesta:** definir contrato backend (rango + expendios marcados → registros por expendio) y conectar; la impresión requiere capturas de formato.
- **Pruebas:** E2E consulta con datos sembrados dev; estados vacío/error/cargando.
- **Criterio:** `Task.Delay(1500)` eliminado de ConsultarAsync.

## M-05 · Notificaciones estado vacío sin backend
- **Categoría:** FUNC. INCOMPLETA · **Prioridad:** BAJA · **Plataforma:** Mobile (definición de producto pendiente)
- **Evidencia:** `ViewModels/NotificacionesViewModel.cs` — `_sinNotificaciones = true` fijo, sin datos inventados (regla del proyecto).
- **Solución propuesta:** cuando exista definición de producto: tabla + endpoint + lista. Sin acción hoy.

## M-06 · `auth/sesiones` + `auth/sesiones/revocar` sin consumidor
- **Categoría:** FUNC. INCOMPLETA · **Prioridad:** MEDIA · **Plataforma:** Mobile
- **Evidencia:** `Services/AuthService.cs:33-38` expone ambos; `ViewModels/DispositivosEnlazadosViewModel.cs` muestra SOLO Bluetooth bonded. Endpoint verificado vivo (200, lista con dispositivo/fechas/`actual`).
- **Impacto:** el billetero no puede ver/revocar dispositivos con sesión activa desde la app (función de seguridad disponible a medio construir).
- **Solución propuesta:** en DispositivosEnlazadosPage listar sesiones backend (dispositivo+último acceso+actual) junto al BT, con revocación por fila (POST revocar con `{id_sesion}`; confirmación tipo modal Cerrar Sesión).
- **Pruebas:** E2E revocar sesión secundaria no mata la actual; 401 si token inválido.
- **Criterio:** DispositivosEnlazadosViewModel consume `SesionesAsync`/`RevocarSesionAsync`.

## M-07 · App Mobile sin proyecto de tests
- **Categoría:** RIESGO · **Prioridad:** ALTA · **Plataforma:** Mobile
- **Evidencia:** `find` de `*test*` en el repo (fuera de obj/bin) = 0 resultados. Cobertura real: 11 scripts E2E Python ad-hoc en `~/` (fuera de repo, sin versionar).
- **Impacto:** cada regresión (p. ej. la de M-01, que pasó 6 días sin notarse) solo se detecta manualmente; los E2E no viven con el código.
- **Solución propuesta (fases):** (1) migrar los `e2e_mobile_*.py` a `tests/e2e/` del repo con README de uso (valores esperados parametrizados por BD); (2) unit tests del parser (`BilleteParser` 34/35/TEC/UNIV/Rasca-Prorra — puro, sin Android) con xUnit; (3) smoke de build en CI si existe.
- **Criterio:** `dotnet test` corre verde; parser cubierto por casos de esquina (subcódigo cruzado, longitud inválida).

## M-08 · Dead code: `Data/TiendasDisponiblesData.cs`
- **Categoría:** RIESGO · **Prioridad:** BAJA · **Plataforma:** Mobile
- **Evidencia:** 0 referencias (solo mención en comentario de `Data/SesionVentaLotenal.cs:9`).
- **Solución propuesta:** eliminar en el próximo pase de limpieza (con verificación grep previa y registro en el commit).
- **Criterio:** grep del símbolo = solo historia git.

## M-09 · Emoji ✕ en comentario
- **Categoría:** RIESGO · **Prioridad:** BAJA · **Plataforma:** Mobile
- **Evidencia:** `ViewModels/NuevoDepositoViewModel.cs:352` — `/// <summary>✕ del pad...` (U+2715). XAML limpio (0 emojis en Views/ViewModels evaluables).
- **Solución propuesta:** cambiar el glifo por texto ("botón X del pad") en el próximo pase. No afecta UI.

## B-01 · CORS `Origin: ['*']` en API autenticada
- **Categoría:** RIESGO · **Prioridad:** MEDIA · **Plataforma:** Backend
- **Evidencia:** `controllers/api/BaseApiController.php:95-101` — `'Origin' => ['*']`, `Access-Control-Request-Headers' => ['*']`, `'Access-Control-Allow-Credentials' => null`.
- **Análisis:** con `Allow-Credentials` nulo, Yii emite `Access-Control-Allow-Origin: *` — no habilita robo de tokens Bearer desde browsers (el header Authorization NO se envía con credenciales cookie). Riesgo residual: cualquier origen puede llamar la API desde un browser SIN token (los endpoints públicos: login) y leer respuestas de errores; endurecimiento pendiente.
- **Solución propuesta:** lista blanca de orígenes por entorno (config/params) + `Origin` explícito; mantener `OPTIONS` para el emulator/dev pero acotar headers (`Authorization, Content-Type`).
- **Pruebas:** curl con `Origin: https://evil.example` → sin `Access-Control-Allow-Origin` reflejo de ese dominio.
- **Criterio:** `'*'` eliminado de producción.

## B-02 · `caja/ficha-detalle` y `caja/billete-info` sin permiso ni scoping
- **Categoría:** RIESGO · **Prioridad:** ALTA · **Plataforma:** Backend (Desktop)
- **Evidencia:** `controllers/api/CajaController.php:386-393` — `actionFichaDetalle($id)`: `FichasPago::find()->where(['id_ficha_pago' => $id])...` sin `requirePermiso` y sin `scopeQueryPorSede`; `:587-605` — `actionBilleteInfo($numero)`: consulta `billetes_loteria`/`premios` sin permiso ni scope.
- **Impacto:** cualquier usuario autenticado (cualquier rol/tienda/CEDIS) puede leer fichas de pago y estado de boletos de TODA la cadena. Es el mismo patrón ya hallado en la auditoría previa (actionFichaDetalle sin requirePermiso) — sigue abierto.
- **Solución propuesta:** `requirePermiso('caja.pagos')` en ficha-detalle (los hermanos `eliminar-ficha`/`aplicar-pago` ya lo exigen) + scoping por sede del usuario; `billete-info` → permiso de caja y filtro por tiendas del scope (o justificar como catálogo global con decisión explícita).
- **Pruebas:** Codeception: 403 sin permiso; ficha de otra tienda → 404/403 según decisión.
- **Criterio:** 0 actions de Caja sin marcador de protección.

## B-03 · Ruta muerta `GET api/reportes-sorteos-cerrados/sorteos` → 404
- **Categoría:** BUG CONFIRMADO · **Prioridad:** MEDIA · **Plataforma:** Backend
- **Evidencia:** `config/web.php:535` registra `'GET api/reportes-sorteos-cerrados/sorteos'`; `controllers/api/ReportesSorteosCerradosController.php` solo define `actionIndex/ actionDetalle/ actionZonas/ actionTiendas/ actionOptions` (líneas 63/301/376/402/434). Verificado en vivo: HTTP 404.
- **Impacto:** endpoint publicado que responde 404 (ruido de contrato; un consumidor futuro tropezaría).
- **Solución propuesta:** decidir: (a) implementar `actionSorteos` (listado de sorteos del reporte), o (b) eliminar la ruta. 1 línea si (b).
- **Criterio:** la ruta responde 200 con datos o no existe.

## B-04 · Models Yii desincronizados de la BD (~55 de 93)
- **Categoría:** RIESGO · **Prioridad:** MEDIA · **Plataforma:** Backend
- **Evidencia:** cruce programático model-vs-DESCRIBE: 55/93 models con diferencias. Casos con `rules()` sobre columnas inexistentes:
  - `models/Billeteros.php` — rules usan `nombre_billetero`, `id_tienda`, `usuario_creacion/modificacion`, `saldo_actual` (columna `id_tienda` NO existe en `billeteros`; BD real usa `nombres/ape_paterno/ape_materno`, etc.)
  - `models/Cedis.php` — rules con `usuario_creacion/modificacion` inexistentes; `@property ciudad/estado/codigo_postal/email` declarados SIN columna (desync ya conocido, re-confirmado; BD real: `codigo_org_ln/numero_tienda_ln/responsable`)
  - `models/ReintegrosPagados.php` / `models/PremiosPagados.php` — rules con `id_tienda`, `id_turno`, `cantidad` NO presentes en las tablas reales.
- **Impacto:** la validación de esos models NO corre (Yii descarta rules de columnas inexistentes al validar atributos no seguros... o falla al setear); riesgo latente de datos inválidos y confusión de contrato. Ningún controller mobile los usa para escritura crítica (verificado: VentasController usa VentaService), por eso prioridad media.
- **Solución propuesta:** pase de alineación por model usado: `php yii gii/model` diff o generación manual @property desde DESCRIBE; empezar por los 4 citados. NO tocar migrations.
- **Pruebas:** script de verificación model-vs-BD (SELECT/DESCRIBE) en verde.
- **Criterio:** 0 `rules()` sobre columnas inexistentes en models usados activamente.

## B-06 · Suite Codeception data-dependiente (32 F / 11 E, no regresión)
- **Categoría:** RIESGO · **Prioridad:** MEDIA · **Plataforma:** Backend
- **Evidencia:** corrida completa 2026-10-20: 162 tests, 119 OK / 32 F / 11 E. Análisis de cada bloque:
  - `VentasLoteriaAppCest` (3 F): "La siembra debe producir filas" — la siembra en TX del test no es visible para `runAction` (causa exacta PENDIENTE DE VALIDACIÓN, requiere escribir BD con rollback).
  - `VentasTecReporteCest` (3 E): `No quedan boletos TEC asignados para esta prueba` — el billetero 1 de BD dev ya no tiene material TEC disponible.
  - `LoginFormCest` (1 F): login WEB del sitio (`site/login`) — entorno, no API.
  - `ComoVoyDatosCest/ReportesCumplimientoMeta/DestruccionPapelLn/DestruccionPremios/InventarioAlmacen/InventariosTurnoVisible` (24 F): esperan semillas/turnos/tiendas de un snapshot que la BD dev actual no tiene.
  - Suites mobile-relevantes NUEVAS: PermisosVentaEnforcementCest 7/7 OK, VentasSorteosTecAppCest 5/5, TiempoAireAppCest 14/14, PremiosConsultarEscaneoCest 5/5 — la funcionalidad de la app está cubierta y VERDE.
- **Impacto:** la suite no es un semáforo confiable (fallos esperados mezclados con posibles regresiones).
- **Solución propuesta:** (1) seeds idempotentes por suite (`_before` siembra lo que necesita o SKIP claro); (2) separar las suites data-dependent en un grupo `--skip-group dev-data`; (3) investigar la visibilidad de TX del módulo Yii2 (B-06a).
- **Criterio:** `codecept run functional` verde o con fails SOLO documentados como data-skip.

## B-07 · Comisión de proveedor 12% hardcode
- **Categoría:** PENDIENTE DE VALIDACIÓN · **Prioridad:** MEDIA · **Plataforma:** Backend
- **Evidencia:** `controllers/api/PaquetesRecibidosController.php:31` — `const COMISION_PROVEEDOR_PCT = 12.0;` usada por `resolverCostoCompraUnitarioSorteo()` (:613-627) para LN/Zodiaco (serie $800 → $704 → $35.20/cachito), solo lectura (costo de compra).
- **Impacto:** si el % cambia por contrato, requiere código+deploy. No es bug (regla de negocio del ticket 2 15/10); el permiso está correcto (`checkPermiso()` → `requireAnyPermiso(['almacen.inventario','ventas.recepcion'])` cubre todo el controller).
- **Solución propuesta:** mover a `configuracion_sistema` o `params` si se espera variar; decisión de negocio.

## B-08 · TAE simulado persistiría como 'simulada' si se consumiera
- **Categoría:** PENDIENTE DE VALIDACIÓN · **Prioridad:** BAJA · **Plataforma:** Backend
- **Evidencia:** `TiempoAireVentaService` inserta `ventas_electronicas` con `estado_operacion` real/simulada (enum, DEFAULT 'real', UNIQUE folio); reportes Desktop (`ReportesController:2793`, `VentasElectronicasController:63`) excluyen 'simulada' con `COALESCE(estado_operacion,'real')`. La app HOY no consume el POST (M-03).
- **Impacto:** al integrar TAE con el proveedor simulado, las recargas quedarían como 'simulada' (filtradas de reportes) — comportamiento deseado mientras dure el piloto, PERO la app debe reflejar el estado (hoy no lo muestra porque no consume).
- **Solución propuesta:** al integrar (trabajo TAECEL): responder `estado_operacion` en el POST y mostrarlo en la app; idempotencia por folio ya existe (UNIQUE + reintento no duplica, TiempoAireAppCest lo cubre).

## B-12 · Scripts E2E ad-hoc desactualizados
- **Categoría:** RIESGO · **Prioridad:** BAJA · **Plataforma:** QA
- **Evidencia:** `~/e2e_mobile_reportes.py` usa `desde/hasta` (backend espera `fecha_inicio/fecha_fin`) y expectativas del snapshot BD 2026-09-22 → 23 FAILs por drift de datos (los servicios de la app SÍ usan los params correctos, verificado).
- **Solución propuesta:** corrección de params + expectativas parametrizadas; migrar al repo (ver M-07).

---

## Prioridad de ejecución propuesta (a autorizar)
1. **P1 — ALTO:** M-01 (BaseUrl, 1 línea) · B-02 (permisos/scoping Caja) · M-07 (migrar E2E al repo).
2. **P2 — MEDIO:** M-02 (saldo: primero decisión de negocio) · B-03 (ruta muerta) · B-01 (CORS lista blanca) · B-06 (seeds/skip de suite) · B-04 (models 4 críticos) · M-06 (sesiones en Dispositivos).
3. **P3 — BAJO:** M-04/M-05 (contratos pendientes) · B-07/B-08 (validar con negocio) · M-08/M-09 (limpieza) · B-12.

**Nada de lo anterior está implementado.** Cada corrección requiere autorización expresa; las de backend van a `dev`/`dev-mobile` según alcance, las mobile a `master` (flujo inspección→commit→push del usuario).
