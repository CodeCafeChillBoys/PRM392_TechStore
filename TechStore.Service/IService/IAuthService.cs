

using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;

namespace TechStore.Service.IService
{
    public interface IAuthService
    {
        Task<ApiResponse<TwoFactorLoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<UserResponse>> RegisterAsync(CreateUserRequest request);
        Task<ApiResponse<bool>> VerifyEmailLinkAsync(string verifyToken);
        Task<ApiResponse<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request);
    }
}