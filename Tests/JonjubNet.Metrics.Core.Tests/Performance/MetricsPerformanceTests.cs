using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Metrics.Core.Tests.Performance
{
    /// <summary>
    /// Tests de performance para componentes de métricas
    /// Ejecutar con: dotnet run --project Tests/JonjubNet.Metrics.Core.Tests -c Release
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class MetricsPerformanceTests
    {
        private MetricRegistry _registry = null!;
        private MetricBus _bus = null!;
        private MetricsClient _client = null!;
        private Counter _counter = null!;
        private Gauge _gauge = null!;
        private Histogram _histogram = null!;

        [GlobalSetup]
        public void Setup()
        {
            _registry = new MetricRegistry();
            _bus = new MetricBus(capacity: 10000);
            _client = new MetricsClient(_registry, _bus);
            _counter = _registry.GetOrCreateCounter("test_counter", "Test counter");
            _gauge = _registry.GetOrCreateGauge("test_gauge", "Test gauge");
            _histogram = _registry.GetOrCreateHistogram("test_histogram", "Test histogram");
        }

        [Benchmark]
        public void Counter_Increment()
        {
            _counter.Inc(value: 1.0);
        }

        [Benchmark]
        public void Counter_Increment_WithTags()
        {
            var tags = new Dictionary<string, string> { ["env"] = "prod", ["service"] = "api" };
            _counter.Inc(tags, 1.0);
        }

        [Benchmark]
        public void Gauge_Set()
        {
            _gauge.Set(value: 42.5);
        }

        [Benchmark]
        public void Histogram_Observe()
        {
            _histogram.Observe(value: 10.5);
        }

        [Benchmark]
        public void MetricsClient_Increment()
        {
            _client.Increment("benchmark_counter", 1.0);
        }

        [Benchmark]
        public void MetricBus_TryWrite()
        {
            var evt = new MetricEvent
            {
                Name = "test_metric",
                Type = MetricType.Counter,
                Value = 1.0
            };
            _bus.TryWrite(evt);
        }

        [Benchmark]
        public void MetricRegistry_GetOrCreateCounter()
        {
            _registry.GetOrCreateCounter("new_counter", "Description");
        }

        [Fact]
        public void RunBenchmarks()
        {
            // Este test puede ejecutarse manualmente para ver los resultados
            // BenchmarkRunner.Run<MetricsPerformanceTests>();
        }
    }
}

