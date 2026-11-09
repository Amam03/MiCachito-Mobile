#!/usr/bin/env python3
# Migrado a tests/e2e/ (M-07, auditoria 2026-10-20): configuracion por entorno
# en e2e_common.py (E2E_BASE_URL / E2E_MOBILE_USER / E2E_MOBILE_PASS / E2E_DOT_*).
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from e2e_common import (  # noqa: E402
    BASE,
    TIMEOUT,
    MOBILE_USER,
    MOBILE_PASS,
    DOT_USER,
    DOT_PASS,
)

"""E2E Depósitos Mobile — /api/mobile/depositos (nueva integración).

Contrato:
  GET  /mobile/depositos          historial del billetero de la sesión
  POST /mobile/depositos          registro (folio servidor, estatus pendiente)
  GET  /mobile/depositos/cuentas  catálogo de cuentas activas

Billetero de prueba: mobile_dev -> expendio 11 -> billetero 1.
Limpieza: NO crea registros permanentes de prueba — los depósitos POSTeados
se registran de verdad (estatus 'pendiente'), igual que haría un billetero;
se listan al final para revisión manual.
"""
import requests

BASE = BASE  # del helper e2e_common
fail = 0


def req(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = requests.request(method, BASE + path, json=body, headers=headers, timeout=TIMEOUT)
    try:
        return r.status_code, r.json()
    except ValueError:
        return r.status_code, {"raw": r.text[:300]}


def check(nombre, cond, detalle=""):
    global fail
    marca = "OK " if cond else "FAIL"
    if not cond:
        fail += 1
    print(f"[{marca}] {nombre}" + (f" -> {detalle}" if detalle and not cond else ""))


# 1) Login mobile_dev
code, r = req("POST", "/mobile/auth/login", {"username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-depositos"})
check("login mobile_dev", r.get("success"), str(r)[:200])
TOKEN = r["data"]["token"]

# 2) Catálogo de cuentas activas
code, r = req("GET", "/mobile/depositos/cuentas", token=TOKEN)
check("GET cuentas 200", code == 200 and r.get("success"), str(r)[:200])
cuentas = r.get("data") or []
print(f"    cuentas activas: {len(cuentas)}")
for c in cuentas:
    print(f"    id={c['id_cuenta']} {c['banco']} {c['numero_cuenta']} label='{c['label']}'")
check("catálogo no vacío", len(cuentas) > 0, str(cuentas)[:200])
ID_CUENTA = cuentas[0]["id_cuenta"]

# 3) Auth: sin token -> 401
code, r = req("GET", "/mobile/depositos")
check("sin token -> 401", code == 401, f"{code} {str(r)[:120]}")

# 4) Historial inicial (todos los estatus, más reciente primero)
code, r = req("GET", "/mobile/depositos", token=TOKEN)
check("GET historial 200", code == 200 and r.get("success"), str(r)[:200])
historial = r.get("data") or []
print(f"    historial: {len(historial)} depósitos")
for d in historial[:5]:
    print(f"    {d['folio_deposito']} {d['banco'] or '(sin banco)'} ${d['monto']} {d['estatus']} ref={d.get('referencia')}")
fechas = [d.get("fecha_deposito") or "" for d in historial]
check("orden descendente", fechas == sorted(fechas, reverse=True), str(fechas)[:200])

# 5) POST válido: folio servidor + estatus pendiente
code, r = req("POST", "/mobile/depositos", {
    "id_cuenta_bancaria": ID_CUENTA,
    "fecha_deposito": "2026-10-07",
    "hora_deposito": "10:30",
    "monto": 1500.50,
    "referencia": "E2E-DEP-1",
    "observaciones": "Prueba E2E - Sorteo 14201",
}, token=TOKEN)
check("POST depósito 200", code == 200 and r.get("success"), str(r)[:300])
nuevo = r.get("data") or {}
check("folio servidor DEP-", str(nuevo.get("folio_deposito", "")).startswith("DEP-"), str(nuevo))
check("estatus pendiente", nuevo.get("estatus") == "pendiente", str(nuevo))
check("id_deposito int", isinstance(nuevo.get("id_deposito"), int), str(nuevo))
ID_DEPOSITO = nuevo.get("id_deposito")
FOLIO_NUEVO = nuevo.get("folio_deposito")

# 6) El historial lo incluye con banco (JOIN) y hora normalizada HH:mm:ss
code, r = req("GET", "/mobile/depositos", token=TOKEN)
historial = r.get("data") or []
match = next((d for d in historial if d["id_deposito"] == ID_DEPOSITO), None)
check("POST aparece en historial", match is not None, str([d['folio_deposito'] for d in historial])[:300])
if match:
    check("banco presente (JOIN)", bool(match.get("banco")), str(match)[:200])
    check("hora normalizada 10:30:00", match.get("hora_deposito") == "10:30:00", str(match.get("hora_deposito")))
    check("monto decimal 1500.5", abs(match.get("monto", 0) - 1500.50) < 0.001, str(match.get("monto")))

# 7) Validaciones: 422 en cuerpo inválido
validaciones = [
    ("sin id_cuenta", {"fecha_deposito": "2026-10-07", "monto": 100}),
    ("cuenta inexistente", {"id_cuenta_bancaria": 999999, "fecha_deposito": "2026-10-07", "monto": 100}),
    ("sin fecha", {"id_cuenta_bancaria": ID_CUENTA, "monto": 100}),
    ("fecha inválida", {"id_cuenta_bancaria": ID_CUENTA, "fecha_deposito": "basura", "monto": 100}),
    ("fecha strtotime", {"id_cuenta_bancaria": ID_CUENTA, "fecha_deposito": "2026-13-45", "monto": 100}),
    ("monto 0", {"id_cuenta_bancaria": ID_CUENTA, "fecha_deposito": "2026-10-07", "monto": 0}),
    ("monto texto", {"id_cuenta_bancaria": ID_CUENTA, "fecha_deposito": "2026-10-07", "monto": "abc"}),
    ("hora 99:99", {"id_cuenta_bancaria": ID_CUENTA, "fecha_deposito": "2026-10-07", "hora_deposito": "99:99", "monto": 100}),
]
for nombre, body in validaciones:
    code, r = req("POST", "/mobile/depositos", body, token=TOKEN)
    check(f"422 {nombre}", code == 422, f"{code} {str(r)[:150]}")

# 8) Escalation: id_billetero/estatus/tipo_deposito del request se IGNORAN
code, r = req("POST", "/mobile/depositos", {
    "id_cuenta_bancaria": ID_CUENTA,
    "fecha_deposito": "2026-10-07",
    "monto": 200,
    "id_billetero": 999,
    "estatus": "validado",
    "tipo_deposito": "efectivo",
}, token=TOKEN)
check("POST ignora campos del request", code == 200 and r.get("success"), str(r)[:200])
if r.get("success"):
    id_esc = r["data"]["id_deposito"]
    code, r = req("GET", "/mobile/depositos", token=TOKEN)
    esc = next((d for d in (r.get("data") or []) if d["id_deposito"] == id_esc), None)
    check("estatus forzado pendiente", esc is not None and esc["estatus"] == "pendiente", str(esc)[:200])

# 9) Aislamiento: otro billetero NO ve los depósitos de mobile_dev
code, r = req("POST", "/mobile/auth/login", {"username": DOT_USER, "password": DOT_PASS, "dispositivo": "e2e-depositos-2"})
if r.get("success"):
    TOKEN2 = r["data"]["token"]
    code, r = req("GET", "/mobile/depositos", token=TOKEN2)
    folios_otro = {d["folio_deposito"] for d in (r.get("data") or [])}
    check("aislamiento entre billeteros", FOLIO_NUEVO not in folios_otro, str(list(folios_otro))[:200])
else:
    print("[SKIP] aislamiento: e2e_dot no disponible")

print()
print(f"Depósitos E2E de prueba registrados (estatus pendiente, para revisión): {FOLIO_NUEVO}")
print(f"RESULTADO: {'FALLAS=' + str(fail) if fail else 'TODO OK'}")
raise SystemExit(1 if fail else 0)
