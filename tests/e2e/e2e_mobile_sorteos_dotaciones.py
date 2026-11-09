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

# -*- coding: utf-8 -*-
"""
E2E Sorteos Mobile — dos dotaciones 4024/4025 del MISMO id_sorteo con
numeracion TRASLAPADA y material de AMBAS asignado al MISMO billetero.

Escenario exacto pedido (fix atribucion por mad.serie):
- mismo id_sorteo (sorteo nuevo "MAYOR E2E 4024/4025", producto Mayor LN);
- dos numero_sorteo: 4024 y 4025 (numerico, como produccion);
- billetes traslapados: mismos numero_billete 0101-0103 en ambas dotaciones;
- consignas de AMBAS dotaciones al MISMO billetero 11 (via API real);
- verifico: lista mobile con DOS filas separadas MAYOR - 4024 y
  MAYOR - 4025, cada mov asociado a su dotacion correcta, montos por
  mad.total, y detalles por dotacion sin mezcla.

Flujo (todo via API, sin toques directos de BD salvo setup/teardown
del expendio mobile de prueba):
  login almacen -> POST /paquetes-recibidos x2 (uno por dotacion)
  -> autorizar x2 -> POST /movimientos-almacen E-TRASPASO x2
  -> consigna 4024 (0101-0103) -> consigna 4025 (0101-0103)
  -> login mobile (expendio nuevo billetero 11) -> GET /mobile/sorteos
  -> GET /mobile/sorteos/<id>?numero_sorteo=4024|4025

Antes del fix: el JOIN por billete+MAX() atribuia AMBAS consignas a
'4025' (MAX('4024','4025')) -> una sola fila, montos mezclados.
"""
import datetime
import json
import subprocess
import sys
import urllib.request

import requests

BASE = BASE  # del helper e2e_common
TODAY = datetime.date.today().isoformat()
CEDIS = 1
BILLETERO = 11          # GABRIELA ROMERO SOLIS: cedis 1, sin cartera/movs
PRODUCTO = 1            # Sorteo Mayor (LN)
DRAWS = ("4024", "4025")
BILLETES = ["0101", "0102", "0103"]   # traslape total entre dotaciones
PRECIO = 70

TOKEN_ALM = None
TOKEN_MOB = None
RESULTS = []
ID_SORTEO = None
IDS = {}


def log(msg):
    print(msg, flush=True)


def record(paso, expected, obtained, ok):
    RESULTS.append((paso, expected, obtained, "PASS" if ok else "FAIL"))
    log(("  [PASS] " if ok else "  [FAIL] ") + paso + " | esperado: " + expected + " | obtenido: " + obtained)


def sql(query):
    php = (
        "$db = require '/app/config/db.php';"
        "$pdo = new PDO($db['dsn'], $db['username'], $db['password'],"
        " [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);"
        "$st = $pdo->query(" + json.dumps(query) + ");"
        "$rows = [];"
        "while ($r = $st->fetch(PDO::FETCH_ASSOC)) { $rows[] = $r; }"
        "echo json_encode($rows);"
    )
    out = subprocess.run(
        ["docker", "exec", "micachito-php", "php", "-r", php],
        capture_output=True, text=True, timeout=30,
    )
    if out.returncode != 0:
        raise RuntimeError("SQL fallo: " + out.stderr[-400:])
    return json.loads(out.stdout or "[]")


def sql_exec(stmt):
    php = (
        "$db = require '/app/config/db.php';"
        "$pdo = new PDO($db['dsn'], $db['username'], $db['password'],"
        " [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);"
        "$n = $pdo->exec(" + json.dumps(stmt) + ");"
        "echo json_encode(['affected' => $n]);"
    )
    out = subprocess.run(
        ["docker", "exec", "micachito-php", "php", "-r", php],
        capture_output=True, text=True, timeout=30,
    )
    if out.returncode != 0:
        raise RuntimeError("SQL exec fallo: " + out.stderr[-400:])
    return json.loads(out.stdout or "{}")


def login_almacen():
    global TOKEN_ALM
    r = requests.post(BASE + "/auth/login", data={"username": "almacen", "password": "almacen123"}, timeout=TIMEOUT)
    body = r.json()
    TOKEN_ALM = body["data"]["token"]
    log("login almacen OK (token ...%s)" % TOKEN_ALM[-6:])
    return r.status_code == 200 and body.get("success")


