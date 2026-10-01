# Cambios: pasarela de pago Niubiz + módulo de Reportes

Rama: `desarrollo-yanet`
Base de datos: `SistemaVentas`

Este documento resume los dos entregables de este avance para que cualquier
compañero del equipo pueda retomar el trabajo sin tener que reconstruir el
contexto leyendo todo el código.

---

## 1. Resumen

Se agregaron dos funcionalidades independientes al POS:

| Entregable | Qué es | Dónde vive |
|---|---|---|
| Pasarela Niubiz | Cobro real con tarjeta vía WebView2 antes de registrar la venta | `Models/Pagos/`, `Services/Pagos/`, `Views/Dialogs/` |
| Reportes | Pantalla con 10 reportes, exportación a CSV e impresión | `Models/Reporte/`, `Services/Reporte/`, `Views/UserControls/ReportesControl.*`, `SQL/` |

El build queda en **0 errores y 0 advertencias**.

---

## 2. Pasarela de pago (Niubiz)

### 2.1 Qué cambió en el flujo de venta

Antes, `VentaPOSControl` registraba la venta directamente sin importar el
método de pago. Ahora, cuando el método de pago es **`TARJETA`**, la venta
solo se registra **después de que Niubiz apruebe el cobro**. Para efectivo y
Yape/Plin el flujo no cambió.

### 2.2 Secuencia del cobro con tarjeta

```
1. ObtenerProximoNumeroPago()      -> "000000000001" (usa la secuencia SeqPagoPOS)
2. CrearIntento(...)               -> registra el intento en IntentoPagoPOS
3. ObtenerTokenSeguridadAsync()    -> POST /api.security/v1/security  (Basic auth)
4. CrearSesionAsync(...)           -> POST /api.ecommerce/v2/ecommerce/token/session/{merchantId}
                                     devuelve sessionKey
5. CheckoutNiubizDialog            -> WebView2 carga checkout.html y el script de Niubiz
6. AutorizarPagoAsync(...)         -> POST /api.authorization/v3/authorization/ecommerce/{merchantId}
7. Si actionCode == "000"          -> RegistrarVenta(...) y estado APROBADO
   Si no                          -> estado RECHAZADO y la venta NO se registra
   Si el usuario cierra           -> estado CANCELADO y la venta NO se registra
```

### 2.3 Archivos nuevos

- `Models/Pagos/IntentoPago.cs`: entidad de la tabla `dbo.IntentoPagoPOS`.
- `Models/Pagos/ResultadoPagoNiubiz.cs`: resultado tipado de la autorización.
- `Services/Pagos/NiubizConfigService.cs`: lee la sección `Niubiz` de
  `appsettings.json` y decide qué URLs usar según `Ambiente` (QA / PRD).
- `Services/Pagos/NiubizService.cs`: los tres llamados HTTP (token, sesión,
  autorización).
- `Services/Pagos/IntentoPagoRepository.cs`: acceso a los 4 procedimientos
  `sp_pago_*`.
- `Views/Dialogs/CheckoutNiubizDialog.xaml(.cs)`: ventana de cobro.
- `Views/Dialogs/NiubizCheckout/checkout.html`: puente entre WPF y el script de
  Niubiz.

### 2.4 Cómo se comunican WPF y el HTML

`checkout.html` se sirve en un host virtual (`checkoutniubiz.invalid`) con
`SetVirtualHostNameToFolderMapping`. El script de Niubiz invoca
`culqiSuccessCallback` y `culqiDangerCallback`, que envían un `postMessage`
JSON a WPF. Del lado C# se escucha en `CoreWebView2_WebMessageReceived`, que
lee `{ "token": "..." }` o `{ "error": true, "codigo": "...", "mensaje": "..." }`.

`checkout.html` está marcado en el `.csproj` como `CopyToOutputDirectory` para
que se copie junto al ejecutable.

### 2.5 Dependencia agregada

`Microsoft.Web.WebView2` 1.0.4191.47.

### 2.6 Estados guardados en `IntentoPagoPOS`

`APROBADO`, `RECHAZADO`, `CANCELADO`. Cuando queda `APROBADO` se guarda también
el `IdVenta` de la venta generada, así que se puede conciliar el cobro con la
venta.

