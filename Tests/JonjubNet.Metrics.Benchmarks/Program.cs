using BenchmarkDotNet.Running;
using JonjubNet.Metrics.Benchmarks;

namespace JonjubNet.Metrics.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running JonjubNet.Metrics Benchmarks...");
            Console.WriteLine();

            var summary = BenchmarkRunner.Run(new[]
            {
                typeof(MetricBusBenchmark),
                typeof(CounterBenchmark),
                typeof(MetricsClientBenchmark),
                typeof(PerformanceOptimizationsBenchmark)
            });

            Console.WriteLine();
            Console.WriteLine("Benchmarks completed!");
        }
    }
}
