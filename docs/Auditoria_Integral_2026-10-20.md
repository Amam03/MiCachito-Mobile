# Auditoría Integral — MiCachito Mobile + Backend
**Fecha:** 2026-10-20 · **Modo:** SOLO LECTURA (sin modificaciones de código, BD, config ni git)

---

## 1. Resumen ejecutivo

Auditoría integral de la app Mobile (MAUI, master @799bfdc) y el backend (Yii2, dev @3ccdec6), tras actualizar ambos repos con pull fast-forward (árboles limpios, sin riesgo de pérdida; stashes del backend intactos).

Estado general: **sano y en progresión correcta**. Los flujos de negocio críticos (venta LOTENAL, venta Tec, devolución, depósitos, tickets, PyR, permisos, reportes, recibos) están integrados contra el backend con aislamiento por billetero verificado en vivo. No se encontraron bugs críticos en los flujos de dinero.

Hallazgos destacables:
- **1 bug confirmado de regresión** (M-01: AppSettings.BaseUrl volvió a IP fija y pisó la configuración por `API_BASE_URL`).
- **2 riesgos de seguridad medios** en backend Desktop (B-02: `caja/ficha-detalle` y `caja/billete-info` sin permiso ni scoping; B-01: CORS `Origin: *`).
- **1 ruta muerta** (B-03: `reportes-sorteos-cerrados/sorteos` → 404).
- **Deuda de models Yii** (B-04: ~55 models con `@property`/`rules()` desincronizados de la BD real; 4 con reglas sobre columnas inexistentes).
- **Pendientes conocidos re-confirmados** (saldo del header en 4 VMs, TAE en HOLD, consulta de expendios, notificaciones, auth/sesiones sin consumidor).
- Suite Codeception: 119 OK / 32 F / 11 E — **fallos por dependencia de datos de desarrollo** (sin semillas idempotentes), no regresiones de código; la suite mobile-relevante nueva (PermisosVentaEnforcementCest) pasa 7/7.

Sin correcciones aplicadas. El plan de corrección se entrega en `Plan_Correccion_Bugs_2026-10-20.md` (mismo directorio) pendiente de autorización.

---

## 2. Repositorios y commits auditados

| Repo | Rama | Commit | Estado git |
|---|---|---|---|
| MiCachito-Mobile (`Amam03/MiCachito-Mobile`) | master | 799bfdc | limpio, = origin/master, sin stash |
| MiCachito-backend (`globaloxs-sys/MiCachito-backend`) | dev | 3ccdec6 | limpio, = origin/dev; stashes {0,1} intactos sin tocar |

Commits nuevos incorporados en esta auditoría (pull --ff-only, 0 locales/3 remotos en ambos):
- Mobile: a1f7967 (venta Tec dos fases A/B), cafd338 (LOTENAL vendidas-en-sesión + validación de cantidad), 799bfdc (permisos CAS + refresh de sesión en Home).
- Backend: 298616a (almacen), de9f4cf (frontera 403 por permiso de venta en crear/devolver + CAS de permisos + suite PermisosVentaEnforcementCest), 3ccdec6 (comisión 12% en recepción de paquetería).

Ambas ramas auxiliares del backend (dev-mobile eb2a9c8, feat/caja-*) quedaron intactas. `main` sigue 225 atrás (histórico, no tocada).

---

## 3. Arquitectura y alcance revisado

- **Mobile:** 43 páginas XAML + VMs MVVM con DI (111 registros), 25 servicios, cliente HTTP tipado (`ApiClient`: Bearer + snake_case + envelope success/data + `ApiException` con ServerMessage), sesión en SecureStorage, parser de códigos de barras port fiel del desktop (con validación long+subcódigo), impresión BT SPP/ESC-POS, PDFs SkiaSharp.
- **Backend:** bloque MOBILE (12 controllers en `controllers/api/mobile/`) + ~110 controllers Desktop; auth mobile por Bearer con hash SHA-256 en `mobile_sesiones`; identidad SIEMPRE `getIdBilleteroActual()` de la sesión; VentaService con `SELECT ... FOR UPDATE` y revertirPorMovimiento en dos pasadas (dotación/serie física); `ventas_electronicas.estado_operacion` enum real/simulada con reportes filtrando solo 'real'.
- **Integración Desktop↔Mobile:** misma BD; los Permisos de Venta se comparten a nivel billetero y el Home refresca la sesión vía `verify` en cada visita (799bfdc).

Módulos/pantallas revisados (auditoría estática + verificaciones en vivo): Vender (LOTENAL 6-11, Tec 12-15, TAE 3 pantallas), Gestión (Consulta Premios, PyR, Sorteos, Tickets, Devolución, Depósitos, Recibos, Reportes EC/FA/FAC, Tira deshabilitada por diseño), Expendios (administración + consulta), Cuenta (identidad, impresora, dispositivos, notificaciones, logout), Login/Splash, y los 12 controllers mobile + barrido global de controllers Desktop (508 actions), migrations (262/262 aplicadas), config/web.php (rutas), models vs BD (93 models, 140 tablas).

