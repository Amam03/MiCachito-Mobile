# AUDITORIA BACKEND — TIEMPO AIRE PARA MiCachito.Mobile

Fecha: 2026-08-18
Objetivo: Entender como funciona Tiempo Aire en el backend actual (Sr. Billetero) y que se puede reutilizar para Mobile.

---

## 1. SISTEMA ACTUAL — QUE EXISTE

### 1.1 Tabla: ventas_electronicas

Es el CORAZON del sistema de Tiempo Aire. Registra cada transaccion de venta digital (TAE / SB LN).

**Schema completo** (migracion m260615_000002):

```
ventas_electronicas
├── id_venta            PK auto_increment
├── id_billetero        FK → billeteros.id_billetero (NOT NULL)
├── id_liquidacion      FK → liquidaciones_billeteros (NULLABLE — sin liquidar)
├── fecha               DATE NOT NULL
├── hora                TIME NULL
├── tipo_venta          ENUM('TAE', 'SB LN') NOT NULL
├── producto            VARCHAR(100) NOT NULL   (ej: "TELCEL_PA", "AT&T", "MOVISTAR")
├── destino             VARCHAR(100) NULL        (numero telefonico del cliente)
├── folio_transaccion   VARCHAR(50) NULL         (ej: "VE-31-202606-001")
├── transaccion         VARCHAR(50) NULL         (ej: "T0031000611")
├── subtotal            DECIMAL(12,2)            (monto de la recarga)
├── comision            DECIMAL(12,2)            (comision del billetero)
├── ret_isr             DECIMAL(12,2)            (retencion ISR)
├── fondo_ahorro        DECIMAL(12,2)            (fondo de ahorro)
├── total               DECIMAL(12,2)            (subtotal - comision - isr - ahorro)
└── fecha_creacion      DATETIME (auto)
```

Indices: id_billetero, id_liquidacion, fecha, tipo_venta
FKs: CASCADE en billetero, SET NULL en liquidacion

### 1.2 Tabla: billeteros

Cada billetero tiene configuracion de Tiempo Aire:

**Columnas relevantes** (incluyendo migracion m260703_000001):

```
billeteros
├── id_billetero            PK
├── nombre_completo         VARCHAR(200)
├── limite_credito          DECIMAL    (credito autorizado, ej: $150,000)
├── saldo_actual            DECIMAL    (saldo utilizado)
├── comision_porcentaje     DECIMAL    (comision general, ej: 12%)
├── estatus                 VARCHAR    ('activo'|'inactivo')
├── tiene_tiempo_aire       TINYINT(1) (0|1 — habilitado para TAE)
├── tiene_prod_digitales    TINYINT(1) (0|1 — habilitado para prod digitales)
├── comision_tiempo_aire    DECIMAL(5,2) (comision especifica para TAE)
├── limite_venta_diario     DECIMAL(12,2) (limite de venta diario TAE)
├── retencion_isr           DECIMAL(5,2) (% retencion ISR)
├── fondo_ahorro            DECIMAL(5,2) (% fondo de ahorro)
└── tipo_config             VARCHAR(20) ('NORMAL'|...)
```

**Saldo disponible calculado** (Billeteros.php linea 132):
```php
public function getSaldoDisponible()
{
    return ($this->limite_credito ?? 0) - ($this->saldo_actual ?? 0);
}
```

### 1.3 Tabla: liquidaciones_billeteros

Agrupa ventas electronicas en cortes mensuales para cobranza:

```
liquidaciones_billeteros
├── id_liquidacion      PK
├── folio_liquidacion   VARCHAR(20) UNIQUE (ej: "LIQ-31-202604")
├── id_billetero        FK → billeteros
├── fecha_liquidacion   DATE
├── fecha_corte         DATE
├── subtotal            DECIMAL(12,2)  (suma de subtotales)
├── comision            DECIMAL(12,2)  (suma de comisiones)
├── ret_isr             DECIMAL(12,2)  (suma de retenciones ISR)
├── fondo_ahorro        DECIMAL(12,2)  (suma de fondos de ahorro)
├── total               DECIMAL(12,2)  (suma de totales)
├── estatus             ENUM('pendiente','pagada')
└── observaciones       TEXT
```

### 1.4 Tabla: cartera_billeteros

Control de saldos pendientes por billetero:

```
cartera_billeteros
├── id_cartera          PK
├── id_billetero        FK
├── id_sorteo           FK NULLABLE
├── folio_documento     VARCHAR(50)
├── tipo_documento      ENUM('entrega','venta','consignacion','devolucion','otros_cargos','gastos_notariales')
├── fecha_documento     DATE
├── monto_original      DECIMAL
├── saldo_pendiente     DECIMAL
├── monto_pagado        DECIMAL
├── estatus             ENUM('vigente','vencido','pagado','parcial')
```

