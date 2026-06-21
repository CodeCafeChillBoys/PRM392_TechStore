using System;

namespace TechStore.Domain.Constants
{
    public static class AuthMessages
    {
        public const string InvalidRefreshToken = "Invalid refresh token";
        public const string RefreshTokenRevoked = "Refresh token has been revoked";
        public const string RefreshTokenExpired = "Refresh token expired";
        public const string TokenRefreshed = "Token refreshed successfully";
        
        public const string EmailAlreadyExists = "Email already exists";
        public const string UserRegistered = "User registered successfully";
        
        public const string InvalidCredentials = "Email hoặc mật khẩu không chính xác.";
        public const string AuthCodeSent = "Mã xác thực đã được gửi về email của bạn.";
        public const string LinkSent = "Liên kết xác thực đã được gửi về email của bạn.";
        
        public const string InvalidVerifyToken = "Mã xác thực link không hợp lệ.";
        public const string VerifyLinkExpired = "Liên kết xác thực đã hết hạn.";
        public const string VerifyLinkSuccess = "Liên kết xác thực email thành công.";
        
        public const string SessionNotFoundOrInvalid = "Phiên đăng nhập không tồn tại hoặc token không hợp lệ.";
        public const string SessionExpired = "Mã xác thực/Phiên đăng nhập đã hết hạn.";
        public const string EmailLinkNotVerified = "Bạn chưa hoàn tất xác nhận liên kết gửi trong hòm thư email.";
        public const string InvalidOtp = "Mã OTP không chính xác.";
        public const string UserNotFound = "Người dùng không tồn tại.";
        public const string LoginSuccess = "Đăng nhập thành công.";
        public const string SessionNotPending = "Phiên đăng nhập không còn ở trạng thái chờ xác thực.";
        public const string VerifyDeviceSuccess = "Xác thực thiết bị thành công.";
        public const string OtpSentSuccess = "Đã gửi mã OTP qua email.";
    }
}
