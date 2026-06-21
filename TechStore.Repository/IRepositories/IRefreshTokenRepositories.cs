using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Domain.Models;

namespace TechStore.Repository.IRepositories
{
    public interface IRefreshTokenRepositories
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task AddAsync(RefreshToken token);
    }
}