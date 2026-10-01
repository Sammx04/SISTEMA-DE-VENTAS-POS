using ProySistemaVentas.Models.Pagos;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProySistemaVentas.Services.Pagos
{
    public class NiubizService
    {
        private const string EndpointSecurity = "/api.security/v1/security";
        private const string EndpointSession = "/api.ecommerce/v2/ecommerce/token/session/{merchantId}";
        private const string EndpointAuthorization = "/api.authorization/v3/authorization/ecommerce/{merchantId}";

        private static readonly HttpClient Http =
            new HttpClient();

        public async Task<string> ObtenerTokenSeguridadAsync()
        {
            NiubizConfigService.ValidarCredenciales();

            string baseUrl =
                NiubizConfigService.UrlApi.TrimEnd('/');

            using HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    baseUrl + EndpointSecurity);

            string credenciales =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{NiubizConfigService.Usuario}:{NiubizConfigService.Clave}"));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credenciales);

            request.Content =
                new StringContent(
                    string.Empty,
                    Encoding.UTF8,
                    "text/plain");

            using HttpResponseMessage respuesta =
                await Http.SendAsync(request).ConfigureAwait(false);

            string cuerpo =
                await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"No se pudo obtener el token de seguridad de Niubiz " +
                    $"(HTTP {(int)respuesta.StatusCode}): {cuerpo}");
            }

            string token =
                cuerpo.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Niubiz devolvió un token de seguridad vacío.");
            }

            return token;
        }

        public async Task<string> CrearSesionAsync(
            string securityToken,
            decimal monto,
            string numeroOrdenPago,
            string clienteIp,
            string correoCliente,
            string idCliente,
            bool clienteRegistrado)
        {
            NiubizConfigService.ValidarCredenciales();

            string baseUrl =
                NiubizConfigService.UrlApi.TrimEnd('/');

            string urlSesion =
                baseUrl +
                EndpointSession.Replace("{merchantId}", NiubizConfigService.MerchantId);

            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Post, urlSesion);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            request.Headers.Add("Authorization", securityToken);

            string montoTexto =
                monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            string telefono =
                string.IsNullOrWhiteSpace(idCliente)
                    ? "000000000"
                    : idCliente;

            var cuerpo = new
            {
                channel = "web",
                amount = montoTexto,
                antifraud = new
                {
                    clientIp = string.IsNullOrWhiteSpace(clienteIp)
                        ? "127.0.0.1"
                        : clienteIp,
                    merchantDefineData = new
                    {
                        MDD4 = string.IsNullOrWhiteSpace(correoCliente)
                            ? "cliente@hayben.pe"
                            : correoCliente,
                        MDD21 = clienteRegistrado ? 1 : 0,
                        MDD32 = idCliente,
                        MDD75 = clienteRegistrado ? "Registrado" : "Invitado",
                        MDD77 = clienteRegistrado ? 1 : 0
                    }
                },
                purchaseNumber = numeroOrdenPago,
                recurrenceMaxAmount = montoTexto
            };

            request.Content =
                new StringContent(
                    JsonSerializer.Serialize(cuerpo),
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage respuesta =
                await Http.SendAsync(request).ConfigureAwait(false);

            string json =
                await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear la sesión de pago en Niubiz " +
                    $"(HTTP {(int)respuesta.StatusCode}): {json}");
            }

            using JsonDocument documento =
                JsonDocument.Parse(json);

            string? sessionKey =
                documento.RootElement.TryGetProperty("sessionKey", out var clave)
                    ? clave.GetString()
                    : documento.RootElement.TryGetProperty("sessionkey", out var claveMin)
                        ? claveMin.GetString()
                        : null;

            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                throw new InvalidOperationException(
                    $"Niubiz no devolvió sessionKey: {json}");
            }

            return sessionKey!;
        }

        public async Task<ResultadoPagoNiubiz> AutorizarPagoAsync(
            string securityToken,
            string transactionToken,
            decimal monto,
            string numeroOrdenPago)
        {
            NiubizConfigService.ValidarCredenciales();

            string baseUrl =
                NiubizConfigService.UrlApi.TrimEnd('/');

            string urlAutorizacion =
                baseUrl +
                EndpointAuthorization.Replace("{merchantId}", NiubizConfigService.MerchantId);

            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Post, urlAutorizacion);

            request.Headers.Add("Authorization", securityToken);

            var cuerpo = new
            {
                channel = "web",
                captureType = "manual",
                countable = true,
                order = new
                {
                    tokenId = transactionToken,
                    purchaseNumber = numeroOrdenPago,
                    amount = monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    currency = NiubizConfigService.Moneda
                },
                dataMap = new
                {
                    urlAddress = $"http://localhost:{NiubizConfigService.PuertoRetornoLocal}",
                    serviceLocationCityName = NiubizConfigService.CodigoCiudad,
                    serviceLocationCountrySubdivisionCode = NiubizConfigService.CodigoCiudad,
                    serviceLocationCountryCode = NiubizConfigService.CodigoPais,
                    serviceLocationPostalCode = NiubizConfigService.CodigoPostal
                }
            };

            request.Content =
                new StringContent(
                    JsonSerializer.Serialize(cuerpo),
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage respuesta =
                await Http.SendAsync(request).ConfigureAwait(false);

            string json =
                await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);

            ResultadoPagoNiubiz resultado =
                new ResultadoPagoNiubiz
                {
                    RespuestaCruda = json
                };

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized ||
                respuesta.StatusCode == HttpStatusCode.Forbidden)
            {
                resultado.CodigoRespuesta = "401";
                resultado.MensajeRespuesta =
                    "El token de Niubiz no es válido. Inicie sesión nuevamente.";

                return resultado;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                resultado.CodigoRespuesta =
                    ((int)respuesta.StatusCode).ToString();

                resultado.MensajeRespuesta =
                    $"Niubiz devolvió HTTP {(int)respuesta.StatusCode}: {json}";

                return resultado;
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(json);

                JsonElement raiz =
                    documento.RootElement;

                if (raiz.TryGetProperty("order", out JsonElement orden))
                {
                    if (orden.TryGetProperty("actionCode", out JsonElement accion))
                    {
                        resultado.CodigoRespuesta =
                            accion.GetString();

                        resultado.Aprobado =
                            accion.GetString() == "000";
                    }

                    if (orden.TryGetProperty("authorizationCode", out JsonElement autorizacion))
                    {
                        resultado.CodigoAutorizacion =
                            autorizacion.GetString();
                    }
                }

                if (raiz.TryGetProperty("dataMap", out JsonElement dataMap))
                {
                    if (string.IsNullOrEmpty(resultado.CodigoRespuesta) &&
                        dataMap.TryGetProperty("ACTION_CODE", out JsonElement accionMapa))
                    {
                        resultado.CodigoRespuesta =
                            accionMapa.GetString();

                        resultado.Aprobado =
                            accionMapa.GetString() == "000" ||
                            dataMap.TryGetProperty("STATUS", out JsonElement estado) &&
                            string.Equals(
                                estado.GetString(),
                                "Authorized",
                                StringComparison.OrdinalIgnoreCase);
                    }

                    if (string.IsNullOrEmpty(resultado.CodigoAutorizacion) &&
                        dataMap.TryGetProperty("AUTHORIZATION_CODE", out JsonElement autorizacionMapa))
                    {
                        resultado.CodigoAutorizacion =
                            autorizacionMapa.GetString();
                    }

                    if (dataMap.TryGetProperty("CARD", out JsonElement tarjeta))
                    {
                        resultado.TarjetaEnmascarada =
                            tarjeta.GetString();
                    }

                    if (dataMap.TryGetProperty("TRANSACTION_ID", out JsonElement transaccion))
                    {
                        resultado.IdTransaccionExterna =
                            transaccion.GetString();
                    }

                    if (dataMap.TryGetProperty("RESPONSE_MESSAGE", out JsonElement mensaje))
                    {
                        resultado.MensajeRespuesta =
                            mensaje.GetString();
                    }
                }

                if (string.IsNullOrEmpty(resultado.CodigoRespuesta) &&
                    raiz.TryGetProperty("actionCode", out JsonElement accionPlana))
                {
                    resultado.CodigoRespuesta =
                        accionPlana.GetString();

                    resultado.Aprobado =
                        accionPlana.GetString() == "000";
                }
            }
            catch (JsonException ex)
            {
                resultado.CodigoRespuesta = "500";
                resultado.MensajeRespuesta =
                    $"No se pudo interpretar la respuesta de Niubiz: {ex.Message}";
            }

            return resultado;
        }
    }
}