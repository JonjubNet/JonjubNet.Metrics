using JonjubNet.Metrics.Configuration;
using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Collections.Concurrent;

namespace JonjubNet.Metrics.Services
{
    /// <summary>
    /// Implementación del servicio de métricas usando Prometheus
    /// </summary>
    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly MetricsConfiguration _configuration;
        private readonly ConcurrentDictionary<string, Counter> _counters = new();
        private readonly ConcurrentDictionary<string, Gauge> _gauges = new();
        private readonly ConcurrentDictionary<string, Histogram> _histograms = new();
        private readonly ConcurrentDictionary<string, Histogram> _timers = new();

        public MetricsService(
            ILogger<MetricsService> logger,
            IOptions<MetricsConfiguration> configuration)
        {
            _logger = logger;
            _configuration = configuration.Value;
        }

        public async Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? labels = null)
        {
            if (!_configuration.Enabled || !_configuration.Counter.Enabled)
                return;

            try
            {
                var counter = _counters.GetOrAdd(name, _ => 
                    Metrics.CreateCounter(name, $"Counter metric: {name}", GetLabelNames(labels)));

                var labelValues = GetLabelValues(labels);
                counter.WithLabels(labelValues).Inc(value);

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
                var gauge = _gauges.GetOrAdd(name, _ => 
                    Metrics.CreateGauge(name, $"Gauge metric: {name}", GetLabelNames(labels)));

                var labelValues = GetLabelValues(labels);
                gauge.WithLabels(labelValues).Set(value);

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
                var histogram = _histograms.GetOrAdd(name, _ => 
                    Metrics.CreateHistogram(name, $"Histogram metric: {name}", new HistogramConfiguration
                    {
                        Buckets = _configuration.Histogram.DefaultBuckets,
                        LabelNames = GetLabelNames(labels)
                    }));

                var labelValues = GetLabelValues(labels);
                histogram.WithLabels(labelValues).Observe(value);

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
                var timer = _timers.GetOrAdd(name, _ => 
                    Metrics.CreateHistogram(name, $"Timer metric: {name}", new HistogramConfiguration
                    {
                        Buckets = _configuration.Timer.DefaultBuckets,
                        LabelNames = GetLabelNames(labels)
                    }));

                var labelValues = GetLabelValues(labels);
                timer.WithLabels(labelValues).Observe(duration);

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
                var labels = new Dictionary<string, string>
                {
                    ["method"] = metrics.Method,
                    ["endpoint"] = metrics.Endpoint,
                    ["status_code"] = metrics.StatusCode.ToString()
                };

                // Agregar etiquetas adicionales
                foreach (var label in metrics.Labels)
                {
                    labels[label.Key] = label.Value;
                }

                // Registrar duración
                if (_configuration.Middleware.HttpMetrics.TrackRequestDuration)
                {
                    await RecordHistogramAsync("http_request_duration_ms", metrics.DurationMs, labels);
                }

                // Registrar contador de requests
                await RecordCounterAsync("http_requests_total", 1, labels);

                // Registrar tamaño de request
                if (_configuration.Middleware.HttpMetrics.TrackRequestSize)
                {
                    await RecordHistogramAsync("http_request_size_bytes", metrics.RequestSizeBytes, labels);
                }

                // Registrar tamaño de response
                if (_configuration.Middleware.HttpMetrics.TrackResponseSize)
                {
                    await RecordHistogramAsync("http_response_size_bytes", metrics.ResponseSizeBytes, labels);
                }

                _logger.LogDebug("Recorded HTTP metrics for {Method} {Endpoint}", metrics.Method, metrics.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording HTTP metrics");
            }
        }

        public async Task RecordDatabaseMetricsAsync(DatabaseMetrics metrics)
        {
            if (!_configuration.Enabled || !_configuration.Middleware.DatabaseMetrics.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>
                {
                    ["operation"] = metrics.Operation,
                    ["table"] = metrics.Table,
                    ["database"] = metrics.Database,
                    ["success"] = metrics.IsSuccess.ToString()
                };

                // Agregar etiquetas adicionales
                foreach (var label in metrics.Labels)
                {
                    labels[label.Key] = label.Value;
                }

                // Registrar duración de consulta
                if (_configuration.Middleware.DatabaseMetrics.TrackQueryDuration)
                {
                    await RecordHistogramAsync("database_query_duration_ms", metrics.DurationMs, labels);
                }

                // Registrar contador de consultas
                if (_configuration.Middleware.DatabaseMetrics.TrackQueryCount)
                {
                    await RecordCounterAsync("database_queries_total", 1, labels);
                }

                // Registrar registros afectados
                await RecordHistogramAsync("database_records_affected", metrics.RecordsAffected, labels);

                _logger.LogDebug("Recorded database metrics for {Operation} on {Table}", metrics.Operation, metrics.Table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording database metrics");
            }
        }

        public async Task RecordBusinessMetricsAsync(BusinessMetrics metrics)
        {
            if (!_configuration.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>
                {
                    ["operation"] = metrics.Operation,
                    ["metric_type"] = metrics.MetricType,
                    ["category"] = metrics.Category,
                    ["success"] = metrics.IsSuccess.ToString()
                };

                // Agregar etiquetas adicionales
                foreach (var label in metrics.Labels)
                {
                    labels[label.Key] = label.Value;
                }

                // Registrar valor de la métrica
                await RecordGaugeAsync($"business_{metrics.MetricType.ToLower()}", metrics.Value, labels);

                // Registrar duración si está disponible
                if (metrics.DurationMs > 0)
                {
                    await RecordHistogramAsync("business_operation_duration_ms", metrics.DurationMs, labels);
                }

                _logger.LogDebug("Recorded business metrics for {Operation} - {MetricType}", metrics.Operation, metrics.MetricType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording business metrics");
            }
        }

        public async Task RecordSystemMetricsAsync(SystemMetrics metrics)
        {
            if (!_configuration.Enabled)
                return;

            try
            {
                var labels = new Dictionary<string, string>
                {
                    ["metric_type"] = metrics.MetricType,
                    ["unit"] = metrics.Unit,
                    ["instance"] = metrics.Instance
                };

                // Agregar etiquetas adicionales
                foreach (var label in metrics.Labels)
                {
                    labels[label.Key] = label.Value;
                }

                // Registrar métrica del sistema
                await RecordGaugeAsync($"system_{metrics.MetricType.ToLower()}", metrics.Value, labels);

                _logger.LogDebug("Recorded system metrics for {MetricType}", metrics.MetricType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording system metrics");
            }
        }

        private string[] GetLabelNames(Dictionary<string, string>? labels)
        {
            return labels?.Keys.ToArray() ?? Array.Empty<string>();
        }

        private string[] GetLabelValues(Dictionary<string, string>? labels)
        {
            return labels?.Values.ToArray() ?? Array.Empty<string>();
        }
    }
}