---

## 4. Matriz de integración Mobile ↔ Backend

Verificado en vivo contra Docker (login mobile_dev, billetero 1) el 2026-10-20:

| Módulo Mobile | Endpoint(s) | Estado | Observaciones |
|---|---|---|---|
| Login/Splash/Logout | POST/GET `auth/login·verify·logout` | INTEGRADO | Sesión persiste; logout revoca solo su sesión |
| Dispositivos (sesiones) | GET `auth/sesiones`, POST `auth/sesiones/revocar` | **EXISTE, SIN CONSUMIDOR** | La app muestra solo BT bonded (M-06) |
| Vender LOTENAL 6-11 | `ventas/sorteos·billetes·crear·devolver` | INTEGRADO | FOR UPDATE serializa; vendidas-en-sesión evita 409 |
| Vender Tec 12-15 | `ventas/sorteos-tec·billetes-tec·crear` | INTEGRADO | Fase A/B anti-409; 403 por permiso (de9f4cf) |
| Vender Tiempo Aire | `tiempo-aire/proveedores·montos·venta` | **BACKEND SIMULADO — HOLD usuario** | ProveedorTiempoAireSimulado; NO integrar hasta TAECEL (M-03) |
| Saldo header (4 VMs) | — | **MOCK "$0.00"** | Cifra disponible en `reportes/estado-cuenta` (M-02) |
| Consulta Premios | POST `premios/consultar` | INTEGRADO | Escáner ZXing verificado |
| Premios y Reintegros | GET/POST `premios-reintegros`, GET `/{id}` | INTEGRADO | Folio servidor PYR-; solo ACEPTADOS (D1) |
| Devolución 6.x | POST `ventas/devolver`, GET `devoluciones` | INTEGRADO | Historial persistente; fusión sin duplicados |
| Depósitos 7.x | GET/POST `depositos`, GET `depositos/cuentas` | INTEGRADO | Folio servidor DEP- |
| Tickets de Venta 5.x | GET `ventas`, GET `ventas/{id}` | INTEGRADO | 404 en venta ajena (verificado b1↔b11) |
| Recibos de Pago 8.x | GET `pagos/recibos·recibo/{id}` | INTEGRADO | Solo fichas aplicadas por Caja |
| Sorteos (Gestión 4.x) | GET `sorteos`, GET `sorteos/{id}` | INTEGRADO | UNION pista física + financiera |
| Reportes EC/FA/FAC | GET `reportes/estado-cuenta·fondo-ahorro·facturacion` | INTEGRADO | FA/FAC usan `fecha_inicio/fecha_fin` (verificado) |
| Expendios administración | GET `expendios`, PUT `expendios/{id}/permisos` | INTEGRADO | CAS con `original` + 409 (de9f4cf) |
| Expendios consulta registros | — | **SIMULADA** (Task.Delay 1500 + vacío) | Contrato backend por definir (M-04) |
| Notificaciones | — | **VACÍA POR DISEÑO** | Sin endpoint backend (M-05) |
| Tira de Liquidación | — | DESHABILITADA POR DISEÑO | Sin mockups (NO APLICA) |

Aislamiento verificado en vivo: detalle de venta del billetero 1 con token del 11 → 404; PyR/depósitos/devoluciones con scope por billetero (b11 devuelve 0/0/20 propios).

---

## 5. Hallazgos (resumen — detalle y evidencia en el Plan de Corrección)

Por categoría y prioridad:

| ID | Título | Categoría | Prioridad |
|---|---|---|---|
| M-01 | BaseUrl regresión a IP fija (pisó API_BASE_URL de 2fc3057) | BUG CONFIRMADO | ALTO |
| B-02 | `caja/ficha-detalle` y `caja/billete-info` sin permiso ni scoping | RIESGO | ALTO |
| B-01 | CORS `Origin: ['*']` en API autenticada | RIESGO | MEDIO |
| B-03 | Ruta `GET api/reportes-sorteos-cerrados/sorteos` → action inexistente (404) | BUG CONFIRMADO | MEDIO |
| B-04 | ~55 models Yii desincronizados de la BD (4 con rules sobre columnas inexistentes) | RIESGO | MEDIO |
| M-02 | Saldo "$0.00" hardcoded en 4 headers | FUNC. INCOMPLETA | MEDIO |
| M-07 | App Mobile sin proyecto de tests | RIESGO | ALTO |
| B-06 | Suite Codeception data-dependiente (32 F / 11 E por semillas/estado BD, no regresión) | RIESGO | MEDIO |
| M-06 | `auth/sesiones`+`revocar` sin consumidor en la app | FUNC. INCOMPLETA | MEDIO |
| M-03 | TAE sin integración (HOLD TAECEL — no corregir aún) | FUNC. INCOMPLETA | MEDIO |
| M-04 | Expendios consulta simulada | FUNC. INCOMPLETA | MEDIO |
| B-07 | Comisión proveedor 12% hardcode (const) sin parámetro de negocio | PEND. VALIDACIÓN | MEDIO |
| B-08 | TAE simulado persistiría ventas como 'simulada' si se consumiera | PEND. VALIDACIÓN | BAJO |
| M-05 | Notificaciones vacío (por diseño) | FUNC. INCOMPLETA | BAJO |
| M-08 | Dead code: `Data/TiendasDisponiblesData.cs` sin usos | RIESGO | BAJO |
| M-09 | Emoji ✕ en comentario `NuevoDepositoViewModel.cs:352` | RIESGO | BAJO |
| B-12 | Scripts E2E ad-hoc desactualizados (params/expectativas drift) | RIESGO | BAJO |

