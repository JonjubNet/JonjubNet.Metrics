using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Shared.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JonjubNet.Metrics.Shared.Security;

namespace JonjubNet.Metrics.OpenTelemetry
{
    /// <summary>
    /// Exporter de métricas para OpenTelemetry Collector
    /// </summary>
    public class OTLPExporter : IMetricsSink
    {
        private readonly OTLOptions _options;
        private readonly ILogger<OTLPExporter>? _logger;
        private readonly HttpClient _httpClient;
        private readonly EncryptionService? _encryptionService;
        private readonly bool _encryptInTransit;
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptionsCache.GetDefault();

        public string Name => "OpenTelemetry";
        public bool IsEnabled => _options.Enabled;

        public OTLPExporter(
            IOptions<OTLOptions> options,
            ILogger<OTLPExporter>? logger = null,
            HttpClient? httpClient = null,
            EncryptionService? encryptionService = null,
            SecureHttpClientFactory? secureHttpClientFactory = null,
            bool encryptInTransit = false,
            bool enableTls = true)
        {
            _options = options.Value;
            _logger = logger;
            _encryptionService = encryptionService;
            _encryptInTransit = encryptInTransit;
            
            // Usar SecureHttpClientFactory si TLS está habilitado y está disponible
            if (enableTls && secureHttpClientFactory != null && !string.IsNullOrEmpty(_options.Endpoint))
            {
                _httpClient = secureHttpClientFactory.CreateSecureClient(_options.Endpoint);
            }
            else
            {
                _httpClient = httpClient ?? new HttpClient();
            }
        }

        /// <summary>
        /// Exporta métricas desde el Registry (método principal optimizado)
        /// </summary>
        public async ValueTask ExportFromRegistryAsync(MetricRegistry registry, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var otlpPayload = ConvertRegistryToOTLPFormat(registry);
                var url = GetOTLPUrl();

                HttpContent content;
                
                // Compresión opcional para batches grandes
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(otlpPayload, JsonOptions);
                
                // Encriptación en tránsito si está habilitada
                if (_encryptInTransit && _encryptionService != null)
                {
                    jsonBytes = _encryptionService.Encrypt(jsonBytes);
                    _logger?.LogDebug("Metrics encrypted for transit to OTLP endpoint");
                }
                
                if (_options.EnableCompression && jsonBytes.Length > 1024)
                {
                    var compressed = CompressionHelper.CompressGZip(jsonBytes);
                    content = new ByteArrayContent(compressed);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    content.Headers.ContentEncoding.Add("gzip");
                    if (_encryptInTransit)
                    {
                        content.Headers.Add("X-Encrypted", "true");
                    }
                }
                else
                {
                    content = new ByteArrayContent(jsonBytes);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    if (_encryptInTransit)
                    {
                        content.Headers.Add("X-Encrypted", "true");
                    }
                }
                
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("OTLP export failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported metrics to OTLP endpoint {Endpoint}", _options.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to OTLP");
            }
        }

        /// <summary>
        /// Exporta métricas desde una lista de puntos (DEPRECATED)
        /// </summary>
        [Obsolete("Use ExportFromRegistryAsync instead")]
        public async ValueTask ExportAsync(IReadOnlyList<MetricPoint> points, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
                return;

            try
            {
                var otlpPayload = ConvertToOTLPFormat(points);
                var url = GetOTLPUrl();

                HttpContent content;
                
                // Compresión opcional para batches grandes
                if (_options.EnableCompression && points.Count > 50)
                {
                    var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(otlpPayload, JsonOptions);
                    var compressed = CompressionHelper.CompressGZip(jsonBytes);
                    content = new ByteArrayContent(compressed);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    content.Headers.ContentEncoding.Add("gzip");
                }
                else
                {
                    content = JsonContent.Create(otlpPayload, options: JsonOptions);
                }
                
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("OTLP export failed with status {StatusCode}: {Error}", 
                        response.StatusCode, errorContent);
                }
                else
                {
                    _logger?.LogDebug("Exported {Count} metrics to OTLP endpoint {Endpoint}", 
                        points.Count, _options.Endpoint);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to OTLP");
                // Don't throw - metrics should not break the application
            }
        }

