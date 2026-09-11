# NOTAS — Reportes / Estado de Cuenta (mockups 9.x)

Fase: SOLO INTERFAZ. Las 3 pestañas de Reportes están implementadas:

- **Estado de Cuenta (9.1)**: ver más abajo (esta misma nota).
- **Fondo de Ahorro (9.2)**: ver `NOTAS_FONDO_AHORRO.md` (commit 5eb5973).
- **Facturación (9.3)**: ver `NOTAS_FACTURACION.md` (commit c566350).

## Qué está implementado

- `Views/ReportesPage.xaml` + `ReportesViewModel`: UNA página con header
  azul "Reportes" y tab bar interno de 3 pestañas. Al entrar siempre abre
  Estado de Cuenta (pestaña 0). Estados: inicial (botón azul "Generar
  Reporte" centrado), generado (resumen + sorteos + totales, todo $0.00 y
  SIN registros de sorteos por directriz del usuario) y descargado (FAB
  PDF adicional a la izquierda).
- Overlays: "Procesando...." al generar y "Descargando...." al descargar
  (scrim #80707070, spinner, ~2 s simulados).
- FABs azules 56 px: descarga (derecha, tras generar) y PDF (izquierda,
  tras descargar; abre el visor del sistema con fallback Share).
- PDF: `Services/EstadoDeCuentaPdfService.cs` (SkiaSharp, carta 612×792):
  logo Mi Cachito, título "Estado de Cuenta Informativo", Fecha de
  Emisión + Vendedor/Plaza (izquierda) y tabla resumen de 6 conceptos
  (derecha, $0.00), tabla principal de 7 columnas con encabezado rojo
  #F44336 VACÍA, totales $0.00 y pie rojo #E53935 institucional (mismo
  de Recibos de Pago).
- Descarga real a la carpeta pública Descargas vía MediaStore
  (`DescargasService` reutilizado), nombre `estado-cuenta-<dd-MM-yyyy>.pdf`.
- Fechas en pantalla y nombres de archivo SIEMPRE en español (es-MX)
  vía `Helpers/FormatosFecha.cs` (ver "Fechas en español" abajo).

## Pendientes de backend (NO implementar todavía)

1. **Fuente del reporte**: `Services/EstadoCuentaService.ObtenerAsync()`
   devuelve TODO en cero y SIN sorteos — punto único de conexión. Al
   integrar: reemplazar por la llamada al endpoint que devuelva el
   estado de cuenta del vendedor autenticado.
2. **Resumen de saldo**: Fondo de Ahorro, Fideicomiso, Pagarés, Bolsa
   Electrónica, Garantía Total, Capacidad de Crédito — conceptos y
   cálculo definidos por el backend (posible fuente: cuentas por
   vendedor/agente en el sistema financiero del CEDIS).
3. **Listado de sorteos**: la estructura `SorteoEstadoCuenta`
   (Sorteo/Fecha/Cantidad/Vencido/Consigna/Pagos/Total) queda vacía;
   llenar con los sorteos reales del vendedor. Los sorteos de los
   mockups (ZODIACO ESPECIAL 1724, MAYOR 3990, ESPECIAL 305) son solo
   referencia visual — NO usarlos como datos.
4. **Totales**: Vencido/Consigna/Pagos/Total se calculan ya como sumas
   de la colección (dejar que el backend valide contra sus propios
   acumulados).
5. **Datos del PDF**: Vendedor y Plaza van vacíos en esta fase —
   llenarlos desde la sesión del usuario autenticado. La tabla resumen
   del PDF debe recibir los mismos valores que la pantalla.
6. **Pestañas Fondo de Ahorro (9.2) y Facturación (9.3)**: YA
   implementadas — ver sus notas. Pendiente de backend: conectar sus
   services (`FondoAhorroService`, `FacturacionService`) a los
   endpoints reales.

## Fechas en español (regla de proyecto)

- NUNCA usar `CurrentCulture` para meses por nombre: el dispositivo
  puede correr en en-US y renderizar "28-October-2025" (bug real
  Recibos de Pago, hallado en la auditoría 2026-09-10).
- `Helpers/FormatosFecha.cs` es la única fuente: `MesEspanol(d)`,
  `FechaArchivo(d)` ("09-octubre-2026"), `FechaLarga(d)` (pantalla),
  `FechaHoraLarga(fecha, hora)`. Todas con es-MX explícito.
- Nombre del archivo PDF según el mockup de su pantalla: EC usa
  `estado-cuenta-<dd-MM-yyyy>.pdf`, FA/FAC usan `<nombre>-<dd-mes-aaaa>.pdf`
  — cada uno replica su mockup; NO unificar.

## Supuestos declarados (corregibles)

- a. Vendedor/Plaza del PDF vacíos (no inventar datos de negocio).
- b. FAB descarga visible desde el estado "generado" (spec); el mockup
  "Reporte Generado" no lo muestra.
- c. Pestaña activa: icono+label azul #4125F4; inactivas gris #9E9E9E.
- d. Overlays ~2 s; la descarga genera el PDF real y lo guarda en
  Descargas (comportamiento igual a Recibos de Pago).
