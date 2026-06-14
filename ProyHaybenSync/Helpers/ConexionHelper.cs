using Microsoft.Data.SqlClient;
using ProySistemaVentas.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Helpers
{
    public static class ConexionHelper
    {
        public static bool ProbarConexion()
        {
            try
            {
                using SqlConnection cn =
                    new SqlConnection(ConexionService.ObtenerCadenaConexion());

                cn.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
