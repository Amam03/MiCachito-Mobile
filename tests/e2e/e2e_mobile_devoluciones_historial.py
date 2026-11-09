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

"""E2E Historial persistente de Devolución 6.x — MiCachito Mobile.

Contrato:
  GET  /mobile/devoluciones        historial del billetero de la sesión
                                  (movimientos_almacen tipo devolucion,
                                  fecha DESC) — persiste entre sesiones
  POST /mobile/ventas/devolver     registro (F4, intacto) — el historial
                                  debe reflejar lo registrado

Cobertura:
  1) Seguridad: 401 sin token; token revocado.
  2) Persistencia: POST devolver (billetero 11, billete 128 dotación 4025)
     -> aparece en GET devoluciones con folio/estatus/cachitos correctos.
  3) Detalle de fila: folio, estatus, sorteo (nombre+dotación),
     fecha_movimiento, total_cachitos, total_monto.
  4) Aislamiento: billetero 1 NO ve el movimiento del 11; el 11 no ve
     la devolución 27 del 1 (E2E-DEV-BIL-1).
  5) Regresión: POST devolver sigue validando (billete ajeno -> 403,
     estado no asignado -> 409); literales de Vender intactas.
  6) Limpieza: restaurar billete 128 a 'asignado' + borrar el movimiento
     de prueba (por folio), volver a verificar historial.
"""
import json
import urllib.request
import urllib.error

BASE = BASE  # del helper e2e_common
fail = 0
creados = []  # folios de movimientos de prueba a limpiar


def req(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=20) as resp:
            return resp.status, json.loads(resp.read().decode() or "{}")
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read().decode() or "{}")
        except ValueError:
            return e.code, {}
    except Exception as ex:  # red caída
        return 0, {"error": str(ex)}


def check(nombre, cond, detalle=""):
    global fail
    marca = "OK " if cond else "FAIL"
    if not cond:
        fail += 1
    print(f"[{marca}] {nombre}" + (f" -> {detalle}" if detalle and not cond else ""))


# ── Setup: login ambos billeteros ─────────────────────────────────────
code, r = req("POST", "/mobile/auth/login", {
    "username": MOBILE_USER, "password": MOBILE_PASS,
    "dispositivo": "e2e-dev-hist"})
check("login mobile_dev (billetero 1)", r.get("success"), str(r)[:150])
T1 = r["data"]["token"]

code, r = req("POST", "/mobile/auth/login", {
    "username": DOT_USER, "password": DOT_PASS,
    "dispositivo": "e2e-dev-hist-b11"})
check("login e2e_dot (billetero 11)", r.get("success"), str(r)[:150])
T11 = r["data"]["token"]

# ── 1) Seguridad ──────────────────────────────────────────────────────
code, _ = req("GET", "/mobile/devoluciones")
check("sin token -> 401", code == 401, f"{code}")

# Historial VACÍO no debe fallar (lista limpia)
code, r = req("GET", "/mobile/devoluciones", token=T11)
check("GET historial 200 (b11)", code == 200 and r.get("success"), f"{code} {str(r)[:150]}")
h11_antes = r.get("data") or []
print(f"    historial b11 inicial: {len(h11_antes)} filas")

# ── 2) Persistencia: registrar devolución y verla en el historial ────
# Billete 128: numero 0101, serie 01, dotación 4025, sorteo 41, asignado al 11
code, r = req("POST", "/mobile/ventas/devolver", token=T11, body={
    "items": [{
        "numero_billete": "0101",
        "serie": "01",
        "fraccion": None,
        "signo_codigo": None,
        "numero_sorteo": "4025",
    }]})
check("POST devolver (b11, billete 128 dot 4025) 200", code == 200 and r.get("success"),
      f"{code} {str(r)[:200]}")
if r.get("success"):
    dev = r["data"]
    creados.append(dev["folio"])
    print(f"    creado: id={dev['id_movimiento']} folio={dev['folio']} "
          f"estatus={dev['estatus']} cachitos={dev['total_cachitos']}")
    FOLIO = dev["folio"]
    IDMOV = dev["id_movimiento"]
else:
    FOLIO = IDMOV = None

code, r = req("GET", "/mobile/devoluciones", token=T11)
h11 = r.get("data") or []
fila = next((d for d in h11 if d.get("folio") == FOLIO), None) if FOLIO else None
check("devolución registrada APARECE en historial (persistencia)", fila is not None,
      str(h11)[:300])

