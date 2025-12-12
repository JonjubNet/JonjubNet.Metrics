using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Metrics.Core;

namespace JonjubNet.Metrics.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class MetricsClientBenchmark
    {
        private MetricsClient _client = null!;
        private Dictionary<string, string> _tags = null!;

        [GlobalSetup]
        public void Setup()
        {
            var registry = new MetricRegistry();
            var bus = new MetricBus(capacity: 10000);
            _client = new MetricsClient(registry, bus);
            _tags = new Dictionary<string, string> { ["env"] = "prod" };
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Cleanup handled by GC
        }

        [Benchmark]
        public void Increment()
        {
            _client.Increment("test_counter", 1.0, _tags);
        }

        [Benchmark]
        public void SetGauge()
        {
            _client.SetGauge("test_gauge", 42.5, _tags);
        }

        [Benchmark]
        public void ObserveHistogram()
        {
            _client.ObserveHistogram("test_histogram", 10.5, _tags);
        }
    }
}

