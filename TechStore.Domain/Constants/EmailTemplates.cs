using System;

namespace TechStore.Domain.Constants
{
    public static class EmailTemplates
    {
        public const string LoginVerificationSubject = "TechStore - Yêu Cầu Xác Nhận Đăng Nhập";
        public const string OtpSubject = "TechStore - Mã Xác Thực OTP";

        public static string GetOtpEmailBody(string fullName, string otpCode)
        {
            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; background-color: #0f172a; color: #f8fafc; border-radius: 16px; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3); border: 1px solid #1e293b;"">
    <div style=""text-align: center; margin-bottom: 30px;"">
        <h2 style=""color: #f59e0b; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">TechStore</h2>
        <p style=""color: #94a3b8; font-size: 14px; margin-top: 5px;"">Mã xác thực OTP của bạn</p>
    </div>
    
    <div style=""background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding: 25px; border-radius: 12px; border: 1px solid #334155; margin-bottom: 30px; text-align: center;"">
        <h3 style=""color: #f1f5f9; margin-top: 0; font-size: 18px; font-weight: 600;"">Mã xác thực đăng nhập</h3>
        <p style=""color: #cbd5e1; line-height: 1.6; font-size: 15px; margin-bottom: 20px;"">Vui lòng nhập mã OTP dưới đây vào ứng dụng để hoàn tất quá trình đăng nhập:</p>
        
        <div style=""display: inline-block; background-color: #1e293b; border: 2px dashed #f59e0b; padding: 15px 40px; border-radius: 12px; margin: 10px 0;"">
            <span style=""font-size: 32px; font-weight: bold; color: #f59e0b; letter-spacing: 5px;"">{otpCode}</span>
        </div>
    </div>

    <div style=""text-align: center; border-top: 1px solid #334155; padding-top: 20px; color: #64748b; font-size: 13px;"">
        <p>Mã OTP này sẽ hết hạn sau 15 phút.</p>
        <p style=""margin-bottom: 0;"">Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email hoặc đổi mật khẩu tài khoản của bạn để bảo mật.</p>
    </div>
</div>";
        }

        public static string GetLoginVerificationLinkEmailBody(string fullName, string verifyDeviceLink)
        {
            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; background-color: #0f172a; color: #f8fafc; border-radius: 16px; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.3); border: 1px solid #1e293b;"">
    <div style=""text-align: center; margin-bottom: 30px;"">
        <h2 style=""color: #38bdf8; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">TechStore</h2>
        <p style=""color: #94a3b8; font-size: 14px; margin-top: 5px;"">Xác thực yêu cầu đăng nhập</p>
    </div>
    
    <div style=""background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%); padding: 25px; border-radius: 12px; border: 1px solid #334155; margin-bottom: 30px;"">
        <h3 style=""color: #f1f5f9; margin-top: 0; font-size: 18px; font-weight: 600;"">Xin chào {fullName},</h3>
        <p style=""color: #cbd5e1; line-height: 1.6; font-size: 15px;"">Chúng tôi đã nhận được yêu cầu xác thực đăng nhập qua liên kết của bạn. Vui lòng bấm vào nút bên dưới để xác nhận thiết bị và hoàn tất đăng nhập:</p>
    </div>

    <div style=""margin-bottom: 30px; text-align: center;"">
        <a href=""{verifyDeviceLink}"" style=""display: inline-block; background: linear-gradient(90deg, #0284c7 0%, #0369a1 100%); color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(2, 132, 199, 0.3);"">Xác thực thiết bị</a>
    </div>

    <div style=""text-align: center; border-top: 1px solid #334155; padding-top: 20px; color: #64748b; font-size: 13px;"">
        <p>Liên kết xác thực này sẽ hết hạn trong 15 phút.</p>
        <p style=""margin-bottom: 0;"">Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email để đảm bảo an toàn cho tài khoản.</p>
    </div>
</div>";
        }
    }
}
