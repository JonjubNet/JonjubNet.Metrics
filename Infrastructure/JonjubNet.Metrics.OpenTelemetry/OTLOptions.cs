using JonjubNet.Metrics.Shared.Configuration;

namespace JonjubNet.Metrics.OpenTelemetry
{
    /// <summary>
    /// Opciones de configuración para OpenTelemetry
    /// </summary>
    public class OTLOptions : MetricsSinkOptions
    {
        public string Endpoint { get; set; } = "http://localhost:4318";
        public OtlpProtocol Protocol { get; set; } = OtlpProtocol.HttpJson;
        public bool EnableCompression { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public enum OtlpProtocol
    {
        HttpProtobuf,
        HttpJson,
        Grpc
    }
}
