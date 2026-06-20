using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace TechStoreAPI.config
{
    public static class JwtConfiguration
    {

        public static IServiceCollection AddJwtConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    configuration["Jwt:Key"]!))
                    };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("=================================");
                        Console.WriteLine($"PATH: {context.Request.Path}");
                        Console.WriteLine($"AUTH HEADER: {context.Request.Headers.Authorization}");
                        Console.WriteLine("=================================");

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("=================================");
                        Console.WriteLine("TOKEN VALIDATED");
                        Console.WriteLine($"USER: {context.Principal?.Identity?.Name}");
                        Console.WriteLine("=================================");

                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("=================================");
                        Console.WriteLine(context.Exception.ToString());
                        Console.WriteLine("=================================");
                        return Task.CompletedTask;
                    },

                    OnChallenge = context =>
                    {
                        Console.WriteLine("=================================");
                        Console.WriteLine("JWT CHALLENGE TRIGGERED");
                        Console.WriteLine($"ERROR: {context.Error}");
                        Console.WriteLine($"DESCRIPTION: {context.ErrorDescription}");
                        Console.WriteLine("=================================");

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}