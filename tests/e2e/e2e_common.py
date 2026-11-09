"""Utilidades comunes de los E2E de Mi Cachito Mobile (tests/e2e/).

Centraliza lo que cada script repetía por su cuenta: URL base del backend,
credenciales de los billeteros de prueba y login de sesión mobile.

CONFIGURACIÓN (variables de entorno, con defaults de desarrollo):

  E2E_BASE_URL      URL base de la API (default http://localhost:8080/api;
                    en el host del emulador Android equivale a
                    http://10.0.2.2:8080/api desde el emulador).
  E2E_TIMEOUT_S     timeout por petición en segundos (default 15).
  E2E_MOBILE_USER   usuario del expendio de la app (default mobile_dev,
                    expendio 11 -> billetero 1).
  E2E_MOBILE_PASS   password del expendio de la app.
  E2E_DOT_USER      segundo billetero de aislamiento (default e2e_dot,
                    billetero 11).
  E2E_DOT_PASS      password del segundo billetero.

Los defaults de usuario/password son los SEMBRADOS de la BD de desarrollo
(Docker). En otro entorno, exporta las variables antes de correr.

ADVERTENCIA: varios scripts EJECUTAN escrituras reales (POST de venta,
devolución, depósito) como lo haría un billetero: corren SOLO contra la BD
de desarrollo. Ningún script borra ni modifica datos existentes; los
registros creados quedan con folio/marca E2E- para revisión.
"""
import json
import os
import urllib.error
import urllib.request

BASE = os.environ.get("E2E_BASE_URL", "http://localhost:8080/api")
TIMEOUT = float(os.environ.get("E2E_TIMEOUT_S", "15"))
MOBILE_USER = os.environ.get("E2E_MOBILE_USER", "mobile_dev")
MOBILE_PASS = os.environ.get("E2E_MOBILE_PASS", "DevMobile2026!")
DOT_USER = os.environ.get("E2E_DOT_USER", "e2e_dot")
DOT_PASS = os.environ.get("E2E_DOT_PASS", "DevMobile2026!")


def req(method, path, body=None, token=None, timeout=None):
    """Petición JSON; devuelve (status_code, dict)."""
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(
        BASE + path, data=data, headers=headers, method=method
    )
    try:
        with urllib.request.urlopen(r, timeout=timeout or TIMEOUT) as resp:
            return resp.status, json.loads(resp.read() or b"{}")
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read() or b"{}")
        except ValueError:
            return e.code, {"raw": "no-json"}


class _Checks:
    """Contador de OK/FAIL con el formato [OK]/[FAIL] de los scripts."""

    def __init__(self):
        self.fails = 0
        self.total = 0

    def check(self, nombre, cond, detalle=""):
        self.total += 1
        marca = "OK " if cond else "FAIL"
        if not cond:
            self.fails += 1
        print(f"[{marca}] {nombre}" + (f" -> {detalle}" if detalle and not cond else ""))

    def resumen(self, nombre):
        print()
        if self.fails:
            print(f"FALLOS: {self.fails} de {self.total} ({nombre})")
            raise SystemExit(1)
        print(f"TODO OK: {self.total} ({nombre})")


def login_mobile(dispositivo="e2e-mobile"):
    """Login del expendio de la app; devuelve (token, data) o (None, resp)."""
    code, r = req("POST", "/mobile/auth/login", {
        "username": MOBILE_USER,
        "password": MOBILE_PASS,
        "dispositivo": dispositivo,
    })
    data = r.get("data") if isinstance(r.get("data"), dict) else {}
    ok = code == 200 and r.get("success") is True
    return (data.get("token"), data) if ok else (None, r)


def login_dot(dispositivo="e2e-dot"):
    """Login del segundo billetero (aislamiento b1 <-> b11)."""
    code, r = req("POST", "/mobile/auth/login", {
        "username": DOT_USER,
        "password": DOT_PASS,
        "dispositivo": dispositivo,
    })
    data = r.get("data") if isinstance(r.get("data"), dict) else {}
    ok = code == 200 and r.get("success") is True
    return (data.get("token"), data) if ok else (None, r)
