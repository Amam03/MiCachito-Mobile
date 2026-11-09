#!/usr/bin/env python3
"""E2E de sesiones activas (M-06): GET /mobile/auth/sesiones y POST
/mobile/auth/sesiones/revocar.

Verifica el contrato que consume SesionesActivasViewModel:
  1. La lista solo trae sesiones del PROPIO expendio (aislamiento con el
     segundo billetero: las sesiones de e2e_dot NUNCA aparecen).
  2. La sesión con la que se consulta viene actual=true; las demás false.
  3. Revocar una sesión SECUNDARIA del mismo expendio NO revoca la actual:
     el token sigue vivo tras la revocación (verify 200).
  4. La sesión revocada desaparece de la lista y su token muere (401).
  5. id_sesion inválido/ausente -> 422; sesión de OTRO expendio -> 404.
  6. Sin token / token basura -> 401 (los endpoints exigen sesión).

ESCRITURAS: revoca SOLO sesiones E2E desechables creadas aquí mismo
(dos logins "e2e-sesiones-*"). Ninguna sesión de otro dispositivo
del expendio se toca. Corre contra la BD de desarrollo.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from e2e_common import BASE, TIMEOUT, req, login_mobile, login_dot  # noqa: E402
from e2e_common import _Checks  # noqa: E402

c = _Checks()


def sesiones(token):
    return req("GET", "/mobile/auth/sesiones", token=token)


def verificar(token):
    return req("GET", "/mobile/auth/verify", token=token)


def revocar(token, id_sesion):
    return req("POST", "/mobile/auth/sesiones/revocar", {"id_sesion": id_sesion}, token=token)


# --- login de las sesiones E2E desechables (este script los revoca) ---
tok_a, r = login_mobile("e2e-sesiones-a")
c.check("login sesion A (e2e-sesiones-a)", bool(tok_a), str(r)[:120])
tok_b, r = login_mobile("e2e-sesiones-b")
c.check("login sesion B (e2e-sesiones-b)", bool(tok_b), str(r)[:120])
tok_dot, r = login_dot("e2e-sesiones-dot")
c.check("login dot (aislamiento)", bool(tok_dot), str(r)[:120])
assert tok_a and tok_b and tok_dot

# --- 1. lista: solo sesiones del propio expendio, actual=true solo en la propia ---
code, r = sesiones(tok_a)
data = r.get("data") or []
c.check("GET sesiones 200", code == 200 and r.get("success") is True, f"http={code}")
c.check("la lista es un arreglo", isinstance(data, list))
campos = {"id_sesion", "dispositivo", "fecha_creacion", "ultimo_acceso", "fecha_expiracion", "actual"}
if data:
    c.check("campos del contrato (id/dispositivo/fechas/actual)", campos.issubset(set(data[0].keys())), str(data[0].keys()))
ids_a = {s.get("id_sesion") for s in data}
c.check("sesion B visible desde A (mismo expendio)", any(s.get("dispositivo") == "e2e-sesiones-b" for s in data))
c.check("actual=true SOLO en la sesion que consulta", [s.get("actual") for s in data].count(True) == 1
        and next(s for s in data if s.get("actual"))["dispositivo"] == "e2e-sesiones-a")

# aislamiento: sesiones del dot NUNCA en la lista del expendio de la app
code_dot, r_dot = sesiones(tok_dot)
ids_dot = {s.get("id_sesion") for s in (r_dot.get("data") or [])}
c.check("aislamiento: 0 sesiones del 2do expendio en la lista", not (ids_a & ids_dot))

# --- 2. revocar la sesion SECUNDARIA (B) no mata la actual (A) ---
code, r = verificar(tok_b)
ses_b = (r.get("data") or {}).get("sesion", {}).get("id_sesion")
c.check("verify de B devuelve id_sesion", code == 200 and ses_b is not None, f"http={code}")

code, r = revocar(tok_a, ses_b)
c.check("revocar B desde A (mismo expendio): 200", code == 200 and r.get("success") is True, f"http={code} {str(r)[:120]}")

code, r = verificar(tok_a)
c.check("la sesion ACTUAL (A) sigue viva tras revocar B", code == 200 and r.get("success") is True, f"http={code}")

code, r = verificar(tok_b)
c.check("el token de B quedo muerto (401)", code == 401, f"http={code}")

code, r = sesiones(tok_a)
data = r.get("data") or []
c.check("B desaparece de la lista", not any(s.get("id_sesion") == ses_b for s in data))

# --- 3. validaciones de revocar ---
code, r = revocar(tok_a, 0)
c.check("id_sesion=0 -> 422", code == 422, f"http={code}")
code, r = revocar(tok_a, -5)
c.check("id_sesion negativo -> 422", code == 422, f"http={code}")
code, r = req("POST", "/mobile/auth/sesiones/revocar", {}, token=tok_a)
c.check("sin id_sesion -> 422", code == 422, f"http={code}")

# sesion de OTRO expendio (la del dot): 404, y NO se revoca
code, r = sesiones(tok_dot)
ses_dot = ((r.get("data") or [{}])[0]).get("id_sesion")
code, r = revocar(tok_a, ses_dot)
c.check("revocar sesion AJENA (2do expendio) -> 404", code == 404, f"http={code} {str(r)[:120]}")
code, r = verificar(tok_dot)
c.check("la sesion ajena NO se revoco (verify 200)", code == 200, f"http={code}")

# --- 4. sin token / token basura ---
code, r = req("GET", "/mobile/auth/sesiones")
c.check("GET sesiones sin token -> 401", code == 401, f"http={code}")
code, r = req("GET", "/mobile/auth/sesiones", token="token-invalido-e2e")
c.check("GET sesiones token basura -> 401", code == 401, f"http={code}")
code, r = req("POST", "/mobile/auth/sesiones/revocar", {"id_sesion": 1}, token="token-invalido-e2e")
c.check("revocar token basura -> 401", code == 401, f"http={code}")

# --- limpieza: revocar la sesion A de este script (best-effort) ---
code, r = verificar(tok_a)
ses_a = (r.get("data") or {}).get("sesion", {}).get("id_sesion")
if ses_a:
    revocar(tok_a, ses_a)
# la del dot tambien (creada aqui)
code, r = verificar(tok_dot)
ses_dot2 = (r.get("data") or {}).get("sesion", {}).get("id_sesion")
if ses_dot2:
    revocar(tok_dot, ses_dot2)

c.resumen("sesiones activas M-06")
