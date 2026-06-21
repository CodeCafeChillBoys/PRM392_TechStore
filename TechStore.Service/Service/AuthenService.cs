using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public partial class AuthenService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthenService(IUnitOfWork unitOfWork, IJwtService jwtService, IConfiguration configuration, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var existingToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken);
            if (existingToken == null)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.InvalidRefreshToken
                };
            }
            if (existingToken.IsRevoked)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.RefreshTokenRevoked
                };
            }

            if (existingToken.ExpiryDate < DateTime.UtcNow)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.RefreshTokenExpired
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
            await _unitOfWork.CompleteAsync();
            return new ApiResponse<LoginResponse>
            {
                success = true,
                message = AuthMessages.TokenRefreshed,
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
                    message = AuthMessages.EmailAlreadyExists
                };
            }

            var fullName = string.IsNullOrWhiteSpace(request.FullName)
                ? request.Email.Split('@')[0]  // nếu null or "" thì cắt email lấy phía trc
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
            await _unitOfWork.CompleteAsync();

            return new ApiResponse<UserResponse>
            {
                success = true,
                message = AuthMessages.UserRegistered,
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

        public async Task<ApiResponse<TwoFactorLoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.PassWord, user.PasswordHash))
            {
                return new ApiResponse<TwoFactorLoginResponse>
                {
                    success = false,
                    message = AuthMessages.InvalidCredentials
                };
            }

            var verifyToken = Guid.NewGuid().ToString();

            var session = new LoginSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                VerifyToken = verifyToken,
                Status = "Pending",
                IsOtpSent = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(15)
            };

            await _unitOfWork.LoginSessions.AddAsync(session);
            await _unitOfWork.CompleteAsync();

            var apiDomain = _configuration.GetSection("Jwt")["Issuer"] ?? "http://localhost:5173";
            var verifyDeviceLink = $"{apiDomain}/api/auth/verify-device?token={verifyToken}";
            var sendOtpLink = $"{apiDomain}/api/auth/send-otp?token={verifyToken}";

            var emailBody = EmailTemplates.GetLoginVerificationOptionsEmailBody(user.FullName, verifyDeviceLink, sendOtpLink);

            await _emailService.SendEmailAsync(user.Email, EmailTemplates.LoginVerificationSubject, emailBody);

            return new ApiResponse<TwoFactorLoginResponse>
            {
                success = true,
                message = AuthMessages.LinkSent,
                Data = new TwoFactorLoginResponse { VerifyToken = verifyToken }
            };
        }


        private async Task<ApiResponse<LoginSession>> GetAndValidateSessionAsync(string token, bool cleanUpIfExpired = false)
        {
            var sessions = await _unitOfWork.LoginSessions.FindAsync(s => s.VerifyToken == token);
            var session = sessions.FirstOrDefault();

            if (session == null)
            {
                return new ApiResponse<LoginSession>
                {
                    success = false,
                    message = AuthMessages.SessionNotFoundOrInvalid
                };
            }

            if (session.ExpiredAt < DateTime.UtcNow)
            {
                if (cleanUpIfExpired)
                {
                    _unitOfWork.LoginSessions.Remove(session);
                    await _unitOfWork.CompleteAsync();
                }
                return new ApiResponse<LoginSession>
                {
                    success = false,
                    message = AuthMessages.SessionExpired
                };
            }

            return new ApiResponse<LoginSession>
            {
                success = true,
                Data = session
            };
        }
    }
}