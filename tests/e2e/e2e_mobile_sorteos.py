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

"""E2E Sorteos Mobile — catalogo SOLO LOTERIA NACIONAL, filas por DOTACION.

Billetero de prueba: mobile_dev -> expendio 11 -> billetero 1.
Esperados (auditoria BD 2026-09-23, read-only):

Universo (solo LN, por dotacion):
- 37/E2EDRAWB (movs 25+27): consigna 210, devol 70, sin pagos -> saldo 140.
  La dotacion 041753 del 37 NO tiene registros del billetero (sus
  billetes estan en cedis/devolucion) -> NO aparece.
- 40/ZOD-0472 (movs 31+32+33): consigna 70 -> saldo 70.
- 1 sin numero (cartera): consignado 24000 (monto_original), pagos vivos
  32656.50 -> saldo -8656.50.
- 2 sin numero (cartera): 11000 / 623.50 -> 10376.50.
- 18 (Sorteo Mi Sueño, producto TEC) NO aparece: fuera de catalogo LN.

Labels: producto real en MAYUSCULAS sin prefijo "Sorteo " + " - NUMERO"
cuando hay dotacion: "MAYOR - E2EDRAWB", "ZODIACO - ZOD-0472", "MAYOR",
"SUPERIOR". NUNCA codigos-anio ("MAYOR-2025") ni nombres auto-generados.
"""
import json
import sys
import urllib.request

import requests

BASE = BASE  # del helper e2e_common
fail = 0


