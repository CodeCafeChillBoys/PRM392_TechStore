using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Repository.Repositories;
using TechStore.Service.IService;
using TechStore.Service.Service;

namespace TechStoreAPI.config
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<IUserRepositories, UserRepositories>();
            services.AddScoped<IRefreshTokenRepositories, RefreshTokenRepositories>();
            services.AddScoped<IAuthService, AuthenService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(
              typeof(IGenericRepository<>),
              typeof(GenericRepository<>));
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IOrderService, OrderService>();  
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            services.Configure<BrevoSettings>(configuration.GetSection("BrevoSettings"));
            services.AddScoped<IEmailService, BrevoEmailService>();

            return services;
        }
    }
}