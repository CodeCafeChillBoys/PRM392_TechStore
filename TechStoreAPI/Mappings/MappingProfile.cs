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
        }
    }

}
