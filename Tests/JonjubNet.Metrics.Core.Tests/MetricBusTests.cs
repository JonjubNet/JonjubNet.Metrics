using FluentAssertions;
using JonjubNet.Metrics.Core;
using Xunit;

namespace JonjubNet.Metrics.Core.Tests
{
    public class MetricBusTests
    {
        [Fact]
        public void TryWrite_ShouldReturnTrue_WhenChannelHasCapacity()
        {
            // Arrange
            var bus = new MetricBus(capacity: 10);
            var metricEvent = new MetricEvent
            {
                Name = "test_metric",
                Type = MetricType.Counter,
                Value = 1.0
            };

            // Act
            var result = bus.TryWrite(metricEvent);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task WriteAsync_ShouldWriteEvent()
        {
            // Arrange
            var bus = new MetricBus(capacity: 10);
            var metricEvent = new MetricEvent
            {
                Name = "test_metric",
                Type = MetricType.Counter,
                Value = 1.0
            };

            // Act
            var result = await bus.WriteAsync(metricEvent);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ReadAllAsync_ShouldReadWrittenEvents()
        {
            // Arrange
            var bus = new MetricBus(capacity: 10);
            var metricEvent = new MetricEvent
            {
                Name = "test_metric",
                Type = MetricType.Counter,
                Value = 1.0
            };

            // Act
            bus.TryWrite(metricEvent);
            bus.Complete();

            var events = new List<MetricEvent>();
            await foreach (var evt in bus.ReadAllAsync())
            {
                events.Add(evt);
            }

            // Assert
            events.Should().HaveCount(1);
            events[0].Name.Should().Be("test_metric");
            events[0].Type.Should().Be(MetricType.Counter);
            events[0].Value.Should().Be(1.0);
        }

        [Fact]
        public async Task TryWrite_ShouldDropOldest_WhenChannelIsFull()
        {
            // Arrange
            var bus = new MetricBus(capacity: 2);
            var event1 = new MetricEvent { Name = "event1", Type = MetricType.Counter, Value = 1.0 };
            var event2 = new MetricEvent { Name = "event2", Type = MetricType.Counter, Value = 2.0 };
            var event3 = new MetricEvent { Name = "event3", Type = MetricType.Counter, Value = 3.0 };

            // Act
            bus.TryWrite(event1);
            bus.TryWrite(event2);
            bus.TryWrite(event3); // Should drop event1
            bus.Complete();

            // Assert
            var events = new List<MetricEvent>();
            await foreach (var evt in bus.ReadAllAsync())
            {
                events.Add(evt);
            }

            events.Should().HaveCount(2);
            events.Should().NotContain(e => e.Name == "event1");
            events.Should().Contain(e => e.Name == "event2");
            events.Should().Contain(e => e.Name == "event3");
        }

        [Fact]
        public void Dispose_ShouldCompleteWriter()
        {
            // Arrange
            var bus = new MetricBus(capacity: 10);

            // Act
            bus.Dispose();

            // Assert
            bus.TryWrite(new MetricEvent { Name = "test", Type = MetricType.Counter, Value = 1.0 })
                .Should().BeFalse();
        }
    }
}

