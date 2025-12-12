using System.Collections.Concurrent;
using JonjubNet.Metrics.Core;

namespace JonjubNet.Metrics.Shared.Resilience
{
    /// <summary>
    /// Cola acotada y lock-free para métricas
    /// </summary>
    public class MetricQueue
    {
        private readonly ConcurrentQueue<MetricEvent> _queue;
        private readonly int _maxCapacity;
        private int _currentCount;

        public MetricQueue(int maxCapacity = 10000)
        {
            _queue = new ConcurrentQueue<MetricEvent>();
            _maxCapacity = maxCapacity;
            _currentCount = 0;
        }

        /// <summary>
        /// Intenta encolar un evento (non-blocking)
        /// </summary>
        public bool TryEnqueue(MetricEvent metricEvent)
        {
            if (_currentCount >= _maxCapacity)
                return false;

            _queue.Enqueue(metricEvent);
            Interlocked.Increment(ref _currentCount);
            return true;
        }

        /// <summary>
        /// Intenta desencolar un evento
        /// </summary>
        public bool TryDequeue(out MetricEvent? metricEvent)
        {
            if (_queue.TryDequeue(out metricEvent))
            {
                Interlocked.Decrement(ref _currentCount);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtiene el número de elementos en la cola
        /// </summary>
        public int Count => _currentCount;

        /// <summary>
        /// Indica si la cola está llena
        /// </summary>
        public bool IsFull => _currentCount >= _maxCapacity;
    }
}

