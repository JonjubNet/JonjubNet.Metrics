using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Shared.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JonjubNet.Metrics.HealthChecks
{
    /// <summary>
    /// Extensiones para integrar health checks de métricas con ASP.NET Core
    /// </summary>
    public static class MetricsHealthCheckExtensions
    {
        /// <summary>
        /// Agrega health check de métricas al sistema de health checks de ASP.NET Core
        /// </summary>
        public static IHealthChecksBuilder AddMetricsHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "metrics",
            HealthStatus failureStatus = HealthStatus.Degraded,
            IEnumerable<string>? tags = null)
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    // ELIMINADO: Bus ya no se necesita
                    var sinks = sp.GetServices<IMetricsSink>();
                    var scheduler = sp.GetService<MetricFlushScheduler>();
                    var options = sp.GetRequiredService<IOptions<Shared.Configuration.MetricsOptions>>();
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<MetricsHealthCheck>>();
                    var healthCheck = new MetricsHealthCheck(sinks, scheduler, options, logger);
                    return new MetricsHealthCheckAdapter(healthCheck);
                },
                failureStatus,
                tags));
        }
    }

    /// <summary>
    /// Adaptador de IMetricsHealthCheck a IHealthCheck de ASP.NET Core
    /// </summary>
    internal class MetricsHealthCheckAdapter : IHealthCheck
    {
        private readonly IMetricsHealthCheck _metricsHealthCheck;

        public MetricsHealthCheckAdapter(IMetricsHealthCheck metricsHealthCheck)
        {
            _metricsHealthCheck = metricsHealthCheck;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var overallHealth = _metricsHealthCheck.GetOverallHealth();

            var status = overallHealth.IsHealthy ? HealthStatus.Healthy : HealthStatus.Degraded;

            var data = new Dictionary<string, object>
            {
                ["BusQueueUtilization"] = $"{overallHealth.BusHealth.QueueUtilizationPercent:F2}%",
                ["SchedulerRunning"] = overallHealth.SchedulerHealth.IsRunning,
                ["SinksCount"] = overallHealth.SinksHealth.Count,
                ["HealthySinks"] = overallHealth.SinksHealth.Values.Count(s => s.IsHealthy)
            };

            foreach (var sink in overallHealth.SinksHealth)
            {
                data[$"Sink_{sink.Key}_Enabled"] = sink.Value.IsEnabled;
                data[$"Sink_{sink.Key}_Healthy"] = sink.Value.IsHealthy;
            }

            var result = new HealthCheckResult(
                status,
                overallHealth.OverallStatusMessage ?? "Metrics system is healthy",
                data: data);

            return Task.FromResult(result);
        }
    }
}
