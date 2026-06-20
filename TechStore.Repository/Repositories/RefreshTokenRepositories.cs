using Microsoft.EntityFrameworkCore;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Repository.IRepositories;

namespace TechStore.Repository.Repositories
{
    public class RefreshTokenRepositories : IRefreshTokenRepositories
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepositories(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == token);
        }


        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }
    }
}