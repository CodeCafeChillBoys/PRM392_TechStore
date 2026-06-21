namespace TechStore.Domain.Models
{
    public class BrevoSettings
    {
        public bool UseMock { get; set; } = true;
        public string ApiKey { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = "no-reply@techstore.com";
        public string SenderName { get; set; } = "TechStore";
    }
}