        private string GetOTLPUrl()
        {
            return _options.Protocol switch
            {
                OtlpProtocol.HttpProtobuf => $"{_options.Endpoint}/v1/metrics",
                OtlpProtocol.HttpJson => $"{_options.Endpoint}/v1/metrics",
                OtlpProtocol.Grpc => throw new NotSupportedException("gRPC protocol requires additional libraries"),
                _ => $"{_options.Endpoint}/v1/metrics"
            };
        }

        private object ConvertRegistryToOTLPFormat(MetricRegistry registry)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var metrics = new List<object>();

            // Convertir Counters
            foreach (var counter in registry.GetAllCounters().Values)
            {
                foreach (var (key, value) in counter.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = counter.Name,
                        description = counter.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asInt = (long?)value,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = gauge.Name,
                        description = gauge.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asDouble = (double?)value,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Histograms
            foreach (var histogram in registry.GetAllHistograms().Values)
            {
                foreach (var (key, data) in histogram.GetAllData())
                {
                    var tags = ParseKey(key);
                    metrics.Add(new
                    {
                        name = histogram.Name,
                        description = histogram.Description,
                        unit = "",
                        data = new
                        {
                            dataPoints = new[]
                            {
                                new
                                {
                                    asDouble = (double?)data.Sum,
                                    timeUnixNano = timestamp,
                                    attributes = tags.Select(tagKvp => new
                                    {
                                        key = tagKvp.Key,
                                        value = new { stringValue = tagKvp.Value }
                                    }).ToArray()
                                }
                            }
                        }
                    });
                }
            }

            // Convertir Summaries
            foreach (var summary in registry.GetAllSummaries().Values)
            {
                foreach (var (key, data) in summary.GetAllData())
                {
                    var tags = ParseKey(key);
                    var quantiles = data.GetQuantiles();
                    foreach (var quantile in quantiles)
                    {
                        metrics.Add(new
                        {
                            name = summary.Name,
                            description = summary.Description,
                            unit = "",
                            data = new
                            {
                                dataPoints = new[]
                                {
                                    new
                                    {
                                        asDouble = (double?)quantile.Value,
                                        timeUnixNano = timestamp,
                                        attributes = tags.Concat(new[] { new KeyValuePair<string, string>("quantile", quantile.Key.ToString()) })
                                            .Select(tagKvp => new
                                            {
                                                key = tagKvp.Key,
                                                value = new { stringValue = tagKvp.Value }
                                            }).ToArray()
                                    }
                                }
                            }
                        });
                    }
                }
            }

            var resourceMetrics = new
            {
                resourceMetrics = new[]
                {
                    new
                    {
                        resource = new { },
                        scopeMetrics = new[]
                        {
                            new
                            {
                                scope = new { },
                                metrics = metrics.ToArray()
                            }
                        }
                    }
                }
            };

            return resourceMetrics;
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }

        private object ConvertToOTLPFormat(IReadOnlyList<MetricPoint> points)
        {
            // Simplified OTLP format - for production, use OpenTelemetry.Proto
            var resourceMetrics = new
            {
                resourceMetrics = new[]
                {
                    new
                    {
                        resource = new { },
                        scopeMetrics = new[]
                        {
                            new
                            {
                                scope = new { },
                                metrics = points.Select(p => new
                                {
                                    name = p.Name,
                                    description = "",
                                    unit = "",
                                    data = new
                                    {
                                        dataPoints = new[]
                                        {
                                            new
                                            {
                                                asInt = p.Type == MetricType.Counter ? (long?)p.Value : null,
                                                asDouble = p.Type != MetricType.Counter ? (double?)p.Value : null,
                                                timeUnixNano = ((DateTimeOffset)p.Timestamp).ToUnixTimeMilliseconds() * 1_000_000,
                                                attributes = p.Tags?.Select(kvp => new
                                                {
                                                    key = kvp.Key,
                                                    value = new { stringValue = kvp.Value }
                                                }).ToArray() ?? Array.Empty<object>()
                                            }
                                        }
                                    }
                                }).ToArray()
                            }
                        }
                    }
                }
            };

            return resourceMetrics;
        }
    }
}
