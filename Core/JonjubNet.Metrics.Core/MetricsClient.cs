using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Core.MetricTypes;
using JonjubNet.Metrics.Core.Aggregation;

namespace JonjubNet.Metrics.Core
{
    /// <summary>
    /// Implementación del cliente de métricas (Fast Path)
    /// Optimizado: Solo escribe al Registry - todos los sinks leen del Registry
    /// </summary>
    public class MetricsClient : IMetricsClient
    {
        private readonly MetricRegistry _registry;

        public MetricsClient(MetricRegistry registry)
        {
            _registry = registry;
        }

        public Counter CreateCounter(string name, string description = "")
        {
            return _registry.GetOrCreateCounter(name, description);
        }

        public Gauge CreateGauge(string name, string description = "")
        {
            return _registry.GetOrCreateGauge(name, description);
        }

        public Histogram CreateHistogram(string name, string description = "", double[]? buckets = null)
        {
            return _registry.GetOrCreateHistogram(name, description, buckets);
        }

        public Summary CreateSummary(string name, string description = "", double[]? quantiles = null)
        {
            return _registry.GetOrCreateSummary(name, description, quantiles);
        }

        public void Increment(string name, double value = 1.0, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var counter = CreateCounter(name);
            counter.Inc(tags, value);
        }

        public void SetGauge(string name, double value, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var gauge = CreateGauge(name);
            gauge.Set(tags, value);
        }

        public void ObserveHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            // SOLO escritura al Registry - todos los sinks leen del Registry
            var histogram = CreateHistogram(name);
            histogram.Observe(tags, value);
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            var histogram = CreateHistogram(name);
            return TimerMetric.Start(histogram, tags);
        }

        /// <summary>
        /// Crea o obtiene un summary con ventana deslizante
        /// </summary>
        public SlidingWindowSummary CreateSlidingWindowSummary(
            string name, 
            string description, 
            TimeSpan windowSize, 
            double[]? quantiles = null)
        {
            return _registry.GetOrCreateSlidingWindowSummary(name, description, windowSize, quantiles);
        }

        /// <summary>
        /// Observa un valor en un summary con ventana deslizante
        /// </summary>
        public void ObserveSlidingWindowSummary(
            string name, 
            TimeSpan windowSize, 
            double value, 
            Dictionary<string, string>? tags = null)
        {
            var summary = CreateSlidingWindowSummary(name, "", windowSize);
            summary.Observe(tags, value);
        }

        /// <summary>
        /// Agrega un valor al agregador de métricas
        /// </summary>
        public void AddToAggregator(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            _registry.Aggregator.AddValue(metricName, value, tags);
        }

        /// <summary>
        /// Obtiene el valor agregado de una métrica
        /// </summary>
        public double? GetAggregatedValue(
            string metricName, 
            AggregationType aggregationType, 
            Dictionary<string, string>? tags = null)
        {
            return _registry.Aggregator.GetAggregatedValue(metricName, aggregationType, tags);
        }

        /// <summary>
        /// Obtiene estadísticas completas de una métrica agregada
        /// </summary>
        public AggregatedMetricStats? GetAggregatedStats(string metricName, Dictionary<string, string>? tags = null)
        {
            return _registry.Aggregator.GetStats(metricName, tags);
        }
    }
}
