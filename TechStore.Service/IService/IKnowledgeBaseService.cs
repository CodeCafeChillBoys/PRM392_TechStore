using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.Models;
using TechStore.Domain.DTOs.Chat;

namespace TechStore.Service.IService
{
    public interface IKnowledgeBaseService
    {
        Task<IEnumerable<KnowledgeItem>> GetAllKnowledgeAsync();
        Task<KnowledgeItem> AddKnowledgeAsync(KnowledgeUpdateRequest request);
        Task<bool> DeleteKnowledgeAsync(int id);
    }
}
