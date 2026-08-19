# INVESTIGACION — INTEGRACION REAL DE TIEMPO AIRE

Fecha: 2026-08-18
Objetivo: Determinar si existe una integracion real con proveedores/agregadores de recargas y si se puede reutilizar para Mobile.

---

## 1. CONCLUSION PRINCIPAL

**NO EXISTE ninguna integracion real con proveedores o agregadores de recargas de Tiempo Aire en el backend ni en el frontend desktop.**

El sistema MiCachito (que sustituye a "Sr. Billetero") NO ejecuta recargas. Solo REGISTRA transacciones de venta digital (TAE / SB LN) como datos historicos para reportes, liquidaciones y estados de cuenta de Contabilidad Corporativo.

---

## 2. EVIDENCIA ENCONTRADA

### 2.1 Sin libreria HTTP cliente

`composer.json` tiene unicamente:
- yiisoft/yii2 ~2.0.45
- yiisoft/yii2-bootstrap5 ~2.0.2
- yiisoft/yii2-symfonymailer ~2.0.3

NO hay Guzzle, Symfony HttpClient, ni ninguna libreria para consumo de APIs externas.

### 2.2 Sin llamadas HTTP externas

Busqueda de `curl_`, `Guzzle`, `file_get_contents.*http`, `HttpClient`, `http_build_query`, `stream_context`, `fopen.*http` en todo el backend:

**Resultado: 0 coincidencias.**

El backend no realiza ninguna solicitud HTTP a servicios externos.

### 2.3 Sin URLs de APIs externas

Busqueda de `https?://[a-zA-Z0-9]` en todo el backend:

**Resultado: Solo URLs de Yii framework (yiiframework.com), GitHub, Packagist, Docker, y localhost.**

Ninguna URL apunta a un carrier (Telcel, AT&T, Movistar) o agregador de recargas.

### 2.4 Sin variables de entorno ni configuracion de integracion

- `config/params.php`: solo tiene adminEmail, senderEmail, senderName. Ninguna config de TAE.
- No existe archivo `.env` en el backend.
- `config/db.php.example`: solo credenciales de MySQL.
- `docker-compose.yml`: solo servicio PHP+Apache en puerto 8000. Sin servicios externos.

### 2.5 Sin agregadores/plataformas de pago

Busqueda de `openpay|conekta|stripe|paypal|recargafacil|prepaid|recauda|soap|wsdl|rest.?client`:

**Resultado: 0 coincidencias relevantes.** Las unicas apariciones de "electronico" son:
- Tipo de metodo de pago (efectivo, electronico, transferencia, cheque, tarjeta)
- Tipo de contenido de paquetes (loteria_nacional, instantanea, electronico, otros)
- Comentario en VentasElectronicas model: "venta digital (TAE / SB LN)"

### 2.6 VentasElectronicasController — solo lectura

El controlador `VentasElectronicasController.php` (444 lineas) tiene EXACTAMENTE 7 acciones, todas GET:

| Accion | Metodo | Descripcion |
|--------|--------|-------------|
| actionResumen | GET | Resumen mensual agrupado |
| actionLiquidaciones | GET | Lista de liquidaciones |
| actionLiquidacionConceptos | GET | Desglose por tipo de venta |
| actionLiquidacionTransacciones | GET | Detalle de transacciones |
| actionLiquidacionResumen | GET | Totales y monto en letra |
| actionMonitorSaldo | GET | Garantia/saldo/disponible |
| actionEstadoCuenta | GET | Estado de cuenta completo |

**NO existe actionCreate, actionStore, actionRegistrar, actionRecarga, ni ninguna accion POST.**

### 2.7 Frontend desktop — solo lectura + mocks

`api_client.py` tiene 7 metodos para ventas electronicas, todos GET:
- get_resumen_ventas_electronicas
- get_liquidaciones_billeteros
- get_liquidacion_conceptos
- get_liquidacion_transacciones
- get_liquidacion_resumen
- get_monitor_saldo_venta_electronica
- get_estado_cuenta_venta_electronica

No hay ningun POST/PUT para crear o procesar recargas.

Todos los datos TAE en el frontend desktop estan en mocks:
- `mocks/ventas_electronicas_mock.py` — datos simulados
- `mocks/tira_liquidacion_mock.py` — tiras simuladas
- `mocks/estado_cuenta_mock.py` — estados de cuenta simulados

### 2.8 "Sr. Billetero" es el sistema anterior, NO un proveedor externo

Referencias encontradas:
- `config/web.php` linea 470: `// VENTAS ELECTRONICAS (TAE / SB LN - Sr. Billetero)`
- `README_LOCAL.md`: "El sistema esta diseñado para replicar la funcionalidad de Sr. Billetero"
- `docs/SINONIMOS_TERMINOLOGIA.md`: "Sub-modulo Sr. Billetero" con terminos como VENTAS ELECTRONICAS, CONCILIACION TAE, TIRAS DE LIQUIDACION

"Sr. Billetero" es el sistema LEGADO que MiCachito sustituye. No es un proveedor de recargas ni un agregador.

### 2.9 TAE en arqueos de tienda — ingreso manual

