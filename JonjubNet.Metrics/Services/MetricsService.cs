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
                return;

            try
            {
                await SendMetricsAsync("technical", metrics, _configuration.Technical);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas técnicas de forma asíncrona");
            }
        }

        public async Task RecordFunctionalMetricsAsync(FunctionalMetrics metrics)
        {
            if (!ShouldRecordMetrics("functional", _configuration.Functional))
                return;

            try
            {
                await SendMetricsAsync("functional", metrics, _configuration.Functional);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas funcionales de forma asíncrona");
            }
        }

        public async Task RecordOperationalMetricsAsync(OperationalMetrics metrics)
        {
            if (!ShouldRecordMetrics("operational", _configuration.Operational))
                return;

            try
            {
                await SendMetricsAsync("operational", metrics, _configuration.Operational);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas operativas de forma asíncrona");
            }
        }

        public void RecordTechnicalMetrics(TechnicalMetrics metrics)
        {
            if (!ShouldRecordMetrics("technical", _configuration.Technical))
                return;

            try
            {
                _ = Task.Run(async () => await SendMetricsAsync("technical", metrics, _configuration.Technical));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas técnicas de forma síncrona");
            }
        }

        public void RecordFunctionalMetrics(FunctionalMetrics metrics)
        {
            if (!ShouldRecordMetrics("functional", _configuration.Functional))
                return;

            try
            {
                _ = Task.Run(async () => await SendMetricsAsync("functional", metrics, _configuration.Functional));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas funcionales de forma síncrona");
            }
        }

        public void RecordOperationalMetrics(OperationalMetrics metrics)
        {
            if (!ShouldRecordMetrics("operational", _configuration.Operational))
                return;

            try
            {
                _ = Task.Run(async () => await SendMetricsAsync("operational", metrics, _configuration.Operational));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar métricas operativas de forma síncrona");
            }
        }

        private async Task SendMetricsAsync(string endpoint, object metrics, MetricsTypeConfiguration typeConfig)
        {
            try
            {
                // Aplicar filtros si están configurados
                if (!ShouldSendMetrics(metrics, typeConfig))
                    return;

                // Enriquecer métricas con información del servicio
                var enrichedMetrics = EnrichMetrics(metrics);

                var json = JsonConvert.SerializeObject(enrichedMetrics);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Usar endpoint configurado o por defecto
                var endpointUrl = GetEndpointUrl(endpoint);
                var response = await _httpClient.PostAsync($"{_configuration.MetricsServiceUrl}{endpointUrl}", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("El microservicio de métricas respondió con código {StatusCode}: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conectividad con el microservicio de métricas");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout al comunicarse con el microservicio de métricas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar métricas al microservicio");
            }
        }

        private bool ShouldRecordMetrics(string metricType, MetricsTypeConfiguration typeConfig)
        {
            // Verificar si las métricas están habilitadas globalmente
            if (!_configuration.Enabled)
                return false;

            // Verificar si este tipo de métrica está habilitado
            if (!typeConfig.Enabled)
                return false;

            // Verificar límite de velocidad
            if (typeConfig.RateLimitPerMinute > 0)
            {
                var key = $"{metricType}_{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";
                var count = _rateLimitCounters.AddOrUpdate(key, 1, (k, v) => v + 1);
                
                if (count > typeConfig.RateLimitPerMinute)
                {
                    _logger.LogDebug("Límite de velocidad alcanzado para métricas {MetricType}: {Count}/{Limit}", 
                        metricType, count, typeConfig.RateLimitPerMinute);
                    return false;
                }
            }

            // Verificar muestreo
            if (typeConfig.SamplingIntervalMs > 0)
            {
                var key = $"{metricType}_sampling";
                var now = DateTime.UtcNow;
                var lastSent = _lastSentTimes.GetOrAdd(key, now);
                
                if ((now - lastSent).TotalMilliseconds < typeConfig.SamplingIntervalMs)
                {
                    return false;
                }
                
                _lastSentTimes[key] = now;
            }

            return true;
        }

        private bool ShouldSendMetrics(object metrics, MetricsTypeConfiguration typeConfig)
        {
            // Aplicar filtros específicos según el tipo de métrica
            if (metrics is TechnicalMetrics technical)
            {
                if (_configuration.Filters.MinExecutionTimeMs > 0 && 
                    technical.ExecutionTimeMs < _configuration.Filters.MinExecutionTimeMs)
                {
                    return false;
                }
            }
            else if (metrics is OperationalMetrics operational)
            {
                if (_configuration.Filters.FilterByStatusCode && 
                    _configuration.Filters.ExcludedStatusCodes.Contains(operational.StatusCode))
                {
                    return false;
                }
            }

            return true;
        }

        private object EnrichMetrics(object metrics)
        {
            // Enriquecer métricas con información del servicio detectada automáticamente
            if (metrics is TechnicalMetrics technical)
            {
                technical.ServiceName = _configuration.ServiceName;
                technical.Environment = _configuration.Environment;
                technical.Version = _configuration.Version;
                technical.Timestamp = DateTime.UtcNow;
            }
            else if (metrics is FunctionalMetrics functional)
            {
                functional.ServiceName = _configuration.ServiceName;
                functional.Environment = _configuration.Environment;
                functional.Timestamp = DateTime.UtcNow;
            }
            else if (metrics is OperationalMetrics operational)
            {
                operational.ServiceName = _configuration.ServiceName;
                operational.Environment = _configuration.Environment;
                operational.Timestamp = DateTime.UtcNow;
            }

            return metrics;
        }

        private string GetEndpointUrl(string endpoint)
        {
            return endpoint switch
            {
                "technical" => _configuration.Endpoints.Technical,
                "functional" => _configuration.Endpoints.Functional,
                "operational" => _configuration.Endpoints.Operational,
                "health" => _configuration.Endpoints.Health,
                "custom" => _configuration.Endpoints.Custom,
                _ => $"/api/metrics/{endpoint}"
            };
        }
    }
}

