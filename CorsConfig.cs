using Microsoft.AspNetCore.Cors.Infrastructure;

namespace webprogbackend.Configurations
{
    public static class CorsConfig
    {
        public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>();

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy", builder =>
                {
                    builder.WithOrigins(corsSettings.AllowedOrigins)
                           .WithMethods(corsSettings.AllowedMethods)
                           .WithHeaders(corsSettings.AllowedHeaders)
                           .WithExposedHeaders(corsSettings.ExposedHeaders)
                           .SetIsOriginAllowed(origin => true) // Geliştirme ortamı için
                           .AllowCredentials();
                });

                // API Gateway için özel politika
                options.AddPolicy("ApiGatewayPolicy", builder =>
                {
                    builder.WithOrigins(corsSettings.ApiGatewayOrigin)
                           .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                           .WithHeaders("Authorization", "Content-Type")
                           .AllowCredentials();
                });

                // Frontend uygulaması için özel politika
                options.AddPolicy("FrontendPolicy", builder =>
                {
                    builder.WithOrigins(corsSettings.FrontendOrigin)
                           .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                           .WithHeaders("Authorization", "Content-Type")
                           .AllowCredentials();
                });
            });
        }
    }

    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; }
        public string[] AllowedMethods { get; set; }
        public string[] AllowedHeaders { get; set; }
        public string[] ExposedHeaders { get; set; }
        public string ApiGatewayOrigin { get; set; }
        public string FrontendOrigin { get; set; }
    }
} 