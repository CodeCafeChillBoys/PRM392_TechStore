using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public ProductService(IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            // Gửi thông báo sản phẩm mới đến tất cả các thiết bị trong hệ thống
            await _notificationService.BroadcastNotificationAsync(
              new NotificationRequest
              {
                  Title = $"✨ Siêu phẩm mới: {product.Name}",
                  Body = $"Sản phẩm {product.Name} thuộc thương hiệu {product.Brand} đã chính thức có mặt tại TechStore. Khám phá ngay!",
                  Type = NotificationType.Promo,
                  Icon = NotificationIcon.ShoppingBag,
                  Tone = NotificationTone.Accent
              }
            );

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
