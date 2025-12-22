using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace cos.ApiCallers
{
    public class SmsApiCaller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsApiCaller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsApiCaller(IConfiguration configuration, ILogger<SmsApiCaller> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> SendOtpSms(string mobileno, string otp, string? zone = null, string? circle = null, string? ssa = null)
        {
            try
            {
                var baseUrl = _configuration["SmsSettings:BaseUrl"] ?? "https://osbmsg.cdr.bsnl.co.in/osb/EAINotificationService/EAISendNotificationRest";
                var peId = _configuration["SmsSettings:PE_ID"] ?? "1401643660000016974";
                var tmId = _configuration["SmsSettings:TM_ID"] ?? "1407176509482415833";
                
                // Default values if not provided
                zone = zone ?? _configuration["SmsSettings:DefaultZone"] ?? "W";
                circle = circle ?? _configuration["SmsSettings:DefaultCircle"] ?? "MH";
                ssa = ssa ?? _configuration["SmsSettings:DefaultSSA"] ?? "STR";

                // Format mobile number: remove leading 91 if present and add 0 prefix
                string formattedMobile = mobileno.Trim();
                if (formattedMobile.StartsWith("91"))
                {
                    formattedMobile = formattedMobile.Substring(2);
                }
                if (!formattedMobile.StartsWith("0"))
                {
                    formattedMobile = "0" + formattedMobile;
                }

                // Message body template: only first #var# is replaced with OTP
                // Template: "{#var#}is the OTP for{#var#}at BSNL{#var#}.The OTP is valid for{#var#}minutes.Do not share with anyone."
                // Only first #var# is variable (OTP), others are fixed: Login, MITRA PORTAL, 5
                var messageBody = $"{otp} is the OTP for Login at BSNL MITRA PORTAL.The OTP is valid for 5 minutes.Do not share with anyone.";

                HttpClientHandler clientHandler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                    UseProxy = false
                };

                using (var client = new HttpClient(clientHandler))
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var requestData = new
                    {
                        TransactionId = 10001,
                        Environment = "Production",
                        SourceProcess = "CRM",
                        MessageType = "SMS",
                        From = "BSNLSD",
                        To = formattedMobile,
                        PE_ID = peId,
                        TM_ID = tmId,
                        ZONE = zone,
                        SSA = ssa,
                        CIRCLE = circle,
                        MessageBody = messageBody
                    };

                    var smspostData = JsonConvert.SerializeObject(requestData);
                    StringContent content = new StringContent(smspostData, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(baseUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        // Return in the expected format for backward compatibility
                        return JsonConvert.SerializeObject(new { Message_Id = responseContent, Error = (string?)null });
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"SMS API call failed with status {response.StatusCode}: {errorContent}");
                        return JsonConvert.SerializeObject(new { Message_Id = (string?)null, Error = $"HTTP {response.StatusCode}: {errorContent}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending OTP SMS");
                return JsonConvert.SerializeObject(new { Message_Id = (string?)null, Error = ex.Message });
            }
        }
    }
}

