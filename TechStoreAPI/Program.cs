using Microsoft.Extensions.DependencyInjection;
using TechStore.Repository.IRepositories;
using TechStore.Repository.Repositories;
using TechStore.Service.IService;
using TechStore.Service.Service;
using TechStoreAPI.config;
using TechStoreAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDatabase(builder.Configuration);

// ── JWT Authentication (từ nhánh Auth) ───────────────────────────────────
builder.Services.AddJwtConfiguration(builder.Configuration);

// ── VNPay + Order services (từ nhánh checkout/billing) ───────────────────
builder.Services.AddServices(builder.Configuration);

// ── Dependency Injection (từ nhánh develop — Auth/Cart/Device/Email) ─────
builder.Services.AddDependencyInjection(builder.Configuration);

// ── Swagger (từ nhánh develop — có JWT Bearer) ───────────────────────────
builder.Services.AddSwaggerConfiguration();
builder.Services.AddEndpointsApiExplorer();


// ── Controllers + JSON options ────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

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
app.MapHub<TechStoreAPI.Hubs.TrackingHub>("/trackingHub");
app.MapControllers();

app.Run();
