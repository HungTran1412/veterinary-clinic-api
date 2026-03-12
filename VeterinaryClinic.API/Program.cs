using VeterinaryClinic.Business;
using Askmethat.Aspnet.JsonLocalizer.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Business.Services; // Using Interface
using VeterinaryClinic.Data;
using VeterinaryClinic.Infrastructure.Services; // Using Implementation
using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;
using VeterinaryClinic.Shared.ContextAccessor;
using Swashbuckle.AspNetCore.SwaggerUI;
using Askmethat.Aspnet.JsonLocalizer.JsonOptions; // Thêm using này

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
    
    // Thêm comment XML vào Swagger (nếu file tồn tại)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
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

// Đăng ký Password Hasher (Interface nằm ở Business, Implementation nằm ở Infrastructure)
builder.Services.AddScoped<IBcryptPasswordHasher, PasswordHasher>();


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
    options.UseBaseName = false; // Thử thêm option này
    options.CacheDuration = TimeSpan.FromMinutes(15);
    options.FileEncoding = System.Text.Encoding.UTF8;
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
        
        // Inject file JS để thêm nút Copy
        c.InjectJavascript("/js/custom-swagger.js");
    });
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRequestLocalization();

app.UseAuthorization();

app.MapControllers();

app.Run();