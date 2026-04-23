// SeedTestData.cs — Ejecutar pasando argumento "seed" al app: WpfApp1.exe seed
// Genera 1 mes de datos de prueba realistas en la base de datos.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace WpfApp1
{
    internal class ProductoSeed
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public double Precio { get; set; }
        public double Costo { get; set; }
        public int Peso { get; set; } // peso relativo para la selección aleatoria
        public string Tipo { get; set; } = "";
    }

    public static class SeedTestData
    {
        public static void Ejecutar()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoardGameCafe.db");
            string cadena = $"Data Source={dbPath}";

            if (!File.Exists(dbPath))
            {
                System.Windows.MessageBox.Show("No se encontró la base de datos. Ejecuta la app primero para crearla.",
                    "Seed Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            using var conexion = new SqliteConnection(cadena);
            conexion.Open();

            // Limpiar ventas anteriores (preservar productos, usuarios, mesas)
            ExecuteNQ(conexion, "DELETE FROM VentasDetalle");
            ExecuteNQ(conexion, "DELETE FROM VentasCabecera");

            // Restablecer stock
            ResetearStock(conexion);

            var random = new Random(42);

            // ══════════════════════════════════════════════
            // CATÁLOGO DE PRODUCTOS
            // ══════════════════════════════════════════════
            var productosCafe = new List<ProductoSeed>
            {
                new() { Id = 1, Nombre = "Café Americano", Precio = 8.0,  Costo = 3.0, Peso = 35, Tipo = "CAFETERIA" },
                new() { Id = 2, Nombre = "Frappé Oreo",    Precio = 15.0, Costo = 7.0, Peso = 20, Tipo = "CAFETERIA" },
                new() { Id = 3, Nombre = "Papas Fritas",   Precio = 10.0, Costo = 4.0, Peso = 15, Tipo = "CAFETERIA" },
                new() { Id = 4, Nombre = "Sandwich Club",  Precio = 18.0, Costo = 8.0, Peso = 12, Tipo = "CAFETERIA" },
                new() { Id = 5, Nombre = "Pepsi",          Precio = 5.0,  Costo = 2.0, Peso = 25, Tipo = "CAFETERIA" },
            };

            var juegos = new List<ProductoSeed>
            {
                new() { Id = 6,  Nombre = "Catan",    Precio = 40.0, Costo = 0.0, Peso = 25, Tipo = "JUEGOS" },
                new() { Id = 7,  Nombre = "Monopoly", Precio = 30.0, Costo = 0.0, Peso = 20, Tipo = "JUEGOS" },
                new() { Id = 8,  Nombre = "UNO",      Precio = 15.0, Costo = 0.0, Peso = 30, Tipo = "JUEGOS" },
                new() { Id = 9,  Nombre = "Risk",     Precio = 35.0, Costo = 0.0, Peso = 10, Tipo = "JUEGOS" },
                new() { Id = 10, Nombre = "Jenga",    Precio = 20.0, Costo = 0.0, Peso = 15, Tipo = "JUEGOS" },
            };

            int[] usuariosIds = { 1, 2 };
            int[] mesasIds = { 1, 2, 3, 4, 5 };

            // ══════════════════════════════════════════════
            // GENERAR VENTAS DE 30 DÍAS
            // ══════════════════════════════════════════════
            DateTime fechaInicio = DateTime.Now.Date.AddDays(-30);
            DateTime fechaFin = DateTime.Now.Date;
            int totalVentas = 0;
            int totalDetalles = 0;

            // Patrón de negocio por hora (peso relativo)
            int[] horasOperacion = { 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            int[] pesosHora = { 5, 15, 20, 18, 10, 8, 18, 20, 15, 12, 22, 25, 12 };

            // Peso por día de semana
            var pesosDia = new Dictionary<DayOfWeek, double>
            {
                { DayOfWeek.Monday, 0.7 }, { DayOfWeek.Tuesday, 0.8 },
                { DayOfWeek.Wednesday, 0.9 }, { DayOfWeek.Thursday, 1.0 },
                { DayOfWeek.Friday, 1.4 }, { DayOfWeek.Saturday, 1.8 },
                { DayOfWeek.Sunday, 1.5 }
            };

            using var transaction = conexion.BeginTransaction();

            for (DateTime fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
            {
                double factorDia = pesosDia[fecha.DayOfWeek];
                int ventasDelDia = (int)(random.Next(8, 18) * factorDia);

                for (int v = 0; v < ventasDelDia; v++)
                {
                    int hora = SeleccionarPorPeso(horasOperacion, pesosHora, random);
                    int minuto = random.Next(0, 60);
                    DateTime fechaVenta = fecha.AddHours(hora).AddMinutes(minuto).AddSeconds(random.Next(0, 60));

                    // No generar ventas futuras
                    if (fechaVenta > DateTime.Now) continue;

                    int idUsuario = usuariosIds[random.Next(usuariosIds.Length)];
                    int idMesa = mesasIds[random.Next(mesasIds.Length)];

                    var detallesVenta = new List<(int idProd, int cantidad, double precio, double costo, string tipo)>();

                    // 40% solo café, 25% solo juego, 35% mixta
                    int tipoVenta = random.Next(100);

                    if (tipoVenta < 40)
                    {
                        int numProductos = random.Next(1, 4);
                        var selec = SeleccionarProductos(productosCafe, numProductos, random);
                        foreach (var p in selec)
                            detallesVenta.Add((p.Id, random.Next(1, 4), p.Precio, p.Costo, p.Tipo));
                    }
                    else if (tipoVenta < 65)
                    {
                        var j = SeleccionarProductos(juegos, 1, random).First();
                        detallesVenta.Add((j.Id, 1, j.Precio, j.Costo, j.Tipo));
                    }
                    else
                    {
                        var j = SeleccionarProductos(juegos, 1, random).First();
                        detallesVenta.Add((j.Id, 1, j.Precio, j.Costo, j.Tipo));

                        int numCafe = random.Next(1, 3);
                        var cafeSelec = SeleccionarProductos(productosCafe, numCafe, random);
                        foreach (var p in cafeSelec)
                            detallesVenta.Add((p.Id, random.Next(1, 3), p.Precio, p.Costo, p.Tipo));
                    }

                    double subtotal = detallesVenta.Sum(d => d.precio * d.cantidad);

                    // 10% chance de descuento (5-15%)
                    double descuento = 0;
                    if (random.Next(100) < 10)
                        descuento = Math.Round(subtotal * (random.Next(5, 16) / 100.0), 2);

                    double totalPagado = subtotal - descuento;

                    InsertarVenta(conexion, transaction, fechaVenta, idUsuario, idMesa,
                        subtotal, descuento, totalPagado, detallesVenta);

                    totalVentas++;
                    totalDetalles += detallesVenta.Count;
                }
            }

            transaction.Commit();

            // Actualizar stock final
            ActualizarStockFinal(conexion);

            System.Windows.MessageBox.Show(
                $"✅ Datos de prueba generados exitosamente\n\n" +
                $"📊 Ventas generadas: {totalVentas}\n" +
                $"📋 Detalles: {totalDetalles}\n" +
                $"📅 Período: {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}\n\n" +
                $"Reinicia la aplicación para ver los datos en el Dashboard.",
                "Seed Completado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private static void InsertarVenta(SqliteConnection conexion, SqliteTransaction transaction,
            DateTime fechaVenta, int idUsuario, int idMesa, double subtotal, double descuento,
            double totalPagado, List<(int idProd, int cantidad, double precio, double costo, string tipo)> detalles)
        {
            string fechaStr = fechaVenta.ToString("yyyy-MM-dd HH:mm:ss");

            using (var cmd = new SqliteCommand(@"
                INSERT INTO VentasCabecera (IdUsuario, IdMesa, FechaVenta, Subtotal, DescuentoAplicado, TotalPagado, Estado)
                VALUES (@u, @m, @f, @s, @d, @t, 'Pagada')", conexion, transaction))
            {
                cmd.Parameters.AddWithValue("@u", idUsuario);
                cmd.Parameters.AddWithValue("@m", idMesa);
                cmd.Parameters.AddWithValue("@f", fechaStr);
                cmd.Parameters.AddWithValue("@s", subtotal);
                cmd.Parameters.AddWithValue("@d", descuento);
                cmd.Parameters.AddWithValue("@t", totalPagado);
                cmd.ExecuteNonQuery();
            }

            long idVenta;
            using (var cmd = new SqliteCommand("SELECT last_insert_rowid()", conexion, transaction))
                idVenta = (long)cmd.ExecuteScalar()!;

            foreach (var det in detalles)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO VentasDetalle (IdVenta, IdProducto, Cantidad, PrecioVendido, CostoAplicado, Subtotal, TipoOrigen)
                    VALUES (@v, @p, @c, @pv, @ca, @s, @t)", conexion, transaction);
                cmd.Parameters.AddWithValue("@v", idVenta);
                cmd.Parameters.AddWithValue("@p", det.idProd);
                cmd.Parameters.AddWithValue("@c", det.cantidad);
                cmd.Parameters.AddWithValue("@pv", det.precio);
                cmd.Parameters.AddWithValue("@ca", det.costo);
                cmd.Parameters.AddWithValue("@s", det.precio * det.cantidad);
                cmd.Parameters.AddWithValue("@t", det.tipo);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ResetearStock(SqliteConnection conexion)
        {
            ExecuteNQ(conexion, @"
                UPDATE Productos SET StockActual = 200 WHERE IdProducto = 1;
                UPDATE Productos SET StockActual = 120 WHERE IdProducto = 2;
                UPDATE Productos SET StockActual = 100 WHERE IdProducto = 3;
                UPDATE Productos SET StockActual = 80  WHERE IdProducto = 4;
                UPDATE Productos SET StockActual = 150 WHERE IdProducto = 5;
            ");

            ExecuteNQ(conexion, @"
                UPDATE LotesInventario SET CantidadDisponible = 200, CantidadInicial = 200 WHERE IdProducto = 1;
                UPDATE LotesInventario SET CantidadDisponible = 120, CantidadInicial = 120 WHERE IdProducto = 2;
                UPDATE LotesInventario SET CantidadDisponible = 100, CantidadInicial = 100 WHERE IdProducto = 3;
                UPDATE LotesInventario SET CantidadDisponible = 80,  CantidadInicial = 80  WHERE IdProducto = 4;
                UPDATE LotesInventario SET CantidadDisponible = 150, CantidadInicial = 150 WHERE IdProducto = 5;
            ");
        }

        private static void ActualizarStockFinal(SqliteConnection conexion)
        {
            // Restar consumo real de ventas
            ExecuteNQ(conexion, @"
                UPDATE Productos SET StockActual = StockActual - COALESCE(
                    (SELECT SUM(d.Cantidad) FROM VentasDetalle d 
                     INNER JOIN VentasCabecera v ON d.IdVenta = v.IdVenta 
                     WHERE d.IdProducto = Productos.IdProducto AND v.Estado = 'Pagada'), 0)
                WHERE EsInventariable = 1");

            // Si alguno quedó negativo, simular un restock parcial
            ExecuteNQ(conexion, @"
                UPDATE Productos SET StockActual = CASE
                    WHEN StockActual < 0 THEN ABS(StockActual) % 12 + 3
                    ELSE StockActual
                END WHERE EsInventariable = 1");

            // Forzar stock bajo en un par de productos para generar alertas visibles
            ExecuteNQ(conexion, @"
                UPDATE Productos SET StockActual = 3 WHERE IdProducto = 3;
                UPDATE Productos SET StockActual = 4 WHERE IdProducto = 4;
            ");

            // Sincronizar lotes
            ExecuteNQ(conexion, @"
                UPDATE LotesInventario SET CantidadDisponible = 
                    (SELECT StockActual FROM Productos WHERE Productos.IdProducto = LotesInventario.IdProducto)
                WHERE EXISTS (SELECT 1 FROM Productos WHERE Productos.IdProducto = LotesInventario.IdProducto AND EsInventariable = 1)
            ");
        }

        private static List<ProductoSeed> SeleccionarProductos(List<ProductoSeed> productos, int cantidad, Random random)
        {
            var seleccionados = new List<ProductoSeed>();
            var disponibles = new List<ProductoSeed>(productos);

            for (int i = 0; i < Math.Min(cantidad, disponibles.Count); i++)
            {
                int totalPeso = disponibles.Sum(p => p.Peso);
                int r = random.Next(totalPeso);
                int acumulado = 0;
                int idx = 0;

                for (int j = 0; j < disponibles.Count; j++)
                {
                    acumulado += disponibles[j].Peso;
                    if (r < acumulado) { idx = j; break; }
                }

                seleccionados.Add(disponibles[idx]);
                disponibles.RemoveAt(idx);
            }

            return seleccionados;
        }

        private static int SeleccionarPorPeso(int[] opciones, int[] pesos, Random random)
        {
            int total = pesos.Sum();
            int r = random.Next(total);
            int acumulado = 0;

            for (int i = 0; i < opciones.Length; i++)
            {
                acumulado += pesos[i];
                if (r < acumulado) return opciones[i];
            }

            return opciones[^1];
        }

        private static void ExecuteNQ(SqliteConnection conexion, string sql)
        {
            using var cmd = new SqliteCommand(sql, conexion);
            cmd.ExecuteNonQuery();
        }
    }
}
