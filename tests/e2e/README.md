# E2E — Mi Cachito Mobile (tests/e2e/)

Scripts E2E de la integración Mobile ↔ Backend, corridos contra el backend de
desarrollo (Docker `micachito-php` + `micachito-mysql`). Migrados del set
histórico que vivía fuera del repositorio (M-07, auditoría integral
2026-10-20) con la configuración adaptada: **nada de IPs, usuarios ni
passwords fijos en el código** — todo sale de variables de entorno con
defaults de desarrollo.

## Requisitos

- Backend corriendo (`docker-compose up` del backend, o PHP local en 8080).
- Python 3.8+ y `requests` (`pip install requests`) — los scripts que usan
  `urllib` no necesitan nada extra.
- BD de desarrollo con los billeteros de prueba sembrados
  (`mobile_dev` → expendio 11 → billetero 1; `e2e_dot` → billetero 11).

## Configuración (variables de entorno)

| Variable | Default | Uso |
|---|---|---|
| `E2E_BASE_URL` | `http://localhost:8080/api` | URL base de la API |
| `E2E_TIMEOUT_S` | `15` | timeout por petición (s) |
| `E2E_MOBILE_USER` | `mobile_dev` | expendio de la app |
| `E2E_MOBILE_PASS` | `DevMobile2026!` | password del expendio |
| `E2E_DOT_USER` | `e2e_dot` | 2º billetero (aislamiento) |
| `E2E_DOT_PASS` | `DevMobile2026!` | password del 2º |
| `E2E_ADMIN_USER` / `E2E_ADMIN_PASS` | `admin` / `admin123` | solo `e2e_tec_cliente.py` (login Desktop para ver el reporte) |

Ejemplos:

```bash
# todo default (backend local en 8080):
python3 tests/e2e/e2e_mobile_premios.py

# contra el backend del host desde otra máquina:
E2E_BASE_URL=http://192.168.100.56:8080/api python3 tests/e2e/e2e_mobile_premios.py

# más lento (red WiFi):
E2E_TIMEOUT_S=30 python3 tests/e2e/e2e_mobile_sorteos_dotaciones.py
```

## Scripts y qué cubre cada uno

| Script | Módulo | Escrituras |
|---|---|---|
| `e2e_mobile_premios.py` | Consulta de Premios (POST premios/consultar) | NO |
| `e2e_mobile_sorteos.py` | Gestión > Sorteos (GET) | NO |
| `e2e_mobile_sorteos_dotaciones.py` | Vender LOTENAL: dotaciones/series/fracciones | NO (solo lecturas del inventario) |
| `e2e_mobile_ventas_historial.py` | Tickets de Venta (GET ventas + detalle, aislamiento b1↔b11) | NO |
| `e2e_mobile_devoluciones_historial.py` | Devolución (GET historial, POST devolver + cancelar-estatus) | SÍ (devolución de prueba) |
| `e2e_mobile_premios_reintegros.py` | Premios y Reintegros (GET/POST/detalle) | SÍ (captura de prueba) |
| `e2e_mobile_depositos.py` | Depósitos (GET/POST, folio servidor DEP-) | SÍ (depósito pendiente) |
| `e2e_mobile_pagos.py` | Recibos de Pago (GET) | NO (requiere semillas) |
| `e2e_mobile_reportes.py` | Reportes EC/FA/FAC (GET, params reales) | NO |
| `e2e_mobile_sesiones.py` | Sesiones Activas M-06 (GET sesiones + revocar, aislamiento b1↔b11, actual viva tras revocar secundaria) | SÍ (revoca solo sesiones E2E creadas por el script) |
| `e2e_tec_cliente.py` | Venta Sorteos Tec con datos de cliente + reporte Desktop | SÍ (venta TEC de prueba) |

**Scripts NO migrados (a propósito):**

- `e2e_dev_*.py`, `e2e_trazabilidad.py`: son del flujo Desktop/backend
  (usuario `almacen`), viven con el backend, no con la app.
- `seed_pagos_flujo_real.py`: SEMILLA que crea datos permanentes en la BD
  (fichas/pagos/cartera) — no es un test, es una herramienta de siembra
  destructiva para QA manual; no debe correrse en lote.
- DEPENDEN DE SEMILLAS / SNAPSHOT (fallarán con drift de datos — NO es
  regresión mientras las validaciones de forma/auth pasen):
  - `e2e_mobile_pagos.py`: espera los recibos sembrados por la herramienta
    `seed_pagos_flujo_real.py` (recibos 54/56 de sep-2026, fichas de caja).
    Correrla primero (escribe BD de desarrollo de verdad) o ejecutar solo
    sus checks de forma (200 lista / 404 / 401, que sí corren verdes).
  - `e2e_mobile_reportes.py`: verifica cifras exactas del snapshot
    2026-09-22 de la BD de desarrollo; falla si la BD avanzó (drift).
  Revisar el detalle de cada FAIL antes de asumir regresión.

## Convenciones

- Todo script termina con `FALLOS: N` y exit code 1 si algo falló —
  aptos para CI.
- Los que escriben crean registros reales como lo haría un billetero
  (estatus pendiente/aplicado); **ninguno borra ni modifica datos
  existentes**.
- Credenciales y URL SIEMPRE desde `e2e_common.py` (o variables propias
  documentadas arriba) — no hardcodear de vuelta.

## Unit tests

Los unit tests del parser de códigos (`BilleteParser`) están en
`tests/MiCachito.Mobile.Tests/` (xUnit, .NET): `dotnet test` desde la raíz.
No requieren Android ni backend — son puros.
