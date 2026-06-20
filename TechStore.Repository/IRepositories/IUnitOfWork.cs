using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechStore.Repository.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepositories Users { get; }
        IRefreshTokenRepositories RefreshTokens { get; }
        public Task<int> SaveChangesAsync();
    }
}
