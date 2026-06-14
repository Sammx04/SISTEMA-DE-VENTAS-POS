using Microsoft.Data.SqlClient;
using ProySistemaVentas.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Services
{
    public class UsuarioService
    {
        public UsuarioLogin? ObtenerUsuario(string usuario)
        {
            try
            {
                using SqlConnection cn =
                    new SqlConnection(
                        ConexionService.ObtenerCadenaConexion());

                using SqlCommand cmd =
                    new SqlCommand("sp_login", cn);

                cmd.CommandType =
                    System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue(
                    "@usuario",
                    usuario);

                cn.Open();

                using SqlDataReader dr =
                    cmd.ExecuteReader();

                if (dr.Read())
                {
                    return new UsuarioLogin
                    {
                        IdUsuario = Convert.ToInt32(
                            dr["id_usuario"]),

                        Nombre = dr["nombre"].ToString()!,

                        Usuario = dr["usuario"].ToString()!,

                        Password = dr["password"].ToString()!,

                        Rol = dr["rol"].ToString()!
                    };
                }

                return null;
            }
            catch
            {
                throw;
            }
        }

        public bool CrearUsuario(
            string nombre, string usuario, string password, int idRol)
        {
            try
            {
                using SqlConnection cn =
                    new SqlConnection(
                        ConexionService.ObtenerCadenaConexion());

                using SqlCommand cmd =
                    new SqlCommand(
                        "sp_crear_usuario",
                        cn);

                cmd.CommandType =
                    System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue(
                    "@nombre",
                    nombre);

                cmd.Parameters.AddWithValue(
                    "@usuario",
                    usuario);

                cmd.Parameters.AddWithValue(
                    "@password",
                    password);

                cmd.Parameters.AddWithValue(
                    "@id_rol",
                    idRol);

                cn.Open();

                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                throw;
            }
        }

        public List<Rol> ObtenerRoles()
        {
            List<Rol> roles = new();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            string sql =
                "SELECT id_rol, nombre FROM roles";

            using SqlCommand cmd =
                new SqlCommand(sql, cn);

            cn.Open();

            using SqlDataReader dr =
                cmd.ExecuteReader();

            while (dr.Read())
            {
                roles.Add(new Rol
                {
                    IdRol = Convert.ToInt32(
                        dr["id_rol"]),

                    Nombre = dr["nombre"].ToString()!
                });
            }

            return roles;
        }

        public List<UsuarioListado> ListarUsuarios()
        {
            List<UsuarioListado> lista = new();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_listar_usuarios",
                    cn);

            cmd.CommandType =
                System.Data.CommandType.StoredProcedure;

            cn.Open();

            using SqlDataReader dr =
                cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new UsuarioListado
                {
                    IdUsuario = Convert.ToInt32(
                        dr["id_usuario"]),

                    Nombre = dr["nombre"].ToString()!,

                    Usuario = dr["usuario"].ToString()!,

                    Rol = dr["rol"].ToString()!,

                    Estado = Convert.ToBoolean(
                        dr["estado"])
                });
            }

            return lista;
        }

        public List<UsuarioListado> BuscarUsuarios(string texto)
        {
            List<UsuarioListado> lista = new();

            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_buscar_usuario",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue(
                "@texto",
                texto);

            cn.Open();

            using SqlDataReader dr =
                cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new UsuarioListado
                {
                    IdUsuario = Convert.ToInt32(dr["id_usuario"]),
                    Nombre = dr["nombre"].ToString()!,
                    Usuario = dr["usuario"].ToString()!,
                    Rol = dr["rol"].ToString()!,
                    Estado = Convert.ToBoolean(dr["estado"])
                });
            }

            return lista;
        }

        public bool CambiarEstadoUsuario( int idUsuario,bool estado)
        {
            using SqlConnection cn =
                new SqlConnection(
                    ConexionService.ObtenerCadenaConexion());

            using SqlCommand cmd =
                new SqlCommand(
                    "sp_cambiar_estado_usuario",
                    cn);

            cmd.CommandType =
                CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue(
                "@id_usuario",
                idUsuario);

            cmd.Parameters.AddWithValue(
                "@estado",
                estado);

            cn.Open();

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
