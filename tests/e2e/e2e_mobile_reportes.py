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

"""E2E Reportes Mobile — FASE 1 (backend).

Billetero de prueba: mobile_dev -> expendio 11 -> billetero 1.
Valores esperados calculados desde auditoria BD 2026-09-22 (read-only).

Formula Vencido (aprobada por el usuario, difiere del plan persistido):
monto_original de docs vencidos, SIN descontar pagos. Filas por sorteo SOLO
con documentos consignacion (revision ronda 2: la deuda de venta se reporta
solo en Pagares del resumen; al_corriente SI evalua todos los docs vivos).
"""
import json
import sys
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
               "dispositivo": "e2e-reportes"})
check("login mobile_dev", r.get("success") and code == 200, str(r)[:200])
TOKEN = r["data"]["token"]
print(f"    token: {TOKEN[:12]}...")

# ===================== ESTADO DE CUENTA =====================
code, ec = req("GET", "/mobile/reportes/estado-cuenta", token=TOKEN)
check("GET estado-cuenta 200", code == 200, f"{code} {str(ec)[:200]}")
d = ec.get("data") or {}
check("EC json completo", True)
print("    EC json:", json.dumps(d, ensure_ascii=False))

check("billetero.nombre", d.get("billetero", {}).get("nombre") == "Billetero Uno Test",
      str(d.get("billetero")))
check("billetero.cedis", d.get("billetero", {}).get("cedis") == "CEDIS Puebla",
      str(d.get("billetero")))
c = d.get("conceptos") or {}
check("fondo_ahorro 1500", aprox(c.get("fondo_ahorro", -1), 1500), str(c))
check("fideicomiso 35000", aprox(c.get("fideicomiso", -1), 35000), str(c))
check("pagares 12000", aprox(c.get("pagares", -1), 12000), str(c))
check("bolsa_electronica 0", aprox(c.get("bolsa_electronica", -1), 0), str(c))
check("garantia_total 48500", aprox(c.get("garantia_total", -1), 48500), str(c))
check("capacidad_credito 103000", aprox(c.get("capacidad_credito", -1), 103000), str(c))
check("al_corriente es bool False", d.get("al_corriente") is False,
      repr(d.get("al_corriente")))

sorteos = {s["sorteo"]: s for s in (d.get("sorteos") or [])}
# Labels F4-formato: "numero_recepcion - nombre_sorteo" o solo nombre si el
# sorteo no tiene numero de recepcion (sorteos 1/2 = seeds de cartera sin
# paquetes/billetes/inventario en la cadena de recepcion).
check("2 sorteos (Sorteo Mayor, Sorteo Superior)", set(sorteos) == {"Sorteo Mayor", "Sorteo Superior"},
      str(set(sorteos)))
may = sorteos.get("Sorteo Mayor", {})
sup = sorteos.get("Sorteo Superior", {})
# Sorteo 1: fila = SOLO cartera 1 CONS (24,000, vencida). VTA 12/14 fuera
# de la tabla (van a Pagares del resumen). Pagos vivos cartera 1:
# 24,000 + 3,656.50 + 5,000 = 32,656.50 -> Total negativo por sobrepago.
check("MAYOR fecha 2026-02-25", may.get("fecha") == "2026-02-25", str(may.get("fecha")))
check("MAYOR cantidad 0 (sin movs consigna del sorteo 1)", may.get("cantidad") == 0, str(may.get("cantidad")))
check("MAYOR vencido 24000 (monto_original, sin descontar pagos)",
      aprox(may.get("vencido", -1), 24000), str(may))