### 1.5 Tabla: fichas_pago

Pagos aplicados a venta electronica:

```
fichas_pago
├── folio_ficha         VARCHAR
├── id_billetero        FK NULLABLE
├── tipo_pago           VARCHAR ('transferencia', etc.)
├── monto               DECIMAL
├── fecha_pago          DATE
├── aplicado_a          VARCHAR ('venta_electronica', etc.)
├── estatus             VARCHAR ('aplicado', etc.)
```

---

## 2. ENDPOINTS EXISTENTES

### 2.1 Ventas Electronicas (solo lectura/reportes)

**Controlador**: VentasElectronicasController.php
**Rutas** (config/web.php lineas 470-480):

| Metodo | Ruta | Accion | Descripcion |
|--------|------|--------|-------------|
| GET | api/ventas-electronicas/resumen | actionResumen | Resumen mensual agrupado por mes/tipo/zona/producto |
| GET | api/ventas-electronicas/liquidaciones | actionLiquidaciones | Lista de liquidaciones (cabeceras) |
| GET | api/ventas-electronicas/liquidaciones/<id>/conceptos | actionLiquidacionConceptos | Desglose por tipo de venta |
| GET | api/ventas-electronicas/liquidaciones/<id>/transacciones | actionLiquidacionTransacciones | Detalle de transacciones |
| GET | api/ventas-electronicas/liquidaciones/<id>/resumen | actionLiquidacionResumen | Totales y monto en letra |
| GET | api/ventas-electronicas/monitor-saldo | actionMonitorSaldo | Garantia/saldo/disponible por billetero |
| GET | api/ventas-electronicas/estado-cuenta/<id_billetero> | actionEstadoCuenta | Estado de cuenta completo |

**PERMISO requerido**: `caja.cobros` en todos los endpoints

### 2.2 NO EXISTEN endpoints para:

- POST/crear una recarga de Tiempo Aire
- GET catalogo de proveedores de TAE (Telcel, AT&T, etc.)
- GET montos disponibles por proveedor
- GET saldo disponible del billetero autenticado
- GET historial de recargas del billetero
- POST confirmar/procesar una recarga

---

## 3. CATALOGO DE PROVEEDORES Y MONTOS

### 3.1 No existe tabla de catalogo

NO hay una tabla `proveedores_tiempo_aire` ni `montos_tiempo_aire` en la BD.

Los productos TAE estan **hardcodeados en el seed** (m260615_000004):
```php
private $productosTAE = ['TELCEL_PA', 'AT&T', 'MOVISTAR', 'UNEFON'];
private $montosTAE = [50, 100, 200, 500];
```

Esto es solo para datos de prueba. En produccion, el campo `producto` en `ventas_electronicas` acepta cualquier string (VARCHAR 100).

### 3.2 Tabla proveedores existente

Existe `proveedores` pero es para proveedores de mercancia (loteria, instantanea, servicios):
```
tipo_proveedor: ENUM('loteria_nacional','instantanea','servicios','otros')
```
No tiene tipos para carriers de Tiempo Aire.

### 3.3 En MiCachito.Mobile

La app ya tiene los 34 proveedores hardcodeados en `Data/TiempoAireData.cs` con sus montos. Esto esta bien para UI pero eventualmente necesitara un catalogo en BD.

---

## 4. FLUJO ACTUAL DE TIEMPO AIRE (SR. BILLETERO)

### 4.1 Como funciona hoy

El sistema actual NO realiza recargas en tiempo real. El flujo es:

1. El billetero realiza recargas en su sistema externo (Sr. Billetero app propia)
2. Las transacciones se registran en `ventas_electronicas` como registro historico
3. Periodicamente Contabilidad Corporativo genera liquidaciones mensuales
4. Las liquidaciones agrupan ventas electronicas para cobranza
5. Los pagos se registran via `fichas_pago` con `aplicado_a = 'venta_electronica'`

### 4.2 Calculo financiero por transaccion (del seed)

```php
$comision    = $subtotal * $comisionPct / 100;   // ej: 12% del monto
$retIsr      = $subtotal * 0.02;                 // 2% fijo
$fondoAhorro = 0.0;                              // generalmente 0
$total       = $subtotal - $comision - $retIsr - $fondoAhorro;
```

### 4.3 Monitor de saldo (actionMonitorSaldo)

Calcula saldo pendiente = SUM(liquidaciones_billeteros.total WHERE estatus='pendiente')

