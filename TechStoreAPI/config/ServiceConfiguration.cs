using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechStore.Domain.Settings;
using TechStore.Service.IServices;
using TechStore.Service.Services;

namespace TechStoreAPI.config
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind VNPay settings from appsettings.json
            services.Configure<VnpaySettings>(
                configuration.GetSection(VnpaySettings.SectionName));

            // Register application services
            services.AddScoped<IVnpayService, VnpayService>();
            services.AddScoped<IOrderService, OrderService>();

            return services;
        }
    }
}
