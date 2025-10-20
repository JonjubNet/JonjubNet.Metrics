namespace JonjubNet.Metrics.Configuration
{
    /// <summary>
    /// Configuración para el servicio de métricas
    /// </summary>
    public class MetricsConfiguration
    {
        public const string SectionName = "Metrics";

        /// <summary>
        /// Habilitar el registro de métricas globalmente
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nombre del servicio actual
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Entorno de ejecución
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Versión del servicio
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Configuración de contadores
        /// </summary>
        public CounterConfiguration Counter { get; set; } = new();

        /// <summary>
        /// Configuración de gauges
        /// </summary>
        public GaugeConfiguration Gauge { get; set; } = new();

        /// <summary>
        /// Configuración de histogramas
        /// </summary>
        public HistogramConfiguration Histogram { get; set; } = new();

        /// <summary>
        /// Configuración de timers
        /// </summary>
        public TimerConfiguration Timer { get; set; } = new();

        /// <summary>
        /// Configuración de exportación
        /// </summary>
        public ExportConfiguration Export { get; set; } = new();

        /// <summary>
        /// Configuración de middleware
        /// </summary>
        public MiddlewareConfiguration Middleware { get; set; } = new();
    }

    /// <summary>
    /// Configuración para contadores
    /// </summary>
    public class CounterConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double DefaultIncrement { get; set; } = 1;
        public bool EnableLabels { get; set; } = true;
        public Dictionary<string, CounterServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración para gauges
    /// </summary>
    public class GaugeConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool EnableLabels { get; set; } = true;
        public Dictionary<string, GaugeServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración para histogramas
    /// </summary>
    public class HistogramConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double[] DefaultBuckets { get; set; } = { 0.1, 0.5, 1.0, 2.5, 5.0, 10.0 };
        public bool EnableLabels { get; set; } = true;
        public Dictionary<string, HistogramServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración para timers
    /// </summary>
    public class TimerConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double[] DefaultBuckets { get; set; } = { 0.1, 0.5, 1.0, 2.5, 5.0, 10.0 };
        public bool EnableLabels { get; set; } = true;
        public Dictionary<string, TimerServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración de exportación
    /// </summary>
    public class ExportConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int ExportIntervalSeconds { get; set; } = 30;
        public string[] Formats { get; set; } = { "Prometheus", "JSON" };
        public PrometheusConfiguration Prometheus { get; set; } = new();
        public JsonConfiguration JSON { get; set; } = new();
        public FileConfiguration File { get; set; } = new();
    }

    /// <summary>
    /// Configuración de Prometheus
    /// </summary>
    public class PrometheusConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string Endpoint { get; set; } = "/metrics";
        public int Port { get; set; } = 9090;
    }

    /// <summary>
    /// Configuración de JSON
    /// </summary>
    public class JsonConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string Endpoint { get; set; } = "/metrics/json";
        public int Port { get; set; } = 9091;
    }

    /// <summary>
    /// Configuración de archivo
    /// </summary>
    public class FileConfiguration
    {
        public bool Enabled { get; set; } = false;
        public string Path { get; set; } = "./metrics";
        public string FileName { get; set; } = "metrics.json";
        public bool RotationEnabled { get; set; } = true;
        public int MaxFileSizeMB { get; set; } = 100;
    }

    /// <summary>
    /// Configuración de middleware
    /// </summary>
    public class MiddlewareConfiguration
    {
        public bool Enabled { get; set; } = true;
        public HttpMetricsConfiguration HttpMetrics { get; set; } = new();
        public DatabaseMetricsConfiguration DatabaseMetrics { get; set; } = new();
    }

    /// <summary>
    /// Configuración de métricas HTTP
    /// </summary>
    public class HttpMetricsConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool TrackRequestDuration { get; set; } = true;
        public bool TrackRequestSize { get; set; } = true;
        public bool TrackResponseSize { get; set; } = true;
        public bool TrackStatusCode { get; set; } = true;
        public string[] ExcludePaths { get; set; } = { "/health", "/metrics", "/swagger" };
    }

    /// <summary>
    /// Configuración de métricas de base de datos
    /// </summary>
    public class DatabaseMetricsConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool TrackQueryDuration { get; set; } = true;
        public bool TrackQueryCount { get; set; } = true;
        public bool TrackConnectionCount { get; set; } = true;
    }

    // Configuraciones específicas por servicio
    public class CounterServiceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double Increment { get; set; } = 1;
        public string[] Labels { get; set; } = Array.Empty<string>();
    }

    public class GaugeServiceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string[] Labels { get; set; } = Array.Empty<string>();
    }

    public class HistogramServiceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double[] Buckets { get; set; } = { 0.1, 0.5, 1.0, 2.5, 5.0, 10.0 };
        public string[] Labels { get; set; } = Array.Empty<string>();
    }

    public class TimerServiceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double[] Buckets { get; set; } = { 0.1, 0.5, 1.0, 2.5, 5.0, 10.0 };
        public string[] Labels { get; set; } = Array.Empty<string>();
    }
}
