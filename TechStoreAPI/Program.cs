using Microsoft.Extensions.DependencyInjection;
using TechStore.Repository.IRepositories;
using TechStore.Repository.Repositories;
using TechStore.Service.IService;
using TechStore.Service.Service;
using TechStoreAPI.config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddDependencyInjection();
// 1. Đăng ký Generic Repository và Unit Of Work
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 2. Đăng ký Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(config =>
{
    // Lệnh này sẽ tự động quét toàn bộ Project để tìm tất cả các file MappingProfile
    config.AddMaps(typeof(Program).Assembly);
}); 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
