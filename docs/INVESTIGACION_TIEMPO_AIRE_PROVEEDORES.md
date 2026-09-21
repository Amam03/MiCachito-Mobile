# Investigación: Tiempo Aire en Mi Cachito Mobile — Proveedores de recargas (TAECEL, Seycel/Sivetel, Red Nacional de Pagos)

Fecha de corte: 21 de septiembre de 2026 (CST). Elaborado para preparar una propuesta de integración.
Arquitectura evaluada: **Mi Cachito Mobile → Backend Mi Cachito (Yii2) → API proveedor → Operador**. Credenciales de proveedor SOLO en backend.

Criterios de clasificación usados en todo el documento:
- **CONFIRMADO (fuente oficial/API)**: operador y producto/monto publicados por el proveedor u operador en fuente oficial.
- **OPERADOR confirmado**: la marca aparece en catálogo/lista oficial del proveedor, pero el sub-producto o los montos NO están publicados (requieren login o cotización).
- **REQUIERE CONFIRMACIÓN**: mapeo plausible pero sin evidencia pública directa.
- **NO ENCONTRADO**: sin evidencia en fuentes oficiales del proveedor.
- Todo lo no publicado se marca: **"No confirmado públicamente — solicitar al proveedor"**.

---

## 0. Estado actual de Mi Cachito (fase 1 — revisión de app y backend)

**App móvil (MiCachito-Mobile, .NET MAUI, MVVM):**
- Flujo UI-only implementado desde agosto: `TiempoAirePage → MontosTiempoAirePage → NumeroTelefonoTiempoAirePage` (selección de proveedor → monto → número → botón Finalizar con TODO, sin recarga real).
- 34 proveedores hardcodeados en `Data/TiempoAireData.cs` con montos fijos; modelos en `Models/Entities/ProveedorTiempoAire.cs`; ViewModels `MontosTiempoAireViewModel` y `NumeroTelefonoTiempoAireViewModel`; endpoints en `Api/ApiEndpoints.cs` (sin endpoint TAE real).
- Documentación existente: `docs/TIEMPO_AIRE_INTEGRACION.md`, `docs/TIEMPO_AIRE_BACKEND.md`, `docs/backend-integracion.md` (esboza `POST /api/mobile/tae` y `GET /api/mobile/tae/historial` sobre la tabla `ventas_electronicas`), `docs/Arquitectura.md`.

**Backend (MiCachito-backend, Yii2):**
- **CERO integración con proveedor**: ninguna llamada HTTP externa, ningún endpoint POST de recarga. Existe solo registro histórico (`ventas_electronicas`), flags en `billeteros` (`tiene_tiempo_aire`, `comision_tiempo_aire`, `limite_venta_diario`) y GETs de reportes.
- Conclusión: la integración completa (proveedor + endpoints + flujo real) está por diseñar; la app ya define el UX y el catálogo esperado.

**Discrepancias detectadas catálogo app vs spec (verificadas con mockups OCR y fuentes oficiales):**
1. **Movistar $0 = error de transcripción**: el mockup original dice **$40** (confirmado por OCR ampliado del mockup). No existe "recarga de $0" en Movistar.
2. **"Virquin Mobile" = Virgin Mobile** (el mockup dice "Virqgin mobile").
3. **"DIRY Móvil" = Diri Móvil** — Diri Telecomunicaciones S.A. de C.V. (Monterrey, desde 1994), operador que da servicio a PilloFon y Space Móvil.
4. **redi Coppel**: presente en la app (34º proveedor, montos 60–350) pero AUSENTE del spec (33). MVNO de Grupo Coppel (redicoppel.com, servicios prestados por Oriónidas S.A. de C.V. sobre red Altán). Decidir si se agrega al alcance.
5. **CFE TEIT / Internet para el Bienestar**: la app los lista como 2 proveedores; ambos son productos de la OMV de la CFE (CFE TEIT). Posible consolidación.
6. **Telcel X Tiempo (10, 20, 25)**: corresponde al producto real **"Internet por Tiempo"** de Telcel (horas de navegación: ~$10/1 h, $20/2 h, $25/4 h). Montos coinciden con el catálogo oficial del operador (no del proveedor TAE).

**Chip Macropay (investigación especial):**
- Es el **MVNO de la cadena de crédito Macropay de Mérida** (chipmacropay.mx), lanzado en dic-2024. Montos oficiales de recarga publicados por el operador: **$50, $70, $100, $120, $130, $200, $250, $500** (+ planes anuales $570/$1,045/$2,090) e **Internet Portátil $110/$210/$410**. El catálogo de la app (50, 70, 100, 130, 200, 250, 500) coincide con los planes oficiales del operador.
- Disponibilidad por API de proveedores: **Seycel** lo lista como carrier "Macropay" en su catálogo oficial; **RNP** publica logo "Macropay" entre sus compañías; **TAECEL** no lo lista en ninguna página oficial revisada (NO ENCONTRADO → preguntar).

---

## 1. Resumen ejecutivo

- **Ninguno de los tres proveedores publica públicamente la matriz de montos por operador.** Los tres exigen credenciales/login para ver el catálogo real de product+SKU+monto (TAECEL: `getProducts`; Seycel/Sivetel: documentación por correo; RNP/Axios: `GET /v1/products` con token). Por tanto, **0 de los ~350 montos del catálogo Mi Cachito quedan "confirmados por API" en esta fase** — todos requieren credenciales o respuesta del proveedor. Esto es lo primero a cerrar con cada uno.
- **Cobertura de operadores (marcas) en catálogos públicos**: Seycel publica el catálogo más amplio y visible (62 productos de recarga en su sitio, cubre las 33 marcas del spec incluida Macropay y Valor Telecom); TAECEL lista ~36 carriers (30/31 del spec; le falta Macropay); RNP publica ~49 logos (25/33 del spec; sin Cierto, CompartFON, Netwey, Yobi ni Valor Telecom visibles).
- **Documentación técnica**: TAECEL y Seycel/Sivetel NO publican su manual de API (se entrega tras registro/levantamiento). RNP declara API REST con OpenAPI 3.0, webhooks HMAC-SHA256, SDKs y rate limits, pero los links de "Documentación API" de su sitio no llevan a ninguna parte; la documentación técnica real encontrada es la del **portal público docs.axiosmobile.mx** (Axios Mobile, la plataforma técnica detrás de RNP), con endpoints, auth JWT/API-Key, polling de estatus y catálogo documentados — es la documentación pública más completa de las tres, pero está centrada en los productos Axios y **no confirma que un distribuidor RNP obtenga acceso API al catálogo multimarca completo**.
- **Costos publicados**: RNP es el único con esquema publicado y detallado (6%–7.5% saldo bonificado, $0 mensualidad, fee $6 en servicios, 1% pines, tope 7.5% por pago único de $300). Seycel publica 6% (con un ejemplo de $1,000→$1,065 = 6.5% en la misma página; a aclarar) y distribuidor con 5% a afiliados. TAECEL: comisión **por cotización** ("Pregúntanos por el porcentaje"); referencias no vinculantes de revendedor (5%) y LinkedIn (6%).
- **Ejemplo $100** (único modelo publicado, saldo bonificado): con 6% el costo real es $94.34 (margen $5.66); con 7.5% es $93.02 (margen $6.98). Ver sección 7.
- **Advertencias de identidad**: "Seycel" y "Sivetel" son **dos empresas distintas** (Seycel Comunicaciones, CDMX/svxcel.com; Sivetel, Querétaro) con ofertas casi idénticas — el spec las junta; se cubren ambas. RNP no publica su razón social; sus citas de prensa no fueron verificables en búsquedas y Wayback no muestra snapshots 2022–2023; su app no aparece en Google Play bajo ese nombre (las apps del ecosistema son de Axios Mobile, publicadas por tae.capital). La plataforma técnica es Axios Mobile/Axios Comunicaciones S. de R.L. de C.V. → **validación comercial y legal obligatoria antes de contratar**.
- **No hay un "ganador"** — ver matriz y comparación: Seycel gana en amplitud de catálogo publicado, RNP en transparencia de costos y documentación pública de la plataforma, TAECEL en trayectoria/integraciones POS conocidas (eleventa, TheFactory, DisPRO, Odoo) y claridad del proceso de integración.