### 2.7 Configuración

Va en la sección `Niubiz` de `appsettings.json`. Cambiar `"Ambiente"` a `"PRD"`
hace que el servicio use las URLs de producción automáticamente.

> **Ojo:** las credenciales de Niubiz están en texto plano en `appsettings.json`
> y ese archivo está versionado. Ver la sección 6.

---

## 3. Módulo de Reportes

### 3.1 Los 10 reportes

| # | Reporte | Filtros disponibles | Procedimiento |
|---|---|---|---|
| 1 | Ventas por día | periodo, usuario, método de pago | `sp_Reporte_VentasPorDia` |
| 2 | Detalle de ventas | periodo, usuario, método de pago, buscar | `sp_Reporte_VentasDetalle` |
| 3 | Detalle de productos vendidos | periodo, usuario, método de pago, categoría, buscar | `sp_Reporte_LineasVenta` |
| 4 | Productos más vendidos | periodo, categoría, Top N | `sp_Reporte_ProductosVendidos` |
| 5 | Ventas por usuario | periodo, usuario, método de pago | `sp_Reporte_VentasPorUsuario` |
| 6 | Ventas por categoría | periodo, categoría | `sp_Reporte_VentasPorCategoria` |
| 7 | Ventas por tipo de precio | periodo, categoría | `sp_Reporte_VentasPorTipoPrecio` |
| 8 | Stock actual | categoría, ubicación, estado de stock, buscar | `sp_Reporte_StockActual` |
| 9 | Valorización de inventario | categoría, ubicación | `sp_Reporte_ValorizacionInventario` |
| 10 | Movimientos de stock (Kardex) | periodo, usuario | `sp_Reporte_MovimientosStock` |

Además hay dos procedimientos de apoyo para los filtros:
`sp_Reporte_Usuarios` y `sp_Reporte_Categorias`.

### 3.2 Indicadores y salidas

Cada reporte muestra tarjetas de indicadores arriba (registros, subtotal, total,
unidades, ticket promedio) y tiene dos botones:

- **Exportar CSV**: escribe UTF-8 **con BOM** a propósito, porque sin BOM Excel
  rompe los acentos. Usa `;` como separador porque el Excel en español lo
  espera así.
- **Imprimir**: arma un `FlowDocument` en horizontal (los reportes de ventas
  tienen muchas columnas y en vertical se partirían de forma ilegible).

### 3.3 Cómo se organizan las columnas

Cada reporte declara sus columnas con una arreglo de `ColumnaReporte`
(encabezado, propiedad a enlazar, ancho, alineación). El ancho `"*"` se
convierte en `DataGridLengthUnitType.Star`, y los valores monetarios se
muestran ya formateados con propiedades `...Texto` en los modelos
(por ejemplo `TotalTexto`), no con `StringFormat` en el binding. Así se evita
el clásico problema de la coma decimal con la configuración regional.

### 3.4 Índices creados

`VentaPOS` y `DetalleVentaPOS` solo tenían primary key, así que cualquier
filtro por fecha terminaba en un SCAN completo de tabla:

- `IX_VentaPOS_FechaVenta` sobre `VentaPOS(FechaVenta)`
- `IX_VentaPOS_Usuario_Fecha` sobre `VentaPOS(id_usuario, FechaVenta)`
- `IX_DetalleVentaPOS_IdVenta` sobre `DetalleVentaPOS(IdVenta)`
- `IX_DetalleVentaPOS_IdProducto` sobre `DetalleVentaPOS(id_producto)`
- `IX_StockSucursal_IdVariante` sobre `StockSucursal(IdVariante, TipoUbicacion)`

---

## 4. Dos cosas que hay que respetar en la base

Estas dos confusiones ya costaron tiempo, así que conviene documentarlas.

### 4.1 `VentaPOS.IGV` NO es el IGV tributario

La columna `VentaPOS.IGV` **no almacena el IGV peruano del 18%**. La lógica del
POS la calcula como `Subtotal * 0.05` y solo la aplica cuando el método de pago
es `TARJETA`, o sea, es el **recargo por uso de tarjeta**:

