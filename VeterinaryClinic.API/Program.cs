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
using Serilog;
using VeterinaryClinic.Shared.ContextAccessor;

// Cấu hình Serilog tối thiểu để ghi ra Console
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Thêm Serilog vào pipeline của host
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Cấu hình CORS: Cho phép mọi nguồn (Dùng cho Dev)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Cấu hình Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Veterinary Clinic API", Version = "v1" });
    
    c.DocInclusionPredicate((docName, apiDesc) => true);
    
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

// 1. Đăng ký DbContext
builder.Services.AddDbContext<VeterinaryClinicDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<VeterinaryClinicReadDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateSpecializationCommand).Assembly));

// 3. Đăng ký Cache Service
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// 4. Đăng ký Email Service (Infrastructure)
builder.Services.AddScoped<IEmailService, EmailService>();

// 5. Đăng ký Context Accessor
builder.Services.AddScoped<IContextAccessor, HttpContextAccessorWrapper>();
builder.Services.AddScoped<Func<IContextAccessor>>(sp => () => sp.GetRequiredService<IContextAccessor>());


// 6. Cấu hình Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "VeterinaryClinic_";
});

// 7. Cấu hình JSON Localization
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
        c.DisplayRequestDuration(); 
        c.EnableDeepLinking();      
        c.ShowExtensions();
        
        // c.InjectJavascript("/js/custom-swagger.js");
    });
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRequestLocalization();

app.UseAuthorization();

app.MapControllers();

app.Run();