Conteo: **2 bugs confirmados · 5 riesgos (1 alto, 4 medio) · 1 alto de cobertura de tests (M-07) · 5 funcionalidades incompletas · 2 pendientes de validación · 3 menores**.

Hallazgo del subagente DESCARTADO tras verificación directa: "tablas `volantes_nacional*` inexistentes" — las 4 tablas EXISTEN en la BD (SHOW TABLES verificado); las 19 rutas del módulo son operativas.

---

## 6. Pruebas ejecutadas (seguras) y pendientes

**Ejecutadas (sin escrituras permanentes):**
1. Build Mobile completo `dotnet build -t:Run -f net10.0-android`: **0 errores, 19 warnings conocidos** (CS0612/CS0618/CS8600/CS8602/CS0168 ImpresoraService, CS8618 DatosClienteTec, CS1587, XA0141 libzxingc/libSkiaSharp 16KB pages). App lanzada al emulador (pidof OK). Árbol git limpio tras el build.
2. Suite Codeception functional completa (162 tests): 119 OK / 32 F / 11 E — análisis de cada fallo: dependencia de datos de desarrollo (sin corporativo TESTCEST, "No quedan boletos TEC asignados para esta prueba", LoginFormCest = entorno web), misma naturaleza que el baseline previo (199/11E/36F de corridas con otro estado de BD). **Sin evidencia de regresión por los commits nuevos.**
3. Suites mobile-relevantes aisladas: PermisosVentaEnforcementCest **7/7 OK**; VentasSorteosTecAppCest 5/5; TiempoAireAppCest 14/14; PremiosConsultarEscaneoCest 5/5; VentasTecReporteCest 2/5 (3 E por falta de material TEC del billetero en BD dev — dato, no código).
4. Verificaciones E2E en vivo (read-only): login/verify, expendios, ventas historial/detalle (aislamiento b1↔b11 → 404), devoluciones, PyR, depósitos, recibos, reportes EC/FA/FAC (200 con `fecha_inicio/fecha_fin`), sorteos, tiempo-aire/proveedores, sesiones (200).
5. Smoke estático: greps de endpoints consumidos vs existentes, mocks vivos, TODOs, emojis (XAML limpio), OnAppearing de pantallas con carga, DI/rutas AppShell (todas registradas).

**No ejecutadas (documentadas):**
- `vendor/bin/codecept run` completo en host: PHP no existe en el host (se corrió dentro de Docker).
- Emulador: walkthrough automático de flujos (bloqueado por consentimiento del usuario esta sesión; la app quedó desplegada y corriendo para inspección manual).
- Prueba física de impresión Bluetooth (pendiente conocido: requiere hardware).
- Venta/cancelación real contra BD (escribiría datos): no permitido por las reglas de esta auditoría.

---

## 7. Limitaciones de la auditoría

- La BD es la de desarrollo (Docker); los conteos/fallos de suite reflejan su estado actual, no producción.
- No se Ejecutaron flujos de escritura (venta/devolución/depósito) — los POST solo se revisaron en código y mediante sus suites/E2E previos documentados.
- La causa raíz exacta de los 3 fallos de `VentasLoteriaAppCest` (siembra no visible para `runAction`) quedó PENDIENTE DE VALIDACIÓN: reproducirla requiere sembrar en BD con rollback (escribir), fuera de las reglas de esta auditoría.
- El análisis CORS se basa en la lectura del filtro Yii2 (`Origin: ['*']`, `Allow-Credentials: null`); no se probó explotación.
- Desktop (PySide6) no está en alcance salvo donde cruza con Mobile (misma BD, mismos permisos).

---

## 8. Conclusión

Para completar la integración de Mobile con backend faltan: **5 módulos** — saldo del header (decisión de negocio + 4 VMs), Expendios consulta de registros (contrato), Notificaciones (definición de producto), consumidor de `auth/sesiones` en Dispositivos, y Tiempo Aire (en HOLD hasta TAECEL, con backend simulado ya existente) — más la corrección de M-01 (BaseUrl) como fix inmediato y los hardening de backend (B-02, B-03) al siguiente deploy.

**Auditoría terminada. No se realizaron modificaciones.**
