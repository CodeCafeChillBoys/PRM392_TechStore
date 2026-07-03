using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TechStore.Domain.DTOs.Chat;
using TechStore.Service.IService;
using TechStore.Repository.IRepositories;
using System.Linq;

namespace TechStore.Service.Service
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IUnitOfWork _unitOfWork;

        public GeminiService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            IKnowledgeBaseService knowledgeBaseService, 
            IUnitOfWork unitOfWork)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _knowledgeBaseService = knowledgeBaseService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
        {
            var apiKey = _configuration["GeminiApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var knowledges = await _knowledgeBaseService.GetAllKnowledgeAsync();
            var systemInstruction = "Bạn là trợ lý AI cho TechStore. Hãy trả lời câu hỏi của khách hàng thật lịch sự.\n" 
                                    + "Thông tin cửa hàng:\n"
                                    + string.Join("\n", knowledges.Select(k => $"- {k.Category}: {k.Content}"));

            var payload = new JsonObject
            {
                ["systemInstruction"] = new JsonObject
                {
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = systemInstruction } }
                },
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray { new JsonObject { ["text"] = request.Message } }
                    }
                },
                ["tools"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["functionDeclarations"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["name"] = "search_product",
                                ["description"] = "Tìm kiếm thông tin sản phẩm trong database theo từ khóa (keyword).",
                                ["parameters"] = new JsonObject
                                {
                                    ["type"] = "OBJECT",
                                    ["properties"] = new JsonObject
                                    {
                                        ["keyword"] = new JsonObject
                                        {
                                            ["type"] = "STRING",
                                            ["description"] = "Từ khóa tìm kiếm (ví dụ: iPhone, Samsung)"
                                        }
                                    },
                                    ["required"] = new JsonArray { "keyword" }
                                }
                            }
                        }
                    }
                }
            };

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new ChatResponse { Reply = $"Xin lỗi, tôi không thể xử lý yêu cầu lúc này (Lỗi kết nối API 1). Details: {err}" };
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(jsonString);
            var part = json?["candidates"]?[0]?["content"]?["parts"]?[0];

            if (part?["functionCall"] != null)
            {
                var functionCall = part["functionCall"];
                var functionName = functionCall["name"]?.ToString();
                
                if (functionName == "search_product")
                {
                    var keyword = functionCall["args"]?["keyword"]?.ToString() ?? "";
                    
                    // Thực hiện truy vấn DB
                    var products = await _unitOfWork.Products.GetAllAsync();
                    var searchResults = products.Where(p => p.Name.ToLower().Contains(keyword.ToLower()))
                                                .Select(p => new { name = p.Name, price = p.Price, stock = p.StockQuantity })
                                                .ToList();

                    // Chuẩn bị payload lần 2 gửi lại Gemini
                    var secondPayload = new JsonObject
                    {
                        ["systemInstruction"] = payload["systemInstruction"]!.DeepClone(),
                        ["contents"] = new JsonArray
                        {
                            new JsonObject // User msg
                            {
                                ["role"] = "user",
                                ["parts"] = new JsonArray { new JsonObject { ["text"] = request.Message } }
                            },
                            new JsonObject // Model's function call msg
                            {
                                ["role"] = "model",
                                ["parts"] = new JsonArray { new JsonObject { ["functionCall"] = functionCall!.DeepClone() } }
                            },
                            new JsonObject // Function response msg
                            {
                                ["role"] = "function",
                                ["parts"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["functionResponse"] = new JsonObject
                                        {
                                            ["name"] = "search_product",
                                            ["response"] = new JsonObject
                                            {
                                                ["name"] = "search_product",
                                                ["content"] = JsonSerializer.SerializeToNode(searchResults)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };

                    var secondContent = new StringContent(secondPayload.ToJsonString(), Encoding.UTF8, "application/json");
                    var secondResponse = await _httpClient.PostAsync(url, secondContent);
                    
                    if (secondResponse.IsSuccessStatusCode)
                    {
                        var secondJsonString = await secondResponse.Content.ReadAsStringAsync();
                        var secondJson = JsonNode.Parse(secondJsonString);
                        var finalReply = secondJson?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                        return new ChatResponse { Reply = finalReply ?? "Không có câu trả lời." };
                    }
                    else
                    {
                        return new ChatResponse { Reply = "Lỗi khi xử lý function response từ AI." };
                    }
                }
            }
            else if (part?["text"] != null)
            {
                var reply = part["text"]?.ToString();
                return new ChatResponse { Reply = reply ?? "Lỗi phản hồi văn bản." };
            }

            return new ChatResponse { Reply = "Không thể hiểu phản hồi từ AI." };
        }
    }
}
