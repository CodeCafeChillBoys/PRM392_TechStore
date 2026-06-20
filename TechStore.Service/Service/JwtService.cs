using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TechStore.Domain.DTOs.Request;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;


        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                // Base64UrlEncode là lựa chọn tốt hơn cho URL (không dùng +, /)
                return Convert.ToBase64String(randomNumber).Replace('+', '-').Replace('/', '_').Replace("=", "");
            }
        }

        public async Task<string> GenerateToken(UserRequest user)
        {
            // 1. Lấy cấu hình Secret Key
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            // 2. Định nghĩa Claims (Thông tin người dùng muốn lưu trong token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // 3. Tạo Security Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60")),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}