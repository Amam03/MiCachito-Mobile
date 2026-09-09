# NOTAS — Reportes / Estado de Cuenta (mockups 9.x)

Fase: SOLO INTERFAZ. Pestañas Fondo de Ahorro y Facturación quedaron como
placeholders navegables ("En preparación") — su contenido es desarrollo
posterior.

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
6. **Pestañas Fondo de Ahorro (9.2) y Facturación (9.3)**: mocks
   existentes en docs/UI/Gestion; implementar selección de rango de
   fechas + detalle + PDF cuando toque su etapa.

## Supuestos declarados (corregibles)

- a. Pestañas 2 y 3 muestran placeholder "En preparación" (navegables).
- b. Vendedor/Plaza del PDF vacíos (no inventar datos de negocio).
- c. FAB descarga visible desde el estado "generado" (spec); el mockup
  "Reporte Generado" no lo muestra.
- d. Pestaña activa: icono+label azul #4125F4; inactivas gris #9E9E9E.
- e. Overlays ~2 s; la descarga genera el PDF real y lo guarda en
  Descargas (comportamiento igual a Recibos de Pago).
