using System.Threading.Tasks;
using TechStore.Domain.DTOs.Chat;

namespace TechStore.Service.IService
{
    public interface IGeminiService
    {
        Task<ChatResponse> SendMessageAsync(ChatRequest request);
    }
}
