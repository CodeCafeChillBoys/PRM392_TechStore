using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechStore.Domain.Models;

namespace TechStore.Repository.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepositories Users { get; }
        IRefreshTokenRepositories RefreshTokens { get; }
        IProductRepository Products { get; }
        IGenericRepository<Category> Categories { get; }
        Task<int> CompleteAsync();
    }
}