```
Total = Subtotal + recargo del 5% (solo con tarjeta)
```

Por eso en todos los modelos y pantallas nuevas el campo se llama
`RecargoTarjeta`, no `IGV`. Si alguien lo lee como IGV va a reportar mal.

### 4.2 `DetalleVentaPOS.id_producto` lleva guion bajo

Es `id_producto`, no `IdProducto`. Con la colación `CI_AS` el TSQL no se
queja de mayúsculas, pero el guion sí importa. En el script SQL se resuelve
con alias explícitos (`d.id_producto AS IdProducto`) para no romper los DTOs
de C#.

### 4.3 Cuál tabla es la buena

- `VentaPOS` + `DetalleVentaPOS`: **la fuente autoritativa** de los reportes.
- `ventas`: tabla legacy. `detalle_venta` ya no se llena, así que joined contra
  ella da resultados vacíos. No usar.

---

## 5. Cómo arrancar

### 5.1 Reportes

Ejecutar **una vez** sobre `SistemaVentas`:

```sql
-- ruta del archivo en el repo
ProyHaybenSync/SQL/sp_reportes_SistemaVentas.sql
```

El script es re-ejecutable (usa `CREATE OR ALTER` e índices con
`IF NOT EXISTS`), así que se puede volver a correr sin miedo.

Desde línea de comandos:

```powershell
sqlcmd -S <SERVIDOR> -d SistemaVentas -E -C -i "ProyHaybenSync\SQL\sp_reportes_SistemaVentas.sql"
```

Requiere **SQL Server 2016 o superior** porque `CREATE OR ALTER` no existe en
2012.

### 5.2 Pasarela

No hay que correr nada: los 4 procedimientos `sp_pago_*` y la tabla
`IntentoPagoPOS` ya están en la base. Ver la sección 7.

### 5.3 Herramienta de prueba de humo

`test/ProyectoPruebas` es una consola que prueba el flujo de Niubiz sin abrir
el POS. Se agregó a la solución y se ejecuta aparte:

```powershell
dotnet run --project test/ProyectoPruebas -- --db
dotnet run --project test/ProyectoPruebas -- --transaction-token <token>
```

---

## 6. Pendientes y riesgos conocidos

### 6.1 Credenciales en texto plano (urgente)

`ProyHaybenSync/appsettings.json` y `test/ProyectoPruebas/appsettings.json`
contienen las credenciales reales de Niubiz y **están versionados** en un
repositorio público. Se subiría así a propósito, pero hay que saberlo.

Pendiente: migrar a user-secrets, variables de entorno o un gestor de
secretos, y sacar `appsettings.json` del tracking de git.

### 6.2 Kardex siempre vacío

`dbo.MovimientoStock` está definida en el script de la base, pero **ningún
procedimiento la escribe**. `sp_ActualizarStockAlmacen`,
`sp_TransferirStockATienda` y `sp_POS_RegistrarVenta` modifican
`StockSucursal` sin registrar el movimiento.

Por eso `sp_Reporte_MovimientosStock` devuelve la estructura vacía sin fallar:
el reporte 10 no va a mostrar nada hasta que se instrumente el registro de
movimientos.

### 6.3 `sp_sistemaVentas.sql` no está en el repo

Los procedimientos del sistema viven fuera del repositorio. Los `sp_pago_*`
solo existen en la base local de desarrollo. Si alguien levanta la base desde
cero, le faltarán. Conviene versionarlos.

### 6.4 Datos sueltos

- `Variantes.PrecioMayor` de la variante "Talla M" (`IdVariante` 7) está en
  NULL en la base. Si debería tener precio mayorista, falta cargarlo.
- La tabla `Categorias` tiene 1 registro y `VentaPOS` está vacía, así que los
  reportes de ventas se probaron solo con estructura de columnas, no con
  datos reales.
- El archivo `checkout.html` no tiene BOM y su `charset` es UTF-8. Funciona,
  pero si se edita desde un editor que guarde en otra codificación, los
  acentos de la página se van a romper.

---

## 7. Archivos tocados en este avance

### Nuevos

