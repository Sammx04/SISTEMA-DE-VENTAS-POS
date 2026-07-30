using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models.InventarioTienda;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace ProySistemaVentas.Services.InventarioTienda
{
    public class InventarioTiendaService
    {
        private readonly string _cadenaConexion;

        public InventarioTiendaService()
        {
            _cadenaConexion =
                ConexionService.ObtenerCadenaConexion();
        }

        // =====================================================
        // LISTAR PRODUCTOS DEL INVENTARIO / TIENDA
        // =====================================================

        public List<InventarioTiendaProducto>
            ListarInventarioTienda()
        {
            Dictionary<int, InventarioTiendaProducto>
                productosAgrupados =
                    new Dictionary<
                        int,
                        InventarioTiendaProducto>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_ListarInventarioTienda",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                int idProducto =
                    Convert.ToInt32(
                        reader["IdProducto"]);

                /*
                 * Se crea un solo objeto por producto.
                 * Las variantes se van agregando dentro
                 * de su colección.
                 */
                if (!productosAgrupados.TryGetValue(
                    idProducto,
                    out InventarioTiendaProducto producto))
                {
                    producto =
                        new InventarioTiendaProducto
                        {
                            IdProducto =
                                idProducto,

                            CodigoBase =
                                ObtenerTexto(
                                    reader,
                                    "CodigoBase"),

                            NombreProducto =
                                ObtenerTexto(
                                    reader,
                                    "NombreProducto"),

                            DescripcionProducto =
                                ObtenerTexto(
                                    reader,
                                    "DescripcionProducto"),

                            IdCategoria =
                                ObtenerEntero(
                                    reader,
                                    "IdCategoria"),

                            NombreCategoria =
                                ObtenerTexto(
                                    reader,
                                    "NombreCategoria"),

                            TieneVariantes =
                                ObtenerBooleano(
                                    reader,
                                    "TieneVariantes"),

                            Variantes =
                                new ObservableCollection<
                                    InventarioTiendaVariante>()
                        };

                    productosAgrupados.Add(
                        idProducto,
                        producto);
                }

                InventarioTiendaVariante variante =
                    new InventarioTiendaVariante
                    {
                        IdProducto =
                            idProducto,

                        IdVariante =
                            ObtenerEntero(
                                reader,
                                "IdVariante"),

                        NombreProducto =
                            ObtenerTexto(
                                reader,
                                "NombreProducto"),

                        VarianteDescripcion =
                            ObtenerTexto(
                                reader,
                                "VarianteDescripcion"),

                        CodigoBarras =
                            ObtenerTexto(
                                reader,
                                "CodigoBarras"),

                        PrecioUnidad =
                            ObtenerDecimal(
                                reader,
                                "PrecioUnidad"),

                        PrecioMayor =
                            ObtenerDecimal(
                                reader,
                                "PrecioMayor"),

                        StockAlmacen =
                            ObtenerEntero(
                                reader,
                                "StockAlmacen"),

                        StockTienda =
                            ObtenerEntero(
                                reader,
                                "StockTienda")
                    };

                producto.Variantes.Add(
                    variante);
            }

            List<InventarioTiendaProducto> resultado =
                productosAgrupados
                    .Values
                    .OrderBy(
                        producto =>
                            producto.NombreProducto)
                    .ToList();

            /*
             * Después de haber agregado todas las variantes,
             * recalculamos totales, estados y visibilidad
             * del botón de variantes.
             */
            foreach (
                InventarioTiendaProducto producto
                in resultado)
            {
                if (producto.Variantes.Count > 1)
                {
                    producto.TieneVariantes = true;
                }

                producto.ActualizarTotales();
            }

            return resultado;
        }

        // =====================================================
        // LISTAR USUARIOS ACTIVOS
        // =====================================================

        public List<UsuarioMovimiento>
            ListarUsuariosActivos()
        {
            List<UsuarioMovimiento> usuarios =
                new List<UsuarioMovimiento>();

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_ListarUsuariosActivos",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                usuarios.Add(
                    new UsuarioMovimiento
                    {
                        IdUsuario =
                            ObtenerEntero(
                                reader,
                                "IdUsuario"),

                        Nombre =
                            ObtenerTexto(
                                reader,
                                "Nombre"),

                        Usuario =
                            ObtenerTexto(
                                reader,
                                "Usuario"),

                        Rol =
                            ObtenerTexto(
                                reader,
                                "Rol")
                    });
            }

            return usuarios;
        }

        // =====================================================
        // TRANSFERIR STOCK DE ALMACÉN A TIENDA
        // =====================================================

        public (
            bool Exitoso,
            int StockAlmacen,
            int StockTienda)
            TransferirStockATienda(
                int idVariante,
                int cantidad,
                int idUsuario)
        {
            if (idVariante <= 0)
            {
                throw new ArgumentException(
                    "La variante seleccionada no es válida.",
                    nameof(idVariante));
            }

            if (cantidad <= 0)
            {
                throw new ArgumentException(
                    "La cantidad debe ser mayor que cero.",
                    nameof(cantidad));
            }

            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar al responsable del movimiento.",
                    nameof(idUsuario));
            }

            using SqlConnection cn =
                new SqlConnection(
                    _cadenaConexion);

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_TransferirStockATienda",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.Add(
                "@IdVariante",
                SqlDbType.Int).Value =
                idVariante;

            cmd.Parameters.Add(
                "@Cantidad",
                SqlDbType.Int).Value =
                cantidad;

            cmd.Parameters.Add(
                "@IdUsuario",
                SqlDbType.Int).Value =
                idUsuario;

            cn.Open();

            using SqlDataReader reader =
                cmd.ExecuteReader();

            if (!reader.Read())
            {
                return (
                    false,
                    0,
                    0);
            }

            bool exitoso =
                ObtenerEntero(
                    reader,
                    "Resultado") == 1;

            int stockAlmacen =
                ObtenerEntero(
                    reader,
                    "StockAlmacen");

            int stockTienda =
                ObtenerEntero(
                    reader,
                    "StockTienda");

            return (
                exitoso,
                stockAlmacen,
                stockTienda);
        }

        // =====================================================
        // MÉTODOS AUXILIARES PARA LEER SQL SERVER
        // =====================================================

        private static string ObtenerTexto(
            SqlDataReader reader,
            string nombreColumna)
        {
            int indice =
                reader.GetOrdinal(
                    nombreColumna);

            if (reader.IsDBNull(indice))
            {
                return string.Empty;
            }

            return Convert
                .ToString(
                    reader[indice])
                ?.Trim()
                ?? string.Empty;
        }

        private static int ObtenerEntero(
            SqlDataReader reader,
            string nombreColumna)
        {
            int indice =
                reader.GetOrdinal(
                    nombreColumna);

            if (reader.IsDBNull(indice))
            {
                return 0;
            }

            return Convert.ToInt32(
                reader[indice]);
        }

        private static decimal ObtenerDecimal(
            SqlDataReader reader,
            string nombreColumna)
        {
            int indice =
                reader.GetOrdinal(
                    nombreColumna);

            if (reader.IsDBNull(indice))
            {
                return 0m;
            }

            return Convert.ToDecimal(
                reader[indice]);
        }

        private static bool ObtenerBooleano(
            SqlDataReader reader,
            string nombreColumna)
        {
            int indice =
                reader.GetOrdinal(
                    nombreColumna);

            if (reader.IsDBNull(indice))
            {
                return false;
            }

            return Convert.ToBoolean(
                reader[indice]);
        }
    }
}