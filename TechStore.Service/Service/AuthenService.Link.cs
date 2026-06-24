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
        public async Task<ApiResponse<LoginResponse>> VerifyEmailLinkAsync(string token)
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

            if (session.Status != "Pending")
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.SessionNotPending
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

            var expiresIn = (int)TimeSpan.FromMinutes(double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"] ?? "60")).TotalSeconds;

            await HandleDeviceRegistrationAndNotificationAsync(user, session);

            session.Status = "Approved";
            session.AccessToken = accessToken;
            session.RefreshToken = refreshToken;
            session.ExpiresIn = expiresIn;

            _unitOfWork.LoginSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = AuthMessages.VerifyDeviceSuccess,
                Data = new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = expiresIn
                }
            };
        }

        public async Task<ApiResponse<bool>> SendOtpTriggerAsync(string token)
        {
            var sessionResult = await GetAndValidateSessionAsync(token, cleanUpIfExpired: true);
            if (!sessionResult.success)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = sessionResult.message,
                    Data = false
                };
            }
            var session = sessionResult.Data!;

            if (session.Status != "Pending")
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.SessionNotPending,
                    Data = false
                };
            }

            var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.UserNotFound,
                    Data = false
                };
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            session.OtpCode = otpCode;
            session.IsOtpSent = true;

            var emailBody = EmailTemplates.GetOtpEmailBody(user.FullName, otpCode);
            await _emailService.SendEmailAsync(user.Email, EmailTemplates.OtpSubject, emailBody);

            _unitOfWork.LoginSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            return new ApiResponse<bool>
            {
                success = true,
                message = AuthMessages.OtpSentSuccess,
                Data = true
            };
        }

        public async Task<ApiResponse<bool>> SendVerifyEmailLinkAsync(string token)
        {
            var sessionResult = await GetAndValidateSessionAsync(token, cleanUpIfExpired: true);
            if (!sessionResult.success)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = sessionResult.message,
                    Data = false
                };
            }
            var session = sessionResult.Data!;

            if (session.Status != "Pending")
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.SessionNotPending,
                    Data = false
                };
            }

            var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
            if (user == null)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.UserNotFound,
                    Data = false
                };
            }

            var apiDomain = _configuration.GetSection("Jwt")["Issuer"] ?? "http://localhost:5173";
            var verifyEmailLink = $"{apiDomain}/api/auth/verify-email-link?token={token}";

            var emailBody = EmailTemplates.GetLoginVerificationLinkEmailBody(user.FullName, verifyEmailLink);
            await _emailService.SendEmailAsync(user.Email, EmailTemplates.LoginVerificationSubject, emailBody);

            return new ApiResponse<bool>
            {
                success = true,
                message = AuthMessages.EmailLinkSentSuccess,
                Data = true
            };
        }
    }
}
