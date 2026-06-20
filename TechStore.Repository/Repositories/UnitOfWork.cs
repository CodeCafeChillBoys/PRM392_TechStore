using TechStore.Repository.Data;
using TechStore.Repository.IRepositories;

namespace TechStore.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IUserRepositories Users { get; }

        public IRefreshTokenRepositories RefreshTokens { get; }

        public UnitOfWork(ApplicationDbContext context, IUserRepositories users, IRefreshTokenRepositories refreshTokens)
        {
            _context = context;
            Users = users;
            RefreshTokens = refreshTokens;
        }


        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
