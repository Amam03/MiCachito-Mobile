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

# E2E de /api/mobile/premios/consultar — 10 casos del plan tecnico.
import json
import urllib.request

BASE = BASE  # del helper e2e_common


def post(path, payload, token=None):
    req = urllib.request.Request(
        BASE + path,
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json", **({"Authorization": f"Bearer {token}"} if token else {})},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        return {"_http": e.code, **json.loads(e.read() or b"{}")}


def show(name, resp):
    d = resp.get("data", resp)
    line = " | ".join(
        f"{k}={d.get(k)}" for k in
        ("resultado", "premio_cachito", "premio_serie", "reintegro", "tipo_premio", "numero_vigesimos", "fuente", "nombre_sorteo", "serie_cruda", "motivo")
        if k in d
    )
    print(f"[{name}] http={resp.get('_http', 200)} {line}")


# --- login (sesion mobile) ---
r = post("/mobile/auth/login", {"username": MOBILE_USER, "password": MOBILE_PASS, "dispositivo": "e2e-premios"})
token = r.get("data", {}).get("token")
print("login:", "OK token" if token else r)
assert token, "login fallo"

C = "/mobile/premios/consultar"

# 1. GANADOR sabana especial 041753 (19-jul): billete 1 serie 05 fr 15
show("1 ganador-sabana", post(C, {"numero_billete": "00001", "serie": "05", "fraccion": 15, "fecha_sorteo": "2026-07-19"}, token))
# 2. REINTEGRO tradicional 014021 (21-jul): billete 53 serie 02 fr 15 -> fallback serie 00
show("2 reintegro-trad", post(C, {"numero_billete": "00053", "serie": "02", "fraccion": 15, "fecha_sorteo": "2026-07-21"}, token))
# 3. mismo billete 53 fr 07 (reintegro aplica a todo el billete)
show("3 reintegro-fr07", post(C, {"numero_billete": "00053", "serie": "02", "fraccion": 7, "fecha_sorteo": "2026-07-21"}, token))
# 4. GANADOR multi-vigesimos billete 326 (014021, 12 vig, $875 total) -> $72.92/cachito
show("4 multi-vig", post(C, {"numero_billete": "00326", "serie": "01", "fraccion": 15, "fecha_sorteo": "2026-07-21"}, token))
# 5. Zodiaco 34dig: billete 1 signo 05 fr 15 -> serie 05 del 041753
show("5 zodiaco", post(C, {"numero_billete": "00001", "serie": "05", "fraccion": 15, "fecha_sorteo": "2026-07-19", "es_zodiaco": True}, token))
# 6. SIN PREMIO: billete inexistente
show("6 sin-premio", post(C, {"numero_billete": "59999", "serie": "01", "fraccion": 1, "fecha_sorteo": "2026-07-21"}, token))
# 7. sorteo sin sabana (Mayor real 4010, 21-abr-2026)
show("7 sin-sabana", post(C, {"numero_billete": "03787", "serie": "01", "fraccion": 15, "fecha_sorteo": "2026-04-21"}, token))
# 8. codigo invalido / payload incompleto
show("8 invalido", post(C, {"numero_billete": "", "fraccion": 15}, token))
# 9. misma fecha multi-sorteo 2026-07-23 (16 sorteos, sin sabana)
show("9 multi-sorteo", post(C, {"numero_billete": "00010", "serie": "01", "fraccion": 15, "fecha_sorteo": "2026-07-23"}, token))
# 10. sin token (rechazo auth)
show("10 sin-token", post(C, {"numero_billete": "00001", "serie": "05", "fraccion": 15, "fecha_sorteo": "2026-07-19"}))
