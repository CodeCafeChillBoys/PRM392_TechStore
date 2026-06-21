using System;
using System.Linq;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;

namespace TechStore.Service.Service
{
    public partial class AuthenService
    {
        public async Task<ApiResponse<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var sessionResult = await GetAndValidateSessionAsync(request.VerifyToken, cleanUpIfExpired: true);
            if (!sessionResult.success)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = sessionResult.message
                };
            }
            var session = sessionResult.Data!;

            if (session.Status != "Pending")
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.SessionNotPending
                };
            }

            if (!session.IsOtpSent || session.OtpCode != request.OtpCode)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.InvalidOtp
                };
            }

            var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
            if (user == null)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.UserNotFound
                };
            }

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

            _unitOfWork.LoginSessions.Remove(session);
            await _unitOfWork.CompleteAsync();

            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = AuthMessages.LoginSuccess,
                Data = new LoginResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"] ?? "60")).TotalSeconds
                }
            };
        }

        public async Task<ApiResponse<LoginResponse>> GetSessionStatusAsync(string token)
        {
            var sessionResult = await GetAndValidateSessionAsync(token, cleanUpIfExpired: true);
            if (!sessionResult.success)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = sessionResult.message
                };
            }
            var session = sessionResult.Data!;

            if (session.Status == "Approved")
            {
                var response = new LoginResponse
                {
                    AccessToken = session.AccessToken ?? string.Empty,
                    RefreshToken = session.RefreshToken ?? string.Empty,
                    ExpiresIn = session.ExpiresIn
                };

                _unitOfWork.LoginSessions.Remove(session);
                await _unitOfWork.CompleteAsync();

                return new ApiResponse<LoginResponse>
                {
                    success = true,
                    message = AuthMessages.LoginSuccess,
                    Data = response
                };
            }

            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = "Pending",
                Data = null
            };
        }
    }
}
