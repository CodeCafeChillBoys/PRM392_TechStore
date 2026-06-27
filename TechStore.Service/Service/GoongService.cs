using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class GoongService : IGoongService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GoongService(
       HttpClient httpClient,
       IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        public async Task<string> GetRouteAsync(double originLat, double originLng, double destLat, double destLng)
        {
            var apiKey = _configuration["Goong:ApiKey"];

            var url =
                $"https://rsapi.goong.io/Direction" +
                $"?origin={originLat},{originLng}" +
                $"&destination={destLat},{destLng}" +
                $"&vehicle=car" +
                $"&api_key={apiKey}";

            return await _httpClient.GetStringAsync(url);
        }
    }
}