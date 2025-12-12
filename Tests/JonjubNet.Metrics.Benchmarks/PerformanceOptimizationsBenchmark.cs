using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Shared.Utils;
using System.Text.Json;

namespace JonjubNet.Metrics.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class PerformanceOptimizationsBenchmark
    {
        private Dictionary<string, string> _tags = null!;

        [GlobalSetup]
        public void Setup()
        {
            _tags = new Dictionary<string, string> 
            { 
                ["env"] = "prod", 
                ["service"] = "api",
                ["version"] = "1.0.0"
            };
        }

        [Benchmark(Baseline = true)]
        public Dictionary<string, string> CreateDictionary_New()
        {
            return new Dictionary<string, string> { ["env"] = "prod" };
        }

        [Benchmark]
        public Dictionary<string, string> CreateDictionary_Pool()
        {
            var dict = CollectionPool.RentTagDictionary();
            dict["env"] = "prod";
            return dict;
        }

        [Benchmark]
        public string SerializeJson_NewOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(_tags, options);
        }

        [Benchmark]
        public string SerializeJson_CachedOptions()
        {
            var options = JsonSerializerOptionsCache.GetDefault();
            return JsonSerializer.Serialize(_tags, options);
        }

        [Benchmark]
        public void ParallelSinkProcessing()
        {
            // Simular procesamiento paralelo de múltiples sinks
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.CompletedTask);
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}

