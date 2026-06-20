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
        IProductRepository Products { get; }
        IGenericRepository<Category> Categories { get; }
        IOrderRepository Orders { get; }
        Task<int> CompleteAsync();
    }
}
