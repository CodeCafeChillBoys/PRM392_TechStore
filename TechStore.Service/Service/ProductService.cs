using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        public async Task<Product> CreateProductAsync(Product product, IFormFile? image)
        {
            if (image != null)
            {
                product.ImageUrl = await SaveProductImageAsync(image);
            }

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

        public async Task<Product?> UpdateProductAsync(Product product, IFormFile? image)
        {
            if (image != null)
            {
                product.ImageUrl = await SaveProductImageAsync(image);
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.CompleteAsync();
            return product;
        }

        /// <summary>
        /// Lưu ảnh sản phẩm vào wwwroot/uploads/products/ — cùng cách
        /// OrderService.ConfirmDeliveryAsync lưu ảnh xác nhận giao hàng.
        /// </summary>
        private static async Task<string> SaveProductImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/uploads/products/{uniqueFileName}";
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
