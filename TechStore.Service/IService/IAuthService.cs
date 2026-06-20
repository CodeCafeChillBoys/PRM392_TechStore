using TechStore.Service.DTO.Request;
using TechStore.Service.DTO.Respone;

namespace TechStore.Service.IService
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ApiResponse<UserResponse>> RegisterAsync(CreateUserRequest request);




    }
}