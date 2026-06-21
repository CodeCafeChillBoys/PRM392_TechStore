using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;   
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();
            return product;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            // Thay vì dùng GetAllAsync() mặc định, hãy gọi hàm riêng của ProductRepository để lấy luôn thông tin Category
            return await _unitOfWork.Products.GetProductsWithCategoryAsync();
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _unitOfWork.Products.GetByIdAsync(id);
        }
    }
}