check("MAYOR consigna 24000 (solo docs consignacion)", aprox(may.get("consigna", -1), 24000), str(may))
check("MAYOR pagos 32656.50", aprox(may.get("pagos", -1), 32656.50), str(may))
check("MAYOR total -8656.50 (consigna-pagos, sobrepago)", aprox(may.get("total", -1), -8656.50), str(may))
# Sorteo 2: cartera 7 CONS (11,000 vencida), pagos 623.50
check("SUPERIOR fecha 2026-02-26", sup.get("fecha") == "2026-02-26", str(sup.get("fecha")))
check("SUPERIOR cantidad 0", sup.get("cantidad") == 0, str(sup.get("cantidad")))
check("SUPERIOR vencido 11000", aprox(sup.get("vencido", -1), 11000), str(sup))
check("SUPERIOR consigna 11000", aprox(sup.get("consigna", -1), 11000), str(sup))
check("SUPERIOR pagos 623.50", aprox(sup.get("pagos", -1), 623.50), str(sup))
check("SUPERIOR total 10376.50", aprox(sup.get("total", -1), 10376.50), str(sup))
t = d.get("totales") or {}
check("totales vencido 35000", aprox(t.get("vencido", -1), 35000), str(t))
check("totales consigna 35000", aprox(t.get("consigna", -1), 35000), str(t))
check("totales pagos 33280", aprox(t.get("pagos", -1), 33280), str(t))
check("totales total 1720", aprox(t.get("total", -1), 1720), str(t))
# Cuadre con el bloque de resumen: Fideicomiso (cartera consignacion viva)
# debe ser IGUAL a totales.consigna de la tabla (misma fuente).
check("cuadre resumen/tabla: fideicomiso == totales.consigna",
      aprox(c.get("fideicomiso", -1), t.get("consigna", -1)), f"{c} vs {t}")

# Montos numericos (no strings)
nums = ([c.get(k) for k in ("fondo_ahorro", "fideicomiso", "pagares", "bolsa_electronica",
                            "garantia_total", "capacidad_credito")]
        + [may.get(k) for k in ("vencido", "consigna", "pagos", "total")]
        + [t.get(k) for k in ("vencido", "consigna", "pagos", "total")])
check("todos los montos son numericos", all(isinstance(v, (int, float)) for v in nums),
      str([v for v in nums if not isinstance(v, (int, float))]))

# ===================== SCOPING: id_billetero del request ignorado =====================
code, ec2 = req("GET", "/mobile/reportes/estado-cuenta", token=TOKEN,
                params={"id_billetero": 31})
check("GET estado-cuenta?id_billetero=31 200 (param ignorado)", code == 200, f"{code}")
check("EC con id_billetero inyectado = EC propia (scope por sesion)",
      (ec2.get("data") or {}) == d, "el JSON cambio al inyectar id_billetero")

# ===================== FONDO DE AHORRO =====================
code, fa = req("GET", "/mobile/reportes/fondo-ahorro", token=TOKEN,
               params={"fecha_inicio": "2026-05-01", "fecha_fin": "2026-06-30"})
check("GET fondo-ahorro 200", code == 200, f"{code} {str(fa)[:200]}")
d = fa.get("data") or {}
print("    FA json:", json.dumps(d, ensure_ascii=False)[:400])
check("titular", d.get("titular") == "Billetero Uno Test", str(d.get("titular")))
check("saldo_inicial 0", aprox(d.get("saldo_inicial", -1), 0), str(d))
check("depositos 1500", aprox(d.get("depositos", -1), 1500), str(d))
check("retiros 0", aprox(d.get("retiros", -1), 0), str(d))
check("saldo_final 1500", aprox(d.get("saldo_final", -1), 1500), str(d))
movs = d.get("movimientos") or []
check("3 movimientos", len(movs) == 3, str(len(movs)))
esperados = [("1", "2026-05-27", "aportacion", 500.0, "Ahorro semanal"),
             ("2", "2026-06-01", "aportacion", 500.0, "Ahorro semanal"),
             ("3", "2026-06-02", "aportacion", 500.0, "Ahorro semanal")]
ok = len(movs) == 3 and all(
    m["folio"] == e[0] and m["fecha"] == e[1] and m["origen"] == e[2]
    and aprox(m["monto"], e[3]) and m["descripcion"] == e[4]
    for m, e in zip(movs, esperados))
check("movimientos exactos (folio/fecha/origen/monto/desc)", ok, str(movs))

# ===================== FACTURACION =====================
code, fac = req("GET", "/mobile/reportes/facturacion", token=TOKEN,
                params={"fecha_inicio": "2026-08-31", "fecha_fin": "2026-08-31"})