Muestra por billetero:
- garantia (limite_credito)
- saldo (liquidaciones pendientes)
- disponible = garantia - saldo (implicito)

---

## 5. MODELO DE CREDITO DEL BILLETERO

### 5.1 Estructura

```
limite_credito  = $150,000    (credito autorizado total)
saldo_actual    = $80,000     (lo que ya ha utilizado)
saldo_disponible = $70,000   (limite_credito - saldo_actual)
```

El campo `saldo_actual` se calcula en algun punto pero NO existe logica visible en el codigo backend que lo actualice automaticamente al registrar una venta electronica. Parece ser actualizado manualmente o por un proceso externo.

### 5.2 Configuracion por billetero

- `tiene_tiempo_aire`: flag 0/1 — el billetero esta autorizado para vender TAE
- `comision_tiempo_aire`: comision especifica para TAE (puede diferir de comision_porcentaje general)
- `limite_venta_diario`: limite de venta diario para TAE

---

## 6. BRECHAS PARA MiCachito.Mobile

### 6.1 Endpoints que FALTAN (no existen)

Para que la app mobile pueda completar el flujo de recarga:

1. **GET api/tiempo-aire/proveedores**
   - Retorna catalogo de proveedores de TAE con sus montos
   - Fuente: tabla nueva o datos estaticos en BD
   - Requiere: auth del billetero

2. **GET api/tiempo-aire/saldo**
   - Retorna saldo disponible del billetero autenticado
   - Usa: billeteros.limite_credito - billeteros.saldo_actual
   - Validacion: tiene_tiempo_aire = 1

3. **POST api/tiempo-aire/recarga**
   - Registra una nueva recarga
   - Body: { id_proveedor, producto, monto, numero_telefono }
   - Acciones:
     - Validar saldo disponible >= monto
     - Validar limite_venta_diario
     - Validar numero telefonico (10 digitos)
     - Insertar en ventas_electronicas
     - Actualizar billeteros.saldo_actual
     - Generar folio_transaccion
     - Registrar en bitacora
   - Response: { id_venta, folio_transaccion, fecha, hora, monto, numero, proveedor }

4. **GET api/tiempo-aire/historial**
   - Retorna recargas recientes del billetero
   - Filtro por fecha (opcional)

5. **GET api/tiempo-aire/comprobante/<id_venta>**
   - Retorna datos para el comprobante en pantalla
   - Datos del billetero, proveedor, monto, numero, fecha, folio

### 6.2 Tablas que FALTAN

1. **proveedores_tiempo_aire** (catalogo de carriers)
   ```
   id_proveedor_tae     PK
   nombre               VARCHAR (Telcel, AT&T, etc.)
   subproducto          VARCHAR NULL (Recarga, Paquete, Internet)
   color_hex            VARCHAR(7) (#254AA6)
   logo                 VARCHAR NULL
   orden                INT
   estatus              VARCHAR ('activo'|'inactivo')
   ```

2. **montos_tiempo_aire** (montos por proveedor)
   ```
   id_monto_tae         PK
   id_proveedor_tae     FK
   monto                DECIMAL(12,2)
   orden                INT
   estatus              VARCHAR
   ```

### 6.3 Relacion usuario-billetero

El backend tiene `users` (tabla de autenticacion) y `billeteros` (tabla de negocio). NO hay una relacion explicita entre ellos en el codigo revisado. Para Mobile, el billetero se autentica con user/password y necesitamos saber a que id_billetero corresponde.

Posibles enfoques:
- Agregar `id_billetero` a la tabla `users`
- O una tabla puente `usuarios_billeteros`
- O que el login retorne el id_billetero asociado

---

## 7. QUE SE PUEDE REUTILIZAR PARA MOBILE

### 7.1 Reutilizable directamente

| Componente | Estado | Notas |
|-----------|--------|-------|
| Tabla `ventas_electronicas` | REUTILIZABLE | Schema exacto para registrar recargas TAE |
| Modelo `VentasElectronicas` | REUTILIZABLE | ActiveRecord con validaciones |
| Tabla `billeteros` (campos TAE) | REUTILIZABLE | tiene_tiempo_aire, comision_tiempo_aire, limite_venta_diario |
| Modelo `Billeteros.getSaldoDisponible()` | REUTILIZABLE | Calculo de saldo disponible |
| Tabla `liquidaciones_billeteros` | REUTILIZABLE | Para futuras liquidaciones de recargas mobile |
| Tabla `fichas_pago` (aplicado_a='venta_electronica') | REUTILIZABLE | Para pagos de recargas |
| `BaseApiController` | REUTILIZABLE | Auth, CORS, permisos, bitacora, response format |
| Calculo financiero (comision, ISR, total) | REUTILIZABLE | Logica del seed replicable en endpoint |
| Formato de folio (VE-{id}-{anio}{mes}-{seq}) | REUTILIZABLE | Convencion ya establecida |

