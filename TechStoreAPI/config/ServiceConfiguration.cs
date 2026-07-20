using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechStore.Domain.Settings;
using TechStore.Service.IService;
using TechStore.Service.Service;

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

            // Register VNPay service (IOrderService is registered in DependencyInjection.cs)
            services.AddScoped<IVnpayService, VnpayService>();

            return services;
        }
    }
}
