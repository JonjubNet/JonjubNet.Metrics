using JonjubNet.Metrics.Configuration;
using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace JonjubNet.Metrics
{
    /// <summary>
    /// Extensiones para configurar la infraestructura de métricas
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de métricas al contenedor de dependencias
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddMetricsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar opciones de métricas
            services.Configure<MetricsConfiguration>(configuration.GetSection(MetricsConfiguration.SectionName));

            // Registrar el servicio de métricas
            services.AddScoped<IMetricsService, MetricsService>();

            // Registrar middleware de métricas HTTP
            services.AddScoped<IMetricsMiddleware, HttpMetricsMiddleware>();

            // Configurar Prometheus si está habilitado
            var metricsConfig = configuration.GetSection(MetricsConfiguration.SectionName).Get<MetricsConfiguration>();
            if (metricsConfig?.Export?.Prometheus?.Enabled == true)
            {
                services.AddPrometheusMetrics();
            }

            return services;
        }

        /// <summary>
        /// Agrega la infraestructura de métricas con configuración personalizada
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <param name="configureOptions">Acción para configurar opciones adicionales</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddMetricsInfrastructure(this IServiceCollection services, IConfiguration configuration, Action<MetricsConfiguration> configureOptions)
        {
            // Configurar opciones de métricas
            services.Configure<MetricsConfiguration>(configuration.GetSection(MetricsConfiguration.SectionName));
            services.Configure(configureOptions);

            // Registrar el servicio de métricas
            services.AddScoped<IMetricsService, MetricsService>();

            // Registrar middleware de métricas HTTP
            services.AddScoped<IMetricsMiddleware, HttpMetricsMiddleware>();

            // Configurar Prometheus si está habilitado
            var metricsConfig = configuration.GetSection(MetricsConfiguration.SectionName).Get<MetricsConfiguration>();
            if (metricsConfig?.Export?.Prometheus?.Enabled == true)
            {
                services.AddPrometheusMetrics();
            }

            return services;
        }

        /// <summary>
        /// Agrega middleware de métricas HTTP al pipeline de ASP.NET Core
        /// </summary>
        /// <param name="app">Builder de la aplicación</param>
        /// <returns>Builder de la aplicación para chaining</returns>
        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpMetricsMiddleware>();
        }
    }
}

