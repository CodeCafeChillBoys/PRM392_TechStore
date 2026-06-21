using System;

namespace TechStore.Domain.Constants
{
    public static class EmailTemplates
    {
        public const string TwoFactorAuthSubject = "TechStore - Xác Thực Đăng Nhập 2 Bước";
        public const string LoginVerificationSubject = "TechStore - Yêu Cầu Xác Nhận Đăng Nhập";

        public static string GetTwoFactorAuthEmailBody(string fullName, string verifyLink, string otpCode)
        {
            return $@"
                <h3>Xin chào {fullName},</h3>
                <p>Bạn vừa thực hiện đăng nhập vào hệ thống TechStore. Vui lòng hoàn tất đăng nhập bằng cách:</p>
                <ol>
                    <li><strong>Nhấp vào liên kết sau để xác nhận phiên đăng nhập:</strong> <a href='{verifyLink}'>Kích hoạt đăng nhập</a></li>
                    <li><strong>Nhập mã OTP sau đây vào ứng dụng:</strong> <span style='font-size: 18px; font-weight: bold; color: #1e88e5;'>{otpCode}</span></li>
                </ol>
                <p>Mã OTP và liên kết xác thực này sẽ hết hạn trong 15 phút.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";
        }

        public static string GetLoginVerificationEmailBody(string fullName, string verifyLink)
        {
            return $@"
                <h3>Xin chào {fullName},</h3>
                <p>Bạn vừa thực hiện đăng nhập vào hệ thống TechStore.</p>
                <p>Vui lòng hoàn tất đăng nhập bằng cách nhấp vào liên kết sau để xác nhận phiên đăng nhập và nhận mã OTP:</p>
                <p><a href='{verifyLink}' style='display: inline-block; padding: 10px 20px; font-size: 16px; color: white; background-color: #1e88e5; text-decoration: none; border-radius: 5px;'>Kích hoạt đăng nhập</a></p>
                <p>Liên kết xác thực này sẽ hết hạn trong 15 phút.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";
        }

        public const string OtpSubject = "TechStore - Mã Xác Thực OTP";

        public static string GetOtpEmailBody(string fullName, string otpCode)
        {
            return $@"
                <h3>Xin chào {fullName},</h3>
                <p>Phiên đăng nhập của bạn đã được xác nhận liên kết thành công.</p>
                <p>Vui lòng nhập mã OTP sau đây vào ứng dụng để hoàn tất đăng nhập:</p>
                <div style='margin: 20px 0;'>
                    <span style='font-size: 24px; font-weight: bold; color: #1e88e5; letter-spacing: 2px;'>{otpCode}</span>
                </div>
                <p>Mã OTP này sẽ hết hạn trong 15 phút.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";
        }
    }
}
