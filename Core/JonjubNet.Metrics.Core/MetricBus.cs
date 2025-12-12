using System.Threading.Channels;

namespace JonjubNet.Metrics.Core
{
    /// <summary>
    /// Bus de métricas acotado (bounded) para eventos de métricas
    /// Usa Channel para comunicación lock-free
    /// </summary>
    public class MetricBus : IDisposable
    {
        private readonly Channel<MetricEvent> _channel;
        private readonly ChannelWriter<MetricEvent> _writer;
        private readonly ChannelReader<MetricEvent> _reader;
        private readonly BoundedChannelOptions _options;
        private readonly int _capacity;
        private long _totalWritten;
        private long _totalDropped;

        public int Capacity => _capacity;

        public MetricBus(int capacity = 10000, BoundedChannelFullMode fullMode = BoundedChannelFullMode.DropOldest)
        {
            _capacity = capacity;
            _options = new BoundedChannelOptions(capacity)
            {
                FullMode = fullMode,
                SingleReader = false,
                SingleWriter = false
            };

            var channel = Channel.CreateBounded<MetricEvent>(_options);
            _channel = channel;
            _writer = channel.Writer;
            _reader = channel.Reader;
        }

        /// <summary>
        /// Obtiene información sobre el estado del bus
        /// </summary>
        public MetricBusStatus GetStatus()
        {
            // Nota: Channel no expone directamente el tamaño actual
            // Usamos estimaciones basadas en escrituras y lecturas
            var estimatedSize = Math.Max(0, _totalWritten - _totalDropped);
            var utilization = _capacity > 0 ? (double)estimatedSize / _capacity * 100.0 : 0.0;
            var isSaturated = utilization >= 90.0;

            return new MetricBusStatus
            {
                Capacity = _capacity,
                EstimatedSize = (int)estimatedSize,
                UtilizationPercent = utilization,
                IsSaturated = isSaturated,
                TotalWritten = _totalWritten,
                TotalDropped = _totalDropped
            };
        }

        /// <summary>
        /// Intenta escribir un evento en el bus (non-blocking)
        /// </summary>
        public bool TryWrite(MetricEvent metricEvent)
        {
            var result = _writer.TryWrite(metricEvent);
            if (result)
            {
                Interlocked.Increment(ref _totalWritten);
            }
            else
            {
                Interlocked.Increment(ref _totalDropped);
            }
            return result;
        }

        /// <summary>
        /// Escribe un evento en el bus de forma asíncrona
        /// </summary>
        public async ValueTask<bool> WriteAsync(MetricEvent metricEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                await _writer.WriteAsync(metricEvent, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Lee eventos del bus
        /// </summary>
        public IAsyncEnumerable<MetricEvent> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return _reader.ReadAllAsync(cancellationToken);
        }

        /// <summary>
        /// Completa el writer (no se pueden escribir más eventos)
        /// </summary>
        public void Complete()
        {
            _writer.Complete();
        }

        public void Dispose()
        {
            Complete();
        }
    }

    /// <summary>
    /// Estado del MetricBus
    /// </summary>
    public class MetricBusStatus
    {
        public int Capacity { get; set; }
        public int EstimatedSize { get; set; }
        public double UtilizationPercent { get; set; }
        public bool IsSaturated { get; set; }
        public long TotalWritten { get; set; }
        public long TotalDropped { get; set; }
    }

    /// <summary>
    /// Evento de métrica para el bus
    /// </summary>
    public record MetricEvent
    {
        public string Name { get; init; } = string.Empty;
        public MetricType Type { get; init; }
        public double Value { get; init; }
        public Dictionary<string, string>? Tags { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}

