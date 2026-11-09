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

"""E2E Recibos de Pago Mobile — contrato nuevo (agrupación por movimiento,
desglose formas/documentos formato mockup 8.1, crédito oficial).

Billetero de prueba: mobile_dev -> expendio 11 -> billetero 1.
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
code, r = req("POST", "/mobile/auth/login", {"username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-pagos"})
check("login mobile_dev", r.get("success"), str(r)[:200])
TOKEN = r["data"]["token"]

# 2) Lista: recibos agrupados por movimiento
code, r = req("GET", "/mobile/pagos/recibos", token=TOKEN)
check("GET recibos 200", code == 200, str(r)[:200])
recibos = r.get("data") or []
print(f"    recibos: {len(recibos)}")
for it in recibos:
    print(f"    id={it['id_ficha_pago']} {it['folio_ficha']} {it['tipo_pago']} ${it['monto']} aplic={it.get('fecha_aplicacion')}")

# El grupo test-uuid-1234 (44-47) y las CC-TEST del billetero 1 fueron
# ELIMINADOS de la BD (limpieza 2026-09-21): no deben aparecer.
folios = {it["folio_ficha"] for it in recibos}
check("datos de prueba eliminados", not any(f.startswith(("FP-20260527", "CC-TEST")) for f in folios), str(folios))
check("movimientos nuevos presentes", {"FP-20260921-0001", "FP-20260921-0003"} <= folios, str(folios))
check("semillas oficiales presentes", {"FP-20260225-0001", "FP-20260301-0001"} <= folios, str(folios))
check("lista = 4 recibos (2 movs reales + 2 semillas)", len(recibos) == 4, str(folios))
# mov1: la card lista UNA entrada con total 4280 (2 fichas agrupadas)
mov1 = next((it for it in recibos if it["folio_ficha"] == "FP-20260921-0001"), None)
check("mov1 total = 4280 (agrupado)", mov1 is not None and abs(mov1["monto"] - 4280.0) < 0.01, str(mov1))
# orden cronológico inverso
fechas = [it.get("fecha_aplicacion") or it.get("fecha_pago") or "" for it in recibos]
check("orden cronologico inverso", fechas == sorted(fechas, reverse=True), str(fechas))

# 3) Detalle mov1 (mockup 8.1): Lotería Nacional 1420 + Efectivo 2860,
#    documentos FACTURA LN negativos con referencia "NUMSORTEO --> FOLIO X"
code, r = req("GET", "/mobile/pagos/recibo/54", token=TOKEN)
check("GET recibo/54 200", code == 200, str(r)[:300])
d = r.get("data") or {}
print(f"    folio={d.get('folio_ficha')} total={d.get('total')} formas={len(d.get('formas') or [])} documentos={len(d.get('documentos') or [])}")
formas = {(f["descripcion"], f["monto"]) for f in (d.get("formas") or [])}
check("formas = LN 1420 + Efectivo 2860", ("Lotería Nacional", 1420.0) in formas and ("Efectivo", 2860.0) in formas, str(formas))
check("total movimiento 4280", abs((d.get("total") or 0) - 4280.0) < 0.01, str(d.get("total")))
docs = d.get("documentos") or []
check("2 documentos pagados", len(docs) == 2, str(docs))
for doc in docs:
    print(f"    doc: {doc['folio_documento']} monto={doc['monto_pago']} ref={doc['referencia']}")
check("ref formato NUMSORTEO --> FOLIO", all("--> FOLIO" in (doc["referencia"] or "") for doc in docs), str(docs))
check("docs del propio billetero", all(doc["folio_documento"].startswith(("CONS-", "VTA-")) for doc in docs), str(docs))

# 4) Detalle mov2 (transferencia con referencia)
code, r = req("GET", "/mobile/pagos/recibo/56", token=TOKEN)
check("GET recibo/56 200", code == 200, str(r)[:300])
d2 = r.get("data") or {}
formas2 = {(f["descripcion"], f["referencia"], f["monto"]) for f in (d2.get("formas") or [])}
print(f"    formas mov2: {formas2}")
check("mov2 transferencia 5000 con referencia", any(f[0] == "Transferencia" and abs(f[2] - 5000.0) < 0.01 for f in formas2), str(formas2))
check("mov2 total 5000", abs((d2.get("total") or 0) - 5000.0) < 0.01, str(d2.get("total")))
check("mov2 1 documento (cartera 1)", len(d2.get("documentos") or []) == 1, str(d2.get("documentos")))

# 5) 404s y scoping
code, r = req("GET", "/mobile/pagos/recibo/99999", token=TOKEN)
check("recibo inexistente 404", code == 404, f"{code} {str(r)[:100]}")
code, r = req("GET", "/mobile/pagos/recibos")
check("sin token 401", code == 401, f"{code}")

print(f"\n{'FALLOS: ' + str(fail) if fail else 'TODO OK'}")
raise SystemExit(1 if fail else 0)
