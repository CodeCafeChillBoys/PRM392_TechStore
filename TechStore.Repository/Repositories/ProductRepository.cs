using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Repository.IRepositories;

namespace TechStore.Repository.Repositories
{
     
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetProductsByBrandAsync(string brand)
        {
            return await _context.Products
                .Where(p => p.Brand.ToLower() == brand.ToLower())
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Specifications)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> QueryProductsAsync(
            string? keyword, 
            string? brand, 
            string? categoryName, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? sortBy, 
            bool descending, 
            int limit)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                // Sử dụng EF.Functions.Like hoặc Contains hỗ trợ dịch SQL tùy hệ quản trị
                query = query.Where(p => p.Name.ToLower().Contains(keyword.ToLower()));
            }
            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand.ToLower() == brand.ToLower());
            }
            if (!string.IsNullOrEmpty(categoryName))
            {
                query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == categoryName.ToLower());
            }
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                if (sortBy.Equals("price", StringComparison.OrdinalIgnoreCase))
                {
                    query = descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price);
                }
                else if (sortBy.Equals("stock", StringComparison.OrdinalIgnoreCase))
                {
                    query = descending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity);
                }
                else if (sortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase))
                {
                    query = descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                }
            }

            return await query.Take(limit).ToListAsync();
        }
    }
}
