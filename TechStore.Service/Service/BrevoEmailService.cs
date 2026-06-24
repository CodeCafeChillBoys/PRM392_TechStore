using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TechStore.Domain.Models;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class BrevoEmailService : IEmailService
    {
        private readonly BrevoSettings _brevoSettings;

        public BrevoEmailService(IOptions<BrevoSettings> brevoOptions)
        {
            _brevoSettings = brevoOptions.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
          
            if (string.IsNullOrWhiteSpace(_brevoSettings.ApiKey))
            {
                throw new InvalidOperationException("Brevo ApiKey is not configured.");
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("api-key", _brevoSettings.ApiKey);
                httpClient.DefaultRequestHeaders.Add("accept", "application/json");

                var payload = new
                {
                    sender = new
                    {
                        name = _brevoSettings.SenderName,
                        email = _brevoSettings.SenderEmail
                    },
                    to = new[]
                    {
                        new { email = toEmail }
                    },
                    subject = subject,
                    htmlContent = body
                };

                try
                {
                    var response = await httpClient.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Brevo API returned error status code: {response.StatusCode}. Content: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Gửi email qua Brevo API thất bại. Chi tiết: {ex.Message}", ex);
                }
            }
        }
    }
}
