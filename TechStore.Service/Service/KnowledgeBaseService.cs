using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.Models;
using TechStore.Domain.DTOs.Chat;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public KnowledgeBaseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<KnowledgeItem>> GetAllKnowledgeAsync()
        {
            return await _unitOfWork.KnowledgeItems.GetAllAsync();
        }

        public async Task<KnowledgeItem> AddKnowledgeAsync(KnowledgeUpdateRequest request)
        {
            var item = new KnowledgeItem
            {
                Category = request.Category,
                Content = request.Content
            };
            
            await _unitOfWork.KnowledgeItems.AddAsync(item);
            await _unitOfWork.CompleteAsync();
            return item;
        }

        public async Task<bool> DeleteKnowledgeAsync(int id)
        {
            var item = await _unitOfWork.KnowledgeItems.GetByIdAsync(id);
            if (item == null) return false;
            
            _unitOfWork.KnowledgeItems.Remove(item);
            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}
