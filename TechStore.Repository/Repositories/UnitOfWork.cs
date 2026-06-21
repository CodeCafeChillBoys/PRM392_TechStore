using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        private IGenericRepository<LoginSession> _loginSessions;
        public IUserRepositories Users { get; }
        public IRefreshTokenRepositories RefreshTokens { get; }

        private IGenericRepository<UserDevice> _userDevices;

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

        public IGenericRepository<LoginSession> LoginSessions =>
             _loginSessions ??= new GenericRepository<LoginSession>(_context);

        public IGenericRepository<UserDevice> UserDevices =>
     _userDevices ??= new GenericRepository<UserDevice>(_context);


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
