using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Repository.IRepositories;

namespace TechStore.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IProductRepository _products;
        private IGenericRepository<Category> _categories;
        private IOrderRepository _orders;
        private IGenericRepository<TwoFactorSession> _twoFactorSessions;
        public IUserRepositories Users { get; }
        public IRefreshTokenRepositories RefreshTokens { get; }

        public UnitOfWork(ApplicationDbContext context, IUserRepositories users, IRefreshTokenRepositories refreshTokens)
        {
            _context = context;
            Users = users;
            RefreshTokens = refreshTokens;
        }

        public IProductRepository Products =>
            _products ??= new ProductRepository(_context);

        public IGenericRepository<Category> Categories =>
             _categories ??= new GenericRepository<Category>(_context);

        public IOrderRepository Orders =>
             _orders ??= new OrderRepository(_context);

        public IGenericRepository<TwoFactorSession> TwoFactorSessions =>
             _twoFactorSessions ??= new GenericRepository<TwoFactorSession>(_context);


        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
