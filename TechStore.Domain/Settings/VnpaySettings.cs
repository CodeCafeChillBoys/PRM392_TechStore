namespace TechStore.Domain.Settings
{
    /// <summary>
    /// VNPay configuration — bound from appsettings.json → "VnpaySettings".
    /// </summary>
    public class VnpaySettings
    {
        public const string SectionName = "VnpaySettings";

        /// <summary>Terminal ID provided by VNPay (vnp_TmnCode).</summary>
        public string TmnCode { get; set; } = string.Empty;

        /// <summary>Secret key for HMAC-SHA512 signature (vnp_HashSecret).</summary>
        public string HashSecret { get; set; } = string.Empty;

        /// <summary>VNPay payment gateway URL.</summary>
        public string PaymentUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL VNPay redirects the user to after payment.
        /// Should match the Return URL configured in VNPay merchant portal.
        /// </summary>
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
