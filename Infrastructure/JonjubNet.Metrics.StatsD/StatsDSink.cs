using System.Net;
using System.Net.Sockets;
using System.Text;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Metrics.StatsD
{
    /// <summary>
    /// Sink de métricas para StatsD
    /// </summary>
    public class StatsDSink : IMetricsSink
    {
        private readonly StatsDOptions _options;
        private readonly ILogger<StatsDSink>? _logger;
        private readonly UdpClient? _udpClient;
        private readonly IPEndPoint? _endPoint;

        public string Name => "StatsD";
        public bool IsEnabled => _options.Enabled;

        public StatsDSink(
            IOptions<StatsDOptions> options,
            ILogger<StatsDSink>? logger = null)
        {
            _options = options.Value;
            _logger = logger;

            if (_options.Enabled)
            {
                try
                {
                    _udpClient = new UdpClient();
                    var hostEntry = Dns.GetHostEntry(_options.Host);
                    _endPoint = new IPEndPoint(hostEntry.AddressList[0], _options.Port);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to initialize StatsD client, will use logging fallback");
                }
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
                var sb = new StringBuilder(1024); // Pre-allocate capacity
                var first = true;

                // Convertir Counters
                foreach (var counter in registry.GetAllCounters().Values)
                {
                    foreach (var (key, value) in counter.GetAllValues())
                    {
                        if (!first) sb.Append('\n');
                        var message = FormatFromRegistry(counter.Name, "counter", value, ParseKey(key));
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.Append(message);
                            first = false;
                        }
                    }
                }

                // Convertir Gauges
                foreach (var gauge in registry.GetAllGauges().Values)
                {
                    foreach (var (key, value) in gauge.GetAllValues())
                    {
                        if (!first) sb.Append('\n');
                        var message = FormatFromRegistry(gauge.Name, "gauge", value, ParseKey(key));
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.Append(message);
                            first = false;
                        }
                    }
                }

                // Similar para Histograms...

                if (sb.Length > 0)
                {
                    if (_udpClient != null && _endPoint != null)
                    {
                        var data = Encoding.UTF8.GetBytes(sb.ToString());
                        await _udpClient.SendAsync(data, data.Length, _endPoint);
                    }
                    else
                    {
                        _logger?.LogDebug("StatsD (fallback): {Messages}", sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to StatsD");
            }
        }

        private string FormatFromRegistry(string name, string type, double value, Dictionary<string, string>? tags)
        {
            var sb = new StringBuilder(64);
            sb.Append(name).Append(':').Append(value);

            string metricTypeSuffix = type switch
            {
                "counter" => "|c",
                "gauge" => "|g",
                "histogram" => "|h",
                "timer" => "|ms",
                _ => "|g"
            };
            sb.Append(metricTypeSuffix);

            if (tags != null && tags.Count > 0)
            {
                sb.Append("|#");
                bool firstTag = true;
                foreach (var kvp in tags)
                {
                    if (!firstTag) sb.Append(',');
                    sb.Append(kvp.Key).Append(':').Append(kvp.Value);
                    firstTag = false;
                }
            }
            return sb.ToString();
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
                // Usar StringBuilder para evitar allocations intermedias
                var sb = new StringBuilder(points.Count * 64); // Estimación: ~64 chars por mensaje
                
                var first = true;
                foreach (var point in points)
                {
                    var message = FormatMetricPoint(point);
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (!first)
                            sb.Append('\n');
                        sb.Append(message);
                        first = false;
                    }
                }

                if (sb.Length > 0)
                {
                    if (_udpClient != null && _endPoint != null)
                    {
                        var data = Encoding.UTF8.GetBytes(sb.ToString());
                        await _udpClient.SendAsync(data, data.Length, _endPoint);
                    }
                    else
                    {
                        // Fallback to logging
                        _logger?.LogDebug("StatsD (fallback): {Messages}", sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting metrics to StatsD");
                // Don't throw - metrics should not break the application
            }
        }

        private string FormatMetricPoint(MetricPoint point)
        {
            // Usar StringBuilder para evitar allocations intermedias
            var sb = new StringBuilder(64); // Estimación inicial
            
            sb.Append(point.Name);
            sb.Append(':');
            sb.Append(point.Value);
            
            // Tipo de métrica
            var typeChar = point.Type switch
            {
                MetricType.Counter => 'c',
                MetricType.Gauge => 'g',
                MetricType.Histogram => 'h',
                MetricType.Timer => 'm',
                _ => 'g'
            };
            sb.Append('|');
            sb.Append(typeChar);
            if (point.Type == MetricType.Timer)
            {
                sb.Append('s'); // "ms" para timer
            }
            
            // Tags
            if (point.Tags != null && point.Tags.Count > 0)
            {
                sb.Append("|#");
                var first = true;
                foreach (var kvp in point.Tags)
                {
                    if (!first)
                        sb.Append(',');
                    sb.Append(kvp.Key);
                    sb.Append(':');
                    sb.Append(kvp.Value);
                    first = false;
                }
            }
            
            return sb.ToString();
        }
    }
}
