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

"""E2E Tickets de Venta Mobile — /api/mobile/ventas + /{id} (nueva integración).

Contrato:
  GET  /mobile/ventas         historial del billetero de la sesión (D5: TODAS,
                              cualquier estatus, fecha descendente)
  GET  /mobile/ventas/{id}    detalle con partidas (404 si es ajena)

Billeteros de prueba:
  mobile_dev -> expendio 11 -> billetero 1 (ventas reales 73/224/225/284/401/402)
  e2e_dot    -> expendio 12 -> billetero 11 (ventas propias 128-136+)

Limpieza: NINGUNA — el flujo es 100% lectura, no toca datos.
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


# 1) Login billetero 1 (mobile_dev)
code, r = req("POST", "/mobile/auth/login", {
    "username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-tickets"})
check("login mobile_dev", r.get("success"), str(r)[:200])
TOKEN = r["data"]["token"]

# 2) Auth: sin token -> 401
code, r = req("GET", "/mobile/ventas")
check("sin token -> 401", code == 401, f"{code} {str(r)[:120]}")

# 3) Historial del billetero 1: exactamente sus ventas, fecha descendente
code, r = req("GET", "/mobile/ventas", token=TOKEN)
check("GET historial 200", code == 200 and r.get("success"), str(r)[:200])
historial = r.get("data") or []
ids = [v["id_venta"] for v in historial]
print(f"    historial: {len(historial)} ventas -> ids {ids}")
check("exactamente las ventas del billetero 1", set(ids) == {73, 224, 225, 284, 401, 402}, str(ids))
fechas = [v["fecha_venta"] or "" for v in historial]
check("orden fecha descendente", fechas == sorted(fechas, reverse=True), str(fechas)[:200])

# 4) Estructura de las filas: folio/tipo/estatus/descripcion/cliente
por_id = {v["id_venta"]: v for v in historial}
v284 = por_id.get(284, {})
check("folio 284 = '1103'", v284.get("folio") == "1103", str(v284)[:200])
check("tipo 284 = lotenal", v284.get("tipo") == "lotenal", str(v284)[:200])
check("total 284 = 30.00", v284.get("total") == 30.0, str(v284.get("total")))
check("descripcion 284 = 'Sorteo 041753' (sorteo REAL de la partida)",
      v284.get("descripcion") == "Sorteo 041753", repr(v284.get("descripcion")))
check("cliente 284 = None (PÚBLICO EN GENERAL)", v284.get("cliente") is None, str(v284.get("cliente"))[:120])
v401 = por_id.get(401, {})
check("tipo 401 = sorteos_tec", v401.get("tipo") == "sorteos_tec", str(v401)[:150])
check("cliente 401 PERLA E2E TEC", (v401.get("cliente") or {}).get("nombre") == "PERLA E2E TEC", str(v401.get("cliente"))[:150])
v402 = por_id.get(402, {})
check("estatus 402 = cancelada (D5: TODAS)", v402.get("estatus") == "cancelada", str(v402)[:150])
v73 = por_id.get(73, {})
check("folio legacy 73 V-20260831-39395", v73.get("folio") == "V-20260831-39395", str(v73)[:150])
check("estatus 73 = devuelta", v73.get("estatus") == "devuelta", str(v73)[:150])

# 5) Detalle LN con billete (284: boleto 0101 serie 01, 30.00)
code, r = req("GET", "/mobile/ventas/284", token=TOKEN)
check("GET detalle 284 200", code == 200 and r.get("success"), str(r)[:200])
d = r.get("data") or {}
check("detalle 284 total 30", d.get("total") == 30.0, str(d)[:200])
check("detalle 284 vendedor = billetero", d.get("vendedor") == "Billetero Uno Test", str(d.get("vendedor")))
check("detalle 284 cedis", d.get("cedis") == "CEDIS Puebla", str(d.get("cedis")))
partidas = d.get("partidas") or []
check("detalle 284 1 partida", len(partidas) == 1, str(len(partidas)))
p = partidas[0] if partidas else {}
check("partida boleto 0101", p.get("numero_billete") == "0101", str(p)[:200])
check("partida serie 01", p.get("serie") == "01", str(p)[:200])
check("partida precio 30", p.get("precio_unitario") == 30.0, str(p.get("precio_unitario")))
check("partida sorteo 'Sorteo 041753'", p.get("nombre_sorteo") == "Sorteo 041753", str(p.get("nombre_sorteo")))
check("total == SUM(partidas)", abs(d.get("total", 0) - sum(x.get("subtotal", 0) for x in partidas)) < 0.01, "")

# 6) Detalle TEC con cliente (401) y detalle multi-partida legacy (73: 3 partidas sin billete)
code, r = req("GET", "/mobile/ventas/401", token=TOKEN)
d = r.get("data") or {}
check("detalle 401 tipo sorteos_tec", d.get("tipo") == "sorteos_tec", str(d)[:150])
check("detalle 401 boleto TEC003", (d.get("partidas") or [{}])[0].get("numero_billete") == "TEC003", str(d.get("partidas"))[:200])
check("detalle 401 cliente PERLA", (d.get("cliente") or {}).get("nombre") == "PERLA E2E TEC", str(d.get("cliente"))[:150])
code, r = req("GET", "/mobile/ventas/73", token=TOKEN)
d = r.get("data") or {}
check("detalle 73 3 partidas legacy", len(d.get("partidas") or []) == 3, str(len(d.get("partidas") or [])))
check("detalle 73 partidas sin billete (null)", all(p.get("numero_billete") is None for p in d.get("partidas") or []), "")
check("detalle 73 total 210 == SUM", abs(d.get("total", 0) - sum(p.get("subtotal", 0) for p in d.get("partidas") or [])) < 0.01, "")

# 7) Aislamiento: venta ajena -> 404 (sin revelar existencia)
code, r = req("GET", "/mobile/ventas/136", token=TOKEN)
check("venta ajena (136, billetero 11) -> 404", code == 404, f"{code} {str(r)[:120]}")
code, r = req("GET", "/mobile/ventas/999999", token=TOKEN)
check("venta inexistente -> 404", code == 404, f"{code} {str(r)[:120]}")

# 8) Aislamiento cruzado: billetero 11 ve SOLO sus ventas
code, r = req("POST", "/mobile/auth/login", {
    "username": DOT_USER, "password": DOT_PASS, "dispositivo": "e2e-tickets-b11"})
check("login e2e_dot (billetero 11)", r.get("success"), str(r)[:200])
T11 = r["data"]["token"]
code, r = req("GET", "/mobile/ventas", token=T11)
h11 = r.get("data") or []
ids11 = {v["id_venta"] for v in h11}
check("billetero 11 no ve las ventas del 1", ids11.isdisjoint({73, 224, 225, 284, 401, 402}), str(sorted(ids11))[:200])
code, r = req("GET", "/mobile/ventas/284", token=T11)
check("billetero 11 pidiendo venta del 1 -> 404", code == 404, f"{code} {str(r)[:120]}")

# 9) Regresión: rutas literales de Vender intactas
code, r = req("GET", "/mobile/ventas/sorteos", token=TOKEN)
check("GET ventas/sorteos (literal) 200", code == 200 and r.get("success"), f"{code} {str(r)[:120]}")
code, r = req("GET", "/mobile/ventas/sorteos-tec", token=TOKEN)
check("GET ventas/sorteos-tec (literal) 200", code == 200 and r.get("success"), f"{code} {str(r)[:120]}")

print()
print("RESULTADO:", "TODO OK" if fail == 0 else f"{fail} FALLOS")
raise SystemExit(0 if fail == 0 else 1)
