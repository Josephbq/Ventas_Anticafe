# Rediseño Completo - COCOCAFE JOGOS Control de Ventas

Sistema WPF de control de ventas para un Board Game Café con paleta negro/café/beige, historial de precios, alertas de stock, dashboard con gráficos y reportes avanzados.

## User Review Required

> [!IMPORTANT]
> **NuGet Packages necesarios** — Necesitarás instalar estos paquetes manualmente:
> - **LiveChartsCore.SkiaSharpView.WPF** (v2.0.0-rc4 o posterior) — Para los gráficos del dashboard (barras, líneas, torta)
> - Los demás paquetes ya están en el proyecto (Dapper, iText, Microsoft.Data.Sqlite)
> 
> Comando para instalar:
> ```
> dotnet add package LiveChartsCore.SkiaSharpView.WPF --version 2.0.0-rc4.2
> ```

> [!WARNING]
> **Base de datos existente** — Como la BD se crea solo si el archivo `.db` no existe, los cambios de esquema (nuevas columnas/tablas) no se aplicarán automáticamente. **Deberás eliminar el archivo `BoardGameCafe.db`** de la carpeta `bin/Debug/net8.0-windows/` para que se recree con el nuevo esquema. Esto borrará datos existentes de prueba.

## Proposed Changes

### 1. Base de Datos — Cambios en el Esquema

#### [MODIFY] [ConexionDB.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Data/ConexionDB.cs)

Cambios en las tablas:

| Tabla | Cambio | Propósito |
|-------|--------|-----------|
| `Productos` | Agregar columna `EstadoProducto TEXT DEFAULT 'Disponible'` | Estados: `Disponible`, `Agotado`, `Dañado` (para juegos), `Suspendido` |
| `Productos` | Agregar columna `Categoria TEXT DEFAULT 'CAFETERIA'` | Diferenciación explícita: `CAFETERIA` / `JUEGOS` |
| `LotesInventario` | Agregar columna `PrecioVentaLote REAL NOT NULL` | Precio de venta asociado a cada lote/ingreso |
| `LotesInventario` | Agregar columna `Notas TEXT` | Notas opcionales (ej: "proveedor X subió precio") |
| `VentasDetalle` | Agregar columna `CostoLote REAL` | Costo del lote usado para calcular ganancia real |
| **[NEW]** `ConfigAlerta` | Nueva tabla | Umbrales de alerta personalizables por producto |

**Lógica de precios por lotes**: Cuando se registra un producto como "Pepsi", se puede agregar un nuevo lote con precio de compra diferente y precio de venta diferente cada vez. El sistema usa el precio del último lote activo para vender.

---

### 2. Modelos — Nuevos y Modificados

#### [MODIFY] [Producto.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/Producto.cs)
- Agregar `StockActual`, `StockMinimo`, `EstadoProducto`, `EsInventariable`, `Activo`

#### [NEW] `Models/LoteInventario.cs`
- Modelo con: `IdLote`, `IdProducto`, `CostoUnitario`, `PrecioVentaLote`, `CantidadInicial`, `CantidadDisponible`, `FechaIngreso`, `Notas`

#### [MODIFY] [ReporteVenta.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/ReporteVenta.cs)
- Agregar campos para: `NombreMesa`, `CantidadItems`, `Categoria` (JUEGOS/CAFETERIA)

#### [NEW] `Models/ReporteDetallado.cs`
- Modelo para reportes con desglose: `NombreProducto`, `Tipo`, `CostoCompra`, `PrecioVenta`, `Ganancia`, `Cantidad`

#### [NEW] `Models/AlertaStock.cs`
- Modelo para alertas: `IdProducto`, `NombreProducto`, `StockActual`, `StockMinimo`, `EstadoProducto`

#### [NEW] `Models/DashboardData.cs`
- Modelos auxiliares para los gráficos: `ProductoPopular`, `VentaPorHora`, `VentaPorDia`, `ResumenCategoria`

---

### 3. Diseño Visual — Paleta Negro/Café/Beige Premium

#### Paleta de colores definitiva:

| Nombre | Hex | Uso |
|--------|-----|-----|
| Negro Profundo | `#1A1514` | Sidebar, fondos principales, textos |
| Café Oscuro | `#3D2B1F` | Headers, bordes activos |
| Café Medio | `#6B4226` | Botones primarios, acentos |
| Café Claro/Canela | `#8C5A35` | Botones secundarios, badges |
| Beige Cálido | `#D9CBB8` | Bordes, separadores |
| Beige Claro | `#EFEBE4` | Fondo de contenido, cards |
| Crema | `#F8F5F0` | Fondo de paneles internos |
| Blanco Cálido | `#FFFDF9` | Fondo de tablas, inputs |
| Rojo Alerta | `#C0392B` | Alertas de stock, errores |
| Verde Éxito | `#27AE60` | Confirmaciones, stock OK |
| Ámbar Advertencia | `#F39C12` | Stock bajo, warnings |

#### [NEW] `Styles/GlobalStyles.xaml` — Resource Dictionary
- Estilos globales para: `Button`, `TextBox`, `ComboBox`, `DataGrid`, `Border` (cards), `ScrollViewer`
- Animaciones hover para botones del sidebar
- Template de botones con bordes redondeados
- Estilo para cards con sombras suaves

#### [MODIFY] [App.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/App.xaml)
- Importar `GlobalStyles.xaml` como MergedDictionary

---

### 4. Vistas — Nuevas y Rediseñadas

