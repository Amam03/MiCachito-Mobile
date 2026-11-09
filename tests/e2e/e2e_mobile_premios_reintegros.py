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

"""E2E Premios y Reintegros Mobile — /api/mobile/premios-reintegros (nueva integración).

Contrato:
  GET  /mobile/premios-reintegros         histórico del billetero (fecha desc)
  POST /mobile/premios-reintegros         guardar captura (folio PYR-YYYYMMDD-NNNN, totales server)
  GET  /mobile/premios-reintegros/{id}    detalle AGRUPADO POR SORTEO (D2)

Reglas D1-D3: solo aceptados (premio/reintegro), folio PYR-, vista agrupada.
El escáner (POST /mobile/premios/consultar) NO se toca: se re-ejecuta su E2E.

Billeteros: mobile_dev (1) y e2e_dot (11). LIMPIEZA: borra los movimientos
de prueba creados al final (canal directo BD, mismo patrón que seed E2E).
"""
import json
import subprocess
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


def sh(cmd):
    return subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=60).stdout


# 1) Login billetero 1
code, r = req("POST", "/mobile/auth/login", {
    "username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-pyr"})
check("login mobile_dev", r.get("success"), str(r)[:200])
TOKEN = r["data"]["token"]

# 2) Histórico inicial (vacío: tabla nueva)
code, r = req("GET", "/mobile/premios-reintegros", token=TOKEN)
check("GET historial 200 (vacío al inicio)", code == 200 and r.get("success"), str(r)[:200])
check("historial inicial vacío", (r.get("data") or []) == [], str(r.get("data"))[:150])

# 3) POST guardar captura (2 premios + 2 reintegros, 2 sorteos distintos)
captura = {
    "boletos": [
        {"tipo": "premio", "codigo_barras": "E2EPYR0001", "numero_billete": "47727",
         "serie": "01", "fraccion": 5, "nombre_sorteo": "Mayor - 3966", "id_sorteo": 37, "valor": 77.00},
        {"tipo": "premio", "codigo_barras": "E2EPYR0002", "numero_billete": "16171",
         "serie": "01", "fraccion": 12, "nombre_sorteo": "Mayor - 3966", "id_sorteo": 37, "valor": 72.92},
        {"tipo": "reintegro", "codigo_barras": "E2EPYR0003", "numero_billete": "05123",
         "serie": "11", "fraccion": 7, "nombre_sorteo": "Mayor - 4010", "id_sorteo": 38, "valor": 42.00},
        {"tipo": "reintegro", "codigo_barras": "E2EPYR0004", "numero_billete": "08455",
         "signo": "GEMINIS", "fraccion": 3, "nombre_sorteo": "Zodiaco - ZOD", "id_sorteo": 39, "valor": 30.00},
    ]
}
code, r = req("POST", "/mobile/premios-reintegros", captura, token=TOKEN)
check("POST guardar 201/200", code in (200, 201) and r.get("success"), str(r)[:300])
m = r.get("data") or {}
check("folio servidor PYR-YYYYMMDD-NNNN (D3)", r.get("success") and m.get("folio", "").startswith("PYR-") and len(m["folio"].split("-")) == 3, str(m)[:200])
import re
check("formato folio 8+4 dígitos", bool(re.match(r"^PYR-\d{8}-\d{4}$", m.get("folio", ""))), m.get("folio", ""))
check("totales server: premios 149.92", m.get("total_premios") == 149.92, str(m.get("total_premios")))
check("totales server: reintegros 72.00", m.get("total_reintegros") == 72.00, str(m.get("total_reintegros")))
check("total general 221.92", m.get("total_general") == 221.92, str(m.get("total_general")))
ID_MOV = m.get("id_movimiento")
check("id_movimiento presente", ID_MOV is not None, str(m)[:150])

# 4) Segundo movimiento (folio consecutivo del MISMO día)
code, r = req("POST", "/mobile/premios-reintegros", {
    "boletos": [
        {"tipo": "premio", "codigo_barras": "E2EPYR0005", "numero_billete": "326",
         "serie": "06", "fraccion": 2, "nombre_sorteo": "Mayor - 014021", "id_sorteo": 39, "valor": 72.92},
    ]
}, token=TOKEN)
m2 = r.get("data") or {}
check("folio consecutivo (-0002)", m2.get("folio", "").endswith("-0002") and m2.get("folio", "").startswith("PYR-"), str(m2.get("folio")))
ID_MOV2 = m2.get("id_movimiento")

# 5) Histórico: 2 movimientos, fecha descendente
code, r = req("GET", "/mobile/premios-reintegros", token=TOKEN)
h = r.get("data") or []
check("historial 2 movimientos", len(h) == 2, str(len(h)))
fechas = [x["fecha_movimiento"] or "" for x in h]
check("orden fecha descendente", fechas == sorted(fechas, reverse=True), str(fechas)[:150])

