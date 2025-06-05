using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Services;
using webprogbackend.Services.Payment;
using webprogbackend.Configurations;
using webprogbackend.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddSingleton(jwtSettings);

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero,
        RequireExpirationTime = true,
        // Role claim mapping
        RoleClaimType = "Role",
        NameClaimType = "Username"
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for: {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOrModerator", policy => policy.RequireRole("Admin", "Moderator"));
    options.AddPolicy("AllUsers", policy => policy.RequireRole("Admin", "User", "Moderator"));
});

// CORS Configuration
builder.Services.ConfigureCors(builder.Configuration);

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();

// DeepSeek Configuration
builder.Services.Configure<DeepSeekSettings>(builder.Configuration.GetSection("DeepSeek"));
builder.Services.AddHttpClient("DeepSeek", client =>
{
    var deepSeekSettings = builder.Configuration.GetSection("DeepSeek").Get<DeepSeekSettings>();
    client.BaseAddress = new Uri(deepSeekSettings.BaseUrl);
    client.Timeout = TimeSpan.FromMilliseconds(deepSeekSettings.Timeout);
    client.DefaultRequestHeaders.Add("HTTP-Referer", deepSeekSettings.HttpReferer);
    client.DefaultRequestHeaders.Add("X-Title", deepSeekSettings.XTitle);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", builder =>
    {
        builder
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true); // Development için
    });
});

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Web Programming E-Commerce API",
        Version = "v1",
        Description = "E-Commerce Backend API with Role-Based Authentication and File Upload Support"
    });

    // JWT Authentication için Swagger konfigürasyonu
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
            Array.Empty<string>()
        }
    });

    // Dosya yükleme için multipart/form-data desteği
    c.OperationFilter<FileUploadOperationFilter>();

    // Schema'ları daha iyi göstermek için
    c.SchemaFilter<FileUploadSchemaFilter>();
});

// Request size limits for file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
});

// Logging
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web Programming E-Commerce API V1");
        c.RoutePrefix = string.Empty; // Swagger UI'yi root'ta çalıştır
        c.DefaultModelsExpandDepth(-1); // Modelleri varsayılan olarak kapalı göster
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Endpoint'leri kapalı göster
        c.EnableDeepLinking(); // Deep linking desteği
        c.EnableFilter(); // Filtreleme özelliği
        c.EnableValidator(); // JSON validator
    });
}

app.UseHttpsRedirection();

// CORS - Authentication'dan önce olmalı
app.UseCors("DefaultPolicy");

// Custom middleware
app.UseRoleAuthorization();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database seeding completed successfully");

        // Email servisini test et
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        if (emailService is EmailService emailServiceImpl)
        {
            var emailTestResult = await emailServiceImpl.TestEmailConfigurationAsync();
            if (emailTestResult)
            {
                logger.LogInformation("✅ Email servisi çalışıyor");
            }
            else
            {
                logger.LogWarning("⚠️ Email servisi yapılandırması kontrol edilmeli");
            }
        }

        // Email ayarlarını logla
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        logger.LogInformation("📧 Email Ayarları:");
        logger.LogInformation($"   SMTP Server: {configuration["EmailSettings:SmtpServer"]}");
        logger.LogInformation($"   Port: {configuration["EmailSettings:Port"]}");
        logger.LogInformation($"   From Email: {configuration["EmailSettings:FromEmail"]}");
        logger.LogInformation($"   From Name: {configuration["EmailSettings:FromName"]}");
        logger.LogInformation($"   Default Recipient: {configuration["EmailSettings:DefaultRecipientEmail"]}");

        // Gmail kullanım uyarısı
        if (configuration["EmailSettings:SmtpServer"] == "smtp.gmail.com")
        {
            logger.LogInformation("📮 Gmail SMTP kullanılıyor - App Password gerekli!");
            logger.LogInformation("   1. Gmail hesabında 2-Step Verification aktif olmalı");
            logger.LogInformation("   2. App Password oluşturulmalı (16 karakter)");
            logger.LogInformation("   3. Normal şifre değil, App Password kullanılmalı");
            logger.LogInformation("   4. https://myaccount.google.com/apppasswords adresinden oluşturabilirsiniz");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database seeding or email service initialization failed");
    }
}



var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 E-Commerce Backend başlatıldı");
startupLogger.LogInformation("📍 Swagger UI: https://localhost:7062/swagger");
startupLogger.LogInformation("🔧 Email Test: https://localhost:7062/api/EmailTest/check-settings");
startupLogger.LogInformation("📧 Mail Testi: POST https://localhost:7062/api/EmailTest/test-gmail");


app.Run();

// FileUploadOperationFilter sınıfı

public class FileUploadSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(IFormFile))
        {
            schema.Type = "string";
            schema.Format = "binary";
        }
    }
}

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // IFormFile parametresi olan metodları kontrol et
        var hasFileParameter = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile) ||
                     p.ParameterType == typeof(IFormFile[]) ||
                     p.ParameterType.GetProperties().Any(prop => prop.PropertyType == typeof(IFormFile)));

        // Veya Consumes attribute'u multipart/form-data olan metodları kontrol et
        var hasFormDataConsumes = context.MethodInfo.GetCustomAttributes(true)
            .OfType<ConsumesAttribute>()
            .Any(attr => attr.ContentTypes.Contains("multipart/form-data"));

        if (hasFileParameter || hasFormDataConsumes)
        {
            // Mevcut parametreleri al
            var parameters = context.MethodInfo.GetParameters();
            var formDataParameter = parameters.FirstOrDefault(p =>
                p.GetCustomAttributes(typeof(FromFormAttribute), false).Any());

            if (formDataParameter != null)
            {
                var properties = new Dictionary<string, OpenApiSchema>();

                // DTO'nun property'lerini al
                foreach (var prop in formDataParameter.ParameterType.GetProperties())
                {
                    if (prop.PropertyType == typeof(IFormFile))
                    {
                        properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        };
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "string"
                        };
                    }
                    else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                    {
                        properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "number",
                            Format = "decimal"
                        };
                    }
                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    {
                        properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "integer",
                            Format = "int32"
                        };
                    }
                    else
                    {
                        properties[prop.Name] = new OpenApiSchema
                        {
                            Type = "string"
                        };
                    }
                }

                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = properties,
                                Required = properties.Keys.ToHashSet()
                            }
                        }
                    }
                };

                // URL parametrelerini kaldır (form-data olarak gönderilecek)
                operation.Parameters?.Clear();
            }
        }
    }
}