#### [MODIFY] [LoginWindow.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/LoginWindow.xaml) + `.cs`
- Rediseñar con la paleta negro/café/beige
- Agregar logo "COCOCAFE JOGOS" con estilo premium
- Botón de ingresar con estilo café dorado

#### [MODIFY] [MainWindow.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/MainWindow.xaml) + `.cs`
- Sidebar rediseñado con iconos y efecto hover activo (borde lateral café al seleccionar)
- Agregar botón "🏠 Dashboard" como primera opción (y que cargue por defecto)
- Header dinámico que muestra el título de la sección actual
- Badge de alerta en el sidebar cuando hay productos con stock bajo

#### [NEW] `Views/DashboardView.xaml` + `.cs` — **Página principal / Dashboard**
- **Cards resumen**: Ventas del día, Productos con stock bajo, Mesas activas, Ganancia del día
- **Gráfico de barras**: Top 5 productos más vendidos (últimos 30 días)
- **Gráfico de línea**: Ventas por hora del día actual
- **Gráfico de torta**: Distribución ventas Juegos vs Cafetería
- **Panel de alertas**: Lista de productos con stock ≤ mínimo o estado Dañado/Agotado
- **Gráfico de barras**: Ventas por día de la semana (últimas 4 semanas)

#### [MODIFY] [POSView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/POSView.xaml) + `.cs`
- Rediseñar con cards más atractivas para los productos
- Mostrar indicador de stock en cada producto
- Productos sin stock o con estado "Dañado" se muestran deshabilitados/grisados
- Separar visualmente productos de JUEGOS y CAFETERÍA con tabs o secciones
- Descontar stock automáticamente al agregar al ticket
- Restaurar stock al quitar del ticket

#### [MODIFY] [InventarioView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/InventarioView.xaml) + `.cs`
- **Panel izquierdo**: Formulario mejorado con:
  - Campo de nombre, categoría (CAFETERIA/JUEGOS), precio de venta
  - Stock inicial, stock mínimo
  - Estado del producto (ComboBox: Disponible, Agotado, Dañado, Suspendido)
  - Botón "Registrar Nuevo Lote" para agregar lotes con precios diferentes
- **Panel derecho**: DataGrid mejorado mostrando:
  - Nombre, Categoría, Precio Venta Actual (del último lote), Stock, Estado
  - Colores condicionales: rojo si stock=0, ámbar si stock ≤ mínimo
- **Sección inferior**: Historial de lotes del producto seleccionado
  - Muestra: Fecha ingreso, Costo unitario, Precio venta del lote, Cantidad inicial, Cantidad disponible

#### [MODIFY] [ReportesView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/ReportesView.xaml) + `.cs`
- **Selector de período**: Botones para Diario / Semanal / Mensual
- **Filtro por categoría**: Todos / Solo Juegos / Solo Cafetería
- **Tabla de resumen**: Total ventas, total costo, ganancia bruta, número de transacciones
- **DataGrid detallado**: Por producto con columnas:
  - Producto, Categoría, Cantidad vendida, Precio promedio venta, Costo promedio, Ganancia
- **Exportar PDF mejorado**: Con secciones separadas por categoría y resumen de ganancias

---

### 5. Estructura final de archivos

```
WpfApp1/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Data/
│   └── ConexionDB.cs
├── Models/
│   ├── Producto.cs
│   ├── Mesa.cs
│   ├── Usuario.cs
│   ├── ReporteVenta.cs
│   ├── LoteInventario.cs          [NEW]
│   ├── ReporteDetallado.cs        [NEW]
│   ├── AlertaStock.cs             [NEW]
│   └── DashboardData.cs           [NEW]
├── Styles/
│   └── GlobalStyles.xaml          [NEW]
└── Views/
    ├── LoginWindow.xaml / .cs
    ├── DashboardView.xaml / .cs   [NEW]
    ├── POSView.xaml / .cs
    ├── InventarioView.xaml / .cs
    └── ReportesView.xaml / .cs
```

---

## Open Questions

> [!IMPORTANT]
> 1. **¿Quieres que instale LiveChartsCore ahora mismo?** — Lo necesito para los gráficos del dashboard. Sin este NuGet, puedo crear todo excepto los gráficos. ¿Ya lo instalaste o prefieres que use gráficos simples dibujados manualmente con `Canvas` y `Rectangle` de WPF (sin NuGet adicional)?

> [!IMPORTANT]
> 2. **Moneda**: Veo que usas `$` pero mencionas "Bs" (bolivianos). **¿Dejo los precios con símbolo `Bs` o `$`?**

> [!NOTE]
> 3. **Juegos como productos**: Los juegos (Catan, etc.) ¿se "venden" como un servicio por tiempo/uso (ej: "1 hora = 5 Bs"), o se registran como producto normal con stock = cantidad de copias del juego?

---

## Verification Plan

### Compilación
- Verificar que el proyecto compila sin errores con `dotnet build`

### Funcional (Manual - por tu parte)
1. Eliminar `BoardGameCafe.db` de `bin/Debug/net8.0-windows/`
2. Ejecutar la app → debe recrear la BD con el nuevo esquema
3. Login con admin/admin123
4. Dashboard: verificar que muestra cards y gráficos (vacíos al inicio)
5. Inventario: Registrar un producto con stock → verificar en la tabla
6. Inventario: Agregar un nuevo lote con precio diferente → verificar historial
7. POS: Vender productos → verificar descuento de stock
8. Dashboard: Después de ventas, verificar que los gráficos se actualizan
9. Reportes: Generar reporte diario, semanal, mensual → exportar PDF
10. Alertas: Reducir stock a 0 → verificar alerta en dashboard
