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
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public ProductsController(IProductService productService, ICategoryService categoryService, IMapper mapper)
        {
            _iService = productService;
            _categoryService = categoryService;
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

        /// <summary>Chữ ký byte đầu file cho từng đuôi ảnh hợp lệ.</summary>
        private static readonly Dictionary<string, byte[]> _imageSignatures = new()
        {
            [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            [".webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF
        };

        /// <summary>
        /// Kiểm tra đuôi file, dung lượng và byte đầu (magic bytes) — chặn
        /// file giả mạo đuôi ảnh. Trả message lỗi, null = hợp lệ.
        /// </summary>
        private static async Task<string?> ValidateImageAsync(IFormFile? image)
        {
            if (image == null) return null;
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!_imageSignatures.TryGetValue(ext, out var signature))
                return "Ảnh phải là định dạng JPG, PNG hoặc WEBP";
            if (image.Length > 5 * 1024 * 1024)
                return "Ảnh tối đa 5MB";
            var header = new byte[signature.Length];
            using (var stream = image.OpenReadStream())
            {
                var read = await stream.ReadAsync(header, 0, header.Length);
                if (read < signature.Length) return "File không phải ảnh hợp lệ";
            }
            if (!header.SequenceEqual(signature))
                return "File không phải ảnh hợp lệ";
            return null;
        }

        [HttpPost]
        //[Authorize(Roles = "Staff")]
        public async Task<ActionResult<ProductResponseDTO>> CreateProduct([FromForm] CreateProductDTO productDto)
        {
            var imageError = await ValidateImageAsync(productDto.Image);
            if (imageError != null) return BadRequest(imageError);

            var category = await _categoryService.GetCategoryByIdAsync(productDto.CategoryId);
            if (category == null) return BadRequest("Danh mục không hợp lệ");

            var newProduct = _mapper.Map<Product>(productDto);
            var createdProduct = await _iService.CreateProductAsync(newProduct, productDto.Image);
            var response = _mapper.Map<ProductResponseDTO>(createdProduct);
            return Ok(response);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromForm] UpdateProductDTO productDto)
        {
            var imageError = await ValidateImageAsync(productDto.Image);
            if (imageError != null) return BadRequest(imageError);

            var category = await _categoryService.GetCategoryByIdAsync(productDto.CategoryId);
            if (category == null) return BadRequest("Danh mục không hợp lệ");

            var existingProduct = await _iService.GetProductByIdAsync(id);
            if (existingProduct == null) return NotFound("Sản phẩm không tồn tại");

            _mapper.Map(productDto, existingProduct);
            await _iService.UpdateProductAsync(existingProduct, productDto.Image);
            return NoContent();
        }
    }
}
