using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WpfApp1.Data // ¡Ojo! Cambia "TuProyectoWPF" por el nombre real de tu proyecto
{
    public static class ConexionDB
    {
        // El archivo se creará en la misma carpeta donde esté tu .exe
        private static string dbName = "BoardGameCafe.db";
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbName);

        // Cadena de conexión que usarás en todas tus consultas
        public static string CadenaConexion = $"Data Source={dbPath}";

        public static void InicializarBaseDeDatos()
        {
            // Si el archivo ya existe, las tablas ya están creadas, salimos.
            if (File.Exists(dbPath)) return;

            // Si no existe, SQLite crea el archivo en blanco al abrir la conexión
            using (var conexion = new SqliteConnection(CadenaConexion))
            {
                conexion.Open();

                // Aquí pegamos TODO el script de diseño relacional usando el arroba (@) para textos multilínea
                string scriptInicial = @"
                    -- 1. Tablas de Configuración y Usuarios
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

                    -- 2. El Corazón del Inventario
                    CREATE TABLE Productos (
                        IdProducto INTEGER PRIMARY KEY AUTOINCREMENT,
                        CodigoBarras TEXT UNIQUE,
                        Nombre TEXT NOT NULL,
                        Tipo TEXT NOT NULL,
                        PrecioVentaBase REAL NOT NULL,
                        StockActual INTEGER DEFAULT 0,
                        StockMinimo INTEGER DEFAULT 0,
                        EsInventariable INTEGER DEFAULT 1,
                        Activo INTEGER DEFAULT 1
                    );

                    CREATE TABLE LotesInventario (
                        IdLote INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdProducto INTEGER NOT NULL,
                        CostoUnitario REAL NOT NULL,
                        CantidadInicial INTEGER NOT NULL,
                        CantidadDisponible INTEGER NOT NULL,
                        FechaIngreso TEXT NOT NULL,
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

                    -- 3. Operación del Local (Mesas y Ventas)
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

                    -- INSERCIONES POR DEFECTO PARA PODER ARRANCAR EL SISTEMA --
                    
                    -- Usuario Admin por defecto
                    INSERT INTO Usuarios (Nombre, Username, Password, Rol) 
                    VALUES ('Administrador General', 'admin', 'admin123', 'Admin');

                    -- Mesas por defecto
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 1', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 2', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Mesa 3', 'Libre');
                    INSERT INTO Mesas (Nombre, Estado) VALUES ('Barra', 'Libre');
                ";

                using (var comando = new SqliteCommand(scriptInicial, conexion))
                {
                    // Ejecuta todo el script de golpe
                    comando.ExecuteNonQuery();
                }
            }
        }
    }
}