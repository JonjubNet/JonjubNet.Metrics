using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Core.MetricTypes;

namespace JonjubNet.Metrics.Prometheus
{
    /// <summary>
    /// Formateador de métricas en formato Prometheus
    /// </summary>
    public class PrometheusFormatter : IMetricFormatter
    {
        public string Format => "Prometheus";

        public string FormatMetrics(IReadOnlyList<MetricPoint> points)
        {
            var output = new System.Text.StringBuilder();

            foreach (var point in points)
            {
                output.AppendLine(FormatMetricPoint(point));
            }

            return output.ToString();
        }

        /// <summary>
        /// Formatea métricas desde el registry
        /// </summary>
        public string FormatRegistry(MetricRegistry registry)
        {
            var output = new System.Text.StringBuilder();

            // Formatear contadores
            foreach (var counter in registry.GetAllCounters().Values)
            {
                output.AppendLine($"# HELP {counter.Name} {counter.Description}");
                output.AppendLine($"# TYPE {counter.Name} counter");

                foreach (var (key, value) in counter.GetAllValues())
                {
                    var labels = FormatLabels(ParseKey(key));
                    output.AppendLine($"{counter.Name}{labels} {value}");
                }
            }

            // Formatear gauges
            foreach (var gauge in registry.GetAllGauges().Values)
            {
                output.AppendLine($"# HELP {gauge.Name} {gauge.Description}");
                output.AppendLine($"# TYPE {gauge.Name} gauge");

                foreach (var (key, value) in gauge.GetAllValues())
                {
                    var labels = FormatLabels(ParseKey(key));
                    output.AppendLine($"{gauge.Name}{labels} {value}");
                }
            }

            // Formatear histogramas
            foreach (var histogram in registry.GetAllHistograms().Values)
            {
                output.AppendLine($"# HELP {histogram.Name} {histogram.Description}");
                output.AppendLine($"# TYPE {histogram.Name} histogram");

                foreach (var (key, data) in histogram.GetAllData())
                {
                    var labels = ParseKey(key);
                    foreach (var bucket in histogram.Buckets)
                    {
                        var bucketLabels = new Dictionary<string, string>(labels) { ["le"] = bucket.ToString() };
                        var count = data.BucketCounts[Array.IndexOf(histogram.Buckets, bucket)];
                        output.AppendLine($"{histogram.Name}_bucket{FormatLabels(bucketLabels)} {count}");
                    }
                    output.AppendLine($"{histogram.Name}_sum{FormatLabels(labels)} {data.Sum}");
                    output.AppendLine($"{histogram.Name}_count{FormatLabels(labels)} {data.Count}");
                }
            }

            // Formatear summaries
            foreach (var summary in registry.GetAllSummaries().Values)
            {
                output.AppendLine($"# HELP {summary.Name} {summary.Description}");
                output.AppendLine($"# TYPE {summary.Name} summary");

                foreach (var (key, data) in summary.GetAllData())
                {
                    var labels = ParseKey(key);
                    var quantiles = data.GetQuantiles();
                    foreach (var quantile in quantiles)
                    {
                        var quantileLabels = new Dictionary<string, string>(labels) { ["quantile"] = quantile.Key.ToString() };
                        output.AppendLine($"{summary.Name}{FormatLabels(quantileLabels)} {quantile.Value}");
                    }
                    output.AppendLine($"{summary.Name}_sum{FormatLabels(labels)} {data.Sum}");
                    output.AppendLine($"{summary.Name}_count{FormatLabels(labels)} {data.Count}");
                }
            }

            return output.ToString();
        }

        private string FormatMetricPoint(MetricPoint point)
        {
            var labels = FormatLabels(point.Tags);
            return $"{point.Name}{labels} {point.Value}";
        }

        private string FormatLabels(Dictionary<string, string>? labels)
        {
            if (labels == null || labels.Count == 0)
                return string.Empty;

            var labelStrings = labels.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\"");
            return "{" + string.Join(",", labelStrings) + "}";
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var pairs = key.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = parts[1];
                }
            }
            return result;
        }
    }
}

