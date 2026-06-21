using Microsoft.Extensions.DependencyInjection;
using TechStore.Repository.IRepositories;
using TechStore.Repository.Repositories;
using TechStore.Service.IService;
using TechStore.Service.Service;
using TechStoreAPI.config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDatabase(builder.Configuration);

// ── JWT Authentication (từ nhánh Auth) ───────────────────────────────────
builder.Services.AddJwtConfiguration(builder.Configuration);

// ── VNPay + Order services (từ nhánh checkout/billing) ───────────────────
builder.Services.AddServices(builder.Configuration);

// ── Dependency Injection (từ nhánh Auth/Cart) ────────────────────────────
builder.Services.AddDependencyInjection();

// Generic Repository và Unit Of Work
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services cho Product, Category, Cart
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICartService, CartService>();


// ── Controllers + JSON options ────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── AutoMapper ────────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(config =>
{
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
