using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechStore.Domain.Models
{
    public class TwoFactorSession
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string OtpCode { get; set; } = string.Empty;

        public string VerifyToken { get; set; } = string.Empty;

        public bool IsOtpVerified { get; set; }

        public bool IsLinkVerified { get; set; }

        public DateTime ExpiredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
    }
}