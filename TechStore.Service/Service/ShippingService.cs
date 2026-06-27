using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class ShippingService : IShippingService
    {
        private readonly IGoongService _goongService;
        private readonly IConfiguration _configuration;

        public ShippingService(IGoongService goongService, IConfiguration configuration)
        {
            _goongService = goongService;
            _configuration = configuration;
        }

        public async Task<ShippingCalculationResponse> CalculateShippingAsync(ShippingCalculationRequest request)
        {
            // 1. Lấy toạ độ cửa hàng từ config
            var originLat = _configuration.GetValue<double>("Goong:StoreLatitude", ShippingConstants.DefaultStoreLatitude);
            var originLng = _configuration.GetValue<double>("Goong:StoreLongitude", ShippingConstants.DefaultStoreLongitude);

            // 2. Gọi Goong Service lấy dữ liệu đường đi dạng JSON
            var rawResponse = await _goongService.GetRouteAsync(
                originLat, originLng,
                request.DestinationLat, request.DestinationLng
            );

            double distanceKm = 0;  // khoảng cách
            double durationMinutes = 0;  // TG vận hành
            string routePolyline = string.Empty; // đường vẽ trên FE
            try
            {
                // 3. Phân tích JsonDocument động
                using var doc = JsonDocument.Parse(rawResponse);
                var root = doc.RootElement; // lấy phần root đầu tiên

                if (root.TryGetProperty("routes", out var routes) &&
                    routes.ValueKind == JsonValueKind.Array &&
                    routes.GetArrayLength() > 0)
                {
                    var route = routes[0];

                    // Lấy chuỗi polyline của lộ trình vẽ lên map
                    if (route.TryGetProperty("overview_polyline", out var polylineProp) &&
                        polylineProp.TryGetProperty("points", out var pointsProp))
                    {
                        routePolyline = pointsProp.GetString() ?? string.Empty;
                    }

                    // Lấy khoảng cách (mét) và thời gian (giây) của leg đầu tiên
                    if (route.TryGetProperty("legs", out var legs) &&
                        legs.ValueKind == JsonValueKind.Array &&
                        legs.GetArrayLength() > 0)
                    {
                        var leg = legs[0];

                        if (leg.TryGetProperty("distance", out var distanceProp) &&
                            distanceProp.TryGetProperty("value", out var distanceVal))
                        {
                            distanceKm = distanceVal.GetDouble() / 1000.0;
                        }

                        if (leg.TryGetProperty("duration", out var durationProp) &&
                            durationProp.TryGetProperty("value", out var durationVal))
                        {
                            durationMinutes = durationVal.GetDouble() / 60.0;
                        }
                    }
                }
                else
                {
                    throw new Exception("Không tìm thấy đường đi khả dụng từ Goong API.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xử lý phản hồi từ Goong API: {ex.Message}", ex);
            }

            // 4. Tính toán phí vận chuyển
            var baseFee = _configuration.GetValue<decimal>("Goong:BaseShippingFee", ShippingConstants.DefaultBaseShippingFee);
            var baseDistance = _configuration.GetValue<double>("Goong:BaseDistanceKm", ShippingConstants.DefaultBaseDistanceKm);
            var perKmFee = _configuration.GetValue<decimal>("Goong:PerKmShippingFee", ShippingConstants.DefaultPerKmShippingFee);

            decimal shippingFee = baseFee;
            if (distanceKm > baseDistance)
            {
                var extraDistance = (decimal)(distanceKm - baseDistance);
                shippingFee += Math.Round(extraDistance * perKmFee, 0);
            }

            return new ShippingCalculationResponse
            {
                DistanceKm = Math.Round(distanceKm, 2),
                DurationMinutes = Math.Round(durationMinutes, 1),
                ShippingFee = shippingFee,
                RoutePolyline = routePolyline
            };
        }
    }
}
