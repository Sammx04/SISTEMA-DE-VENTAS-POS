using ProySistemaVentas.Models.Pagos;
using ProySistemaVentas.Services.Pagos;
using System.Globalization;
using System.Text;

namespace ProyectoPruebas
{
    internal static class Program
    {
        private static int _errores = 0;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string? transactionToken = ObtenerArgumento(args, "--transaction-token");
            bool probarBd = args.Any(a => a == "--db");

            Console.WriteLine("==============================================");
            Console.WriteLine("PRUEBA DE HUMO - Pasarela de pagos NIIUBIZ (QA)");
            Console.WriteLine("==============================================");

            await EjecutarPaso("1. Validación de credenciales", () =>
            {
                NiubizConfigService.ValidarCredenciales();

                Console.WriteLine($"  Ambiente      : {NiubizConfigService.Ambiente}");
                Console.WriteLine($"  MerchantId    : {NiubizConfigService.MerchantId}");
                Console.WriteLine($"  Moneda        : {NiubizConfigService.Moneda}");
                Console.WriteLine($"  ApiUrl        : {NiubizConfigService.UrlApi}");
                Console.WriteLine($"  CheckoutJs    : {NiubizConfigService.CheckoutJs}");
                Console.WriteLine($"  Puerto retorno: {NiubizConfigService.PuertoRetornoLocal}");

                return Task.FromResult(0);
            });

            string securityToken = string.Empty;

            await EjecutarPaso("2. Obtención del token de seguridad", async () =>
            {
                securityToken = await new NiubizService().ObtenerTokenSeguridadAsync();

                if (string.IsNullOrWhiteSpace(securityToken))
                {
                    throw new InvalidOperationException("El token de seguridad viene vacío.");
                }

                Console.WriteLine($"  Token devuelto: {securityToken.Substring(0, Math.Min(12, securityToken.Length))}...");
            });

            string sessionKey = string.Empty;
            string numeroOrdenPago = GenerarNumeroOrden();

            await EjecutarPaso("3. Creación de sesión de pago", async () =>
            {
                NiubizService servicio = new NiubizService();

                sessionKey = await servicio.CrearSesionAsync(
                    securityToken,
                    25.50m,
                    numeroOrdenPago,
                    "127.0.0.1",
                    "cliente@paytest.pe",
                    "20492863249",
                    false);

                if (string.IsNullOrWhiteSpace(sessionKey))
                {
                    throw new InvalidOperationException("La sesión devolvió una sessionKey vacía.");
                }

                Console.WriteLine($"  Orden de pago: {numeroOrdenPago}");
                Console.WriteLine($"  sessionKey    : {sessionKey}");
            });

            if (!string.IsNullOrWhiteSpace(transactionToken))
            {
                await EjecutarPaso("4. Autorización del pago", async () =>
                {
                    NiubizService servicio = new NiubizService();

                    ResultadoPagoNiubiz resultado =
                        await servicio.AutorizarPagoAsync(
                            securityToken,
                            transactionToken,
                            25.50m,
                            numeroOrdenPago);

                    Console.WriteLine($"  Aprobado           : {(resultado.Aprobado ? "SÍ" : "NO")}");
                    Console.WriteLine($"  Código respuesta   : {resultado.CodigoRespuesta}");
                    Console.WriteLine($"  Código autorización: {resultado.CodigoAutorizacion}");
                    Console.WriteLine($"  Id transacción     : {resultado.IdTransaccionExterna}");
                    Console.WriteLine($"  Tarjeta            : {resultado.TarjetaEnmascarada}");
                    Console.WriteLine($"  Mensaje            : {resultado.MensajeRespuesta}");

                    if (!resultado.Aprobado)
                    {
                        throw new InvalidOperationException(
                            $"La tarjeta fue rechazada ({resultado.CodigoRespuesta}).");
                    }
                });
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("  [OMITIDO] Paso 4 (autorización). Para probarlo ejecute:");
                Console.WriteLine("    dotnet run --project test\\ProyectoPruebas -- --transaction-token <TOKEN_CAPTURADO>");
            }

            if (probarBd)
            {
                await EjecutarPaso("5. Prueba de BD (números de pago)", () =>
                {
                    IntentoPagoRepository repo = new IntentoPagoRepository();

                    string siguiente =
                        repo.ObtenerProximoNumeroPago();

                    if (string.IsNullOrWhiteSpace(siguiente))
                    {
                        throw new InvalidOperationException("La BD no devolvió el siguiente número de pago.");
                    }

                    Console.WriteLine($"  Siguiente número de pago: {siguiente}");

                    return Task.FromResult(0);
                });
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("  [OMITIDO] Paso 5 (BD). Para probarlo agregue el argumento --db");
            }

            Console.WriteLine();
            Console.WriteLine("==============================================");

            if (_errores == 0)
            {
                Console.WriteLine("RESULTADO GENERAL: TODO EN ORDEN ✔");
                return 0;
            }

            Console.WriteLine($"RESULTADO GENERAL: {_errores} paso(s) CON ERRORES ✘");
            return 1;
        }

        private static async Task EjecutarPaso(
            string titulo,
            Func<Task> accion)
        {
            Console.WriteLine();
            Console.WriteLine($"-- {titulo} ----------------------------------");
            Console.Out.Flush();

            try
            {
                await accion();
                Console.WriteLine("  Estado: OK");
            }
            catch (Exception ex)
            {
                _errores++;
                Console.WriteLine("  Estado: ERROR");
                Console.WriteLine($"  Tipo    : {ex.GetType().FullName}");
                Console.WriteLine($"  Detalle : {ex.Message}");
            }

            Console.Out.Flush();
        }

        private static string? ObtenerArgumento(
            string[] args,
            string nombre)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], nombre, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string GenerarNumeroOrden()
        {
            return DateTime.UtcNow.ToString(
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture)
                .Substring(2);
        }
    }
}