# 6) Detalle AGRUPADO POR SORTEO (D2) del movimiento 1
code, r = req("GET", f"/mobile/premios-reintegros/{ID_MOV}", token=TOKEN)
d = r.get("data") or {}
check("GET detalle 200", code == 200 and r.get("success"), str(r)[:200])
check("detalle folio == del POST", d.get("folio") == m.get("folio"), str(d.get("folio")))
premios = d.get("premios") or []
reintegros = d.get("reintegros") or []
check("premios: 1 grupo (Mayor - 3966)", len(premios) == 1 and premios[0]["nombre_sorteo"] == "Mayor - 3966", str(premios)[:200])
check("grupo premios 2 boletos", len((premios[0] or {}).get("boletos") or []) == 2, str(premios)[:200])
check("reintegros: 2 grupos (4010, Zodiaco)", len(reintegros) == 2, str(reintegros)[:250])
nombres = {g["nombre_sorteo"] for g in reintegros}
check("grupos reintegros correctos", nombres == {"Mayor - 4010", "Zodiaco - ZOD"}, str(nombres))
b1 = (premios[0]["boletos"])[0]
check("boleto: billete/serie/vig/valor reales", b1.get("numero_billete") == "47727" and b1.get("serie") == "01"
      and str(b1.get("vig")) == "5" and b1.get("valor") == 77.0, str(b1)[:200])
bz = [b for g in reintegros if g["nombre_sorteo"] == "Zodiaco - ZOD" for b in g["boletos"]][0]
check("boleto zodiaco: signo persistido", bz.get("signo") == "GEMINIS", str(bz)[:150])
check("detalle totales == cabecera", d.get("total_premios") == 149.92 and d.get("total_reintegros") == 72.0, str(d)[:150])

# 7) Validaciones D1 y de forma
code, r = req("POST", "/mobile/premios-reintegros", {"boletos": []}, token=TOKEN)
check("captura vacía -> 422", code == 422 and not r.get("success"), f"{code} {str(r)[:120]}")
code, r = req("POST", "/mobile/premios-reintegros", {"boletos": [
    {"tipo": "no_ganador", "codigo_barras": "X1", "valor": 0}]}, token=TOKEN)
check("D1: tipo 'no_ganador' rechazado -> 422", code == 422, f"{code} {str(r)[:150]}")
code, r = req("POST", "/mobile/premios-reintegros", {"boletos": [
    {"tipo": "premio", "codigo_barras": "DUP1", "valor": 10},
    {"tipo": "reintegro", "codigo_barras": "DUP1", "valor": 5}]}, token=TOKEN)
check("duplicados en captura -> 422", code == 422, f"{code} {str(r)[:150]}")
code, r = req("POST", "/mobile/premios-reintegros", {"boletos": [
    {"tipo": "premio", "valor": 10}]}, token=TOKEN)
check("sin codigo_barras -> 422", code == 422, f"{code} {str(r)[:150]}")

# 8) Aislamiento: billetero 11 no ve ni toca los del 1
code, r = req("POST", "/mobile/auth/login", {
    "username": DOT_USER, "password": DOT_PASS, "dispositivo": "e2e-pyr-b11"})
check("login e2e_dot (billetero 11)", r.get("success"), str(r)[:150])
T11 = r["data"]["token"]
code, r = req("GET", "/mobile/premios-reintegros", token=T11)
h11 = r.get("data") or []
ids11 = {x["id_movimiento"] for x in h11}
check("billetero 11 no ve los del 1", ids11.isdisjoint({ID_MOV, ID_MOV2}), str(sorted(ids11))[:150])
code, r = req("GET", f"/mobile/premios-reintegros/{ID_MOV}", token=T11)
check("detalle ajeno -> 404", code == 404, f"{code} {str(r)[:120]}")

# 9) Seguridad
code, r = req("GET", "/mobile/premios-reintegros")
check("sin token -> 401", code == 401, f"{code}")
code, r = req("GET", "/mobile/premios-reintegros/999999", token=TOKEN)
check("id inexistente -> 404", code == 404, f"{code}")

# 10) Escáner INTACTO: E2E de premios/consultar completo (exit 0 del script)
out2 = sh("python3 /home/sistemas/e2e_mobile_premios.py > /dev/null 2>&1; echo $?")
check("escáner premios/consultar intacto (E2E existente exit 0)", out2.strip() == "0", f"exit={out2.strip()}")

# 11) LIMPIEZA: borrar los 2 movimientos de prueba (canal directo)
sql = f"DELETE FROM mobile_premios_reintegros WHERE folio IN ('{m.get('folio')}', '{m2.get('folio')}');"
open("/tmp/pyr_cleanup.sql", "w").write(sql)
out = sh("docker cp /tmp/pyr_cleanup.sql micachito-mysql:/tmp/pyr_cleanup.sql && docker exec micachito-mysql sh -c 'mysql -uroot -p\"$MYSQL_ROOT_PASSWORD\" MiCachito < /tmp/pyr_cleanup.sql' 2>/dev/null")
sql2 = "SELECT COUNT(*) FROM mobile_premios_reintegros WHERE folio LIKE 'PYR-%';"
open("/tmp/pyr_verify.sql", "w").write(sql2)
out = sh("docker cp /tmp/pyr_verify.sql micachito-mysql:/tmp/pyr_verify.sql && docker exec micachito-mysql sh -c 'mysql -uroot -p\"$MYSQL_ROOT_PASSWORD\" -N MiCachito < /tmp/pyr_verify.sql' 2>/dev/null")
check("cleanup BD (0 movimientos de prueba)", out.strip() == "0", out.strip())

print()
print("RESULTADO:", "TODO OK" if fail == 0 else f"{fail} FALLOS")
raise SystemExit(0 if fail == 0 else 1)
