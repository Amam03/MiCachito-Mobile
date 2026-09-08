# Notas de Negocio — Premios y Reintegros (Gestión)

> Documento para futura integración con backend (flujo mockups 3, 3.1,
> 3.2, 3.3). Fase actual: SOLO INTERFAZ con estado local en memoria —
> **no modificar el backend**. Documentación de referencia únicamente.

## Pantallas implementadas (2026-09-08)

| Mockup | Pantalla | Estado |
|--------|----------|--------|
| 3 | `PremiosReintegrosPage` — resumen agregado + lista + FAB "+" | Implementada, pendiente inspección |
| 3.1 / 3.2 | `DetallePremiosReintegrosPage` — header folio/fecha, resumen, pestañas Premios/Reintegros (iconos SVG estrella/ciclo), sorteos expandibles con tabla Billete/Signo/Vig./Valor | Implementada, pendiente inspección |
| 3.3 (captura) | `CapturaPremiosReintegrosPage` — folio incremental, Escanear, resumen + contadores en vivo, Guardar/Cancelar | Implementada, pendiente inspección |
| 3.3 (escaneo) | `EscanearBoletosPage` — cámara integrada reutilizada + diálogo resultado + entrada manual | Implementada, pendiente de inspección |

Decisiones cerradas con el usuario (2026-09-08):

1. Pestaña Premios del detalle: MISMA estructura que Reintegros
   (sorteos expandibles + tabla Billete/Signo/Vig./Valor).
2. Montos del ejemplo: valores de las imágenes ($30.00 por reintegro,
   2 reintegros = $60.00) SOLO como referencia visual temporal.
3. Tras ACEPTAR un boleto escaneado: SEGUIR ESCANEANDO (escaneo
   continuo) hasta que el usuario toque **Guardar** o **mantenga
   presionado Cancelar** (~800 ms). Tap simple en Cancelar solo avisa.

## Estructura del movimiento (para el endpoint futuro)

```
MovimientoPremiosReintegros
├── Folio (int, incremental en la lista local)
├── Fecha (string "DD-mes-AAAA" formato mockup)
├── SorteosPremios  → lista de SorteoPremiosReintegros
│   └── SorteoPremiosReintegros
│       ├── NombreSorteo ("Mayor - 3966" / "Sorteo Mayor 4010")
│       └── Boletos → BoletoPremioReintegro
│           ├── NumeroBillete
│           ├── Signo (zodiacal, o serie LN; "-" si no aplica)
│           ├── Vig (fracción/vigésimo)
│           └── Valor (monto)
└── SorteosReintegros → (misma estructura)
```

## Reglas de negocio documentadas (SIN implementar)

Estas reglas nacen de los mockups y del flujo acordado; requieren
confirmación de negocio y endpoints del backend:

1. **Folio**: en la app es incremental local (FolioSiguiente = max+1).
   El folio real lo debe asignar el backend al Guardar (persistencia).
   El mockup 3.3 muestra "Folio: 3" — consistente con folio generado
   según el movimiento en curso.
2. **Monto por boleto**: la UI local usa $30.00 por reintegro como
   referencia visual (ValorReintegroEjemplo). El monto REAL de premio
   o reintegro debe venir del backend al consultar cada boleto
   (endpoint de consulta de premios por código de barras/QR) — la
   sábana de premios del sorteo define qué boletos son ganadores y
   con cuánto (premio vs reintegro del precio del cachito).
3. **Clasificación Premio vs Reintegro**: hoy usa ConsultaPremiosService
   (mock con 3 boletos demo). En integración: el backend responde el
   tipo (premio mayor / reintegro / sin premio) por boleto consultado;
   "NO TIENE PREMIO" también se agrega? — REGLA PENDIENTE DE NEGOCIO:
   en la UI actual NO TIENE PREMIO NO se acumula (diálogo informativo
   y al ACEPTAR simplemente se descarta sin agregar al movimiento).
4. **Agrupación al Guardar**: boletos agrupados por (tipo, sorteo);
   cada grupo es un encabezado expandible en el detalle.
5. **Dedupe**: el mismo código no se acumula dos veces en el mismo
   movimiento (regla local; backend debería validar boleto único por
   movimiento).
6. **Cancelar**: descarta la captura completa; exige pulsación larga
   para evitar pérdida accidental.
7. **Vigencia (columna Vig.)**: hoy muestra la fracción/vigésimo del
   código escaneado. Para integración: la vigencia real del boleto
   (fecha límite de pago) viene del sorteo (fecha_limite_pago /
   fecha_vencimiento en la tabla sorteos).
8. **Estados del escaneo continuo**: cámara integrada CameraX +
   zxing-cpp (ver NOTAS camara-escaner-zxing.md). El 2º QR de los
   boletos (URL cartilla de derechos) se ignora (solo dígitos).

## Datos del ejemplo (referencia visual, NO fijos)

El servicio siembra UN movimiento (folio 1, "02-abril-2025") con 2
reintegros del sorteo "Mayor - 3966" (billetes 47727 y 16171, $30.00
c/u) — tomados verbatim de los mockups 3/3.1/3.2 para comprobar el
flujo. NO constituyen datos reales del sistema; los movimientos
guardados desde la captura nacen de boletos escaneados en la sesión.
