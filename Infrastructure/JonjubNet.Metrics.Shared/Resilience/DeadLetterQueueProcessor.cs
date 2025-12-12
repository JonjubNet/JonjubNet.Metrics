using JonjubNet.Metrics.Core;
using JonjubNet.Metrics.Core.Interfaces;
using JonjubNet.Metrics.Core.Resilience;
using JonjubNet.Metrics.Core.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JonjubNet.Metrics.Shared.Resilience
{
    /// <summary>
    /// Procesador de Dead Letter Queue que reintenta periódicamente las métricas fallidas
    /// </summary>
    public class DeadLetterQueueProcessor : BackgroundService
    {
        private readonly DeadLetterQueue _deadLetterQueue;
        private readonly IEnumerable<IMetricsSink> _sinks;
        private readonly RetryPolicy? _retryPolicy;
        private readonly ILogger<DeadLetterQueueProcessor>? _logger;
        private readonly TimeSpan _processInterval;
        private readonly int _batchSize;

        public DeadLetterQueueProcessor(
            DeadLetterQueue deadLetterQueue,
            IEnumerable<IMetricsSink> sinks,
            RetryPolicy? retryPolicy = null,
            TimeSpan? processInterval = null,
            int batchSize = 100,
            ILogger<DeadLetterQueueProcessor>? logger = null)
        {
            _deadLetterQueue = deadLetterQueue;
            _sinks = sinks;
            _retryPolicy = retryPolicy;
            _logger = logger;
            _processInterval = processInterval ?? TimeSpan.FromMinutes(5);
            _batchSize = batchSize;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Dead Letter Queue Processor started. Processing interval: {Interval}", _processInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFailedMetricsAsync(stoppingToken);
                    await Task.Delay(_processInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("Dead Letter Queue Processor stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in Dead Letter Queue Processor");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Esperar antes de reintentar
                }
            }
        }

        /// <summary>
        /// Procesa métricas fallidas de la DLQ
        /// </summary>
        private async Task ProcessFailedMetricsAsync(CancellationToken cancellationToken)
        {
            var stats = _deadLetterQueue.GetStats();
            if (stats.Count == 0)
            {
                return; // No hay métricas para procesar
            }

            _logger?.LogInformation("Processing {Count} failed metrics from DLQ", stats.Count);

            var processedCount = 0;
            var successCount = 0;
            var failedCount = 0;

            // Procesar en batches
            while (_deadLetterQueue.TryDequeue(out var failedMetric) && processedCount < _batchSize)
            {
                try
                {
                    var sink = _sinks.FirstOrDefault(s => s.Name == failedMetric.SinkName && s.IsEnabled);
                    if (sink == null)
                    {
                        _logger?.LogWarning("Sink {SinkName} not found or disabled, skipping metric {MetricName}",
                            failedMetric.SinkName, failedMetric.MetricPoint.Name);
                        failedCount++;
                        continue;
                    }

                    // Intentar reexportar la métrica
                    // Optimizado: pre-allocar capacidad para lista de un elemento
                    var metricPoints = new List<MetricPoint>(1) { failedMetric.MetricPoint };

                    if (_retryPolicy != null)
                    {
                        var result = await _retryPolicy.ExecuteWithResultAsync<bool>(
                            async () =>
                            {
                                await sink.ExportAsync(metricPoints, cancellationToken);
                                return true;
                            },
                            cancellationToken);

                        if (result.Success)
                        {
                            successCount++;
                            _logger?.LogDebug("Successfully re-exported metric {MetricName} to {SinkName}",
                                failedMetric.MetricPoint.Name, failedMetric.SinkName);
                        }
                        else
                        {
                            // Si falla de nuevo, volver a agregar a la DLQ
                            // Usar pool para nuevo diccionario de metadata
                            var newMetadata = CollectionPool.RentDictionary();
                            if (failedMetric.Metadata != null)
                            {
                                foreach (var kvp in failedMetric.Metadata)
                                {
                                    newMetadata[kvp.Key] = kvp.Value;
                                }
                            }
                            newMetadata["reprocess_attempt"] = (failedMetric.RetryCount + 1).ToString();
                            newMetadata["last_reprocess_at"] = DateTime.UtcNow.ToString("O");
                            
                            // Retornar metadata anterior al pool si existe
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                            
                            _deadLetterQueue.Enqueue(new FailedMetric(
                                failedMetric.MetricPoint,
                                failedMetric.SinkName,
                                failedMetric.RetryCount + 1,
                                result.LastException,
                                newMetadata));
                            failedCount++;
                            _logger?.LogWarning("Failed to re-export metric {MetricName} to {SinkName} after retry",
                                failedMetric.MetricPoint.Name, failedMetric.SinkName);
                        }
                    }
                    else
                    {
                        // Sin retry policy, intentar directamente
                        try
                        {
                            await sink.ExportAsync(metricPoints, cancellationToken);
                            successCount++;
                            _logger?.LogDebug("Successfully re-exported metric {MetricName} to {SinkName}",
                                failedMetric.MetricPoint.Name, failedMetric.SinkName);
                            
                            // Retornar metadata al pool cuando se procesa exitosamente
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Si falla, volver a agregar a la DLQ
                            // Usar pool para nuevo diccionario de metadata
                            var newMetadata = CollectionPool.RentDictionary();
                            if (failedMetric.Metadata != null)
                            {
                                foreach (var kvp in failedMetric.Metadata)
                                {
                                    newMetadata[kvp.Key] = kvp.Value;
                                }
                            }
                            newMetadata["reprocess_attempt"] = (failedMetric.RetryCount + 1).ToString();
                            newMetadata["last_reprocess_at"] = DateTime.UtcNow.ToString("O");
                            
                            // Retornar metadata anterior al pool si existe
                            if (failedMetric.Metadata != null)
                            {
                                CollectionPool.ReturnDictionary(failedMetric.Metadata);
                            }
                            
                            _deadLetterQueue.Enqueue(new FailedMetric(
                                failedMetric.MetricPoint,
                                failedMetric.SinkName,
                                failedMetric.RetryCount + 1,
                                ex,
                                newMetadata));
                            failedCount++;
                            _logger?.LogWarning(ex, "Failed to re-export metric {MetricName} to {SinkName}",
                                failedMetric.MetricPoint.Name, failedMetric.SinkName);
                        }
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing failed metric {MetricName}",
                        failedMetric?.MetricPoint.Name ?? "unknown");
                    failedCount++;
                }
            }

            _logger?.LogInformation("DLQ processing completed. Processed: {Processed}, Success: {Success}, Failed: {Failed}",
                processedCount, successCount, failedCount);
        }
    }
}
