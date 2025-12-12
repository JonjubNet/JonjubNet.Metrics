using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Metrics.Core.Tests.Performance
{
    /// <summary>
    /// Tests de performance simples usando xUnit (sin BenchmarkDotNet)
    /// </summary>
    public class MetricsPerformanceSimpleTests
    {
        [Fact]
        public void Counter_Increment_ShouldBeFast()
        {
            var counter = new Counter("test", "Test");
            var start = DateTime.UtcNow;

            for (int i = 0; i < 10000; i++)
            {
                counter.Inc(value: 1.0);
            }

            var elapsed = DateTime.UtcNow - start;
            var opsPerSecond = 10000 / elapsed.TotalSeconds;

            // Debería ser capaz de hacer al menos 100K ops/segundo
            Assert.True(opsPerSecond > 100000, $"Performance too low: {opsPerSecond:F0} ops/sec");
        }

        [Fact]
        public void MetricBus_TryWrite_ShouldBeFast()
        {
            var bus = new MetricBus(capacity: 100000);
            var evt = new MetricEvent
            {
                Name = "test",
                Type = MetricType.Counter,
                Value = 1.0
            };

            var start = DateTime.UtcNow;
            int count = 0;

            for (int i = 0; i < 10000; i++)
            {
                if (bus.TryWrite(evt))
                    count++;
            }

            var elapsed = DateTime.UtcNow - start;
            var opsPerSecond = count / elapsed.TotalSeconds;

            // Debería ser capaz de hacer al menos 50K ops/segundo
            Assert.True(opsPerSecond > 50000, $"Performance too low: {opsPerSecond:F0} ops/sec");
        }

        [Fact]
        public void MetricRegistry_GetOrCreate_ShouldBeFast()
        {
            var registry = new MetricRegistry();
            var start = DateTime.UtcNow;

            for (int i = 0; i < 1000; i++)
            {
                registry.GetOrCreateCounter($"counter_{i}", "Test");
            }

            var elapsed = DateTime.UtcNow - start;
            var opsPerSecond = 1000 / elapsed.TotalSeconds;

            // Debería ser capaz de hacer al menos 10K ops/segundo
            Assert.True(opsPerSecond > 10000, $"Performance too low: {opsPerSecond:F0} ops/sec");
        }
    }
}

