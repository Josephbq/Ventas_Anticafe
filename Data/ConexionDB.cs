using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WpfApp1.Data
{
    public static class ConexionDB
    {
        private static string dbName = "BoardGameCafe.db";
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbName);

        public static string CadenaConexion = $"Data Source={dbPath}";

        public static void InicializarBaseDeDatos()
        {
            if (File.Exists(dbPath)) return;

            using (var conexion = new SqliteConnection(CadenaConexion))
            {
                conexion.Open();

                string scriptInicial = @"
                    -- ══════════════════════════════════════════════
                    -- 1. USUARIOS Y CLIENTES
                    -- ══════════════════════════════════════════════
                    CREATE TABLE Usuarios (
                        IdUsuario INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombre TEXT NOT NULL,
                        Username TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL,
                        Rol TEXT NOT NULL,
                        Activo INTEGER DEFAULT 1
                    );

                    CREATE TABLE Clientes (
                        IdCliente INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombre TEXT NOT NULL,
                        Telefono TEXT,
                        VisitasAcumuladas INTEGER DEFAULT 0,
                        FechaUltimaVisita TEXT
                    );

                    -- ══════════════════════════════════════════════
                    -- 2. INVENTARIO Y PRODUCTOS
                    -- ══════════════════════════════════════════════
                    CREATE TABLE Productos (
                        IdProducto INTEGER PRIMARY KEY AUTOINCREMENT,
                        CodigoBarras TEXT UNIQUE,
                        Nombre TEXT NOT NULL,
                        Tipo TEXT NOT NULL,                        -- 'CAFETERIA' o 'JUEGOS'
                        PrecioVentaBase REAL NOT NULL,
                        StockActual INTEGER DEFAULT 0,
                        StockMinimo INTEGER DEFAULT 0,
                        EsInventariable INTEGER DEFAULT 1,        -- 1=sí (cafetería), 0=no (juegos/servicios)
                        EstadoProducto TEXT DEFAULT 'Disponible',  -- Disponible, Agotado, Dañado, Suspendido
                        Activo INTEGER DEFAULT 1
                    );

                    CREATE TABLE LotesInventario (
                        IdLote INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdProducto INTEGER NOT NULL,
                        CostoUnitario REAL NOT NULL,
                        PrecioVentaLote REAL NOT NULL,
                        CantidadInicial INTEGER NOT NULL,
                        CantidadDisponible INTEGER NOT NULL,
                        FechaIngreso TEXT NOT NULL,
                        Notas TEXT,
                        FOREIGN KEY(IdProducto) REFERENCES Productos(IdProducto)
                    );

                    CREATE TABLE Mermas (
                        IdMerma INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdProducto INTEGER NOT NULL,
                        IdUsuario INTEGER NOT NULL,
                        Cantidad INTEGER NOT NULL,
                        Motivo TEXT,
                        FechaRegistro TEXT NOT NULL,
                        FOREIGN KEY(IdProducto) REFERENCES Productos(IdProducto),
                        FOREIGN KEY(IdUsuario) REFERENCES Usuarios(IdUsuario)
                    );

                    -- ══════════════════════════════════════════════
                    -- 3. MESAS Y VENTAS
                    -- ══════════════════════════════════════════════
                    CREATE TABLE Mesas (
                        IdMesa INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombre TEXT NOT NULL,
                        Estado TEXT DEFAULT 'Libre'
                    );

                    CREATE TABLE VentasCabecera (
                        IdVenta INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdUsuario INTEGER NOT NULL,
                        IdCliente INTEGER,
                        IdMesa INTEGER,
                        FechaVenta TEXT NOT NULL,
                        Subtotal REAL NOT NULL,
                        DescuentoAplicado REAL DEFAULT 0,
                        TotalPagado REAL NOT NULL,
                        Estado TEXT DEFAULT 'Abierta',
                        FOREIGN KEY(IdUsuario) REFERENCES Usuarios(IdUsuario),
                        FOREIGN KEY(IdCliente) REFERENCES Clientes(IdCliente),
                        FOREIGN KEY(IdMesa) REFERENCES Mesas(IdMesa)
                    );

                    CREATE TABLE VentasDetalle (
                        IdDetalle INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdVenta INTEGER NOT NULL,
                        IdProducto INTEGER NOT NULL,
                        Cantidad INTEGER NOT NULL,
                        PrecioVendido REAL NOT NULL,
                        CostoAplicado REAL NOT NULL,
                        Subtotal REAL NOT NULL,
                        TipoOrigen TEXT NOT NULL,
                        FOREIGN KEY(IdVenta) REFERENCES VentasCabecera(IdVenta),
                        FOREIGN KEY(IdProducto) REFERENCES Productos(IdProducto)
                    );

                    -- ══════════════════════════════════════════════
                    -- 4. DATOS INICIALES
                    -- ══════════════════════════════════════════════
                    
                    -- Usuario Admin
                    INSERT INTO Usuarios (Nombre, Username, Password, Rol) 
                    VALUES ('Administrador General', 'admin', 'admin123', 'Admin');

                    -- Usuario Vendedor (prueba)
                    INSERT INTO Usuarios (Nombre, Username, Password, Rol) 
                    VALUES ('Vendedor Prueba', 'vendedor1', 'venta123', 'Vendedor');

                    -- Usuario Super (prueba)
                    INSERT INTO Usuarios (Nombre, Username, Password, Rol) 
                    VALUES ('Supervisor General', 'super1', 'super123', 'Super');

                    -- Mesas
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 1', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 2', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 3', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 4', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Barra', 'Libre');

                    -- Productos de Cafetería (inventariables)
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Café Americano', 'CAFETERIA', 8.00, 50, 10, 1, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Frappé Oreo', 'CAFETERIA', 15.00, 30, 5, 1, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Papas Fritas', 'CAFETERIA', 10.00, 25, 5, 1, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Sandwich Club', 'CAFETERIA', 18.00, 20, 5, 1, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Pepsi', 'CAFETERIA', 5.00, 40, 10, 1, 'Disponible');

                    -- Juegos (NO inventariables - son servicios, stock = copias disponibles)
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Catan', 'JUEGOS', 40.00, 2, 0, 0, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Monopoly', 'JUEGOS', 30.00, 3, 0, 0, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('UNO', 'JUEGOS', 15.00, 4, 0, 0, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Risk', 'JUEGOS', 35.00, 1, 0, 0, 'Disponible');
                    INSERT INTO Productos (Nombre, Tipo, PrecioVentaBase, StockActual, StockMinimo, EsInventariable, EstadoProducto) 
                    VALUES ('Jenga', 'JUEGOS', 20.00, 2, 0, 0, 'Disponible');

                    -- Lotes iniciales para productos de cafetería
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (1, 3.00, 8.00, 50, 50, '2026-04-01', 'Lote inicial café');
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (2, 7.00, 15.00, 30, 30, '2026-04-01', 'Lote inicial frappé');
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (3, 4.00, 10.00, 25, 25, '2026-04-01', 'Lote inicial papas');
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (4, 8.00, 18.00, 20, 20, '2026-04-01', 'Lote inicial sandwich');
                    INSERT INTO LotesInventario (IdProducto, CostoUnitario, PrecioVentaLote, CantidadInicial, CantidadDisponible, FechaIngreso, Notas) 
                    VALUES (5, 2.00, 5.00, 40, 40, '2026-04-01', 'Lote inicial pepsi');
                ";

                using (var comando = new SqliteCommand(scriptInicial, conexion))
                {
                    comando.ExecuteNonQuery();
                }
            }
        }
    }
}