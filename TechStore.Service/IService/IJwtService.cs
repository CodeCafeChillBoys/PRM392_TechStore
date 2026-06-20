using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Service.DTO.Request;

namespace TechStore.Service.IService
{
    public interface IJwtService
    {

        Task<string> GenerateToken(UserRequest user);
        string GenerateRefreshToken();
    }
}