En `arqueos_tienda_mock_service.py` aparece TAE como "productos no inventariados":
```
"TAE TELCEL PAQUETES" — cantidad 100, precio 1.00, total 100.00
"RECARGA AT&T" — cantidad 50, precio 0.90, total 45.00
```

Esto confirma que TAE se registra manualmente durante el arqueo de caja, no proviene de una integracion automatica.

### 2.10 Seed de ventas_electronicas — productos hardcodeados

`m260615_000004_seed_ventas_electronicas.php` tiene:
```php
private $productosTAE = ['TELCEL_PA', 'AT&T', 'MOVISTAR', 'UNEFON'];
private $montosTAE = [50, 100, 200, 500];
```

Estos son datos de PRUEBA para desarrollo. No representan una integracion real.

### 2.11 Sin componentes de integracion

El directorio `components/` tiene unicamente `BitacoraComponent.php` (sistema de auditoria). No hay ningun componente de integracion con carriers, agregadores, o servicios externos de recargas.

---

## 3. COMO FUNCIONA REALMENTE TIEMPO AIRE HOY

### 3.1 Flujo actual (manual, sin integracion)

```
1. El billetero realiza recargas en un sistema EXTERNO (posiblemente la app
   original de Sr. Billetero o directamente con el carrier)

2. Las transacciones se registran en ventas_electronicas:
   - Probablemente importadas desde el sistema externo
   - O capturadas manualmente por Contabilidad Corporativo
   - O generadas por el seed para datos de prueba

3. Contabilidad Corporativo usa los endpoints GET para:
   - Ver resumen mensual de ventas digitales
   - Generar tiras de liquidacion
   - Monitorear saldos de billeteros
   - Consultar estados de cuenta

4. Las liquidaciones agrupan ventas electronicas mensualmente
   para cobranza

5. Los pagos se registran via fichas_pago con aplicado_a='venta_electronica'
```

### 3.2 Que NO hace el sistema actual

- NO ejecuta recargas en tiempo real
- NO se comunica con carriers (Telcel, AT&T, Movistar, etc.)
- NO usa un agregador intermediario
- NO valida si la recarga fue exitosa o fallo
- NO actualiza saldo_actual automaticamente al recargar
- NO tiene catalogo de proveedores en BD
- NO tiene catalogo de montos en BD

---

## 4. RESPUESTAS A LAS PREGUNTAS

### 1. ¿Existe actualmente una integracion real con algun proveedor/agregador?

**NO.** No existe ninguna integracion.

### 2. ¿Donde esta implementada?

**No esta implementada en ningun lado.** No hay codigo, configuracion, libreria, ni URL que indique integracion con un proveedor externo.

### 3. ¿Que sistema la utiliza actualmente?

**Ninguno.** El sistema MiCachito solo registra transacciones historicas. Las recargas reales se realizan fuera de este sistema, probablemente en la aplicacion legada de Sr. Billetero o directamente con cada carrier.

### 4. ¿Que proveedor o plataforma externa utiliza?

**Ninguno identificable.** No hay evidencia de ningun proveedor, agregador, o plataforma externa.

### 5. ¿Como se realiza conceptualmente una recarga?

Hoy: manualmente fuera del sistema. El registro llega a `ventas_electronicas` por importacion o captura manual, con: id_billetero, tipo_venta='TAE', producto (carrier), destino (numero telefonico), subtotal (monto), comision, ret_isr, total, folio_transaccion.

### 6. ¿Que datos necesita enviar?

Para registrar: id_billetero, tipo_venta, producto, destino (telefono), subtotal, comision, ret_isr, fondo_ahorro, total, folio_transaccion, fecha, hora.

### 7. ¿Que respuesta devuelve?

No hay endpoint de recarga. Los endpoints existentes devuelven datos de reportes/consultas en formato `{success: true, data: [...]}`.

### 8. ¿Como se determina si la recarga fue exitosa o fallo?

**No se determina.** No hay logica de validacion de resultado de recarga.

### 9. ¿Existe manejo de folio/transaccion?

Si, pero solo de registro. El campo `folio_transaccion` sigue el formato `VE-{id_billetero}-{anio}{mes}-{seq}` y `transaccion` usa un prefijo `T` (TAE) o `L` (SB LN). Pero esto es solo almacenamiento, no generacion en tiempo real de recarga.

### 10. ¿Podemos reutilizar esa integracion desde una nueva API para Mobile?

**No hay integracion que reutilizar.** Hay que desarrollar una nueva.

### 11. Si NO existe integracion, ¿que evidencia confirma que debemos desarrollar una nueva?

- `billeteros.tiene_tiempo_aire` (flag 0/1) — el sistema sabe que billeteros estan autorizados para TAE
- `billeteros.comision_tiempo_aire` — comision especifica para TAE
- `billeteros.limite_venta_diario` — limite de venta diario TAE
- `billeteros.limite_credito` / `saldo_actual` / `getSaldoDisponible()` — modelo de credito
- `ventas_electronicas` con campo `destino` (numero telefonico) — disenado para registrar recargas
- `ventas_electronicas.tipo_venta = 'TAE'` — tipo especifico para Tiempo Aire
- La app mobile ya tiene 34 proveedores con montos — la UI esta lista, falta el backend
- El usuario billetero tiene flujo: Proveedor → Monto → Numero → Confirmar → Finalizar