def api_alm(method, path, payload=None):
    headers = {"Authorization": "Bearer " + TOKEN_ALM}
    r = requests.request(method, BASE + path, json=payload, headers=headers, timeout=30)
    try:
        body = r.json()
    except Exception:
        body = {"_raw": r.text[:300]}
    return r.status_code, body


def req_mob(method, path, payload=None, token=None, params=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = requests.request(method, BASE + path, json=payload, headers=headers, params=params, timeout=20)
    try:
        return r.status_code, r.json()
    except ValueError:
        return r.status_code, {"raw": r.text[:300]}


def check(nombre, cond, detalle=""):
    marca = "OK " if cond else "FAIL"
    RESULTS.append((nombre, "", detalle, "PASS" if cond else "FAIL"))
    log(f"    [{marca}] {nombre}" + (f" -> {detalle}" if detalle and not cond else ""))


def aprox(a, b, tol=0.01):
    return abs(float(a) - float(b)) <= tol


# ─────────────────────────────────────────────────────────────────────
def paso0_setup():
    """Sorteo nuevo (una fila por producto, como produccion) + expendio mobile del billetero 11."""
    global ID_SORTEO
    log("\n== PASO 0: setup sorteo MAYOR + expendio mobile billetero 11 ==")
    # Sorteo: mismo patron que sorteos reales (una fila por producto vigente);
    # numero_sorteo interno unico (codigo) — la DOTACION es la de recepcion.
    row = sql(
        "SELECT id_sorteo FROM sorteos WHERE numero_sorteo = 'E2E-4024-4025' LIMIT 1"
    )
    if row:
        ID_SORTEO = int(row[0]["id_sorteo"])
        log("  sorteo existente id=%s (re-run)" % ID_SORTEO)
    else:
        sql_exec(
            "INSERT INTO sorteos (numero_sorteo, nombre_sorteo, tipo_sorteo, id_producto, "
            "fecha_sorteo, precio_billete_completo, precio_fraccion, vigente, celebrado, cerrado) "
            "VALUES ('E2E-4024-4025', 'Sorteo Mayor E2E 4024/4025', 'loteria_nacional', %d, "
            "'%s', 1400, %d, 1, 0, 0)" % (PRODUCTO, TODAY, PRECIO)
        )
        # LAST_INSERT_ID() no sirve: sql_exec abre conexion nueva por llamada
        ID_SORTEO = int(
            sql("SELECT id_sorteo FROM sorteos WHERE numero_sorteo = 'E2E-4024-4025'")[0]["id_sorteo"]
        )
        log("  sorteo creado id=%s" % ID_SORTEO)

    # Expendio mobile de prueba sobre el billetero 11 (login mobile)
    existente = sql("SELECT id_expendio FROM billeteros_expendios WHERE usuario = 'e2e_dot'")
    if not existente:
        # password_hash de mobile_dev (DevMobile2026!) — mismo password
        sql_exec(
            "INSERT INTO billeteros_expendios (id_billetero, usuario, password_hash, autorizado) "
            "SELECT 11, 'e2e_dot', (SELECT password_hash FROM billeteros_expendios WHERE usuario = 'mobile_dev'), 1 "
            "FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM billeteros_expendios WHERE usuario = 'e2e_dot')"
        )
    ids = sql("SELECT id_expendio FROM billeteros_expendios WHERE usuario = 'e2e_dot'")
    IDS["expendio"] = int(ids[0]["id_expendio"])
    log("  expendio mobile e2e_dot id=%s (billetero 11)" % IDS["expendio"])

    # Limpieza defensiva de re-runs (movs/paquetes/billetes de corridas previas)
    folios = ["E2E-DOT-A", "E2E-DOT-B", "E2E-DOT-DEV", "E2E-ET-4024", "E2E-ET-4025"]
    for folio in folios:
        sql_exec(
            "DELETE FROM movimientos_almacen_detalle WHERE id_movimiento IN "
            "(SELECT id_movimiento FROM movimientos_almacen WHERE folio = '%s')" % folio
        )
        sql_exec("DELETE FROM movimientos_almacen WHERE folio = '%s'" % folio)
    sql_exec(
        "DELETE FROM paquetes_recibidos_billetes WHERE id_paquete IN "
        "(SELECT id_paquete FROM paquetes_recibidos WHERE folio_guia IN ('E2E-DOT-P24','E2E-DOT-P25'))"
    )
    sql_exec(
        "DELETE FROM paquetes_recibidos_detalle WHERE id_paquete IN "
        "(SELECT id_paquete FROM paquetes_recibidos WHERE folio_guia IN ('E2E-DOT-P24','E2E-DOT-P25'))"
    )
    sql_exec(
        "DELETE FROM paquetes_recibidos WHERE folio_guia IN ('E2E-DOT-P24','E2E-DOT-P25')"
    )
    sql_exec("DELETE FROM billetes_loteria WHERE id_sorteo = %d" % ID_SORTEO)
    sql_exec("DELETE FROM inventario_cedis WHERE id_sorteo = %d" % ID_SORTEO)
    sql_exec("DELETE FROM mobile_sesiones WHERE id_expendio = %d" % IDS["expendio"])
    log("  limpieza de re-run OK")


# ─────────────────────────────────────────────────────────────────────
def paso1_recepcion():
    """Recepcion real: paquete por dotacion + autorizar + E-TRASPASO."""
    log("\n== PASO 1: recepcion (paquete + autorizar + E-TRASPASO) por dotacion ==")
    for draw, folio_paq, folio_et in [
        ("4024", "E2E-DOT-P24", "E2E-ET-4024"),
        ("4025", "E2E-DOT-P25", "E2E-ET-4025"),
    ]:
        billetes = [
            {
                "id_sorteo": ID_SORTEO, "numero_sorteo": draw, "numero_billete": n,
                "serie": "01", "fraccion": 0, "codigo_barras": "E2EDOT" + draw + n,
                "codigo_seguridad": "SD" + n, "serie_sabana": 1, "fecha_sorteo": TODAY,
            }
            for n in BILLETES
        ]
        payload = {
            "paquete": {
                "folio_guia": folio_paq,
                "tipo_contenido": "loteria_nacional",
                "id_cedis_destino": CEDIS,
                "fecha_recepcion": TODAY,
                "estatus": "recibido",
                "observaciones": "E2E dotaciones %s" % draw,
            },
            "detalles": [
                {"id_sorteo": ID_SORTEO, "numero_sorteo": draw,
                 "cantidad_billetes": len(BILLETES), "fecha_sorteo": TODAY},
            ],
            "billetes_individuales": billetes,
        }
        st, body = api_alm("POST", "/paquetes-recibidos", payload)
        id_paquete = (body.get("data") or {}).get("id_paquete") or (body.get("data") or {}).get("id")
        record("paquete %s" % draw, "2xx + id", "HTTP %s id=%s" % (st, id_paquete),
               st in (200, 201) and id_paquete)
        if not id_paquete:
            log("  body: " + json.dumps(body)[:400])
            return False
        IDS["paquete_" + draw] = id_paquete

        st, body = api_alm("POST", "/paquetes-recibidos/%s/autorizar" % id_paquete, {"id_proveedor": 1})
        record("autorizar %s" % draw, "2xx", "HTTP %s" % st, st in (200, 201))

        # E-TRASPASO: detalles con numero_sorteo (como el flujo real de Desktop)
        detalles = [
            {"id_producto": PRODUCTO, "id_sorteo": ID_SORTEO, "numero_sorteo": draw,
             "numero_billete": b["numero_billete"], "serie": b["serie"], "fraccion": 0,
             "cantidad": 1, "precio": PRECIO, "total": PRECIO}
            for b in billetes
        ]
        payload = {
            "movimiento": {
                "folio": folio_et,
                "tipo_movimiento": "E-TRASPASO",
                "id_cedis": CEDIS,
                "id_sorteo": ID_SORTEO,
                "fecha_movimiento": TODAY,
                "estatus": "aplicada",
                "observaciones": "E2E entrada dotacion %s" % draw,
            },
            "detalles": detalles,
            "id_paquete": id_paquete,
        }
        st, body = api_alm("POST", "/movimientos-almacen", payload)
        record("E-TRASPASO %s" % draw, "2xx", "HTTP %s %s" % (st, json.dumps(body)[:120]),
               st in (200, 201))

    # Inventario y billetes por dotacion (traslape deliberado).
    # NOTA: el E-TRASPASO con detalles + id_paquete acredita 2x la cantidad
    # (comportamiento preexistente del backend, igual que el sorteo 37 del
    # E2E de trazabilidad; NO es parte de este fix). Lo que este E2E prueba
    # es la SEPARACION por dotacion: 2 filas, una por numero_sorteo.
    inv = sql(
        "SELECT numero_sorteo, cantidad_disponible, cantidad_asignada FROM inventario_cedis "
        "WHERE id_sorteo = %d AND id_cedis = %d ORDER BY numero_sorteo" % (ID_SORTEO, CEDIS)
    )
    ok = len(inv) == 2 and [r["numero_sorteo"] for r in inv] == ["4024", "4025"]
    record("inventario: DOS filas, una por dotacion (4024, 4025)", "4024 y 4025 separadas",
           json.dumps(inv), ok)

    cross = sql(
        "SELECT numero_sorteo, COUNT(*) n FROM billetes_loteria "
        "WHERE id_sorteo = %d AND numero_billete = '0101' GROUP BY numero_sorteo" % ID_SORTEO
    )
    ok = len(cross) == 2 and all(r["n"] == "1" for r in cross)
    record("billetes 0101 en AMBAS dotaciones (traslape)", "1 fila por dotacion",
           json.dumps(cross), ok)
    return True


# ─────────────────────────────────────────────────────────────────────
def paso2_consignas():
    """Consignas de AMBAS dotaciones al MISMO billetero 11 (via API real)."""
    log("\n== PASO 2: consignas 4024 y 4025 al billetero 11 ==")
    for draw, folio in [("4024", "E2E-DOT-A"), ("4025", "E2E-DOT-B")]:
        detalles = [
            {"id_producto": PRODUCTO, "id_sorteo": ID_SORTEO, "numero_sorteo": draw,
             "numero_billete": n, "serie": "01", "fraccion": 0,
             "cantidad": 1, "precio": PRECIO, "total": PRECIO}
            for n in BILLETES
        ]
        payload = {
            "movimiento": {
                "folio": folio,
                "tipo_movimiento": "consigna",
                "id_cedis": CEDIS,
                "id_billetero": BILLETERO,
                "id_sorteo": ID_SORTEO,
                "fecha_movimiento": TODAY,
                "estatus": "aplicada",
                "observaciones": "E2E consigna dotacion %s" % draw,
            },
            "detalles": detalles,
        }
        st, body = api_alm("POST", "/movimientos-almacen", payload)
        record("consigna %s (3 billetes)" % draw, "2xx", "HTTP %s %s" % (st, json.dumps(body)[:120]),
               st in (200, 201))

    # El billetero tiene material de AMBAS dotaciones con el MISMO numero
    mat = sql(
        "SELECT numero_sorteo, COUNT(*) n FROM billetes_loteria "
        "WHERE id_sorteo = %d AND id_billetero_actual = %d GROUP BY numero_sorteo" % (ID_SORTEO, BILLETERO)
    )
    ok = len(mat) == 2 and all(r["n"] == "3" for r in mat)
    record("billetero con material de AMBAS dotaciones", "3 billetes por dotacion",
           json.dumps(mat), ok)

    # mad.serie debe llevar la dotacion en cada detalle de consigna
    mad = sql(
        "SELECT ma.folio, mad.serie, COUNT(*) n FROM movimientos_almacen_detalle mad "
        "JOIN movimientos_almacen ma ON ma.id_movimiento = mad.id_movimiento "
        "WHERE ma.folio IN ('E2E-DOT-A','E2E-DOT-B') GROUP BY ma.folio, mad.serie"
    )
    espera = {("E2E-DOT-A", "4024"), ("E2E-DOT-B", "4025")}
    obten = {(r["folio"], r["serie"]) for r in mad}
    record("mad.serie = dotacion en consignas", "A->4024, B->4025",
           json.dumps(sorted(obten)), obten == espera)
    return True


# ─────────────────────────────────────────────────────────────────────
def paso3_mobile_lista():
    """GET /mobile/sorteos: DOS filas separadas MAYOR - 4024 / MAYOR - 4025."""
    global TOKEN_MOB
    log("\n== PASO 3: lista mobile (billetero 11) ==")
    st, r = req_mob("POST", "/mobile/auth/login",
                    {"username": DOT_USER, "password": DOT_PASS,
                     "dispositivo": "e2e-dotaciones"})
    if not (r.get("success") and st == 200):
        log("  login mobile fallo: " + json.dumps(r)[:300])
        return False
    TOKEN_MOB = r["data"]["token"]
    log("  login mobile OK (token ...%s)" % TOKEN_MOB[-6:])

    st, l = req_mob("GET", "/mobile/sorteos", token=TOKEN_MOB)
    check("GET /mobile/sorteos 200", st == 200, f"{st} {str(l)[:200]}")
    d = l.get("data") or {}
    sorteos = d.get("sorteos") or []
    log("    lista: " + json.dumps(sorteos, ensure_ascii=False))

    labels = [s["sorteo"] for s in sorteos]
    check("DOS filas separadas (4024 y 4025)", len(sorteos) == 2, str(labels))
    check("labels exactos MAYOR - 4024 / MAYOR - 4025",
          set(labels) == {"MAYOR - 4024", "MAYOR - 4025"}, str(labels))

    por_clave = {(s["id_sorteo"], s["numero_sorteo"]): s for s in sorteos}
    a = por_clave.get((ID_SORTEO, "4024"), {})
    b = por_clave.get((ID_SORTEO, "4025"), {})
    check("fila 4024: consignado 210, devuelto 0, saldo 210",
          aprox(a.get("consignado", -1), 210) and aprox(a.get("devuelto", -1), 0)
          and aprox(a.get("saldo", -1), 210), str(a))
    check("fila 4025: consignado 210, devuelto 0, saldo 210",
          aprox(b.get("consignado", -1), 210) and aprox(b.get("devuelto", -1), 0)
          and aprox(b.get("saldo", -1), 210), str(b))
    check("sin fila sin-numero espuria", (ID_SORTEO, None) not in por_clave,
          str(list(por_clave)))
    return True


# ─────────────────────────────────────────────────────────────────────
def paso4_mobile_detalle():
    """GET /mobile/sorteos/<id>?numero_sorteo=X: cada folio en SU dotacion."""
    log("\n== PASO 4: detalle mobile por dotacion ==")
    for draw, folio in [("4024", "E2E-DOT-A"), ("4025", "E2E-DOT-B")]:
        st, det = req_mob("GET", "/mobile/sorteos/%d" % ID_SORTEO,
                          token=TOKEN_MOB, params={"numero_sorteo": draw})
        check("GET detalle %s 200" % draw, st == 200, f"{st} {str(det)[:200]}")
        d = det.get("data") or {}
        log("    detalle %s: %s" % (draw, json.dumps(d, ensure_ascii=False)[:400]))
        check("label %s = MAYOR - %s" % (draw, draw), d.get("sorteo") == "MAYOR - " + draw,
              str(d.get("sorteo")))
        eyd = {e["folio"]: e for e in (d.get("entregas_devoluciones") or [])}
        check("detalle %s: SOLO su folio (%s), sin mezcla" % (draw, folio),
              set(eyd) == {folio}, str(set(eyd)))
        if folio in eyd:
            e = eyd[folio]
            check("folio %s: series 3, subtotal 210" % folio,
                  e.get("series") == "3" and aprox(e.get("subtotal", -1), 210), str(e))
        r = d.get("resumen") or {}
        check("resumen %s: consignado 210 ventas 210 saldo 210" % draw,
              aprox(r.get("consignado", -1), 210) and aprox(r.get("ventas", -1), 210)
              and aprox(r.get("saldo", -1), 210), str(r))
    return True


# ─────────────────────────────────────────────────────────────────────
def paso5_devolucion():
    """Devolucion de 1 billete de la dotacion 4024 — debe caer en la fila 4024."""
    log("\n== PASO 5: devolucion parcial 4024 ==")
    payload = {
        "movimiento": {
            "folio": "E2E-DOT-DEV",
            "tipo_movimiento": "devolucion",
            "id_cedis": CEDIS,
            "id_billetero": BILLETERO,
            "id_sorteo": ID_SORTEO,
            "fecha_movimiento": TODAY,
            "estatus": "aplicada",
            "observaciones": "E2E devolucion dotacion 4024",
        },
        "detalles": [
            {"id_producto": PRODUCTO, "id_sorteo": ID_SORTEO, "numero_sorteo": "4024",
             "numero_billete": BILLETES[0], "serie": "01", "fraccion": 0,
             "cantidad": 1, "precio": PRECIO, "total": PRECIO},
        ],
    }
    st, body = api_alm("POST", "/movimientos-almacen", payload)
    record("devolucion 1 billete 4024", "2xx", "HTTP %s %s" % (st, json.dumps(body)[:120]),
           st in (200, 201))

    st, l = req_mob("GET", "/mobile/sorteos", token=TOKEN_MOB)
    d = (l.get("data") or {})
    sorteos = d.get("sorteos") or []
    por_clave = {(s["id_sorteo"], s["numero_sorteo"]): s for s in sorteos}
    a = por_clave.get((ID_SORTEO, "4024"), {})
    b = por_clave.get((ID_SORTEO, "4025"), {})
    check("4024 tras devolucion: consignado 210 devuelto 70 saldo 140",
          aprox(a.get("consignado", -1), 210) and aprox(a.get("devuelto", -1), 70)
          and aprox(a.get("saldo", -1), 140), str(a))
    check("4025 INTACTA tras devolucion de 4024 (aislamiento)",
          aprox(b.get("consignado", -1), 210) and aprox(b.get("devuelto", -1), 0)
          and aprox(b.get("saldo", -1), 210), str(b))
    return True


# ─────────────────────────────────────────────────────────────────────
def paso6_regresion_billetero1():
    """El E2E existente del billetero 1 (mobile_dev) no cambia: 4 filas."""
    log("\n== PASO 6: regresion billetero 1 (mobile_dev) ==")
    st, r = req_mob("POST", "/mobile/auth/login",
                    {"username": MOBILE_USER, "password": MOBILE_PASS,
                     "dispositivo": "e2e-dotaciones-reg"})
    if not (r.get("success") and st == 200):
        log("  login mobile_dev fallo: " + json.dumps(r)[:300])
        return False
    tok = r["data"]["token"]
    st, l = req_mob("GET", "/mobile/sorteos", token=tok)
    d = (l.get("data") or {})
    sorteos = d.get("sorteos") or []
    labels = [s["sorteo"] for s in sorteos]
    check("mobile_dev: 4 filas (sin regresion)",
          set(labels) == {"MAYOR - E2EDRAWB", "ZODIACO - ZOD-0472", "MAYOR", "SUPERIOR"},
          str(labels))
    por_clave = {(s["id_sorteo"], s["numero_sorteo"]): s for s in sorteos}
    e2e = por_clave.get((37, "E2EDRAWB"), {})
    check("mobile_dev: 37/E2EDRAWB consigna 210 devol 70 saldo 140",
          aprox(e2e.get("consignado", -1), 210) and aprox(e2e.get("devuelto", -1), 70)
          and aprox(e2e.get("saldo", -1), 140), str(e2e))
    return True


# ─────────────────────────────────────────────────────────────────────
def resumen():
    log("\n" + "=" * 70)
    log("RESUMEN E2E DOTACIONES 4024/4025")
    log("=" * 70)
    fails = 0
    for paso, exp, obt, res in RESULTS:
        log("%-4s %-58s %s" % (res, paso[:58], obt[:60]))
        if res == "FAIL":
            fails += 1
    log("-" * 70)
    log("TOTAL: %d checks, %d FAIL" % (len(RESULTS), fails))
    return fails


def main():
    if not login_almacen():
        log("LOGIN ALMACEN FALLO")
        return 2
    paso0_setup()
    if not paso1_recepcion():
        return 1
    if not paso2_consignas():
        return 1
    if not paso3_mobile_lista():
        return 1
    paso4_mobile_detalle()
    paso5_devolucion()
    paso6_regresion_billetero1()
    return 1 if resumen() else 0


if __name__ == "__main__":
    sys.exit(main())