### 7.2 Reutilizable con adaptacion

| Componente | Cambio necesario |
|-----------|-----------------|
| `VentasElectronicasController` | Agregar acciones POST (crear recarga), GET proveedores, GET saldo, GET historial |
| `config/web.php` | Agregar rutas nuevas para endpoints mobile |
| `Billeteros.saldo_actual` | Implementar actualizacion automatica al registrar recarga (transaccional) |
| Permisos | Nuevo permiso `tiempo_aire.recargar` o reutilizar `caja.cobros` |

### 7.3 Nuevo (no existe)

| Componente | Descripcion |
|-----------|-------------|
| Tabla `proveedores_tiempo_aire` | Catalogo de carriers de TAE |
| Tabla `montos_tiempo_aire` | Montos disponibles por carrier |
| Endpoint POST recarga | Crear transaccion + validar saldo + actualizar saldo |
| Endpoint GET proveedores | Listar carriers y montos |
| Endpoint GET saldo | Saldo disponible del billetero |
| Relacion user-billetero | Vincular autenticacion con billetero |
| Endpoint GET comprobante | Datos para resumen en pantalla |

---

## 8. FLUJO PROPUESTO PARA MOBILE (conceptual)

```
App Mobile                    Backend Yii2
─────────                    ────────────
1. Login ──────────────────→ AuthController (existente)
   ←── token + id_billetero

2. GET proveedores ────────→ NUEVO: api/tiempo-aire/proveedores
   ←── lista de 34 + montos

3. GET saldo ──────────────→ NUEVO: api/tiempo-aire/saldo
   ←── { limite, utilizado, disponible }

4. POST recarga ───────────→ NUEVO: api/tiempo-aire/recarga
   { proveedor, monto,     • Validar saldo >= monto
     numero_telefono }     • Validar limite diario
                           • INSERT ventas_electronicas
                           • UPDATE billeteros.saldo_actual
   ←── { id_venta, folio,  • Bitacora
         fecha, total }    • Response

5. GET comprobante ────────→ NUEVO: api/tiempo-aire/comprobante/<id>
   ←── datos para resumen
```

---

## 9. RIESGOS Y CONSIDERACIONES

1. **No hay integracion con carrier real**: El sistema actual solo registra transacciones, no las ejecuta. Para recargas reales necesitariamos integrar con un proveedor (ej: OpenPay, Conekta, o API directa del carrier).

2. **saldo_actual no se actualiza automaticamente**: No hay logica visible que actualice este campo al insertar una venta. Necesitamos implementarlo en el nuevo endpoint.

3. **Sin concurrencia**: Si dos dispositivos del mismo billetero hacen recargas simultaneas, podrian exceder el limite. Se necesita bloqueo optimista o transaccional con row lock.

4. **Catalogo de proveedores**: Los 34 proveedores estan hardcodeados en la app. Si cambia un monto o se agrega un carrier, requiere actualizacion de la app. Mejor tenerlo en BD y servirlo via API.

5. **Relacion user-billetero**: Pendiente definir. Sin esto, la app no sabe cual billetero hace la recarga.

6. **Comision variable por billetero**: `comision_tiempo_aire` puede diferir de `comision_porcentaje`. El calculo financiero debe usar el correcto.

---

## 10. RESUMEN EJECUTIVO

El backend tiene la estructura de datos necesaria para registrar recargas de Tiempo Aire (tabla `ventas_electronicas`) y el modelo de credito del billetero (tabla `billeteros` con limite_credito, saldo_actual, tiene_tiempo_aire, comision_tiempo_aire, limite_venta_diario). Tambien tiene el sistema de liquidaciones y cobranza asociado.

FALTAN:
- Endpoints para crear recargas (POST), consultar proveedores, saldo, historial y comprobante
- Tabla de catalogo de proveedores y montos
- Relacion user-billetero para autenticacion
- Logica de actualizacion de saldo_actual al recargar
- Validaciones de saldo, limite diario y numero telefonico
- Posible integracion con carrier real para ejecutar la recarga

REUTILIZABLE:
- 100% del schema de ventas_electronicas, billeteros, liquidaciones_billeteros
- BaseApiController, permisos, bitacora, CORS
- Calculo financiero (comision, ISR, total)
- Formato de folios y convenciones
