using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Metrics.Core;

namespace JonjubNet.Metrics.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class MetricBusBenchmark
    {
        private MetricBus _bus = null!;
        private MetricEvent _testEvent = null!;

        [GlobalSetup]
        public void Setup()
        {
            _bus = new MetricBus(capacity: 10000);
            _testEvent = new MetricEvent
            {
                Name = "test_metric",
                Type = MetricType.Counter,
                Value = 1.0,
                Tags = new Dictionary<string, string> { ["env"] = "prod" }
            };
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _bus?.Dispose();
        }

        [Benchmark]
        public bool TryWrite()
        {
            return _bus.TryWrite(_testEvent);
        }

        [Benchmark]
        public async ValueTask<bool> WriteAsync()
        {
            return await _bus.WriteAsync(_testEvent);
        }
    }
}