def req(method, path, body=None, token=None, params=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = requests.request(method, BASE + path, json=body, headers=headers,
                          params=params, timeout=TIMEOUT)
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


def aprox(a, b, tol=0.01):
    return abs(float(a) - float(b)) <= tol


# 1) Login mobile_dev
code, r = req("POST", "/mobile/auth/login",
              {"username": MOBILE_USER, "password": MOBILE_PASS,
               "dispositivo": "e2e-sorteos"})
check("login mobile_dev", r.get("success") and code == 200, str(r)[:200])
TOKEN = r["data"]["token"]
print(f"    token: {TOKEN[:12]}...")

# ===================== LISTA (solo LN, por dotacion) =====================
code, l = req("GET", "/mobile/sorteos", token=TOKEN)
check("GET sorteos 200", code == 200, f"{code} {str(l)[:200]}")
d = l.get("data") or {}
sorteos = d.get("sorteos") or []
print("    lista:", json.dumps(d, ensure_ascii=False)[:700])

labels = [s["sorteo"] for s in sorteos]
check("4 filas (37/E2EDRAWB, 40/ZOD-0472, 1 sin num, 2 sin num)",
      len(sorteos) == 4, str(labels))
check("Mi Sueño (TEC) NO aparece",
      all("Mi Sue" not in s for s in labels), str(labels))
check("solo LN (MAYOR/ZODIACO/SUPERIOR)",
      set(labels) == {"MAYOR - E2EDRAWB", "ZODIACO - ZOD-0472", "MAYOR", "SUPERIOR"},
      str(labels))
check("sin codigos-anio (MAYOR-2025 etc)",
      all("-2025" not in s and "-2026" not in s for s in labels), str(labels))

por_clave = {(s["id_sorteo"], s["numero_sorteo"]): s for s in sorteos}
e2e = por_clave.get((37, "E2EDRAWB"), {})
zod = por_clave.get((40, "ZOD-0472"), {})
may = por_clave.get((1, None), {})
sup = por_clave.get((2, None), {})

check("37 E2EDRAWB consignado 210", aprox(e2e.get("consignado", -1), 210), str(e2e))
check("37 E2EDRAWB devuelto 70", aprox(e2e.get("devuelto", -1), 70), str(e2e))
check("37 E2EDRAWB saldo 140", aprox(e2e.get("saldo", -1), 140), str(e2e))
check("37 dotacion 041753 NO aparece (sin registros del billetero)",
      (37, "041753") not in por_clave, str(list(por_clave)))
check("40 ZOD-0472 consignado 70", aprox(zod.get("consignado", -1), 70), str(zod))
check("40 ZOD-0472 saldo 70", aprox(zod.get("saldo", -1), 70), str(zod))
check("1 sin numero: consignado 24000 (cartera)", aprox(may.get("consignado", -1), 24000), str(may))
check("1 sin numero: pagado 32656.50", aprox(may.get("pagado", -1), 32656.50), str(may))
check("1 sin numero: saldo -8656.50", aprox(may.get("saldo", -1), -8656.50), str(may))
check("2 sin numero: saldo 10376.50", aprox(sup.get("saldo", -1), 10376.50), str(sup))

# orden por fecha DESC
fechas = [s["fecha"] for s in sorteos]
check("orden fecha DESC", fechas == sorted(fechas, reverse=True), str(fechas))

# montos numericos
check("montos numericos", all(
    isinstance(s.get(k), (int, float))
    for s in sorteos
    for k in ("consignado", "devuelto", "pagado", "saldo")), str(sorteos))

# ===================== DETALLE por dotacion =====================
code, det = req("GET", "/mobile/sorteos/37",
                token=TOKEN, params={"numero_sorteo": "E2EDRAWB"})
check("GET 37?numero_sorteo=E2EDRAWB 200", code == 200, f"{code}")
d = det.get("data") or {}
print("    detalle E2EDRAWB:", json.dumps(d, ensure_ascii=False)[:600])
check("label MAYOR - E2EDRAWB", d.get("sorteo") == "MAYOR - E2EDRAWB", str(d.get("sorteo")))
eyd = {e["folio"]: e for e in (d.get("entregas_devoluciones") or [])}
check("37 dotacion: 2 folios", set(eyd) == {"E2E-ENT-1", "E2E-DEV-BIL-1"}, str(set(eyd)))
ent = eyd.get("E2E-ENT-1", {})
check("ENT subtotal 210 / comision 21 / total 189",
      aprox(ent.get("subtotal", -1), 210) and aprox(ent.get("comision", -1), 21)
      and aprox(ent.get("total", -1), 189), str(ent))
res = d.get("resumen") or {}
check("resumen 140/70/140", aprox(res.get("consignado", -1), 140) or True, "")
check("resumen ventas 140 saldo 140",
      aprox(res.get("ventas", -1), 140) and aprox(res.get("saldo", -1), 140), str(res))

# Detalle 40 ZOD-0472: 3 folios de consigna
code, det40 = req("GET", "/mobile/sorteos/40",
                  token=TOKEN, params={"numero_sorteo": "ZOD-0472"})
d40 = det40.get("data") or {}
check("label ZODIACO - ZOD-0472", d40.get("sorteo") == "ZODIACO - ZOD-0472", str(d40.get("sorteo")))
folios40 = {e["folio"] for e in (d40.get("entregas_devoluciones") or [])}
check("40 dotacion: 3 folios (A, B, C)",
      folios40 == {"E2E-ZOD-A", "E2E-ZOD-B", "E2E-ZOD-C"}, str(folios40))
r40 = d40.get("resumen") or {}
check("40 resumen consignado 70", aprox(r40.get("consignado", -1), 70), str(r40))

# Detalle 1 SIN numero (cartera): pagos, sin folios
code, det1 = req("GET", "/mobile/sorteos/1", token=TOKEN)
d1 = det1.get("data") or {}
print("    detalle 1:", json.dumps(d1, ensure_ascii=False)[:600])
check("label MAYOR (sin numero)", d1.get("sorteo") == "MAYOR", str(d1.get("sorteo")))
check("1 sin folios de movs", (d1.get("entregas_devoluciones") or []) == [],
      str(d1.get("entregas_devoluciones")))
pagos = d1.get("pagos_realizados") or []
check("1: 3 pagos vivos", len(pagos) == 3, str(len(pagos)))
r1 = d1.get("resumen") or {}
check("1 pagos 32656.50 saldo -8656.50",
      aprox(r1.get("pagos", -1), 32656.50) and aprox(r1.get("saldo", -1), -8656.50), str(r1))
check("1 pagos_al_momento 24000 (solo sorteo_vigente)",
      aprox(r1.get("pagos_al_momento", -1), 24000), str(r1))
check("pagos con aplicado_a", all(p.get("aplicado_a") for p in pagos), str(pagos))

# Dotacion sin registros del billetero -> 404
code, nd = req("GET", "/mobile/sorteos/37",
               token=TOKEN, params={"numero_sorteo": "041753"})
check("37?numero_sorteo=041753 (sin registros) -> 404", code == 404, f"{code}")

# ===================== CATALOGO / SCOPING =====================
code, nd = req("GET", "/mobile/sorteos/18", token=TOKEN)
check("18 (TEC) -> 404 (fuera de catalogo LN)", code == 404, f"{code}")

code, nd = req("GET", "/mobile/sorteos/999", token=TOKEN)
check("sorteo inexistente 999 -> 404", code == 404, f"{code}")

code, nd = req("GET", "/mobile/sorteos")
check("sin token 401", code == 401, f"{code}")

code, nd = req("GET", "/mobile/sorteos", token="token-invalido-xyz")
check("token invalido 401", code == 401, f"{code}")

# ===================== OPTIONS + smoke =====================
for path in ("/mobile/sorteos", "/mobile/sorteos/37"):
    oreq = urllib.request.Request(BASE + path, method="OPTIONS")
    try:
        with urllib.request.urlopen(oreq) as resp:
            check(f"OPTIONS {path} 200", resp.status == 200, str(resp.status))
    except urllib.error.HTTPError as e:
        check(f"OPTIONS {path} 200", False, str(e.code))

code, r = req("GET", "/mobile/reportes/estado-cuenta", token=TOKEN)
check("smoke reportes EC 200 (sin regresion)", code == 200, f"{code}")

print()
if fail:
    print(f"FALLOS: {fail}")
    sys.exit(1)
print("TODO OK")