check("GET facturacion 200", code == 200, f"{code} {str(fac)[:200]}")
d = fac.get("data") or {}
print("    FAC json:", json.dumps(d, ensure_ascii=False)[:500])
check("vendedor", d.get("vendedor") == "Billetero Uno Test", str(d.get("vendedor")))
check("comision_pct 10", aprox(d.get("comision_pct", -1), 10), str(d.get("comision_pct")))
regs = d.get("registros") or []
check("3 registros (fecha|sorteo)", len(regs) == 3, str(len(regs)))
esp = {
    "041753, E2EDRAWB - Sorteo 041753": ("Mayor", 210.0, 70.0, 140.0, 14.0),
    "ZOD-0472 - Sorteo Zodiaco 0472": ("Zodiaco", 70.0, 0.0, 70.0, 7.0),
    "Sorteo Mi Sueño": ("Mi Sueño", 2100.0, 0.0, 2100.0, 210.0),
}
ok = len(regs) == 3
for r_ in regs:
    e = esp.get(r_.get("sorteo"))
    if e is None:
        ok = False
        break
    if not (r_["fecha"] == "2026-08-31" and r_["categoria"] == e[0]
            and aprox(r_["entrega"], e[1]) and aprox(r_["devolucion"], e[2])
            and aprox(r_["venta"], e[3]) and aprox(r_["ganancia"], e[4])):
        ok = False
        print("    registro divergente:", r_)
check("registros exactos (categoria/entrega/devolucion/venta/ganancia)", ok, str(regs))
total_venta = sum(float(r_["venta"]) for r_ in regs)
check("total venta 2310 (suma app)", aprox(total_venta, 2310), str(total_venta))

# ===================== VALIDACIONES DE FECHAS (400) =====================
casos = [
    ("FA sin fechas", "/mobile/reportes/fondo-ahorro", {}),
    ("FAC sin fechas", "/mobile/reportes/facturacion", {}),
    ("FA solo inicio", "/mobile/reportes/fondo-ahorro", {"fecha_inicio": "2026-05-01"}),
    ("FA formato malo", "/mobile/reportes/fondo-ahorro",
     {"fecha_inicio": "2026/05/01", "fecha_fin": "2026-06-30"}),
    ("FAC formato malo", "/mobile/reportes/facturacion",
     {"fecha_inicio": "05-01-2026", "fecha_fin": "2026-06-30"}),
    ("FA fecha imposible", "/mobile/reportes/fondo-ahorro",
     {"fecha_inicio": "2026-02-30", "fecha_fin": "2026-06-30"}),
    ("FAC fecha imposible", "/mobile/reportes/facturacion",
     {"fecha_inicio": "2026-05-01", "fecha_fin": "2026-13-01"}),
    ("FA invertidas", "/mobile/reportes/fondo-ahorro",
     {"fecha_inicio": "2026-06-30", "fecha_fin": "2026-05-01"}),
    ("FAC invertidas", "/mobile/reportes/facturacion",
     {"fecha_inicio": "2026-06-30", "fecha_fin": "2026-05-01"}),
]
for nombre, path, params in casos:
    code, r = req("GET", path, token=TOKEN, params=params)
    check(f"400 {nombre}", code == 400 and r.get("success") is False,
          f"{code} {str(r)[:150]}")

# ===================== AUTORIZACION (401) =====================
code, r = req("GET", "/mobile/reportes/estado-cuenta")
check("EC sin token 401", code == 401, f"{code}")
code, r = req("GET", "/mobile/reportes/estado-cuenta", token="token-invalido-xyz")
check("EC token invalido 401", code == 401, f"{code}")
code, r = req("GET", "/mobile/reportes/fondo-ahorro",
              params={"fecha_inicio": "2026-05-01", "fecha_fin": "2026-06-30"},
              token="eyJfake.no.importa")
check("FA token invalido 401", code == 401, f"{code}")
code, r = req("GET", "/mobile/reportes/facturacion",
              params={"fecha_inicio": "2026-05-01", "fecha_fin": "2026-06-30"})
check("FAC sin token 401", code == 401, f"{code}")

# Token de sesion revocada/expirada no disponible sin escribir BD: cubierto
# por BaseMobileController::validarToken (hash inexistente = mismo camino 401).

# ===================== OPTIONS (preflight CORS) =====================
for path in ("/mobile/reportes/estado-cuenta", "/mobile/reportes/fondo-ahorro",
             "/mobile/reportes/facturacion"):
    rr = requests.options(BASE + path, timeout=TIMEOUT)
    check(f"OPTIONS {path} 200", rr.status_code == 200, str(rr.status_code))

# ===================== smoke: endpoints previos intactos =====================
code, r = req("GET", "/mobile/pagos/recibos", token=TOKEN)
check("smoke pagos/recibos 200", code == 200 and r.get("success"), f"{code}")
code, r = req("GET", "/mobile/auth/verify", token=TOKEN)
check("smoke auth/verify 200", code == 200 and r.get("success"), f"{code}")

print(f"\n{'FALLOS: ' + str(fail) if fail else 'TODO OK'}")
sys.exit(1 if fail else 0)
