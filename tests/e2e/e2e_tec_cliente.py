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

"""E2E Sorteos Tec con CLIENTE — POST api/mobile/ventas/crear (fix P2).

Verifica que el cliente capturado en la App (pantalla 15) queda registrado
en la venta hecha por POST /mobile/ventas/crear (endpoint que usa la App
en MobileVentasService.CrearVentaTecAsync), sin romper:
- venta TEC existente (folio/total reales, boleto pasa a 'vendido')
- venta sin cliente (LN no manda cliente -> id_cliente NULL)
- contrato de la respuesta (id_venta/folio/total)

Usuarios: mobile_dev (billetero 1) — bil001..005 NO existen en esta BD
local (solo en la otra instalación); usuarios equivalentes ya existentes.
"""
import subprocess, json
import requests

BASE = BASE  # del helper e2e_common
fail = 0


def check(nombre, cond, detalle=""):
    global fail
    marca = "OK " if cond else "FAIL"
    if not cond:
        fail += 1
    print(f"[{marca}] {nombre}" + (f" -> {detalle}" if detalle and not cond else ""))


def sql(query):
    php = ("$db = require '/app/config/db.php';"
           "$pdo = new PDO($db['dsn'], $db['username'], $db['password'],"
           " [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]);"
           "$st = $pdo->query(" + json.dumps(query) + ");"
           "$rows = []; while ($r = $st->fetch(PDO::FETCH_ASSOC)) { $rows[] = $r; }"
           "echo json_encode($rows);")
    out = subprocess.run(["docker", "exec", "micachito-php", "php", "-r", php],
                         capture_output=True, text=True, timeout=30)
    if out.returncode != 0:
        raise RuntimeError("SQL fallo: " + out.stderr[-300:])
    return json.loads(out.stdout or "[]")


# 1) Login Mobile
r = requests.post(BASE + "/mobile/auth/login", json={"username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-tec-cliente"}, timeout=TIMEOUT)
check("1 login mobile", r.json().get("success"), str(r)[:150])
tok = r.json()["data"]["token"]
H = {"Authorization": f"Bearer {tok}"}

# 2) Catálogo real
r = requests.get(BASE + "/mobile/ventas/sorteos-tec", headers=H, timeout=TIMEOUT)
check("2 catalogo sorteos-tec", r.json().get("success"), str(r)[:150])
sorteos = r.json()["data"]["sorteos"]
check("2b con material disponible", len(sorteos) > 0, str(sorteos)[:150])
s = sorteos[0]
ID_SORTEO, PRECIO = s["id_sorteo"], float(s["precio"])

# 3) Billetes
r = requests.get(BASE + f"/mobile/ventas/billetes-tec?id_sorteo={ID_SORTEO}", headers=H, timeout=TIMEOUT)
check("3 billetes-tec", r.json().get("success"), str(r)[:150])
bils = r.json()["data"]["billetes"]
check("3b boletos disponibles", len(bils) > 0, str(bils)[:150])
idb = bils[0]["id_billete"]

# 4-6) Venta CON cliente (como la App: POST ventas/crear)
body = {
    "items": [{"id_billete": idb}],
    "cliente": {"nombre": "PERLA", "apellido_paterno": "E2E", "apellido_materno": "TEC",
                "telefono": "2281234567", "correo": "perla.e2e@test.mx"},
}
r = requests.post(BASE + "/mobile/ventas/crear", json=body, headers=H, timeout=TIMEOUT)
d = r.json()
check("6 POST ventas/crear con cliente", r.status_code == 200 and d.get("success"), str(d)[:200])
v = d.get("data", {})
ID_VENTA, FOLIO, TOTAL = v.get("id_venta"), v.get("folio"), v.get("total")
check("6b folio real", bool(FOLIO), str(v)[:150])
check("6c total real = precio boleto", abs(float(TOTAL or 0) - PRECIO) < 0.01, f"total={TOTAL} precio={PRECIO}")

