using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetAllAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(Guid id)
        {
            return await _unitOfWork.Categories.GetByIdAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.CompleteAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _unitOfWork.Categories.Update(category);
            await _unitOfWork.CompleteAsync();
        }

        public async Task RemoveCategoryAsync(Category category)
        {
            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.CompleteAsync();
        }
    }
}
