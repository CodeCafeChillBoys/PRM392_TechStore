using Microsoft.Extensions.Configuration;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Respone;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class AuthenService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthenService(IUnitOfWork unitOfWork, IJwtService jwtService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _configuration = configuration;
        }


        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.PassWord, user.PasswordHash)) return null;
            var token = await _jwtService.GenerateToken(new UserRequest
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(double.Parse(_configuration.GetSection("Jwt")["RefreshTokenExpirationDays"] ?? "7")),
                IsRevoked = false
            });
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = "Login successful",
                Data = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"] ?? "60")).TotalSeconds
                }
            };
        }

        public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var existingToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
            if (existingToken == null)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = "Invalid refresh token"
                };
            }
            if (existingToken.IsRevoked)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = "Refresh token has been revoked"
                };
            }

            if (existingToken.ExpiryDate < DateTime.UtcNow)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = "Refresh token expired"
                };
            }

            var user = existingToken.User; // lấy ra obj đang sở hữu cái resfeshToken
            var accessToken = await _jwtService.GenerateToken(new UserRequest
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role
            });

            existingToken.IsRevoked = true;

            var newRefreshToken = _jwtService.GenerateRefreshToken();
            await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(double.Parse(_configuration.GetSection("Jwt")["RefreshTokenExpirationDays"] ?? "7")),
                IsRevoked = false
            });
            await _unitOfWork.SaveChangesAsync();
            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = "Token refreshed successfully",
                Data = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = (int)TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"] ?? "60")).TotalSeconds
                }
            };
        }

        public async Task<ApiResponse<UserResponse>> RegisterAsync(CreateUserRequest request)
        {
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ApiResponse<UserResponse>
                {
                    success = false,
                    message = "Email already exists"
                };
            }

            var fullName = string.IsNullOrWhiteSpace(request.FullName)
                ? request.Email.Split('@')[0]
                : request.FullName;

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<UserResponse>
            {
                success = true,
                message = "User registered successfully",
                Data = new UserResponse
                {
                    UserId = newUser.Id,
                    Username = newUser.FullName,
                    Role = newUser.Role.ToString(),
                    IsActive = true,
                    CreatedAt = newUser.CreatedAt
                }
            };
        }
    }
}