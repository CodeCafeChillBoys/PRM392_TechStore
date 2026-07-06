

using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;

namespace TechStore.Service.IService
{
    public interface IAuthService
    {
        Task<ApiResponse<TwoFactorLoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<UserResponse>> RegisterAsync(CreateUserRequest request);
        Task<ApiResponse<UserResponse>> SetUserRoleAsync(string email, string role);
        Task<ApiResponse<LoginResponse>> VerifyEmailLinkAsync(string token);
        Task<ApiResponse<bool>> SendVerifyEmailLinkAsync(string token);
        Task<ApiResponse<bool>> SendOtpTriggerAsync(string token);
        Task<ApiResponse<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request);
        Task<ApiResponse<LoginResponse>> GetSessionStatusAsync(string token);
        Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    }
}