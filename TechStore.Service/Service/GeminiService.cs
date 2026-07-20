using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TechStore.Domain.DTOs.Chat;
using TechStore.Domain.DTOs.Gemini;
using TechStore.Domain.Constants;
using TechStore.Service.IService;
using TechStore.Repository.IRepositories;
using System.Linq;
using System.Collections.Generic;

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

            var systemInstruction = await GetSystemInstructionAsync();
            var requestPayload = new GeminiRequest
            {
                SystemInstruction = new SystemInstruction
                {
                    Parts = new List<Part> { new Part { Text = systemInstruction } }
                },
                Contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part> { new Part { Text = request.Message } }
                    }
                },
                Tools = new List<Tool>
                {
                    new Tool { FunctionDeclarations = GetToolsDeclaration() }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new ChatResponse { Reply = $"Xin lỗi, tôi không thể xử lý yêu cầu lúc này (Lỗi kết nối API 1). Details: {err}" };
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonString);
            var responseContent = geminiResponse?.Candidates?.FirstOrDefault()?.Content;

            var functionCalls = responseContent?.Parts?
                .Where(p => p.FunctionCall != null)
                .Select(p => p.FunctionCall!)
                .ToList();

            string? textReply = responseContent?.Parts?
                .FirstOrDefault(p => p.Text != null)?.Text;

            if (functionCalls != null && functionCalls.Any())
            {
                return await HandleMultipleFunctionCallsAsync(url, functionCalls, request.Message, requestPayload, responseContent!);
            }
            
            if (textReply != null)
            {
                return new ChatResponse { Reply = textReply };
            }

            return new ChatResponse { Reply = "Không thể hiểu phản hồi từ AI." };
        }

        private async Task<string> GetSystemInstructionAsync()
        {
            var knowledges = await _knowledgeBaseService.GetAllKnowledgeAsync();
            var shopInfo = string.Join("\n", knowledges.Select(k => $"- {k.Category}: {k.Content}"));
            return ChatbotConstants.SystemInstructionBase + shopInfo;
        }

        private List<FunctionDeclaration> GetToolsDeclaration()
        {
            return new List<FunctionDeclaration>
            {
                new FunctionDeclaration
                {
                    Name = "query_products",
                    Description = "Tìm kiếm, lọc, sắp xếp và lấy danh sách sản phẩm trong database theo các tiêu chí như từ khóa, thương hiệu, tên danh mục, khoảng giá, sắp xếp theo giá tăng/giảm hoặc số lượng.",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>
                        {
                            ["keyword"] = new PropertySchema { Type = "STRING", Description = "Từ khóa tìm kiếm theo tên sản phẩm (ví dụ: iPhone, Samsung, Pro Max)" },
                            ["brand"] = new PropertySchema { Type = "STRING", Description = "Thương hiệu sản phẩm (ví dụ: Apple, Samsung, Asus)" },
                            ["categoryName"] = new PropertySchema { Type = "STRING", Description = "Tên danh mục sản phẩm (ví dụ: Điện thoại, Laptop, Phụ kiện)" },
                            ["minPrice"] = new PropertySchema { Type = "NUMBER", Description = "Giá bán tối thiểu của sản phẩm" },
                            ["maxPrice"] = new PropertySchema { Type = "NUMBER", Description = "Giá bán tối đa của sản phẩm" },
                            ["sortBy"] = new PropertySchema { Type = "STRING", Description = "Trường cần sắp xếp: 'price' (giá cả), 'stock' (số lượng tồn kho), hoặc 'createdAt' (ngày tạo/mới nhất)" },
                            ["descending"] = new PropertySchema { Type = "BOOLEAN", Description = "Sắp xếp giảm dần nếu true, tăng dần nếu false" },
                            ["limit"] = new PropertySchema { Type = "INTEGER", Description = "Giới hạn số lượng kết quả trả về (mặc định: 10)" }
                        }
                    }
                },
                new FunctionDeclaration
                {
                    Name = "search_product",
                    Description = "Tìm kiếm thông tin sản phẩm trong database theo từ khóa (keyword).",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>
                        {
                            ["keyword"] = new PropertySchema { Type = "STRING", Description = "Từ khóa tìm kiếm (ví dụ: iPhone, Samsung)" }
                        },
                        Required = new List<string> { "keyword" }
                    }
                },
                new FunctionDeclaration
                {
                    Name = "get_product_specifications",
                    Description = "Xem cấu hình chi tiết và các thông số kỹ thuật của một sản phẩm cụ thể theo tên (ví dụ để so sánh hai hoặc nhiều sản phẩm).",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>
                        {
                            ["productName"] = new PropertySchema { Type = "STRING", Description = "Tên sản phẩm cần xem cấu hình chi tiết (ví dụ: iPhone 15 Pro Max, Samsung Galaxy S24 Ultra)" }
                        },
                        Required = new List<string> { "productName" }
                    }
                },
                new FunctionDeclaration
                {
                    Name = "get_order_status",
                    Description = "Tra cứu trạng thái đơn hàng dựa trên mã đơn hàng (ID Guid).",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>
                        {
                            ["orderId"] = new PropertySchema { Type = "STRING", Description = "Mã ID đơn hàng dạng Guid (ví dụ: 4c4bf2f4-6be4-4364-b9c1-1250275ccbfa)" }
                        },
                        Required = new List<string> { "orderId" }
                    }
                },
                new FunctionDeclaration
                {
                    Name = "cancel_order",
                    Description = "Hủy đơn hàng nếu trạng thái đơn hàng hiện tại là Pending (Chờ xử lý).",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>
                        {
                            ["orderId"] = new PropertySchema { Type = "STRING", Description = "Mã ID đơn hàng dạng Guid cần hủy" }
                        },
                        Required = new List<string> { "orderId" }
                    }
                },
                new FunctionDeclaration
                {
                    Name = "escalate_to_human",
                    Description = "Chuyển giao tiếp cuộc trò chuyện của khách hàng sang cho nhân viên hỗ trợ trực tiếp khi gặp câu hỏi khó hoặc khách hàng yêu cầu.",
                    Parameters = new ParametersSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, PropertySchema>()
                    }
                }
            };
        }

        private async Task<ChatResponse> HandleMultipleFunctionCallsAsync(
            string url, 
            List<FunctionCall> functionCalls, 
            string userMessage, 
            GeminiRequest basePayload, 
            Content modelResponseContent)
        {
            var functionResponseParts = new List<Part>();

            foreach (var functionCall in functionCalls)
            {
                var functionName = functionCall.Name;
                object responseData;

                if (functionName == "query_products" || functionName == "search_product")
                {
                    var keyword = functionCall.Args?.ContainsKey("keyword") == true ? functionCall.Args["keyword"]?.ToString() ?? "" : "";
                    var brand = functionCall.Args?.ContainsKey("brand") == true ? functionCall.Args["brand"]?.ToString() ?? "" : "";
                    var categoryName = functionCall.Args?.ContainsKey("categoryName") == true ? functionCall.Args["categoryName"]?.ToString() ?? "" : "";
                    
                    double? minPrice = null;
                    if (functionCall.Args?.ContainsKey("minPrice") == true && double.TryParse(functionCall.Args["minPrice"]?.ToString(), out double minP)) minPrice = minP;

                    double? maxPrice = null;
                    if (functionCall.Args?.ContainsKey("maxPrice") == true && double.TryParse(functionCall.Args["maxPrice"]?.ToString(), out double maxP)) maxPrice = maxP;

                    var sortBy = functionCall.Args?.ContainsKey("sortBy") == true ? functionCall.Args["sortBy"]?.ToString() ?? "" : "";
                    
                    bool descending = false;
                    if (functionCall.Args?.ContainsKey("descending") == true && bool.TryParse(functionCall.Args["descending"]?.ToString(), out bool desc)) descending = desc;

                    int limit = 10;
                    if (functionCall.Args?.ContainsKey("limit") == true && int.TryParse(functionCall.Args["limit"]?.ToString(), out int lim)) limit = lim;

                    decimal? minPriceDecimal = minPrice.HasValue ? (decimal)minPrice.Value : (decimal?)null;
                    decimal? maxPriceDecimal = maxPrice.HasValue ? (decimal)maxPrice.Value : (decimal?)null;

                    var products = await _unitOfWork.Products.QueryProductsAsync(
                        keyword,
                        brand,
                        categoryName,
                        minPriceDecimal,
                        maxPriceDecimal,
                        sortBy,
                        descending,
                        limit
                    );

                    responseData = products.Select(p => new { 
                                                name = p.Name, 
                                                brand = p.Brand,
                                                price = p.Price, 
                                                stock = p.StockQuantity,
                                                category = p.Category != null ? p.Category.Name : null,
                                                description = p.Description
                                            })
                                            .ToList();
                }
                else if (functionName == "get_product_specifications")
                {
                    var productName = functionCall.Args?.ContainsKey("productName") == true ? functionCall.Args["productName"]?.ToString() ?? "" : "";
                    var products = await _unitOfWork.Products.GetProductsWithCategoryAsync();
                    var matchedProduct = products.FirstOrDefault(p => p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchedProduct != null)
                    {
                        responseData = new
                        {
                            name = matchedProduct.Name,
                            brand = matchedProduct.Brand,
                            price = matchedProduct.Price,
                            description = matchedProduct.Description,
                            specifications = matchedProduct.Specifications?.Select(s => new { key = s.SpecKey, value = s.SpecValue }).ToList()
                        };
                    }
                    else
                    {
                        responseData = new { error = $"Không tìm thấy cấu hình chi tiết cho sản phẩm: {productName}." };
                    }
                }
                else if (functionName == "get_order_status")
                {
                    var orderIdStr = functionCall.Args?.ContainsKey("orderId") == true ? functionCall.Args["orderId"]?.ToString() ?? "" : "";
                    if (Guid.TryParse(orderIdStr, out Guid orderId))
                    {
                        var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(orderId);
                        if (order != null)
                        {
                            responseData = new
                            {
                                orderId = order.Id,
                                orderDate = order.OrderDate,
                                totalAmount = order.TotalAmount,
                                status = order.Status,
                                paymentStatus = order.PaymentStatus,
                                shippingAddress = order.ShippingAddress,
                                items = order.OrderDetails?.Select(d => new
                                {
                                    productName = d.Product?.Name,
                                    quantity = d.Quantity,
                                    price = d.UnitPrice
                                }).ToList()
                            };
                        }
                        else
                        {
                            responseData = new { error = "Không tìm thấy đơn hàng tương ứng với mã cung cấp." };
                        }
                    }
                    else
                    {
                        responseData = new { error = "Mã đơn hàng không đúng định dạng Guid hợp lệ." };
                    }
                }
                else if (functionName == "cancel_order")
                {
                    var orderIdStr = functionCall.Args?.ContainsKey("orderId") == true ? functionCall.Args["orderId"]?.ToString() ?? "" : "";
                    if (Guid.TryParse(orderIdStr, out Guid orderId))
                    {
                        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                        if (order != null)
                        {
                            if (order.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                            {
                                order.Status = "Cancelled";
                                order.PaymentStatus = "Cancelled";
                                _unitOfWork.Orders.Update(order);
                                await _unitOfWork.CompleteAsync();
                                responseData = new { success = true, message = "Đơn hàng đã được hủy thành công." };
                            }
                            else
                            {
                                responseData = new { success = false, error = $"Không thể hủy đơn hàng vì trạng thái đơn hàng hiện tại là: {order.Status}." };
                            }
                        }
                        else
                        {
                            responseData = new { success = false, error = "Không tìm thấy đơn hàng tương ứng với mã cung cấp." };
                        }
                    }
                    else
                    {
                        responseData = new { success = false, error = "Mã đơn hàng không đúng định dạng Guid hợp lệ." };
                    }
                }
                else if (functionName == "escalate_to_human")
                {
                    responseData = new
                    {
                        status = "Escalated",
                        message = "Hệ thống đang chuyển cuộc hội thoại của bạn đến nhân viên hỗ trợ trực tiếp. Vui lòng chờ."
                    };
                }
                else
                {
                    responseData = new { error = "Yêu cầu chức năng không được hỗ trợ." };
                }

                functionResponseParts.Add(new Part
                {
                    FunctionResponse = new FunctionResponse
                    {
                        Name = functionName,
                        Response = new
                        {
                            name = functionName,
                            content = responseData
                        }
                    }
                });
            }

            var secondPayload = new GeminiRequest
            {
                SystemInstruction = basePayload.SystemInstruction,
                Contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part> { new Part { Text = userMessage } }
                    },
                    modelResponseContent,
                    new Content
                    {
                        Role = "function",
                        Parts = functionResponseParts
                    }
                }
            };

            var secondContent = new StringContent(JsonSerializer.Serialize(secondPayload), Encoding.UTF8, "application/json");
            var secondResponse = await _httpClient.PostAsync(url, secondContent);
            
            if (secondResponse.IsSuccessStatusCode)
            {
                var secondJsonString = await secondResponse.Content.ReadAsStringAsync();
                var secondGeminiResponse = JsonSerializer.Deserialize<GeminiResponse>(secondJsonString);
                var finalReply = secondGeminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                return new ChatResponse { Reply = finalReply ?? "Không có câu trả lời." };
            }

            var errorDetails = await secondResponse.Content.ReadAsStringAsync();
            return new ChatResponse { Reply = $"Xin lỗi, tôi gặp sự cố khi tổng hợp thông tin (Lỗi xử lý phản hồi hàm). Details: {errorDetails}" };
        }
    }
}
