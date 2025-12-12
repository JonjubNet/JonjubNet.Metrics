using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace JonjubNet.Metrics.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ParallelSinksBenchmark
    {
        private List<IMetricsSink> _sinks = null!;
        private List<MetricPoint> _points = null!;

        [GlobalSetup]
        public void Setup()
        {
            _sinks = new List<IMetricsSink>();
            for (int i = 0; i < 5; i++)
            {
                var mockSink = new Mock<IMetricsSink>();
                mockSink.Setup(s => s.Name).Returns($"Sink{i}");
                mockSink.Setup(s => s.IsEnabled).Returns(true);
                mockSink.Setup(s => s.ExportAsync(It.IsAny<IReadOnlyList<MetricPoint>>(), It.IsAny<CancellationToken>()))
                    .Returns(ValueTask.CompletedTask);
                _sinks.Add(mockSink.Object);
            }

            _points = new List<MetricPoint>();
            for (int i = 0; i < 100; i++)
            {
                _points.Add(new MetricPoint($"metric_{i}", MetricType.Counter, i));
            }
        }

        [Benchmark(Baseline = true)]
        public async Task FlushSinks_Sequential()
        {
            foreach (var sink in _sinks)
            {
                await sink.ExportAsync(_points, CancellationToken.None);
            }
        }

        [Benchmark]
        public async Task FlushSinks_Parallel()
        {
            var tasks = _sinks.Select(sink => sink.ExportAsync(_points, CancellationToken.None));
            await Task.WhenAll(tasks);
        }
    }
}

