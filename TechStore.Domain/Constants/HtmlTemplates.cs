using System;

namespace TechStore.Domain.Constants
{
    public static class HtmlTemplates
    {
        public static string GetVerifyDeviceFailedPage()
        {
            return @"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; margin-top: 100px; background-color: #0f172a; color: #f8fafc; padding: 40px; border-radius: 12px; max-width: 500px; margin-left: auto; margin-right: auto; border: 1px solid #334155; box-shadow: 0 10px 25px rgba(0,0,0,0.3);"">
    <h3 style=""color: #f43f5e; font-size: 22px; margin-bottom: 10px;"">Xác Thực Thiết Bị Thất Bại</h3>
    <p style=""color: #94a3b8; font-size: 15px;"">Liên kết xác thực không hợp lệ, đã hết hạn, hoặc đã được sử dụng.</p>
    <div style=""margin-top: 25px;"">
        <a href=""techstore://login-failed"" style=""display: inline-block; background-color: #f43f5e; color: white; padding: 10px 20px; border-radius: 6px; text-decoration: none; font-weight: 600; font-size: 14px;"">Quay lại ứng dụng</a>
    </div>
</div>";
        }

        public static string GetSendOtpFailedPage()
        {
            return @"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; margin-top: 100px; background-color: #0f172a; color: #f8fafc; padding: 40px; border-radius: 12px; max-width: 500px; margin-left: auto; margin-right: auto; border: 1px solid #334155; box-shadow: 0 10px 25px rgba(0,0,0,0.3);"">
    <h3 style=""color: #f43f5e; font-size: 22px; margin-bottom: 10px;"">Yêu Cầu Gửi OTP Thất Bại</h3>
    <p style=""color: #94a3b8; font-size: 15px;"">Liên kết không hợp lệ, đã hết hạn hoặc phiên đăng nhập đã đóng.</p>
</div>";
        }
    }
}
