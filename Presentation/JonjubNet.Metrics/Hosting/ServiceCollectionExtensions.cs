using System.Security.Cryptography.X509Certificates;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Core.Resilience;
using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Prometheus;
using JonjubNet.Metrics.Shared.Configuration;
using JonjubNet.Metrics.Shared.Health;
using JonjubNet.Metrics.Shared.Resilience;
using JonjubNet.Metrics.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JonjubNet.Metrics.Hosting
{
    /// <summary>
    /// Extensiones para registro de servicios de métricas
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de métricas
        /// </summary>
        public static IServiceCollection AddJonjubNetMetrics(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<MetricsOptions>? configureOptions = null)
        {
            // Configurar opciones nuevas
            services.Configure<MetricsOptions>(options =>
            {
                configuration.GetSection("Metrics").Bind(options);
                configureOptions?.Invoke(options);
            });

            // Configurar MetricsConfiguration para compatibilidad con código existente
            services.Configure<MetricsConfiguration>(configuration.GetSection(MetricsConfiguration.SectionName));

            // Registrar servicios de encriptación
            services.AddSingleton<EncryptionService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<EncryptionService>>();
                
                // Si hay claves configuradas, usarlas; sino generar automáticamente
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKeyBase64) && 
                    !string.IsNullOrEmpty(options.Encryption.EncryptionIVBase64))
                {
                    var key = Convert.FromBase64String(options.Encryption.EncryptionKeyBase64);
                    var iv = Convert.FromBase64String(options.Encryption.EncryptionIVBase64);
                    return new EncryptionService(key, iv, logger);
                }
                
                return new EncryptionService(logger);
            });

            // Registrar SecureHttpClientFactory
            services.AddSingleton<SecureHttpClientFactory>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SecureHttpClientFactory>>();
                
                X509Certificate2? clientCert = null;
                if (!string.IsNullOrEmpty(options.Encryption.EncryptionKeyBase64))
                {
                    // En producción, cargar certificado desde configuración segura
                    // Por ahora, null es aceptable
                }
                
                return new SecureHttpClientFactory(
                    validateCertificates: options.Encryption.ValidateCertificates,
                    clientCertificate: clientCert,
                    logger: logger);
            });

            // Registrar componentes Core
            services.AddSingleton<MetricRegistry>();
            // ELIMINADO: MetricBus ya no se necesita - todos los sinks leen del Registry
            
            // Registrar MetricsClient (simplificado - solo Registry)
            services.AddSingleton<IMetricsClient>(sp =>
            {
                var registry = sp.GetRequiredService<MetricRegistry>();
                return new MetricsClient(registry);
            });

            // Registrar Dead Letter Queue si está habilitada
            services.AddSingleton<DeadLetterQueue>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled)
                {
                    return null!; // Retornar null si está deshabilitada
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueue>>();
                var encryptionService = options.Encryption.EnableAtRest 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                return new DeadLetterQueue(
                    options.DeadLetterQueue.MaxSize, 
                    logger,
                    encryptionService,
                    options.Encryption.EnableAtRest);
            });

            // Registrar Retry Policy si está habilitada
            services.AddSingleton<RetryPolicy>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.RetryPolicy.Enabled)
                {
                    return null!; // Retornar null si está deshabilitada
                }
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<RetryPolicy>>();
                return new RetryPolicy(
                    options.RetryPolicy.MaxRetries,
                    TimeSpan.FromMilliseconds(options.RetryPolicy.InitialDelayMs),
                    options.RetryPolicy.BackoffMultiplier,
                    options.RetryPolicy.JitterPercent,
                    logger);
            });

            // Registrar SinkCircuitBreakerManager si está habilitado
            services.AddSingleton<ISinkCircuitBreakerManager>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.CircuitBreaker.Enabled)
                {
                    return null!; // Retornar null si está deshabilitado
                }
                
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<SinkCircuitBreakerManager>>();
                
                // Crear opciones por defecto
                var defaultOptions = new CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureThreshold = options.CircuitBreaker.Default.FailureThreshold,
                    OpenDuration = TimeSpan.FromSeconds(options.CircuitBreaker.Default.OpenDurationSeconds)
                };
                
                // Crear opciones específicas por sink
                var sinkSpecificOptions = new Dictionary<string, CircuitBreakerOptions>();
                foreach (var kvp in options.CircuitBreaker.Sinks)
                {
                    var sinkOptions = new CircuitBreakerOptions
                    {
                        Enabled = kvp.Value.Enabled,
                        FailureThreshold = kvp.Value.FailureThreshold ?? defaultOptions.FailureThreshold,
                        OpenDuration = TimeSpan.FromSeconds(kvp.Value.OpenDurationSeconds ?? options.CircuitBreaker.Default.OpenDurationSeconds)
                    };
                    sinkSpecificOptions[kvp.Key] = sinkOptions;
                }
                
                return new SinkCircuitBreakerManager(
                    defaultOptions,
                    sinkSpecificOptions,
                    logger,
                    enabled: options.CircuitBreaker.Enabled);
            });

            // Registrar scheduler simplificado (lee del Registry, sin Bus)
            services.AddSingleton<MetricFlushScheduler>(sp =>
            {
                var registry = sp.GetRequiredService<MetricRegistry>();
                var sinks = sp.GetServices<IMetricsSink>();
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<MetricFlushScheduler>>();
                var deadLetterQueue = options.DeadLetterQueue.Enabled 
                    ? sp.GetService<DeadLetterQueue>() 
                    : null;
                var retryPolicy = options.RetryPolicy.Enabled 
                    ? sp.GetService<RetryPolicy>() 
                    : null;
                var circuitBreakerManager = options.CircuitBreaker.Enabled
                    ? sp.GetService<ISinkCircuitBreakerManager>()
                    : null;
                
                return new MetricFlushScheduler(
                    registry,
                    sinks,
                    TimeSpan.FromMilliseconds(options.FlushIntervalMs),
                    logger,
                    deadLetterQueue,
                    retryPolicy,
                    circuitBreakerManager);
            });

            // Registrar procesador de DLQ si está habilitado
            services.AddHostedService<DeadLetterQueueProcessor>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                if (!options.DeadLetterQueue.Enabled || !options.DeadLetterQueue.EnableAutoProcessing)
                {
                    return null!; // No registrar si está deshabilitado
                }
                var deadLetterQueue = sp.GetRequiredService<DeadLetterQueue>();
                var sinks = sp.GetServices<IMetricsSink>();
                var retryPolicy = options.RetryPolicy.Enabled 
                    ? sp.GetService<RetryPolicy>() 
                    : null;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterQueueProcessor>>();
                return new DeadLetterQueueProcessor(
                    deadLetterQueue,
                    sinks,
                    retryPolicy,
                    TimeSpan.FromMilliseconds(options.DeadLetterQueue.ProcessingIntervalMs),
                    options.BatchSize,
                    logger);
            });

            // Registrar background service
            services.AddHostedService<MetricsBackgroundService>();

            // Registrar seguridad
            services.AddSingleton<SecureTagValidator>();

            // Registrar configuración
            // Nota sobre logging: El componente utiliza ILogger estándar de Microsoft.Extensions.Logging
            // para todos los eventos (errores, warnings, información, debug). Si tu proyecto utiliza
            // Jonjub.Logging, puedes configurarlo como proveedor de logging y todos los eventos
            // del componente de métricas se registrarán a través de él automáticamente.
            services.AddSingleton<MetricsConfigurationManager>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<MetricsConfigurationManager>>();
                return new MetricsConfigurationManager(configuration, logger);
            });

            // Configurar Prometheus por defecto
            services.Configure<PrometheusOptions>(options =>
            {
                configuration.GetSection("Metrics:Prometheus").Bind(options);
            });
            services.AddSingleton<PrometheusFormatter>();
            services.AddSingleton<PrometheusExporter>();
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<PrometheusExporter>());

            // Registrar sinks con encriptación si están configurados
            RegisterSinksWithEncryption(services, configuration);

            // Registrar IMetricsService para compatibilidad con código existente
            services.AddScoped<IMetricsService, Services.MetricsService>();

            // Registrar health check (usando el de Shared.Health) - sin Bus
            services.AddSingleton<JonjubNet.Metrics.Shared.Health.IMetricsHealthCheck>(sp =>
            {
                // ELIMINADO: Bus ya no se necesita
                var sinks = sp.GetServices<IMetricsSink>();
                var scheduler = sp.GetService<MetricFlushScheduler>();
                var options = sp.GetRequiredService<IOptions<MetricsOptions>>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<JonjubNet.Metrics.Shared.Health.MetricsHealthCheck>>();
                return new JonjubNet.Metrics.Shared.Health.MetricsHealthCheck(sinks, scheduler, options, logger);
            });

            // Registrar health check para ASP.NET Core
            services.AddHealthChecks()
                .AddCheck<Health.MetricsHealthCheckService>("metrics");

            return services;
        }

        /// <summary>
        /// Registra los sinks con configuración de encriptación desde MetricsOptions
        /// </summary>
        private static void RegisterSinksWithEncryption(IServiceCollection services, IConfiguration configuration)
        {
            // Registrar OpenTelemetry sink con encriptación
            services.Configure<JonjubNet.Metrics.OpenTelemetry.OTLOptions>(options =>
            {
                configuration.GetSection("Metrics:OpenTelemetry").Bind(options);
            });
            services.AddSingleton<JonjubNet.Metrics.OpenTelemetry.OTLPExporter>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<JonjubNet.Metrics.OpenTelemetry.OTLOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<JonjubNet.Metrics.OpenTelemetry.OTLPExporter>>();
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                
                return new JonjubNet.Metrics.OpenTelemetry.OTLPExporter(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: metricsOptions.Encryption.EnableInTransit,
                    enableTls: metricsOptions.Encryption.EnableTls);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<JonjubNet.Metrics.OpenTelemetry.OTLPExporter>());

            // Registrar InfluxDB sink con encriptación
            services.Configure<JonjubNet.Metrics.InfluxDB.InfluxOptions>(options =>
            {
                configuration.GetSection("Metrics:InfluxDB").Bind(options);
            });
            services.AddSingleton<JonjubNet.Metrics.InfluxDB.InfluxSink>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<JonjubNet.Metrics.InfluxDB.InfluxOptions>>();
                var metricsOptions = sp.GetRequiredService<IOptions<MetricsOptions>>().Value;
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<JonjubNet.Metrics.InfluxDB.InfluxSink>>();
                var encryptionService = metricsOptions.Encryption.EnableInTransit 
                    ? sp.GetService<EncryptionService>() 
                    : null;
                var secureHttpClientFactory = metricsOptions.Encryption.EnableTls 
                    ? sp.GetService<SecureHttpClientFactory>() 
                    : null;
                
                return new JonjubNet.Metrics.InfluxDB.InfluxSink(
                    options,
                    logger,
                    httpClient: null,
                    encryptionService,
                    secureHttpClientFactory,
                    encryptInTransit: metricsOptions.Encryption.EnableInTransit,
                    enableTls: metricsOptions.Encryption.EnableTls);
            });
            services.AddSingleton<IMetricsSink>(sp => sp.GetRequiredService<JonjubNet.Metrics.InfluxDB.InfluxSink>());
        }
    }
}
