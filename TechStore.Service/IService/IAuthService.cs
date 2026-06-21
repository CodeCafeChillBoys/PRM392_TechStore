

using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;

namespace TechStore.Service.IService
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<UserResponse>> RegisterAsync(CreateUserRequest request);




    }
}