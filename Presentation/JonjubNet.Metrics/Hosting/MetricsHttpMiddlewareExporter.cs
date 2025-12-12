using JonjubNet.Metrics.Prometheus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JonjubNet.Metrics.Hosting
{
    /// <summary>
    /// Middleware HTTP para exponer métricas Prometheus
    /// </summary>
    public class MetricsHttpMiddlewareExporter
    {
        private readonly RequestDelegate _next;
        private readonly PrometheusExporter _exporter;
        private readonly PrometheusOptions _options;
        private readonly ILogger<MetricsHttpMiddlewareExporter>? _logger;

        public MetricsHttpMiddlewareExporter(
            RequestDelegate next,
            PrometheusExporter exporter,
            IOptions<PrometheusOptions> options,
            ILogger<MetricsHttpMiddlewareExporter>? logger = null)
        {
            _next = next;
            _exporter = exporter;
            _options = options.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar si es la ruta de métricas
            if (context.Request.Path.Equals(_options.Path, StringComparison.OrdinalIgnoreCase))
            {
                if (!_options.Enabled || !_exporter.IsEnabled)
                {
                    context.Response.StatusCode = 503;
                    await context.Response.WriteAsync("Metrics export is disabled");
                    return;
                }

                try
                {
                    var metricsText = _exporter.GetMetricsText();
                    context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    await context.Response.WriteAsync(metricsText);
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error exporting metrics");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Error exporting metrics: {ex.Message}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
