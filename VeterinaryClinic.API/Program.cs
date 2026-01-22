using VeterinaryClinic.Business;
using Askmethat.Aspnet.JsonLocalizer.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Business.Services;
using VeterinaryClinic.Data;
using VeterinaryClinic.Infrastructure.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Đăng ký DbContext sử dụng SQL Server
builder.Services.AddDbContext<VeterinaryClinicDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký MediatR
// Scan assembly chứa các Command/Query Handler (nằm trong project Business)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePetCommand).Assembly));

// 3. Đăng ký Cache Service
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// 4. Đăng ký Email Service (Infrastructure)
builder.Services.AddScoped<IEmailService, EmailService>();

// 5. Cấu hình Redis Cache
// Lưu ý: Nếu không có Redis thật, bạn có thể gặp lỗi khi chạy. 
// Nếu muốn test mà không cần Redis, hãy set "AppSettings:EnableCache" = "false" trong appsettings.json
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "VeterinaryClinic_";
});

// 6. Cấu hình JSON Localization
builder.Services.AddJsonLocalization(options =>
{
    options.ResourcesPath = "wwwroot/Localization";
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.UseAuthorization();

app.MapControllers();

app.Run();