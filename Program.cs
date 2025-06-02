using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Services;
using webprogbackend.Services.Payment;
using webprogbackend.Configurations;

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

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Web Programming E-Commerce API",
        Version = "v1",
        Description = "E-Commerce Backend API with Role-Based Authentication"
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
        c.RoutePrefix = string.Empty; // Swagger UI'yi root'ta çalýþtýr
    });
}

app.UseHttpsRedirection();

// CORS - Authentication'dan önce olmalý
app.UseCors("DefaultPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Run seeder
        await seeder.SeedAsync();

        Console.WriteLine("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database seeding failed: {ex.Message}");
    }
}

app.Run();