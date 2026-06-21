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

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<ProductResponseDTO>> CreateProduct([FromBody] CreateProductDTO productDto)
        {
            var newProduct = _mapper.Map<Product>(productDto);
            var createdProduct = await _iService.CreateProductAsync(newProduct);
            var response = _mapper.Map<ProductResponseDTO>(createdProduct);
            return Ok(response);
        }
    }
}
