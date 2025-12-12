using FluentAssertions;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.MetricTypes;
using Xunit;

namespace JonjubNet.Metrics.Core.Tests
{
    public class MetricsClientTests
    {
        [Fact]
        public void Increment_ShouldCreateCounterAndIncrement()
        {
            // Arrange
            var registry = new MetricRegistry();
            var bus = new MetricBus();
            var client = new MetricsClient(registry, bus);

            // Act
            client.Increment("test_counter", 5.0);

            // Assert
            var counter = registry.GetOrCreateCounter("test_counter", "");
            counter.GetValue().Should().Be(5);
        }

        [Fact]
        public void SetGauge_ShouldCreateGaugeAndSetValue()
        {
            // Arrange
            var registry = new MetricRegistry();
            var bus = new MetricBus();
            var client = new MetricsClient(registry, bus);

            // Act
            client.SetGauge("test_gauge", 42.5);

            // Assert
            var gauge = registry.GetOrCreateGauge("test_gauge", "");
            gauge.GetValue().Should().Be(42.5);
        }

        [Fact]
        public void ObserveHistogram_ShouldCreateHistogramAndObserve()
        {
            // Arrange
            var registry = new MetricRegistry();
            var bus = new MetricBus();
            var client = new MetricsClient(registry, bus);

            // Act
            client.ObserveHistogram("test_histogram", 10.5);

            // Assert
            var histogram = registry.GetOrCreateHistogram("test_histogram", "");
            var data = histogram.GetData();
            data.Should().NotBeNull();
            data!.Count.Should().Be(1);
            data.Sum.Should().Be(10.5);
        }

        [Fact]
        public void CreateCounter_ShouldReturnCounter()
        {
            // Arrange
            var registry = new MetricRegistry();
            var bus = new MetricBus();
            var client = new MetricsClient(registry, bus);

            // Act
            var counter = client.CreateCounter("test_counter", "Description");

            // Assert
            counter.Should().NotBeNull();
            counter.Name.Should().Be("test_counter");
            counter.Description.Should().Be("Description");
        }

        [Fact]
        public void Increment_WithTags_ShouldIncludeTags()
        {
            // Arrange
            var registry = new MetricRegistry();
            var bus = new MetricBus();
            var client = new MetricsClient(registry, bus);
            var tags = new Dictionary<string, string> { ["env"] = "prod" };

            // Act
            client.Increment("test_counter", 1.0, tags);

            // Assert
            var counter = registry.GetOrCreateCounter("test_counter", "");
            counter.GetValue(tags).Should().Be(1);
        }
    }
}

