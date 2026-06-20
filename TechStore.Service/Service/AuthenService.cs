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
    public class AuthenService : IAuthService
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

            var otpCode = new Random().Next(100000, 999999).ToString();
            var verifyToken = Guid.NewGuid().ToString();

            var session = new TwoFactorSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OtpCode = otpCode,
                VerifyToken = verifyToken,
                IsOtpVerified = false,
                IsLinkVerified = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(15)
            };

            await _unitOfWork.TwoFactorSessions.AddAsync(session);
            await _unitOfWork.CompleteAsync();

            var apiDomain = _configuration.GetSection("Jwt")["Issuer"] ?? "http://localhost:5173";
            var verifyLink = $"{apiDomain}/api/auth/verify-link?token={verifyToken}";

            var emailBody = EmailTemplates.GetLoginVerificationEmailBody(user.FullName, verifyLink);

            await _emailService.SendEmailAsync(user.Email, EmailTemplates.LoginVerificationSubject, emailBody);

            return new ApiResponse<TwoFactorLoginResponse>
            {
                success = true,
                message = AuthMessages.LinkSent,
                Data = new TwoFactorLoginResponse { VerifyToken = verifyToken }
            };
        }

        public async Task<ApiResponse<bool>> VerifyEmailLinkAsync(string verifyToken)
        {
            var sessions = await _unitOfWork.TwoFactorSessions.FindAsync(s => s.VerifyToken == verifyToken);
            var session = sessions.FirstOrDefault();

            if (session == null)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.InvalidVerifyToken,
                    Data = false
                };
            }

            if (session.ExpiredAt < DateTime.UtcNow)
            {
                return new ApiResponse<bool>
                {
                    success = false,
                    message = AuthMessages.VerifyLinkExpired,
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

            // Gửi email thứ 2 chứa mã OTP sau khi click link
            var emailBody = EmailTemplates.GetOtpEmailBody(user.FullName, session.OtpCode);
            await _emailService.SendEmailAsync(user.Email, EmailTemplates.OtpSubject, emailBody);

            session.IsLinkVerified = true;
            _unitOfWork.TwoFactorSessions.Update(session);
            await _unitOfWork.CompleteAsync();

            return new ApiResponse<bool>
            {
                success = true,
                message = AuthMessages.VerifyLinkSuccess,
                Data = true
            };
        }

        public async Task<ApiResponse<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var sessions = await _unitOfWork.TwoFactorSessions.FindAsync(s => s.VerifyToken == request.VerifyToken);
            var session = sessions.FirstOrDefault();

            if (session == null)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.SessionNotFoundOrInvalid
                };
            }

            if (session.ExpiredAt < DateTime.UtcNow)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.SessionExpired
                };
            }

            if (!session.IsLinkVerified)
            {
                return new ApiResponse<LoginResponse>
                {
                    success = false,
                    message = AuthMessages.EmailLinkNotVerified
                };
            }

            if (session.OtpCode != request.OtpCode)
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

            _unitOfWork.TwoFactorSessions.Remove(session);
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
    }
}