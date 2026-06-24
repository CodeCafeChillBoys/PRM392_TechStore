namespace TechStore.Domain.DTOs.Response
{
    public class TwoFactorLoginResponse
    {
        public string VerifyToken { get; set; } = string.Empty;
        public string Message { get; set; } = "Hai-yếu-tố xác thực đã được kích hoạt. Hãy kiểm tra hòm thư của bạn.";
    }
}
