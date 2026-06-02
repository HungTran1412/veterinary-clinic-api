using VeterinaryClinic.Business;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VeterinaryClinic.Data;
using VeterinaryClinic.Infrastructure;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;
using VeterinaryClinic.Shared.ContextAccessor;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.Extensions.Localization;
using VeterinaryClinic.API.Localization;
using VeterinaryClinic.API.Extensions;
using VeterinaryClinic.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VeterinaryClinic.Business.Core;
using Hangfire;
using VeterinaryClinic.API;
using VeterinaryClinic.API.Services;
using QuestPDF.Infrastructure;

// Cấu hình Serilog tối thiểu để ghi ra Console
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Thêm Serilog vào pipeline của host
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Bind settings from appsettings.json
builder.Services.Configure<ClinicInfoSettings>(builder.Configuration.GetSection(ClinicInfoSettings.SECTION_NAME));

// Cấu hình CORS
var webAppPolicy = "WebAppPolicy";
builder.Services.AddCors(options =>
{
    // Chính sách cho môi trường Production, chặt chẽ hơn
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? new string[0];
    options.AddPolicy(webAppPolicy,
        policy =>
        {
            if (allowedOrigins.Any())
            {
                policy.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            }
        });

    // Chính sách riêng cho môi trường Development, linh hoạt hơn
    options.AddPolicy("AllowAll_Dev",
        policy =>
        {
            policy.AllowAnyOrigin()
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
    
    // Cấu hình JWT Auth cho Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
    
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

// Cấu hình xác thực JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
    };
    
    // Cấu hình để SignalR có thể nhận token từ query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
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

// Đăng ký Redis Handler
builder.Services.AddScoped<IRedisHandler, RedisHandler>();

// Đăng ký Stored Procedure Helper
builder.Services.AddScoped<IVeterinaryClinicCallStoreHelper, VeterinaryClinicCallStoreHelperHandler>();

// 4. Đăng ký Email Service (Infrastructure)
builder.Services.AddScoped<IEmailService, EmailService>();

// 5. Đăng ký PDF Service
builder.Services.AddScoped<IPdfService, QuestPdfService>();

// 6. Đăng ký Context Accessor
builder.Services.AddScoped<IContextAccessor, HttpContextAccessorWrapper>();
builder.Services.AddScoped<Func<IContextAccessor>>(sp => () => sp.GetRequiredService<IContextAccessor>());

// Đăng ký Password Hasher
builder.Services.AddScoped<IBcryptPasswordHasher, PasswordHasher>();

// Đăng ký JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAppointmentStateMachine, AppointmentStateMachine>();

// Đăng ký Notification Service
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Đăng ký các service từ tầng Infrastructure (bao gồm Cloudinary)
builder.Services.AddApplicationServices(builder.Configuration);


// 7. Cấu hình Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "VeterinaryClinic_";
});

// 8. Cấu hình Custom JSON Localization
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
builder.Services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

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

    // Xóa các nhà cung cấp văn hóa mặc định và chỉ giữ lại những cái cần thiết.
    // Việc này loại bỏ 'AcceptLanguageHeaderRequestCultureProvider', 
    // ngăn hệ thống tự động chọn ngôn ngữ theo cài đặt của trình duyệt.
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider()); // ?culture=vi-VN
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
});

// 9. Cấu hình Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

// 10. Cấu hình SignalR
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options => {
        options.Configuration.ChannelPrefix = "VeterinaryClinicSignalR";
    });

var app = builder.Build();

// Seed data
await app.UseAdminSeeder();

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
    // Sử dụng chính sách CORS linh hoạt cho môi trường Development
    app.UseCors("AllowAll_Dev");
}
else
{
    // Sử dụng chính sách CORS chặt chẽ cho môi trường Production
    app.UseCors(webAppPolicy);
}


app.UseHttpsRedirection();

app.UseStaticFiles();

// The correct order for middleware
app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication();

app.UseAuthorization();

// Enable Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Authorization = new[] { new MyAuthorizationFilter() } // Có thể cấu hình bảo mật ở đây sau
});

app.MapControllers();

// Map SignalR Hubs
app.MapHub<SignalRHub>(builder.Configuration["SignalR:Hubs:NotificationUrl"]);

app.Run();