# 7) Venta real en BD
row = sql(f"SELECT id_venta, folio_venta, total, id_cliente, id_billetero FROM ventas WHERE id_venta = {ID_VENTA}")[0]
check("7 venta en BD", int(row["id_venta"]) == ID_VENTA and row["folio_venta"] == FOLIO, str(row))
ID_CLIENTE = row["id_cliente"]
check("7b id_cliente NO null", ID_CLIENTE is not None, str(row))
# boleto vendido
bl = sql(f"SELECT estatus, id_billetero_actual FROM billetes_loteria WHERE id_billete = {idb}")[0]
check("7c boleto vendido", bl["estatus"] == "vendido", str(bl))

# 8) Cliente registrado
cl = sql(f"SELECT nombres, apellido_paterno, telefono, email, tipo_cliente, estatus FROM clientes WHERE id_cliente = {ID_CLIENTE}")[0]
check("8 cliente en BD", cl["nombres"] == "PERLA" and cl["apellido_paterno"] == "E2E", str(cl))
check("8b telefono/correo", cl["telefono"] == "2281234567" and cl["email"] == "perla.e2e@test.mx", str(cl))

# 9) Reporte de Escritorio ve la venta con cliente (ventas-sorteos-tec-app)
import os as _os
_admin_user = _os.environ.get("E2E_ADMIN_USER", "admin")
_admin_pass = _os.environ.get("E2E_ADMIN_PASS", "admin123")
r2 = requests.post(BASE + "/auth/login", json={"username": _admin_user, "password": _admin_pass}, timeout=TIMEOUT)
if r2.status_code == 200 and r2.json().get("success"):
    tok2 = r2.json()["data"]["token"] if isinstance(r2.json().get("data"), dict) else r2.json()["data"]["token"]
    H2 = {"Authorization": f"Bearer {tok2}"}
    rr = requests.get(BASE + "/ventas-sorteos-tec-app/detalle", headers=H2, timeout=TIMEOUT)
    dd = rr.json()
    filas = dd.get("data", []) if dd.get("success") else []
    if isinstance(filas, dict):
        filas = filas.get("ventas", filas.get("detalle", []))
    fila = next((f for f in filas if str(f.get("folio") or f.get("folio_venta")) == str(FOLIO)), None)
    check("9 reporte escritorio ve venta TEC", fila is not None, str(dd)[:200])
    if fila:
        cliente_txt = str(fila.get("cliente", "")) + str(fila.get("cliente_nombre", ""))
        check("9b reporte muestra cliente", "PERLA" in (str(fila)), str(fila)[:250])
else:
    print("[SKIP] 9 login escritorio admin no disponible en esta BD:", str(r2.json())[:120])

# 10) Venta SIN cliente (LN-style) sigue igual
if len(bils) > 1:
    idb2 = bils[1]["id_billete"]
    r = requests.post(BASE + "/mobile/ventas/crear", json={"items": [{"id_billete": idb2}]}, headers=H, timeout=TIMEOUT)
    d = r.json()
    check("10 venta sin cliente OK", r.status_code == 200 and d.get("success"), str(d)[:200])
    v2 = d.get("data", {})
    row2 = sql(f"SELECT id_cliente FROM ventas WHERE id_venta = {v2.get('id_venta')}")[0]
    check("10b id_cliente NULL (LN no cambia)", row2["id_cliente"] is None, str(row2))
else:
    print("[SKIP] 10 sin segundo boleto disponible")

# 11) No duplicar: el mismo boleto otra vez -> 409
r = requests.post(BASE + "/mobile/ventas/crear", json={"items": [{"id_billete": idb}], "cliente": {"nombre": "X"}}, headers=H, timeout=TIMEOUT)
check("11 reventa mismo boleto -> 409", r.status_code == 409, f"{r.status_code} {str(r.json())[:120]}")

print()
print(f"Ventas E2E creadas (revisar): id={ID_VENTA} folio={FOLIO} cliente={ID_CLIENTE}")
print(f"RESULTADO: {'FALLAS=' + str(fail) if fail else 'TODO OK'}")
raise SystemExit(1 if fail else 0)
