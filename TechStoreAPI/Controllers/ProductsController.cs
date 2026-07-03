using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _iService;
        private readonly IMapper _mapper;

        public ProductsController(IProductService productService, IMapper mapper)
        {
            _iService = productService;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductResponseDTO>>> GetProducts()
        {
            var products = await _iService.GetAllProductsAsync();
            var response = _mapper.Map<IEnumerable<ProductResponseDTO>>(products);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponseDTO>> GetProduct(Guid id)
        {
            var product = await _iService.GetProductByIdAsync(id);
            if (product == null) return NotFound("Sản phẩm không tồn tại");
            var response = _mapper.Map<ProductResponseDTO>(product);
            return Ok(response);
        }

        /// <summary>Kiểm tra đuôi file & dung lượng ảnh. Trả message lỗi, null = hợp lệ.</summary>
        private static string? ValidateImage(IFormFile? image)
        {
            if (image == null) return null;
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return "Ảnh phải là định dạng JPG, PNG hoặc WEBP";
            if (image.Length > 5 * 1024 * 1024)
                return "Ảnh tối đa 5MB";
            return null;
        }

        [HttpPost]
        //[Authorize(Roles = "Staff")]
        public async Task<ActionResult<ProductResponseDTO>> CreateProduct([FromForm] CreateProductDTO productDto)
        {
            var imageError = ValidateImage(productDto.Image);
            if (imageError != null) return BadRequest(imageError);

            var newProduct = _mapper.Map<Product>(productDto);
            var createdProduct = await _iService.CreateProductAsync(newProduct, productDto.Image);
            var response = _mapper.Map<ProductResponseDTO>(createdProduct);
            return Ok(response);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromForm] UpdateProductDTO productDto)
        {
            var imageError = ValidateImage(productDto.Image);
            if (imageError != null) return BadRequest(imageError);

            var existingProduct = await _iService.GetProductByIdAsync(id);
            if (existingProduct == null) return NotFound("Sản phẩm không tồn tại");

            _mapper.Map(productDto, existingProduct);
            await _iService.UpdateProductAsync(existingProduct, productDto.Image);
            return NoContent();
        }
    }
}
