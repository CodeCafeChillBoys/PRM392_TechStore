using Google.Apis.Auth;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;

namespace TechStore.Service.Service
{
    public partial class AuthenService
    {
        public async Task<ApiResponse<LoginResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleClientId = _configuration["Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.GoogleConfigMissing
                };
            }
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = $"{AuthMessages.GoogleInvalidToken} Chi tiết: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = $"{AuthMessages.GoogleAuthFailed} Chi tiết: {ex.Message}"
                };
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    FullName = payload.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = Role.Customer,
                    CreatedAt = DateTime.UtcNow,
                    Wallet = new Wallet
                    {
                        Balance = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CompleteAsync();
            }

            var accessToken = await _jwtService.GenerateToken(new UserRequest
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

            await _unitOfWork.CompleteAsync();

            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                var dummySession = new LoginSession
                {
                    DeviceId = request.DeviceId,
                    DeviceName = request.DeviceName,
                    DeviceType = request.DeviceType,
                    FcmToken = request.FcmToken
                };
                await HandleDeviceRegistrationAndNotificationAsync(user, dummySession);
            }

            var expiresIn = (int)TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"] ?? "60")).TotalSeconds;
            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = AuthMessages.LoginSuccess,
                Data = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = expiresIn
                }
            };
        }

    }
}
