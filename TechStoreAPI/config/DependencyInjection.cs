using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Repository.IRepositories;
using TechStore.Repository.Repositories;
using TechStore.Service.IService;
using TechStore.Service.Service;

namespace TechStoreAPI.config
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
        {

            services.AddScoped<IUserRepositories, UserRepositories>();
            services.AddScoped<IRefreshTokenRepositories, RefreshTokenRepositories>();
            services.AddScoped<IAuthService, AuthenService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(
              typeof(IGenericRepository<>),
              typeof(GenericRepository<>));

            return services;
        }
    }
}