---

## 2. TAECEL — características, pros y contras

**Identidad** (confirmado): TAECEL — Comercio Móvil S. de R.L. de C.V., Querétaro, México, operando desde ~2010. Sitio oficial: taecel.com / portal.taecel.com / app.taecel.com. ~36 compañías de TAE, 80–100+ servicios, pines/gift cards y timbres CFDI. Modelo 100% prepago con **bolsas de saldo separadas** (Tiempo Aire / Pago de Servicios / Timbres CFDI).

**API** (parcialmente confirmado — ver advertencias):
- **REST** (requisito oficial: "programador con experiencia en API REST"). Documentación técnica **NO pública**: se envía por correo tras llenar el "Levantamiento Tecnológico" (PDF oficial) y superar una validación de pruebas. Correos citados: integraciones@taecel.com (levantamiento 2024) y cc@taecel.com (brochure) — confirmar canal vigente.
- Base URL `https://taecel.com/app/api/` inferida de la librería comunitaria [itrendsmx/taecel](https://github.com/itrendsmx/taecel) (Laravel, jul-2022, sin mantenimiento) — **no publicada oficialmente**.
- Endpoints (evidencia de la librería e integradores, NO del manual oficial): `getBalance` (bolsas/saldo), `getProducts` (catálogo carriers + productos con `Monto, Codigo, Categoria, Vigencia`; categorías 1=TAE, 2=Paquetes, 3=Servicios, 4=Gift Cards), `RequestTXN` (recarga: `producto` (código ej. TEL150), `referencia` (10 dígitos), `monto` → `transID`), `StatusTXN` (estatus por transID: Folio, Status, Monto, Nota). Todos POST form-urlencoded; respuesta JSON `{success, error, message, data}`. El módulo Odoo XUBAX sugiere endpoints adicionales de reporte por afiliado no documentados.
- **Autenticación**: par `key`+`nip` en cada petición. Sin evidencia pública de firma HMAC, OAuth, rotación o IP whitelist. HTTPS. 2FA existe en el portal web, no documentado para el API.
- **Sandbox**: SÍ existe, con credenciales de prueba por correo y "matriz de pruebas" (referencia de prueba 5555555520, código TEL150). URL del sandbox no publicada.
- **Folios**: folio de autorización inmediato en recarga exitosa (impreso en ticket).
- **Errores**: campos `success/error/message`; catálogo de códigos NO publicado (viene en el manual privado).
- **Idempotencia**: NO documentada. Práctica de integradores: despachar una sola vez, sondear StatusTXN, NO reintentar automáticamente tras timeout (la recarga "en progreso" se cobra).
- **Reversos**: NO existen. "Una recarga exitosa jamás se revierte y lo que quede en progreso se te cobra" (política citada por integrador Odoo); "NO REALIZA DEVOLUCIONES" (página oficial de cuentas bancarias).
- **Webhooks**: NO documentados; solo polling (StatusTXN).
- **SDKs**: sin SDK oficial público; librería comunitaria Laravel (itrendsmx/taecel) + integraciones POS comerciales (eleventa, TheFactory, DisPRO/CWin, Odoo/XUBAX). Nada oficial para .NET.
- **SLA/límites**: no publicados. 24/7 declarado.

**Costos (publicado / por cotización)**:
- Alta e inscripción: **gratis**, sin mensualidades, sin plazos forzosos (publicado).
- Depósito mínimo TAE: **$50 MXN**; servicios/pines: **$1,000 MXN** (publicado).
- Fondeo por depósito/transferencia con referencias (88…=TAE, 99…=servicios); aplicación L-V 8–21 h, S-D 9–15 h, ≤30 min tras reporte (publicado).
- **Comisión TAE: por cotización** — "Pregúntanos por el porcentaje de comisión" (brochure oficial). Referencias NO vinculantes: 5.0% aplicado por el revendedor DisPRO a sus afiliados; post de LinkedIn de Taecel promueve "6%". **No confirmado públicamente — solicitar al proveedor.**
- Cargo por venta interna (pago de servicios): ~$5.00 MXN por operación (publicado, para servicios, no TAE).

**Pros**: proveedor establecido y foco en integración B2B (cadenas/mayoristas); proceso de integración formal y claro (levantamiento → pruebas → producción); sandbox y matriz de pruebas; catálogo multimarca amplio (~36 carriers, 30/31 del spec); folio de autorización inmediato; esquema mayorista con panel "MI RED"; muchas integraciones POS de referencia.
**Contras**: documentación técnica NO pública (diseño a ciegas hasta recibir el manual); sin webhooks (solo polling); sin reversos y "en progreso se cobra" (riesgo operativo con timeouts móviles); auth simple key+nip sin firma; comisión no publicada; catálogo de montos solo con credenciales; librería de referencia comunitaria desactualizada (2022).

---

## 3. Seycel / Sivetel — características, pros y contras

**Aclaración importante (confirmado)**: son **dos empresas distintas** con oferta equivalente; el spec las agrupa:
- **Seycel Comunicaciones S.A. de C.V.** — empresa 100% mexicana creada en 2012, ~60,000 clientes activos, 20,000+ puntos de venta, "80% personal femenino". Sitios: seycel.com.mx y svxcel.com (misma plataforma). App Android `com.seycelmx.multirec` (10,000+ descargas). Contactos: ventas@seycel.com.mx, soporte@seycel.mx, tel. 5547449333.
- **Sivetel** ("Sistema de Venta de Transacciones Electrónicas") — Querétaro (Paseo de los Enamorados 155-6, Col. Balaustradas, CP 76079). Sitio: sivetel.com, portal app.sivetel.com. App iOS id1480924619. Contacto: contacto@sivetel.com, WhatsApp 4421223214.

**Catálogo publicado (Seycel)** — la fuente pública más amplia de las tres: 62 productos de recarga listados en seycel.mx/Vende-Recargas con logo propio: ABIB, Altcel, **Amigo Sin Límite (Telcel paquetes)**, **AT&T**, Axios, **Bait**, Beneleit, **Bienestar**, Bigcel, **CFE Internet**, **Cierto**, Compartcarga, **Compartfon**, Contigo, Cool Mobile, **Datos Telcel**, **Diri Móvil**, **Flash Mobile**, FRC, **FreedomPop**, Gugacom, **Internet Telcel**, Jesner, JR Móvil, JR Móvil MiFi, Lykephone, **Macropay**, Maifon, Más G, Mega Móvil, Metrocel, Mexfon, **Mi Móvil**, Mobig, **Movistar**, MR, Nemi, **Netwey**, Newww, **OUI**, **Pillofon**, **Redi Coppel**, **Rincel**, Simpati, **Soriana Móvil**, Spot Mobile, **Telcel**, Telmovil, Tricomx, Tuenti, Turbocel, Turbored, **Ultracel**, Unefon, **Valor Telecom**, **Virgin Mobile**, **Weex**, Wik, **Wimo**, **Yobi**, + TAGs PASE/Televía. Cubre las **33 marcas del spec de MiCachito** (incluidas Macropay y Valor Telecom). Sivetel muestra en su home los carriers principales (Telcel, Movistar, Virgin, Flash, paquetes/internet Telcel, Pillofon, Diri, Wimo, Bait, BigCel, Mi Móvil, Compartcarga, Bienestar…).
- **Montos**: NO publicados. Solo "recargas a cualquier compañía desde 10 pesos" (seycel.com.mx). Sub-productos del spec (Bait Internet en Casa, Bait Paquetes, Soriana Móvil Paquetes, Valor Casa/Paquetes, Telcel X Tiempo) no distinguibles en el catálogo público — solicitar matriz.

**API / Web Service (parcialmente confirmado)**:
- **SOAP o REST** (Sivetel, publicado: "la solicitud de información es por medio del protocolo SOAP o REST"); Seycel ofrece "Conexión Web Service" para POS/sistemas. Multiplataforma declarado: apps nativas/React Native/Flutter, POS, e-commerce (WooCommerce/Shopify/Magento), mayoristas. Lenguajes mostrados: JavaScript/Node, PHP, Python, C#, Java, Ruby.
- **Documentación técnica NO pública**: Seycel exige 3 pasos (solicitar integración con formulario por correo a soporte@seycel.mx → validación de compatibilidad → integración con documentación). Sivetel: "Obtenga la documentación necesaria" previo contacto. **No confirmado públicamente — solicitar al proveedor** (endpoints, WSDL, sandbox, auth, folios, errores, idempotencia, reversos, webhooks, límites: todo no publicado).
- Ejemplo visible en la página de Sivetel: `POST {BASEAPI}/ApiWS/consultar` con body `{'cuenta': ...}` — insuficiente para diseñar; no es documentación.
- Un manual SOAP público hallado (manualesderecargas.com → ventatelcel.com/ws, funciones ObtenSaldo/RecargaEWS/VerificaRecarga/ProductosPDV, usuario demo demo123456) pertenece a una **red antigua ajena (recargas.red/ventatelcel)**, NO a Seycel/Sivetel — no debe usarse como referencia de estos proveedores.
- SLA declarado (Sivetel): 24/7, cobertura 99%, ~95 ms.

**Costos (Seycel, publicado; Sivetel no publicado)**:
- Seycel: alta/afiliación **sin costo**, sin mensualidad ni anualidad ("la funcionalidad del sistema es totalmente gratis"); solo pagas el importe de la recarga (y en servicios, monto + comisión).
- **Comisión TAE: 6% publicado** ("Gana el 6% en tus recargas telefónicas", "Obtén 6% de comisión… Telcel y demás compañías"). **Inconsistencia a aclarar**: la misma página ejemplifica "depositas $1,000 → te abonamos $1,065" (= 6.5%).
- Esquema distribuidor: ofreces **5%** a tus afiliados y ganas la diferencia (~$20 por cada $1,000 de depósito de afiliado según la página) + porcentaje por depósito de tus puntos de venta.
- App con marca propia (white label) para redes de usuarios: "por un excelente costo" — por cotización (comercial@seycel.mx).
- Sivetel: comisiones "abonadas al hacer cada compra o depósito" — **porcentaje no publicado — solicitar al proveedor**.

**Pros**: catálogo público más amplio (62 carriers, cubre las 33 marcas del spec, incluida Macropay); comisión publicada (6%); sin costo de alta; empresa con 14 años y 60k clientes (Seycel); web service SOAP+REST declarado y multiplataforma; experiencia en venta por SMS/WhatsApp/Telegram/web/app; TAGs (PASE/Televía) también en catálogo.
**Contras**: documentación técnica completamente no pública (ni endpoints, ni sandbox, ni ejemplos reales); sin evidencia de webhooks; dos empresas similares generan ambigüedad comercial (decidir con cuál se contrata); montos por operador no publicados; inconsistencia 6% vs 6.5% en la propia página; sin referencias públicas de integradores POS grandes.

---

## 4. Red Nacional de Pagos (RNP) — características, pros y contras

**Identidad (parcialmente confirmado — ver riesgos)**: "Red Nacional de Pagos", plataforma 100% mexicana, se declara nacida en 2022 en Tapachula, Chiapas (32 Oriente No. 14, CP 30790). Métricas declaradas: 27,000+ puntos de venta, 32 estados, 8M+ transacciones/mes, 2,400+ distribuidores, uptime 99.98% (2025). Contacto: WhatsApp 9626240829, contacto@rednacionaldepagos.com. **Razón social NO publicada** en el sitio ni en T&C; WHOIS no disponible. **La plataforma técnica es Axios Mobile** (Axios Comunicaciones / Axios Mobile S. de R.L. de C.V.; apps publicadas por tae.capital, eholguin@tae.capital): RNP vende exclusivamente chips Axios y el portal de documentación API del ecosistema es docs.axiosmobile.mx.

**API (documentación pública más completa de las tres, con matices)**:
- Portal **docs.axiosmobile.mx** (público): base `https://apidevstore.axiosmobile.mx/v1` (dev). Endpoints documentados:
  - `POST /v1/auth/:appId` → JWT.
  - **API Key alternativa** (header `x-api-key`, server-to-server, se solicita al soporte Axios) — ideal para backend. Compatibilidad: `GET /v1/products/tae` ✅, `POST /v2/transactions` (eSIM) ✅, portabilidad ✅; **`POST /v1/transactions` (recargas) SOLO JWT** ❌ API Key (a confirmar para recargas).
  - `GET /v1/products/tae` y `GET /v1/products?type=TAE|ACTIVACION` → **catálogo con SKU por carrier y lista de montos** (`carrier, carrierCode, prices[{amount, message, days, data}]`) — verificado vivo (responde 401 sin token: existe y exige credencial).
  - `POST /v1/transactions` (recarga): `carrierName, carrierCode, amount, reference` → `folio` + "Transacción en espera" (código 24).
  - `GET /v1/transactions/check/:folio` — **polling obligatorio cada 2 s por 120 s** mientras código 24; final: confirmation "00" = TRANSACCIÓN EXITOSA, folio de operadora.
  - `GET /v1/balance` → saldoInicial/compras/ventas/comisión/balance.
  - Extras: `POST /v2/transactions` (activación eSIM), portabilidad, validación IMEI.
  - Errores documentados: 401 Missing API key, 403 Invalid/inactive.
- Sitio RNP /recursos declara además: **Referencia REST en formato OpenAPI 3.0, webhooks con HMAC-SHA256 y reintentos exponenciales, SDKs Node/PHP/Python/Java/.NET "open source en GitHub", rate limits 200 req/s por cliente y 10,000 req/min**. **Advertencia**: los botones de "Documentación API" del sitio RNP NO tienen links reales (hallazgo del relevado); los repos GitHub de los SDK no fueron encontrados; el OpenAPI no está publicado. **No confirmado públicamente — solicitar al proveedor.**
- **Montos por operador**: documentado el endpoint que los devuelve (`/v1/products`), pero requiere token → **matriz de montos no confirmable sin credenciales**.
- **Sandbox**: la base documentada es apidevstore (dev); proceso de credenciales: por contacto con soporte (API Key) o registro. No publicado un flujo formal de certificación tipo TAECEL.
- **Reversos/webhooks/folios de otros carriers**: folio propio + folio de operadora en respuesta; reversos y webhooks solo como declaración del sitio RNP (no docs operativas).

**Costos (publicado — el más transparente de los tres)**:
- **Sin mensualidades, sin contratos forzosos, sin penalizaciones**; alta gratuita (registro online, activación <24 h, validación por WhatsApp).
- **Comisión TAE por saldo bonificado**: Estándar **6.0%** (activación); Intermedio **6.5–7.0%** (volumen sostenido); Tope **7.5%** (pago único de **$300 MXN**, permanente, o volumen elevado). Tabla publicada: depositas $100 → $106/$107/$107.50; $1,000 → $1,060/$1,070/$1,075; $5,000 → $5,300/$5,350/$5,375.
- Depósito mínimo: **$50** (guía oficial del distribuidor, PDF). Fondeo SPEI instantáneo / OXXO 1–2 h.
- Pago de servicios: fee plataforma **$6.00** + comisión al cliente $12–$30 (tu ganancia $6–$24). Pines/TAGs: **1%**. No hay nivel superior a 7.5% en TAE.
- Saldo: no caduca; en cancelación de cuenta, reembolso del saldo a CLABE en ≤30 días hábiles (T&C); cancelación posible por inactividad >12 meses.
- Costo del API en sí: **no publicado — solicitar al proveedor** (¿la comisión aplica igual por canal API? ¿hay costo por transacción API o por webhook?).

**Catálogo publicado (logos en /productos/recargas, ~49 marcas)**: Axios Mobile, Axios SIMS, Axios Internet en casa, Axios Internet sin cable, Sin Límite, **Telcel**, **Movistar**, Newww, JR Móvil, MoBig, FRC, **Macropay**, Cynerkia, Rapsodia, Unefon, Usacell, **AT&T**, **Virgin Mobile**, **Bait Internet**, **Bait**, UNEFON Móvil, **Internet para el Bienestar**, Internet Bienestar MiFi, **CFE Móvil**, **Oui**, Nextel, **Freedom Pop**, **Flash Mobile**, **Weex**, Dalefon, Inphonity, **Redi Coppel**, **Rin Cel**, Abib, **Pillofon**, **Diri**, **Mi Móvil**, **Soriana Móvil**, Guga Móvil, LikePhone, **Wimo Telecom**, Bigcel, Bait Hogar, **Bait Casa**, Contigo, **Ultracel** (+ Plus/Pro), Telmóvil. También: "Búsqueda automática del operador por número" y "monto fijo o personalizado según la compañía" (app).
- **Faltantes visibles vs spec**: Cierto, CompartFON, Netwey, Yobi, Valor Telecom (las 3 variantes) — NO aparecen en los logos públicos → preguntar.

**Pros**: costos 100% publicados (6–7.5%, $0 mensualidad, mínimos $50); documentación técnica pública real (docs.axiosmobile.mx: auth JWT/API-Key, catálogo con montos por SKU, polling definido, errores); API Key server-to-server (encaja con arquitectura backend); webhooks declarados con HMAC-SHA256; rate limits y uptime publicados; depósito reembolsable por T&C; chips propios Axios (producto adicional); búsqueda automática de operador por número.
**Contras**: **razón social no publicada** y señales de identidad a verificar (citas de prensa no encontradas en búsquedas, sin snapshots Wayback 2022–2023, app propia no hallada en Google Play bajo "Red Nacional de Pagos"); la documentación pública es de la plataforma Axios (marca distinta) — el acceso API de un distribuidor RNP al catálogo multimarca completo NO está confirmado; recargas solo con JWT (API Key no aplica a `POST /v1/transactions` según la tabla publicada); logos sin montos; sin SLA contractual publicado; faltan 6+ marcas del spec en el catálogo visible.

---

## 5. Matriz de cobertura del catálogo MiCachito

Leyenda: **✓O** = operador confirmado en catálogo oficial público del proveedor (montos NO publicados → requieren credenciales/cotización). **?P** = requiere confirmación del proveedor (mapeo plausible). **✗N** = no encontrado en fuentes públicas del proveedor. En los tres proveedores, **ningún monto específico está confirmado por API pública** — la matriz de montos exige credenciales (TAECEL getProducts / Seycel-Sivetel doc por correo / RNP GET v1/products con token).

| Producto MiCachito (montos) | TAECEL | Seycel/Sivetel | RNP | Notas de evidencia |
|---|---|---|---|---|
| Telcel Recarga (10–500) | ✓O | ✓O | ✓O | TAECEL: select registro + rango $10–500; Seycel: catálogo; RNP: logo |
| Telcel Paquete (10–2400) | ✓O | ✓O ("Amigo Sin Límite") | ✓O ("Sin Límite") | Categoría Paquetes existe en los tres; montos no publicados |
| Telcel Internet (10–500) | ✓O ("Telcel Internet Amigo") | ✓O ("Internet Telcel"/"Datos Telcel") | ?P (sin sub-producto visible) | RNP: solo logo Telcel |
| Telcel X Tiempo (10,20,25) | ?P ("Internet por Tiempo") | ?P | ✗N | Producto real del operador (horas); mapeo por confirmar; RNP no lo lista |
| AT&T (10–1000) | ✓O | ✓O | ✓O | TAECEL: rango publicado $10–500 → monto $1,000 a confirmar |
| Movistar (10–500; "0" = $40) | ✓O | ✓O | ✓O | $0 del spec es error (mockup dice $40) |
| Bait (30–300) | ✓O | ✓O | ✓O | |
| Bait Internet (110,210,410) | ✓O ("Paquete Bait") | ✓O | ✓O ("Bait Internet") | |
| Bait Internet en Casa (99,349) | ?P | ?P | ✓O ("Bait Casa" + "Bait Hogar") | |
| Bait Paquetes (550–2900) | ✓O | ?P | ?P | Montos altos no publicados por nadie |
| Internet para el Bienestar (50–500) | ✓O ("Bienestar") | ✓O ("Bienestar") | ✓O (+ variante MiFi) | Confirmar modalidad TAE vs servicio |
| CFE Internet (35–755) | ✓O ("CFE Teit") | ✓O ("CFE Internet") | ✓O ("CFE Móvil") | Confirmar modalidad TAE vs servicio |
| Virgin Mobile (20–500) | ✓O | ✓O | ✓O | "Virquin" = Virgin Mobile |
| Soriana Móvil (30–500) | ✓O | ✓O | ✓O | |
| Soriana Móvil Paquetes (30–250) | ?P | ?P | ?P | Sub-producto no distinguible en catálogos públicos |
| DIRI Móvil (80–500) | ✓O | ✓O ("Diri Móvil") | ✓O ("Diri") | |
| OUI (10–350) | ✓O | ✓O | ✓O ("Oui") | |
| PilloFon (90–600) | ✓O | ✓O ("Pillofon") | ✓O ("Pillofon") | |
| Cierto (20–500) | ✓O (listas multimarca) | ✓O ("Cierto") | ✗N | RNP: no aparece en logos |
| CompartFON (10–200) | ✓O ("ComparTFon") | ✓O ("Compartfon") | ✗N | RNP: no aparece (sí "Compartcarga" en Seycel) |
| Flash Mobile (10–500) | ✓O | ✓O | ✓O | |
| FreedomPop (30–200) | ✓O | ✓O | ✓O ("Freedom Pop") | |
| Mi Movil (50–750) | ✓O ("Mimovil") | ✓O ("Mi Móvil") | ✓O ("Mi Móvil") | |
| Netwey (40–250) | ✓O (listas multimarca) | ✓O ("Netwey") | ✗N | RNP: no aparece en logos |
| Yobi (30–500) | ✓O ("YOBI") | ✓O ("Yobi") | ✗N | RNP: no aparece en logos |
| Rincel (40–320) | ✓O | ✓O ("Rincel") | ✓O ("Rin Cel") | |
| Ultracel (55–110) | ✓O ("ULTRACEL") | ✓O ("Ultracel") | ✓O (+ Plus/Pro) | |
| VALOR TELECOM (100–590) | ✓O | ✓O ("Valor Telecom") | ✗N | RNP: no aparece en logos |
| VALOR TELECOM CASA (99–439) | ?P | ?P | ✗N | Sub-producto hogar no listado |
| VALOR TELECOM PAQUETES (100–590) | ?P | ?P | ✗N | Sub-producto no listado |
| Wimo Telecom (35–625) | ✓O ("WimoTelecom") | ✓O ("Wimo") | ✓O ("Wimo Telecom") | |
| Weex (10–500) | ✓O | ✓O | ✓O | |
| Chip Macropay (50–500) | ✗N | ✓O ("Macropay") | ✓O ("Macropay") | MVNO Macropay Mérida (dic-2024); montos oficiales del operador coinciden con la app; TAECEL no lo lista |

**Resumen de cobertura de marcas (33 del spec)**:
- **Seycel/Sivetel: 33/33** marcas presentes (100%) — la más completa, incluida Macropay.
- **TAECEL: 30/31** filas con operador visible (97%); solo **Chip Macropay no encontrado**; "Telcel X Tiempo" plausible como "Internet por Tiempo".
- **RNP: 25/33** (76%): no visibles Cierto, CompartFON, Netwey, Yobi, Valor Telecom (3 variantes) y Telcel X Tiempo → confirmar con el proveedor (los logos públicos pueden no reflejar el catálogo API completo).
- **Montos**: 0 confirmados por API pública en los tres. El endpoint de RNP/Axios es el único documentado formalmente que devuelve montos por SKU (con token).
- Los tres ofrecen operadores extra que MiCachito no tiene (p. ej. Unefon, Bigcel, Newww, JR Móvil, redi Coppel, Dalefon, Oui Extra, Turbo, etc.) — oportunidad de ampliar catálogo.

---

## 6. Comparación técnica

| Dimensión | TAECEL | Seycel / Sivetel | RNP (Axios) |
|---|---|---|---|
| Tipo de API | REST (POST form-urlencoded) | SOAP y REST (declarado) | REST JSON (v1) |
| Documentación pública | ❌ (manual por correo tras levantamiento) | ❌ (por correo tras solicitud) | ⚠️ Parcial: docs.axiosmobile.mx público (auth, productos, recarga, polling, saldo); OpenAPI/SDKs/webhooks solo declarados por RNP |
| Catálogo por API | `getProducts` (con credenciales) | No publicado | `GET /v1/products?type=TAE` (con token) — documentado |
| Montos por API | Con credenciales | No publicado | Con token — endpoint documentado |
| Autenticación | key+nip (form) | No publicado | JWT (POST /v1/auth/:appId) + API Key (x-api-key, server-to-server; recargas solo JWT según tabla publicada) |
| Sandbox | Sí (credenciales por correo + matriz de pruebas) | No publicado | Base dev documentada (apidevstore); API Key por soporte |
| Recarga | `RequestTXN` (producto/referencia/monto → transID) | No publicado | `POST /v1/transactions` (carrierName/carrierCode/amount/reference → folio) |
| Estatus | `StatusTXN` (polling) | No publicado | `GET /transactions/check/:folio` — polling 2 s × 120 s (regla oficial) |
| Saldo | `getBalance` (bolsas separadas) | No publicado | `GET /v1/balance` (saldo/día, compras, ventas, comisión) |
| Folios | Folio de autorización | No publicado | Folio propio + folio de operadora |
| Webhooks | ❌ no documentados | ❌ no publicados | ⚠️ Declarados (HMAC-SHA256, retry exponencial) — sin doc operativa |
| Reversos | ❌ (no existen; "en progreso se cobra") | No publicado | No publicado (solo reembolso de saldo en cancelación de cuenta, T&C) |
| Idempotencia | ❌ no documentada (práctica: no reintentar) | No publicado | No publicado (folio + polling mitigan) |
| Errores | success/error/message; catálogo privado | No publicado | 401/403 documentados + códigos confirmation (00 exitosa, 24 en espera) |
| SDKs oficiales | ❌ (comunitaria Laravel + POS comerciales) | ❌ | Declarados Node/PHP/Python/Java/.NET (GitHub no hallado) — no confirmado |
| Límites/SLA | No publicado (24/7 declarado) | 24/7, 99% cobertura, ~95 ms declarados | 200 req/s, 10,000 req/min, uptime 99.98% (declarados) |
| PHP/.NET | Laravel comunitario; POS PHP/Windows existentes | C#/Java/PHP mostrados como compatibles | SDKs declarados incl. .NET y PHP (no verificados) |
| Integraciones de referencia | eleventa, TheFactory, DisPRO, Odoo/XUBAX | SMS/WhatsApp/Telegram/web/app propios; POS genérico | eSIM/portabilidad/IMEI (Axios); POS propio de la red |

**Lectura**: TAECEL tiene el proceso de integración más formal y referencias POS comprobables, pero todo técnico es privado. RNP/Axios publica la especificación técnica más concreta (endpoints, auth, polling, catálogo por SKU) y es la única con auth moderna (JWT/API-Key), pero es documentación de la plataforma Axios y falta confirmar el alcance para distribuidores RNP. Seycel/Sivetel no publican nada técnico: todo requiere contacto previo.

---

## 7. Costos y modelo comercial

### Publicado vs no publicado
| Concepto | TAECEL | Seycel | Sivetel | RNP |
|---|---|---|---|---|
| Alta/integración | Gratis (publicado); integración via levantamiento | Sin costo (publicado) | Gratis (registro) | Gratis (publicado) |
| Mensualidad | $0 (publicado) | $0 (publicado) | $0 ("sin cuota mensual o anual") | $0 (publicado) |
| Comisión TAE | **Por cotización** (brochure) — NO publicado | **6%** publicado (ejemplo de la misma página implica 6.5% — a aclarar) | **No publicado** | **6.0/6.5–7.0/7.5%** publicado (tope: pago único $300) |
| Depósito mínimo | $50 TAE / $1,000 servicios (publicado) | No publicado | No publicado | $50 (publicado, guía oficial) |
| Costo por transacción API | No publicado | No publicado | No publicado | No publicado (¿la comisión aplica igual vía API?) |
| Reversos | No existen | No publicado | No publicado | No publicado |
| Saldo mínimo operativo | No publicado | No publicado | No publicado | No publicado |
| Esquema | Prepago, bolsas separadas no transferibles | Prepago (saldo único p/ TAE; monedero) | Prepago | Prepago, saldo bonificado, no caduca; reembolsable a CLABE ≤30 días hábiles por cancelación (T&C) |

### Ejemplos con el único modelo publicado (saldo bonificado — RNP; Seycel con 6% equivalente)
Costo real de vender una recarga de valor nominal N = N/(1+c); margen = N−N/(1+c).

| Recarga nominal | Margen con 6.0% | Margen con 6.5% | Margen con 7.5% |
|---|---|---|---|
| $10 | $0.57 | $0.61 | $0.70 |
| $50 | $2.83 | $3.05 | $3.49 |
| $100 | $5.66 | $6.10 | $6.98 |
| $500 | $28.30 | $30.52 | $34.88 |

**Respuesta a "¿cuánto cuesta una recarga de $100?"**: con el esquema publicado, $100 de recarga consume $94.34 de saldo real (6%) → margen $5.66; con tope 7.5%: $93.02 → margen $6.98. Para TAECEL y Sivetel el porcentaje NO está publicado → **no puede calcularse sin cotización**.

### Ingreso mensual estimado (recargas de $100, solo comisión publicada)
| Volumen mensual | RNP 6.0% | Seycel 6.0–6.5% | RNP 7.5% (tope, $300 único) |
|---|---|---|---|
| 100 ops | $566 | $566–$610 | $698 |
| 1,000 ops | $5,660 | $5,660–$6,103 | $6,977 |
| 10,000 ops | $56,604 | $56,604–$61,033 | $69,767 |

El pago único de $300 por el tope 7.5% de RNP se recupera con ~90 recargas de $100 (diferencial 1.5 pts ≈ $1.32/recarga).

### Costo total de operación (lo que la comisión no muestra)
1. **Float prepago**: con 1,000 recargas/día de $100 el consumo de saldo es ~$94,340/día (6%). Fondeo diario = float de ~$94k; cada 3 días ~$283k; semanal ~$660k. El costo financiero de ese capital inmovilizado es parte del costo real.
2. **Costo de conciliación**: recargas "en progreso" (timeout) requieren sondeo y conciliación manual; en TAECEL se cobran aunque no se confirmen → provisión por incobrables.
3. **Sin reversos** (TAECEL confirmado; resto no publicado): una recarga caída a un número equivocado es dinero perdido salvo política del operador.
4. **Costo propio de desarrollo**: adaptador por proveedor, colas de sondeo, bitácora, monitor de saldo, alertas — el mismo para los tres.
5. **Costos no publicados en los tres**: costo por llamada API, costos de webhook, comisión diferencial mayorista, política de cambio de comisiones sin preaviso.

---

## 8. Riesgos y puntos pendientes

1. **Matriz de montos no verificable sin credenciales (los tres)**: bloqueante para prometer el catálogo completo de la app. Ningún proveedor publica product+SKU+monto sin login. Acción: pedir credenciales de sandbox a los tres y validar los ~350 montos contra el catálogo de la app (incluyendo los sub-productos: Bait Internet en Casa, Soriana Paquetes, Valor Casa/Paquetes, Telcel X Tiempo).
2. **Identidad/legalidad RNP**: razón social no publicada; citas de prensa (El Financiero/Expansión/Forbes) no verificables en búsquedas; Wayback sin snapshots 2022–2023; app no hallada en Google Play como "Red Nacional de Pagos" (las del ecosistema son de Axios Mobile/tae.capital). Acción: exigir contrato a nombre de la razón social, verificación de cuentas bancarias a nombre de esa razón social, y aclarar la relación RNP ↔ Axios Comunicaciones (¿quién facturará y quién opera el API?).
3. **RNP — alcance API del distribuidor**: docs.axiosmobile.mx documenta el API de la plataforma Axios (enfocada a productos Axios + catálogo TAE por SKU). No está publicado que una cuenta RNP de mayorista obtenga credenciales para recargas multimarca vía API; además, la tabla publicada dice que las **recargas (POST /v1/transactions) NO aceptan API Key** (solo JWT) — inconveniente para backend server-to-server. Acción: pedir contrato de API + credenciales de prueba y confirmar auth para recargas.
4. **Seycel vs Sivetel**: dos empresas distintas; decidir con cuál se negocia y firmar con esa razón social (Seycel Comunicaciones S.A. de C.V. tiene mayor evidencia pública: catálogo, app, comisión).
5. **TAECEL — política de no reversos y "en progreso se cobra"**: riesgo directo de pérdida en recargas con timeout móvil (conectividad intermitente en CEDIS). Mitigación de diseño: idempotencia propia (id_solicitud único), despacho único desde backend, cola de conciliación, y reglas claras al billetero.
6. **TAECEL — documentación privada y librería de referencia desactualizada (2022)**: no diseñar contra endpoints de itrendsmx/taecel hasta validar contra el manual oficial vigente.
7. **Comisiones no publicadas (TAECEL, Sivetel)** y diferencia 6% vs 6.5% en la propia página de Seycel: cotizar por escrito el porcentaje POR OPERADOR (las comisiones suelen variar por carrier) y la política de cambios.
8. **Discrepancias del catálogo a corregir en la app/spec**: Movistar $0→$40; Virquin→Virgin; DIRY→Diri; decidir redi Coppel (en app, fuera de spec); posible consolidación CFE TEIT/Bienestar; "Telcel X Tiempo" nombrarlo "Internet por Tiempo" como el operador.
9. **CFE TEIT / Bienestar como TAE o servicio**: en algunos proveedores los productos de CFE se venden como "pago de servicio" (bolsa distinta, fee distinto) y no como recarga — confirmar modalidad con cada proveedor.
10. **SLA/límites no publicados (TAECEL, Seycel)**: para operar 34 CEDIS con picos (Buen Fin/Navidad) se necesita TPS garantizado y ventanas de mantenimiento por escrito.

---

## 9. Preguntas que debemos enviar a cada proveedor

**A los tres (bloqueantes):**
1. Matriz completa product+SKU+monto por operador (salida real del endpoint de catálogo con credenciales), vigencia y proceso de actualización.
2. Credenciales de sandbox/prueba + reglas de certificación y tiempos.
3. Porcentaje de comisión **por operador** por escrito (incluyendo paquetes e internet) y política de cambios (preaviso).
4. Confirmación de sub-productos del spec: Bait Internet en Casa, Bait Paquetes, Soriana Móvil Paquetes, VALOR TELECOM CASA/PAQUETES, Telcel X Tiempo ("Internet por Tiempo"), CFE Internet vs Internet para el Bienestar (¿TAE o servicio?).
5. Folios, catálogo de errores, idempotencia (¿qué pasa con dos envíos idénticos en segundos?), y manejo oficial de recargas en "en progreso"/timeout.
6. Reversos: ¿existen, en qué casos, costo y tiempo?
7. Webhooks/callbacks: ¿existen, firma, reintentos?
8. Límites (TPS/RPS), monto máximo por transacción, ventanas de mantenimiento, SLA de disponibilidad y soporte.
9. ¿La comisión aplica igual por canal API? ¿Algún costo por llamada/webhook/alta de API?
10. Esquema mayorista para una red de ~34 CEDIS/billeteros: comisión diferencial, credenciales por sucursal, reportes por afiliado, límites por afiliado.
11. Depósito mínimo, saldo mínimo operativo, medios de fondeo y tiempos de aplicación; política de devolución de saldo al terminar.
12. Contrato: razón social, facturación, garantías, penalizaciones.

**Específicas TAECEL:** manual vigente (lista completa de endpoints más allá de getBalance/getProducts/RequestTXN/StatusTXN); URL de producción y sandbox; canal de integración vigente (integraciones@ vs cc@); ¿MACROPAY en catálogo o roadmap?; ¿AT&T $1,000 viable si el rango publicado es $10–500?; ¿panel MI RED gestionable vía API?

**Específicas Seycel/Sivetel:** ¿con cuál entidad se firma?; documentación técnica del web service (SOAP/REST: WSDL/endpoints, auth, sandbox); aclarar 6% vs 6.5% (ejemplo $1,000→$1,065); ¿"Macropay" del catálogo = recargas al MVNO Chip Macropay con montos 50–500?; white label: costo y alcance.

**Específicas RNP/Axios:** razón social y relación contractual RNP↔Axios Comunicaciones; ¿el distribuidor RNP recibe credenciales del API apidevstore/docs.axiosmobile.mx para el catálogo multimarca?; ¿POST /v1/transactions aceptará API Key?; links reales de OpenAPI 3.0, SDKs GitHub y webhooks; ¿por qué Cierto, CompartFON, Netwey, Yobi y Valor Telecom no aparecen en el catálogo público?; ¿"Macropay" = recargas al MVNO Chip Macropay?; costo y alcance del "pago único $300" (¿aplica a red de afiliados?).

---

## 10. Arquitectura propuesta para Mi Cachito

Principios: credenciales SOLO en backend; la app nunca habla con el proveedor; reutilizar el patrón `api/mobile` existente y la tabla `ventas_electronicas` ya prevista en `docs/backend-integracion.md`.

```
MiCachito-Mobile (MAUI)                Backend Yii2                          Proveedor
─────────────────────                  ─────────────────────────────         ──────────
TiempoAirePage (catálogo)  ──GET──▶  GET /api/mobile/tae/catalogo
  │                                    └─ cache del catálogo proveedor
  │                                       (getProducts / /v1/products)
  ▼                                    ┌─────────────────────────────┐
NumeroTelefono + Monto      ──POST─▶ POST /api/mobile/tae            │ TaeService
  │                                    │  body: {proveedor_id,        │ (interfaz)
  │                                    │         monto, telefono,    │  ├ TaecelAdapter
  │                                    │         id_solicitud}      │  ├ SeycelAdapter
  ▼                                    │  1. valida permiso (ventas. │  └ RnpAxiosAdapter
Esperando resultado                   │     crear / tae.vender)     │      solo backend:
(pantalla de estado)                   │  2. valida límite diario    │      key/nip, JWT,
  ◀──estados (WebSocket/ ─────────────│     y saldo billetero/sede  │      API-Key, secrets
      polling móvil)                   │  3. INSERT ventas_          │         │
                                       │     electronicas (PENDING)  │         ▼
                                       │  4. cola → adapter.recarga()│   API proveedor
                                       │  5. sondeo de estatus       │         │
                                       │     (StatusTXN / check/:folio)        ▼
GET /api/mobile/tae/historial ◀───────│  6. UPDATE a SUCCESS/FAILED  │   Operador
                                       │     + folio + comisión      │
                                       │  7. bitácora + webhook in   │
```

**Decisiones de diseño:**
1. **Endpoints**: `GET /api/mobile/tae/catalogo`, `POST /api/mobile/tae` (recarga), `GET /api/mobile/tae/historial`, `GET /api/mobile/tae/{id}/estatus` — sobre `BaseApiController` con `requirePermiso('ventas.crear')` o nuevo `tae.vender`, con `scopeQueryPorSede` (la venta pertenece a la tienda del billetero; `ventas_electronicas` ya contempla id_tienda).
2. **Catálogo desde backend, no hardcode**: reemplazar `TiempoAireData.cs` (34 proveedores fijos) por el catálogo servido por backend (cacheado 1–24 h y versión offline en SQLite local como hoy hace SyncService). Así los montos reflejan el SKU real del proveedor elegido y se resuelven las discrepancias (Movistar $40, Macropay, redi Coppel) de una vez.
3. **Idempotencia propia**: `id_solicitud` (UUID del dispositivo) único en `ventas_electronicas`; el backend rechaza duplicados aunque el proveedor no lo haga. Despacho único: un solo worker procesa cada venta (cola), nunca reintenta automáticamente una recarga sin resolver primero el estatus (regla de los tres proveedores).
4. **Estados**: PENDING → SENT → SUCCESS/FAILED/TIMEOUT con folio propio + folio de operadora + respuesta cruda en bitácora. TIMEOUT se concilia con el reporte/estatus del proveedor antes de dar por perdido el dinero.
5. **Saldo y comisiones**: wallet central por CEDIS/sede en backend (el proveedor ve UN cliente: Mi Cachito); descuento por billetero usando `comision_tiempo_aire` y `limite_venta_diario` ya existentes; el margen (comisión del proveedor) queda en la cuenta central.
6. **Seguridad**: secrets del proveedor en config/env del backend (nunca en la app ni en repos); audit con `registrarBitacora`; sin PII innecesaria (solo teléfono destino).
7. **Adapters intercambiables**: la interfaz `TaeService` (catalogo, recarga, estatus, saldo) permite empezar con un proveedor y sumar otro sin tocar la app — clave porque ningún proveedor cubre todo el catálogo confirmado (Seycel 33/33 marcas pero sin doc pública; TAECEL sin Macropay; RNP sin 6 marcas).
8. **Producto MVP sugerido**: arrancar con el subconjunto del catálogo confirmado por el proveedor elegido en sandbox (probablemente Telcel/AT&T/Movistar/Bait + top MVNOs) y habilitar el resto por configuración, no por código.

---

## Fuentes (consulta: 21-sep-2026)

**TAECEL**
- https://taecel.com/ — sitio oficial
- https://taecel.com/portal/registro-gratis — alta gratis, sin mensualidades
- https://taecel.com/portal/integracion-web-services — requisitos de integración (REST, levantamiento, pruebas → producción)
- https://cdn.taecel.com/src/web/taecel/descargas/integrador-api-taecel.pdf?v270524= — brochure oficial API (comisión por cotización, mínimos, sin devoluciones)
- https://cdn.taecel.com/src/web/taecel/descargas/levantamiento-tecnologico-taecel.pdf?v070624= — levantamiento (manuales por correo)
- https://taecel.com/portal/cuentas-bancarias-para-recargas-electronicas — mínimos $50/$1,000, referencias, NO devoluciones
- https://taecel.com/portal/vende-recargas-telcel — rango $10–$500; paquetes (Amigo Sin Límite, Internet Amigo, Internet por Tiempo, Bait, MiMovil)
- https://portal.taecel.com/ — select de carriers del registro oficial (lista más reciente)
- https://github.com/itrendsmx/taecel — librería comunitaria Laravel (endpoints, base URL, matriz de pruebas; jul-2022, no oficial)
- https://apps.odoo.com/apps/modules/19.0/xb_pos_taecel y …/xb_taecel_network — integrador Odoo (sandbox, no reversos, credenciales tras validación)
- https://www.cwinsystems.com/dispro/HTML/ofertas-y-comisiones.htm — revendedor DisPRO: 5% TAE (referencia NO oficial)

**Seycel / Sivetel**
- https://www.seycel.mx/Vende-Recargas… — catálogo 62 productos (logos/alt) y comisión 6% (ejemplo $1,000→$1,065)
- https://svxcel.com/ (plataforma Seycel) — https://svxcel.com/Como-Vender-Tiempo-Aire (6%, beneficios), https://svxcel.com/Vende-Recargas-Electronicas-Webservice (proceso de integración, soporte@seycel.mx), https://svxcel.com/Conocenos-Y-Vende-Tiempo-Aire (empresa 2012, 60k clientes)
- https://play.google.com/store/apps/details?id=com.seycelmx.multirec — app Seycel
- https://sivetel.com/ y https://sivetel.com/Sitio/recargas-web-service — SOAP/REST, multiplataforma, 24/7, 99%, ~95 ms, ejemplo /ApiWS/consultar
- https://sivetel.com/Sitio/aviso-privacidad — identidad/domicilio Sivetel (Querétaro)
- https://sivetel.com/Sitio/como-gano-vendiendo-recargas — comisiones por depósito (sin % público)
- https://manualesderecargas.com/manuales/webservice.php — manual SOAP de red ajena (recargas.red/ventatelcel) incluido SOLO para descartarlo como fuente de Seycel/Sivetel

**Red Nacional de Pagos / Axios**
- https://rednacionaldepagos.com.mx/ — home (métricas, productos)
- https://rednacionaldepagos.com.mx/productos/recargas — catálogo de compañías (logos), 6–7.5%
- https://rednacionaldepagos.com.mx/comisiones — esquema completo (tablas de bonificación, fee $6, 1% pines)
- https://rednacionaldepagos.com.mx/recursos — declaraciones API: OpenAPI 3.0, webhooks HMAC-SHA256, SDKs, 200 req/s, uptime 99.98% (links de docs sin destino real)
- https://rednacionaldepagos.com.mx/terminos — T&C (reembolso de saldo ≤30 días hábiles, inactividad 12 meses, distribuidores)
- https://rednacionaldepagos.com.mx/empresa — historia/press (citas NO verificadas externamente)
- https://rednacionaldepagos.com.mx/assets/pdf/guia-distribuidor-rnp.pdf — guía oficial del distribuidor (mínimo $50, SPEI/OXXO, todo el modelo comercial)
- https://docs.axiosmobile.mx/ — portal público de documentación API: /recursos/autenticacion, /recursos/apikey, /recursos/recarga, /recursos/consultarproductos, /recursos/consultarproductostodos, /recursos/consultadesaldo, /recursos/check, /recursos/activacionsimv2, /recursos/validarimei
- https://apidevstore.axiosmobile.mx/v1/products/tae — endpoint de catálogo (verificado vivo: 401 sin token, 21-sep-2026)
- Apps Google Play: mx.axiosmobile / mx.axiosmobile.dist (publisher tae.capital, Axios Comunicaciones S. de R.L. de C.V.)

**Operadores / verificaciones de catálogo**
- https://chipmacropay.mx/recharge — montos oficiales Chip Macropay ($50–$500 + anuales; Internet Portátil $110/$210/$410)
- https://www.redicoppel.com/ — redi Coppel (Oriónidas S.A. de C.V., red Altán)
- Mockups docs/UI/ de MiCachito-Mobile (OCR) — Movistar $40 (no $0), "Virqgin"→Virgin
- Diri Telecomunicaciones S.A. de C.V. (Monterrey) — búsquedas oficiales del operador
- Telcel "Internet por Tiempo" — producto del operador (horas de navegación; corresponde a "Telcel X Tiempo")

**Proyecto (fase 1)**
- MiCachito-Mobile: docs/TIEMPO_AIRE_INTEGRACION.md, docs/TIEMPO_AIRE_BACKEND.md, docs/backend-integracion.md, docs/Arquitectura.md, Data/TiempoAireData.cs, Api/ApiEndpoints.cs, ViewModels/*
- MiCachito-backend: búsquedas `tiempo_aire` (0 integración con proveedor), tabla ventas_electronicas, flags en billeteros
