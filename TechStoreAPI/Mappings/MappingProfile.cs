using System;
using AutoMapper;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;

namespace TechStoreAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // CreateMap<Nguồn, Đích>()

            // 1. Mapping từ DTO -> Entity (Dùng cho thao tác Create/Update)
            CreateMap<CreateProductDTO, Product>();
            CreateMap<CategoryDTO, Category>();

            // 2. Mapping từ Entity -> DTO (Dùng khi trả dữ liệu từ DB ra cho API)
            CreateMap<Category, CategoryResponseDTO>();
            CreateMap<Product, ProductResponseDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

            CreateMap<Order, OrderResponseDTO>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty));
            CreateMap<OrderDetail, OrderDetailResponseDTO>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Product != null ? src.Product.Brand : string.Empty))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Product != null ? src.Product.ImageUrl : null));

            CreateMap<Cart, CartResponseDTO>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src => src.Product != null ? src.Product.ImageUrl : null))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price * src.Quantity : 0));

            CreateMap<Notification, NotificationResponseDTO>()
                .ForMember(dest => dest.Unread, opt => opt.MapFrom(src => !src.IsRead))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon.ToString().ToLower()))
                .ForMember(dest => dest.Tone, opt => opt.MapFrom(src => src.Tone.ToString().ToLower()))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src => GetFriendlyTime(src.CreatedAt)));
        }

        private static string GetFriendlyTime(DateTime dateTime)
        {
            var elapsed = DateTime.UtcNow - dateTime;
            if (elapsed.TotalMinutes < 1) return "Vừa xong";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} phút trước";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} giờ trước";
            if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays} ngày trước";
            return dateTime.ToString("dd/MM/yyyy");
        }
    }

}