Todo indica que la infraestructura de datos esta preparada para recibir recargas, pero la integracion con un proveedor/agregador externo nunca se construyo.

---

## 5. QUE SE PUEDE REUTILIZAR

### Reutilizable directamente (estructura de datos)

| Componente | Ubicacion |
|-----------|-----------|
| Tabla ventas_electronicas | migrations/m260615_000002 |
| Modelo VentasElectronicas | models/VentasElectronicas.php |
| Tabla billeteros (campos TAE) | migrations/m260703_000001 |
| Modelo Billeteros + getSaldoDisponible() | models/Billeteros.php |
| Tabla liquidaciones_billeteros | migrations/m260615_000001 |
| BaseApiController (auth, CORS, bitacora) | controllers/api/BaseApiController.php |
| Sistema de permisos | requirePermiso() |
| Formato de folios | VE-{id}-{anio}{mes}-{seq} |
| Calculo financiero | comision, ISR, total |

### NO reutilizable (no existe)

| Componente | Estado |
|-----------|--------|
| Integracion con carrier/agregador | INEXISTENTE |
| Endpoint POST de recarga | INEXISTENTE |
| Catalogo de proveedores en BD | INEXISTENTE |
| Catalogo de montos en BD | INEXISTENTE |
| Cliente HTTP para API externa | INEXISTENTE |
| Manejo de respuesta de recarga | INEXISTENTE |
| Actualizacion automatica de saldo | INEXISTENTE |
| Relacion user-billetero | NO ENCONTRADA |

---

## 6. INFORMACION QUE FALTA CONOCER

1. **¿Con que agregador/carrier se realizan las recargas reales hoy?**
   - El sistema legado de Sr. Billetero tenia alguna integracion?
   - ¿Se usaba un agregador (ej: RecargaFácil, Prepaid Solutions) o integracion directa con carriers?
   - ¿Quien tiene esta informacion?

2. **¿La recarga debe ser en tiempo real o puede ser diferida?**
   - ¿El billetero necesita confirmacion inmediata de exito/fallo?
   - ¿O basta con registrar la intencion de recarga y procesar despues?

3. **¿Quien provee el saldo para las recargas?**
   - ¿MiCachito tiene una cuenta maestra con un agregador?
   - ¿O cada billetero tiene su propia cuenta?
   - ¿El credito del billetero es interno (MiCachito) o externo (carrier)?

4. **Relacion usuario-billetero**
   - ¿Como vincular al usuario autenticado (tabla users) con su id_billetero?
   - ¿Hay un campo id_billetero en users? ¿O una tabla puente?

5. **¿Existen credenciales/API keys de algun proveedor?**
   - En el servidor de produccion (no en el codigo)
   - En configuracion del sistema legado
   - En posesion del equipo de negocio

6. **Comisiones variables por carrier**
   - ¿La comision varia segun el carrier (Telcel vs AT&T vs Movistar)?
   - ¿O es uniforme para todos los carriers?

---

## 7. SIGUIENTE PASO RECOMENDADO

Antes de desarrollar cualquier integracion, necesitamos una **decision de negocio**:

### Opcion A: Integracion con agregador externo
- Investigar y seleccionar un agregador de recargas (ej: OpenPay, RecargaFácil, o similar)
- Obtener credenciales y documentacion de API
- Desarrollar cliente HTTP en el backend
- Crear endpoint POST api/tiempo-aire/recarga que:
  1. Valide saldo del billetero
  2. Llame al API del agregador
  3. Registre el resultado en ventas_electronicas
  4. Actualice saldo_actual

### Opcion B: Registro interno sin integracion externa
- Crear endpoint POST que solo registre la recarga en ventas_electronicas
- La recarga real se realiza por otro canal
- MiCachito.Mobile sirve como portal de captura
- Mas simple pero no ejecuta la recarga

### Opcion C: Modelo hibrido
- Registrar la intencion de recarga (pendiente)
- Un proceso batch posterior procesa las recargas con el agregador
- Actualiza el estatus cuando se confirma

**Recomendacion:** Confirmar con el equipo de negocio cual es el modelo deseado y si existe un agregador ya contratado, antes de comenzar el desarrollo.

---

## 8. RESUMEN EJECUTIVO

NO EXISTE integracion real con proveedores de Tiempo Aire en MiCachito. El backend solo registra transacciones historicas de venta digital (TAE) para reportes de Contabilidad Corporativo. No hay librerias HTTP, no hay URLs externas, no hay credenciales, no hay agregadores, no hay endpoints POST para crear recargas.

La estructura de datos (ventas_electronicas, billeteros con campos TAE) esta preparada para recibir recargas, pero la integracion con un proveedor/agregador externo nunca se construyo.

Antes de implementar, se necesita definir: que agregador/carrier utilizar, si la recarga es en tiempo real o diferida, y como se vincula el usuario autenticado con el billetero.
