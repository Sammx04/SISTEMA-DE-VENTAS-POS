using Microsoft.Extensions.Configuration;
using System;

namespace ProySistemaVentas.Services.Pagos
{
    public static class NiubizConfigService
    {
        private static IConfiguration? _configuration;

        static NiubizConfigService()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string Ambiente
        {
            get
            {
                string ambiente =
                    _configuration["Niubiz:Ambiente"]?.Trim()
                    ?? "QA";

                return ambiente.Equals("PRD", StringComparison.OrdinalIgnoreCase)
                    ? "PRD"
                    : "QA";
            }
        }

        public static bool EsProduccion =>
            Ambiente == "PRD";

        public static string MerchantId =>
            _configuration["Niubiz:MerchantId"]?.Trim() ?? string.Empty;

        public static string Usuario =>
            _configuration["Niubiz:Usuario"]?.Trim() ?? string.Empty;

        public static string Clave =>
            _configuration["Niubiz:Clave"]?.Trim() ?? string.Empty;

        public static string Moneda
        {
            get
            {
                string moneda =
                    _configuration["Niubiz:Moneda"]?.Trim()
                    ?? "PEN";

                return string.IsNullOrWhiteSpace(moneda)
                    ? "PEN"
                    : moneda.ToUpperInvariant();
            }
        }

        public static string UrlApi =>
            EsProduccion
                ? _configuration["Niubiz:UrlApiPRD"]?.Trim() ?? string.Empty
                : _configuration["Niubiz:UrlApiQA"]?.Trim() ?? string.Empty;

        public static string CheckoutJs =>
            EsProduccion
                ? _configuration["Niubiz:CheckoutJsPRD"]?.Trim() ?? string.Empty
                : _configuration["Niubiz:CheckoutJsQA"]?.Trim() ?? string.Empty;

        public static int PuertoRetornoLocal
        {
            get
            {
                string puerto =
                    _configuration["Niubiz:PuertoRetornoLocal"]?.Trim()
                    ?? string.Empty;

                return int.TryParse(puerto, out int valor) && valor > 0
                    ? valor
                    : 51914;
            }
        }

        public static string CodigoCiudad =>
            _configuration["Niubiz:CodigoCiudad"]?.Trim() ?? "LIM";

        public static string CodigoPais =>
            _configuration["Niubiz:CodigoPais"]?.Trim() ?? "PE";

        public static string CodigoPostal =>
            _configuration["Niubiz:CodigoPostal"]?.Trim() ?? "15001";

        public static bool CredencialesCompletas =>
            !string.IsNullOrWhiteSpace(MerchantId) &&
            !string.IsNullOrWhiteSpace(Usuario) &&
            !string.IsNullOrWhiteSpace(Clave) &&
            !string.IsNullOrWhiteSpace(UrlApi) &&
            !string.IsNullOrWhiteSpace(CheckoutJs);

        public static void ValidarCredenciales()
        {
            if (CredencialesCompletas)
            {
                return;
            }

            throw new InvalidOperationException(
                "La pasarela Niubiz no está configurada. " +
                "Complete las credenciales (MerchantId, Usuario, Clave) en la sección Niubiz de appsettings.json.");
        }
    }
}