using VeterinaryClinic.Business;
using Askmethat.Aspnet.JsonLocalizer.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Business.Services;
using VeterinaryClinic.Data;
using VeterinaryClinic.Infrastructure.Services;
using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Cấu hình Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Veterinary Clinic API", Version = "v1" });
    
    // Cho phép hiển thị Controller có GroupName trong document v1
    c.DocInclusionPredicate((docName, apiDesc) => true);
    
    // Nếu bạn muốn chia nhỏ Swagger theo GroupName thì dùng TagActionsBy
    c.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return new[] { api.GroupName };
        }
        if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
        {
            return new[] { controllerActionDescriptor.ControllerName };
        }
        return new[] { "Uncategorized" };
    });
});

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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Veterinary Clinic API v1");
        c.DisplayRequestDuration(); // Hiển thị thời gian phản hồi
        c.EnableDeepLinking();      // Cho phép copy link trực tiếp đến từng endpoint trên thanh địa chỉ trình duyệt
        c.ShowExtensions();
        
        // Inject file JS tùy chỉnh để thêm nút Copy
        c.InjectJavascript("/js/custom-swagger.js");
    });
}

app.UseHttpsRedirection();

// Quan trọng: Phải có UseStaticFiles để load được file js/custom-swagger.js
app.UseStaticFiles();

app.UseRequestLocalization();

app.UseAuthorization();

app.MapControllers();

app.Run();