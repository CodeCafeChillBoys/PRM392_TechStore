using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Service.DTO.Respone
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }
    }
}