using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;


namespace CosApp.PyroUsim
{
    public sealed class PyroUsimSimSaleApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public PyroUsimSimSaleApiClient()
        {
            var apiUrl = GetEnv("PYRO_USIM_LOCAL_API_URL");
            var caCertPath = GetEnv("PYRO_USIM_CA_CERT");
            var clientCertPath = GetEnv("PYRO_USIM_CLIENT_CERT");
            var clientKeyPath = GetEnv("PYRO_USIM_CLIENT_KEY");

            var clientCert = X509Certificate2.CreateFromPemFile(clientCertPath, clientKeyPath);
            clientCert = new X509Certificate2(clientCert.Export(X509ContentType.Pfx));

            var caCert = new X509Certificate2(File.ReadAllBytes(caCertPath));

            var handler = new HttpClientHandler
            {
                Proxy = null,
                UseProxy = false,
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            handler.ClientCertificates.Add(clientCert);

            handler.ServerCertificateCustomValidationCallback = (_, cert, chain, _) =>
            {
                chain!.ChainPolicy.ExtraStore.Add(caCert);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return chain.Build((X509Certificate2)cert!);
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(apiUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<PyroUsimSimSaleResponse> SubmitAsync(
            PyroUsimSimSaleRequest request,
            CancellationToken cancellationToken = default)
        {
            var wireRequest = new PyroUsimSimSaleWireRequest
            {
                simVendor = request.SimVendor,
                circleId = request.CircleId,
                msisdn = request.Msisdn,
                iccid = request.Iccid,
                brand = request.Brand,
                international = request.International ? 1 : 0,
                simType = request.SimType,
                channelName = request.ChannelName,
                method_name = request.MethodName,
                transactionId = GenerateTransactionId()
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "",
                wireRequest,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseResponse((int)response.StatusCode, json);
        }

        private static PyroUsimSimSaleResponse ParseResponse(int statusCode, string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new PyroUsimSimSaleResponse
            {
                StatusCode = statusCode,
                Status = root.GetProperty("status").GetString() ?? "FAILED",
                StatusDescription = root.GetProperty("statusDescription").GetString() ?? "",
                Data = statusCode == 200 && root.TryGetProperty("data", out var data)
                    ? new PyroUsimSimSaleData
                    {
                        TransactionId = data.GetProperty("transactionId").GetString()!,
                        Imsi = data.GetProperty("imsi").GetString()!,
                        Msisdn = data.GetProperty("msisdn").GetString()!,
                        Iccid = data.GetProperty("iccid").GetString()!,
                        Pin1 = data.GetProperty("pin1").GetString()!,
                        Puk1 = data.GetProperty("puk1").GetString()!,
                        Pin2 = data.GetProperty("pin2").GetString()!,
                        Puk2 = data.GetProperty("puk2").GetString()!
                    }
                    : null
            };
        }

        private static string GenerateTransactionId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Span<byte> bytes = stackalloc byte[12];
            RandomNumberGenerator.Fill(bytes);

            return new string(bytes.ToArray().Select(b => chars[b % chars.Length]).ToArray());
        }

        private static string GetEnv(string name) =>
            Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Missing environment variable: {name}");

        public void Dispose() => _httpClient.Dispose();
    }
}

