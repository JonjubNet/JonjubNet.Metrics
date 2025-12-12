using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Models;
using JonjubNet.Metrics.Shared.Configuration;
using JonjubNet.Metrics.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Metrics.Services
{
    /// <summary>
    /// Implementación del servicio de métricas usando la nueva arquitectura
    /// Mantiene compatibilidad con la interfaz IMetricsService existente
    /// </summary>
    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly MetricsConfiguration _configuration;
        private readonly IMetricsClient _metricsClient;
        private readonly SecureTagValidator _tagValidator;

        public MetricsService(
            ILogger<MetricsService> logger,
            IOptions<MetricsConfiguration> configuration,
            IMetricsClient metricsClient,
            SecureTagValidator tagValidator)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _metricsClient = metricsClient;
            _tagValidator = tagValidator;
        }

        public async Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? labels = null)
        {
            if (!_configuration.Enabled || !_configuration.Counter.Enabled)
                return;

            try
            {
                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);
                _metricsClient.Increment(name, value, sanitizedLabels);
                _logger.LogDebug("Recorded counter {Name} with value {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording counter {Name}", name);
            }

            await Task.CompletedTask;
        }

        public async Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? labels = null)
        {
            if (!_configuration.Enabled || !_configuration.Gauge.Enabled)
                return;

            try
            {
                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);
                _metricsClient.SetGauge(name, value, sanitizedLabels);
                _logger.LogDebug("Recorded gauge {Name} with value {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording gauge {Name}", name);
            }

            await Task.CompletedTask;
        }

        public async Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? labels = null)
        {
            if (!_configuration.Enabled || !_configuration.Histogram.Enabled)
                return;

            try
            {
                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);
                _metricsClient.ObserveHistogram(name, value, sanitizedLabels);
                _logger.LogDebug("Recorded histogram {Name} with value {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording histogram {Name}", name);
            }

            await Task.CompletedTask;
        }

        public async Task RecordTimerAsync(string name, double duration, Dictionary<string, string>? labels = null)
        {
            if (!_configuration.Enabled || !_configuration.Timer.Enabled)
                return;

            try
            {
                // Timer se registra como histograma
                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);
                _metricsClient.ObserveHistogram(name, duration / 1000.0, sanitizedLabels); // Convertir ms a segundos
                _logger.LogDebug("Recorded timer {Name} with duration {Duration}ms", name, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording timer {Name}", name);
            }

            await Task.CompletedTask;
        }

        public async Task RecordHttpMetricsAsync(HttpMetrics metrics)
        {
            if (!_configuration.Enabled || !_configuration.Middleware.HttpMetrics.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>(metrics.Labels)
                {
                    ["method"] = metrics.Method,
                    ["endpoint"] = metrics.Endpoint,
                    ["status_code"] = metrics.StatusCode.ToString()
                };

                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);

                // Registrar contador de requests
                _metricsClient.Increment("http_requests_total", 1, sanitizedLabels);

                // Registrar histograma de duración
                _metricsClient.ObserveHistogram("http_request_duration_seconds", metrics.DurationMs / 1000.0, sanitizedLabels);

                // Registrar tamaños si están habilitados
                if (_configuration.Middleware.HttpMetrics.TrackRequestSize)
                {
                    _metricsClient.ObserveHistogram("http_request_size_bytes", metrics.RequestSizeBytes, sanitizedLabels);
                }

                if (_configuration.Middleware.HttpMetrics.TrackResponseSize)
                {
                    _metricsClient.ObserveHistogram("http_response_size_bytes", metrics.ResponseSizeBytes, sanitizedLabels);
                }

                _logger.LogDebug("Recorded HTTP metrics for {Method} {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms", 
                    metrics.Method, metrics.Endpoint, metrics.StatusCode, metrics.DurationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording HTTP metrics");
            }

            await Task.CompletedTask;
        }

        public async Task RecordDatabaseMetricsAsync(DatabaseMetrics metrics)
        {
            if (!_configuration.Enabled || !_configuration.Middleware.DatabaseMetrics.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>(metrics.Labels)
                {
                    ["operation"] = metrics.Operation,
                    ["table"] = metrics.Table,
                    ["database"] = metrics.Database,
                    ["success"] = metrics.IsSuccess.ToString().ToLowerInvariant()
                };

                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);

                // Registrar contador de queries
                _metricsClient.Increment("database_queries_total", 1, sanitizedLabels);

                // Registrar histograma de duración
                _metricsClient.ObserveHistogram("database_query_duration_seconds", metrics.DurationMs / 1000.0, sanitizedLabels);

                _logger.LogDebug("Recorded database metrics for {Operation} on {Table} - Success: {Success}, Duration: {Duration}ms", 
                    metrics.Operation, metrics.Table, metrics.IsSuccess, metrics.DurationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording database metrics");
            }

            await Task.CompletedTask;
        }

        public async Task RecordBusinessMetricsAsync(BusinessMetrics metrics)
        {
            if (!_configuration.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>(metrics.Labels)
                {
                    ["operation"] = metrics.Operation,
                    ["metric_type"] = metrics.MetricType,
                    ["category"] = metrics.Category,
                    ["success"] = metrics.IsSuccess.ToString().ToLowerInvariant()
                };

                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);

                // Registrar como gauge o counter según el tipo
                if (metrics.MetricType == "Revenue" || metrics.MetricType == "Value")
                {
                    _metricsClient.SetGauge($"business_{metrics.MetricType.ToLowerInvariant()}_total", metrics.Value, sanitizedLabels);
                }
                else
                {
                    _metricsClient.Increment($"business_{metrics.MetricType.ToLowerInvariant()}_total", metrics.Value, sanitizedLabels);
                }

                _logger.LogDebug("Recorded business metrics for {Operation} - Type: {MetricType}, Value: {Value}", 
                    metrics.Operation, metrics.MetricType, metrics.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording business metrics");
            }

            await Task.CompletedTask;
        }

        public async Task RecordSystemMetricsAsync(SystemMetrics metrics)
        {
            if (!_configuration.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>(metrics.Labels)
                {
                    ["metric_type"] = metrics.MetricType,
                    ["instance"] = metrics.Instance,
                    ["unit"] = metrics.Unit
                };

                var sanitizedLabels = _tagValidator.ValidateAndSanitize(labels);

                // Registrar como gauge
                _metricsClient.SetGauge($"system_{metrics.MetricType.ToLowerInvariant()}", metrics.Value, sanitizedLabels);

                _logger.LogDebug("Recorded system metrics for {MetricType} - Value: {Value} {Unit}", 
                    metrics.MetricType, metrics.Value, metrics.Unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording system metrics");
            }

            await Task.CompletedTask;
        }
    }
}
