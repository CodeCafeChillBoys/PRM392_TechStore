using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoryController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CategoryResponseDTO>>> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var response = _mapper.Map<IEnumerable<CategoryResponseDTO>>(categories);
            return Ok(response);
        }

        [HttpGet("{id}")] 
        [AllowAnonymous]
        public async Task<ActionResult<CategoryResponseDTO>> GetCategory(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound("Danh mục không tồn tại");
            var response = _mapper.Map<CategoryResponseDTO>(category);
            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<CategoryResponseDTO>> CreateCategory(CategoryDTO categoryDto)
        {
            var newCategory = _mapper.Map<Category>(categoryDto);
            var createdCategory = await _categoryService.CreateCategoryAsync(newCategory);
            var response = _mapper.Map<CategoryResponseDTO>(createdCategory);
            return Ok(response);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateCategory(Guid id, CategoryDTO categoryDto)
        {
            var existingCategory = await _categoryService.GetCategoryByIdAsync(id);
            if (existingCategory == null) return NotFound("Danh mục không tồn tại");

            _mapper.Map(categoryDto, existingCategory);

            await _categoryService.UpdateCategoryAsync(existingCategory);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound("Danh mục không tồn tại");

            await _categoryService.RemoveCategoryAsync(category);
            return NoContent();
        }
    }
}