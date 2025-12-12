using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Shared.Utils;

namespace JonjubNet.Metrics.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    public class ObjectPoolingBenchmark
    {
        [Benchmark(Baseline = true)]
        public void CreateList_WithoutPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var list = new List<MetricEvent>();
                list.Add(new MetricEvent { Name = "test", Type = MetricType.Counter, Value = 1.0 });
                // Simular uso
                _ = list.Count;
            }
        }

        [Benchmark]
        public void CreateList_WithPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var list = CollectionPool<MetricEvent>.RentList();
                try
                {
                    list.Add(new MetricEvent { Name = "test", Type = MetricType.Counter, Value = 1.0 });
                    // Simular uso
                    _ = list.Count;
                }
                finally
                {
                    CollectionPool<MetricEvent>.ReturnList(list);
                }
            }
        }

        [Benchmark]
        public void CreateDictionary_WithoutPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var dict = new Dictionary<string, string>();
                dict["key"] = "value";
                // Simular uso
                _ = dict.Count;
            }
        }

        [Benchmark]
        public void CreateDictionary_WithPool()
        {
            for (int i = 0; i < 1000; i++)
            {
                var dict = CollectionPool<string>.RentStringDictionary();
                try
                {
                    dict["key"] = "value";
                    // Simular uso
                    _ = dict.Count;
                }
                finally
                {
                    CollectionPool<string>.ReturnStringDictionary(dict);
                }
            }
        }
    }
}

