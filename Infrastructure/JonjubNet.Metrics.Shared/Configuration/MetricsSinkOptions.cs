namespace JonjubNet.Metrics.Shared.Configuration
{
    /// <summary>
    /// Opciones base para sinks
    /// </summary>
    public abstract class MetricsSinkOptions
    {
        /// <summary>
        /// Habilitar el sink
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Nombre del sink
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}