```
ProyHaybenSync/Models/Pagos/IntentoPago.cs
ProyHaybenSync/Models/Pagos/ResultadoPagoNiubiz.cs
ProyHaybenSync/Models/Reporte/FiltroReporteInventario.cs
ProyHaybenSync/Models/Reporte/FiltroReportePeriodo.cs
ProyHaybenSync/Models/Reporte/FiltroReporteProductos.cs
ProyHaybenSync/Models/Reporte/InventarioReporte.cs
ProyHaybenSync/Models/Reporte/LineaVentaReporte.cs
ProyHaybenSync/Models/Reporte/MovimientoStockReporte.cs
ProyHaybenSync/Models/Reporte/ReporteTotales.cs
ProyHaybenSync/Models/Reporte/ReporteTotalesLineas.cs
ProyHaybenSync/Models/Reporte/ValorizacionReporte.cs
ProyHaybenSync/Models/Reporte/VentaPorCategoriaReporte.cs
ProyHaybenSync/Models/Reporte/VentaPorTipoPrecioReporte.cs
ProyHaybenSync/Models/Reporte/VentaPorUsuarioReporte.cs
ProyHaybenSync/Models/Reporte/VentaReporteDetalle.cs
ProyHaybenSync/SQL/sp_reportes_SistemaVentas.sql
ProyHaybenSync/Services/Pagos/IntentoPagoRepository.cs
ProyHaybenSync/Services/Pagos/NiubizConfigService.cs
ProyHaybenSync/Services/Pagos/NiubizService.cs
ProyHaybenSync/Services/Reporte/ReporteExportService.cs
ProyHaybenSync/Services/Reporte/ReporteService.cs
ProyHaybenSync/Views/Dialogs/CheckoutNiubizDialog.xaml
ProyHaybenSync/Views/Dialogs/CheckoutNiubizDialog.xaml.cs
ProyHaybenSync/Views/Dialogs/NiubizCheckout/checkout.html
ProyHaybenSync/Views/UserControls/ReportesControl.xaml
ProyHaybenSync/Views/UserControls/ReportesControl.xaml.cs
test/ProyectoPruebas/Program.cs
test/ProyectoPruebas/ProyectoPruebas.csproj
test/ProyectoPruebas/appsettings.json
```

### Modificados

```
ProyHaybenSync/Models/Reporte/ProductoMasVendido.cs   (internal -> public, nuevos campos)
ProyHaybenSync/Models/Reporte/ReporteVenta.cs         (internal -> public, IGV -> RecargoTarjeta)
ProyHaybenSync/ProySistemaVentas.csproj               (+ WebView2, + checkout.html)
ProyHaybenSync/Views/MainView.xaml                     (menú Reportes con evento)
ProyHaybenSync/Views/MainView.xaml.cs                 (+ BtnReportes_Click)
ProyHaybenSync/Views/UserControls/VentaPOSControl.xaml.cs  (flujo Niubiz)
ProyHaybenSync/appsettings.json                        (sección Niubiz, conexión a YANET17)
SlnHaybenSync.sln                                      (+ ProyectoPruebas)
```

---

## 8. Notas de implementación de WPF

Cosas que costaron error y conviene no repetir:

- En un `FlowDocument`, `StackPanel` **no** es un `Block`, así que no se puede
  agregar a `documento.Blocks`. Los títulos se agregan como `Paragraph`
  sueltos.
- `Table` no tiene `Rows`: las filas cuelgan de un `TableRowGroup`.
- `Table` usa `TableColumn` (no `GridColumn`) y su `CellSpacing` es `double`
  (no `Thickness`).
- `TableCell` solo acepta un `Block` en el constructor, y el borde se pone con
  `BorderThickness` y `BorderBrush` de la propia celda, no con un `Border`
  anidado.
- `PrintDialog.PrintDocument` recibe un `DocumentPaginator`, no el
  `FlowDocument`. Se obtiene casteando a `IDocumentPaginatorSource`.
- En `DataGridTextColumn`, `CellStyle` espera un estilo de `DataGridCell`;
  asignarle ahí un estilo de `TextBlock` revienta en runtime. Solo
  `ElementStyle`.