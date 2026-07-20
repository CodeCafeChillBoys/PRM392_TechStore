using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Product> CreateProductAsync(Product product, IFormFile? image);
        Task<Product?> UpdateProductAsync(Product product, IFormFile? image);
    }
}
