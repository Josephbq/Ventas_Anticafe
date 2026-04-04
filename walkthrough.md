# Walkthrough — Rediseño COCOCAFE JOGOS

## Resumen

Se rediseñó completamente la app WPF de control de ventas para un Board Game Café con las siguientes mejoras:

---

## Archivos Creados (Nuevos)

| Archivo | Descripción |
|---------|-------------|
| [GlobalStyles.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Styles/GlobalStyles.xaml) | Sistema de diseño completo: paleta negro/café/beige, estilos de botones, inputs, DataGrid, cards con sombras |
| [DashboardView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/DashboardView.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/DashboardView.xaml.cs) | Dashboard con 4 cards de resumen + 4 gráficos LiveCharts + panel de alertas |
| [LoteInventario.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/LoteInventario.cs) | Modelo para lotes de inventario con precios de compra/venta variables |
| [ReporteDetallado.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/ReporteDetallado.cs) | Modelo para desglose de reportes por producto |
| [AlertaStock.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/AlertaStock.cs) | Modelo de alertas con mensajes y colores dinámicos |
| [DashboardData.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/DashboardData.cs) | Modelos auxiliares para gráficos (ProductoPopular, VentaPorHora, etc.) |

---

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| [ConexionDB.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Data/ConexionDB.cs) | Nuevo esquema con `EstadoProducto`, `EsInventariable`, lotes con `PrecioVentaLote`, datos iniciales (10 productos + 5 lotes) |
| [Producto.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/Producto.cs) | Agregado: `StockActual`, `StockMinimo`, `EstadoProducto`, `EsInventariable` |
| [ReporteVenta.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Models/ReporteVenta.cs) | Agregado: `NombreMesa`, `CantidadItems` |
| [MainWindow.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/MainWindow.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/MainWindow.xaml.cs) | Sidebar premium con badge alertas, reloj, botón Dashboard, títulos dinámicos |
| [LoginWindow.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/LoginWindow.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/LoginWindow.xaml.cs) | Diseño dark premium, drag-to-move, botón cerrar |
| [POSView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/POSView.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/POSView.xaml.cs) | Tabs de categoría, descuento automático de stock, costo por lote, indicadores visuais de stock |
| [InventarioView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/InventarioView.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/InventarioView.xaml.cs) | Modo producto/lote, estados, historial de lotes, coloreo condicional de filas |
| [ReportesView.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/ReportesView.xaml) + [.cs](file:///d:/PROYECTOS/SIYV/WpfApp1/Views/ReportesView.xaml.cs) | Períodos diario/semanal/mensual, filtro por categoría, PDF separado por secciones |
| [App.xaml](file:///d:/PROYECTOS/SIYV/WpfApp1/App.xaml) | Importa `GlobalStyles.xaml` |
| [WpfApp1.csproj](file:///d:/PROYECTOS/SIYV/WpfApp1/WpfApp1.csproj) | Agregado `LiveChartsCore.SkiaSharpView.WPF` |

---

## Features Implementados

### 🎨 Diseño
- Paleta negro (#1A1514) / café (#6B4226, #8C5A35) / beige (#EFEBE4, #D9CBB8)
- Cards con sombras, bordes redondeados, hover effects
- Sidebar con indicador activo y badge de alertas
- Moneda: **Bs** (bolivianos)

### 📊 Dashboard
- **Ventas del día** y **Ganancia del día** (cards)
- **Alertas de stock** y **Mesas activas** (cards)
- **Top 5 productos** más vendidos (bar chart, últimos 30 días)
- **Ventas por hora** del día actual (line chart)
- **Distribución Juegos vs Cafetería** (pie chart)
- **Panel de alertas** de inventario
- **Ventas por día de semana** (bar chart, últimas 4 semanas)

### 💰 Historial de Precios (Lotes)
- Registrar lotes con **costo de compra** y **precio de venta** diferentes
- Al agregar nuevo lote: se actualiza el precio del producto y se suma al stock
- Historial completo visible al seleccionar un producto en inventario

### 📦 Inventario
- **Estados de producto**: Disponible, Agotado, Dañado, Suspendido
- Estado se puede cambiar desde un panel de acciones
- **Stock mínimo** configurable por producto
- Filas coloreadas: 🔴 sin stock, 🟡 stock bajo, 🟠 dañado/suspendido
- **Juegos como servicios** (`EsInventariable=0`): stock = copias disponibles, no se descuenta al vender

### ⚠️ Alertas
- Badge rojo en sidebar cuando hay productos con problemas
- Panel detallado en dashboard con todos los productos en alerta
- Producto automáticamente cambia a "Agotado" cuando stock llega a 0

### 📑 Reportes
- **Diario / Semanal / Mensual** (botones toggle)
- **Filtro por categoría**: Todos / Cafetería / Juegos
- Cards resumen: Total ventas, Costo total, Ganancia bruta, # Transacciones
- DataGrid con desglose por producto (precio promedio, costo promedio, ganancia)
- **PDF exportado** con secciones separadas por categoría y subtotales

---

## Pasos para ejecutar

> [!IMPORTANT]
> 1. **Elimina** el archivo `BoardGameCafe.db` de `bin/Debug/net8.0-windows/` (si existe) para que se recree con el nuevo esquema
> 2. **Restaura** los paquetes NuGet: `dotnet restore`
> 3. **Ejecuta**: `dotnet run` o desde Visual Studio con F5
> 4. **Login**: usuario `admin` / contraseña `admin123`

---

## Compilación
- ✅ `dotnet build` — **0 errores**
- ⚠️ Solo advertencias menores de NuGet (compatibilidad de versiones)