if fila:
    # ── 3) Detalle de la fila ─────────────────────────────────────────
    check("folio coincide", fila["folio"] == FOLIO, str(fila)[:150])
    check("id_movimiento coincide", fila["id_movimiento"] == IDMOV, str(fila)[:150])
    check("estatus = pendiente", fila["estatus"] == "pendiente", fila.get("estatus"))
    check("sorteo trae nombre de producto", bool(fila.get("nombre_producto")),
          str(fila.get("nombre_producto")))
    check("sorteo label = 'MAYOR - 4025' (nombre + dotación)",
          fila.get("sorteo") == "MAYOR - 4025",
          f"sorteo={fila.get('sorteo')!r} dotacion={fila.get('dotacion')!r}")
    check("dotacion = 4025", fila.get("dotacion") == "4025", str(fila.get("dotacion")))
    check("total_cachitos = 1", fila.get("total_cachitos") == 1, str(fila.get("total_cachitos")))
    check("total_monto = 70 (precio sorteo 41)", float(fila.get("total_monto") or 0) == 70.0,
          str(fila.get("total_monto")))
    check("fecha_movimiento presente", bool(fila.get("fecha_movimiento")),
          str(fila.get("fecha_movimiento")))

# ── 4) Aislamiento ────────────────────────────────────────────────────
code, r = req("GET", "/mobile/devoluciones", token=T1)
h1 = r.get("data") or []
ids1 = {d["id_movimiento"] for d in h1}
check("billetero 1 NO ve el movimiento nuevo del 11", IDMOV not in ids1 if IDMOV else False,
      str(sorted(ids1))[:200])
check("billetero 1 SI ve su devolución 27 (E2E-DEV-BIL-1)", 27 in ids1, str(sorted(ids1))[:200])
folios1 = {d["folio"] for d in h1}
check("b1: folio 27 = E2E-DEV-BIL-1", "E2E-DEV-BIL-1" in folios1, str(folios1)[:150])
if h1:
    f27 = next((d for d in h1 if d["id_movimiento"] == 27), {})
    check("b1 fila 27: total_cachitos = 1", f27.get("total_cachitos") == 1, str(f27)[:150])
    check("b1 fila 27: estatus aplicada", f27.get("estatus") == "aplicada", str(f27)[:150])

# ── 5) Regresión: escritura intacta ───────────────────────────────────
code, r = req("POST", "/mobile/ventas/devolver", token=T11, body={
    "items": [{
        "numero_billete": "0102",
        "serie": "01",
        "fraccion": None,
        "signo_codigo": None,
        "numero_sorteo": "E2EDRAWB",
    }]})
check("POST devolver billete ajeno -> 403", code == 403, f"{code} {str(r)[:150]}")

code, r = req("POST", "/mobile/ventas/devolver", token=T11, body={
    "items": [{
        "numero_billete": "0101",
        "serie": "01",
        "fraccion": None,
        "signo_codigo": None,
        "numero_sorteo": "4025",
    }]})
check("POST devolver billete ya devuelto -> 409", code == 409, f"{code} {str(r)[:150]}")

code, r = req("POST", "/mobile/ventas/devolver", token=T11, body={"items": []})
check("POST devolver sin items -> 422", code == 422, f"{code} {str(r)[:120]}")

code, r = req("GET", "/mobile/ventas/sorteos", token=T1)
check("literal GET ventas/sorteos intacta (200)", code == 200 and r.get("success"), f"{code}")
code, r = req("GET", "/mobile/ventas", token=T1)
check("literal GET ventas (tickets) intacta (200)", code == 200 and r.get("success"), f"{code}")
code, r = req("GET", "/mobile/depositos", token=T1)
check("literal GET depositos intacta (200)", code == 200 and r.get("success"), f"{code}")

# ── 6) Limpieza: restaurar billete 128 y borrar el movimiento ─────────
# Se hace vía SQL directo (docker exec mysql) desde el script runner;
# aquí solo se reporta qué hay que limpiar.
print()
if creados:
    print(f"LIMPIEZA PENDIENTE (movimientos de prueba): {creados}")
    print("  DELETE movimientos_almacen + detalle por folio; billete 128 ->"
          " estatus='asignado', ubicacion='cedis', id_billetero_actual=11")

print()
print("RESULTADO:", "TODO OK" if fail == 0 else f"{fail} FALLOS")
raise SystemExit(0 if fail == 0 else 1)
