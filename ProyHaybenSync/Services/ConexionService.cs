using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProySistemaVentas.Services
{
    public static class ConexionService
    {
        private static IConfiguration? _configuration;

        static ConexionService()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string ObtenerCadenaConexion()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }
    }
}
