using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Models;

namespace JonjubNet.Metrics.Examples
{
    /// <summary>
    /// Ejemplo de uso del servicio de métricas
    /// </summary>
    public class UsageExample
    {
        private readonly IMetricsService _metricsService;

        public UsageExample(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }

        /// <summary>
        /// Ejemplo de uso de contadores
        /// </summary>
        public async Task ContadorExample()
        {
            // Contador simple
            await _metricsService.RecordCounterAsync("usuarios_registrados", 1);

            // Contador con etiquetas
            await _metricsService.RecordCounterAsync("ventas_realizadas", 1, 
                new Dictionary<string, string>
                {
                    ["producto"] = "laptop",
                    ["categoria"] = "electronica",
                    ["region"] = "norte"
                });

            // Contador con valor personalizado
            await _metricsService.RecordCounterAsync("items_procesados", 25);
        }

        /// <summary>
        /// Ejemplo de uso de gauges
        /// </summary>
        public async Task GaugeExample()
        {
            // Gauge simple
            await _metricsService.RecordGaugeAsync("memoria_utilizada_mb", 512.5);

            // Gauge con etiquetas
            await _metricsService.RecordGaugeAsync("conexiones_activas", 150,
                new Dictionary<string, string>
                {
                    ["servidor"] = "web-01",
                    ["tipo"] = "http"
                });

            // Gauge para métricas de negocio
            await _metricsService.RecordGaugeAsync("inventario_productos", 1250,
                new Dictionary<string, string>
                {
                    ["almacen"] = "principal",
                    ["categoria"] = "electronica"
                });
        }

        /// <summary>
        /// Ejemplo de uso de histogramas
        /// </summary>
        public async Task HistogramExample()
        {
            // Histograma para tiempos de respuesta
            await _metricsService.RecordHistogramAsync("tiempo_respuesta_ms", 150.5,
                new Dictionary<string, string>
                {
                    ["endpoint"] = "/api/usuarios",
                    ["metodo"] = "GET"
                });

            // Histograma para tamaños de archivo
            await _metricsService.RecordHistogramAsync("tamaño_archivo_bytes", 1024000,
                new Dictionary<string, string>
                {
                    ["tipo"] = "imagen",
                    ["formato"] = "jpg"
                });

            // Histograma para métricas de negocio
            await _metricsService.RecordHistogramAsync("valor_transaccion", 250.75,
                new Dictionary<string, string>
                {
                    ["moneda"] = "USD",
                    ["tipo"] = "compra"
                });
        }

        /// <summary>
        /// Ejemplo de uso de timers
        /// </summary>
        public async Task TimerExample()
        {
            // Timer para operaciones de base de datos
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Simular operación de BD
            await Task.Delay(100);
            
            stopwatch.Stop();
            
            await _metricsService.RecordTimerAsync("consulta_bd_ms", stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, string>
                {
                    ["tabla"] = "usuarios",
                    ["operacion"] = "SELECT"
                });

            // Timer para operaciones de negocio
            stopwatch.Restart();
            
            // Simular procesamiento
            await Task.Delay(50);
            
            stopwatch.Stop();
            
            await _metricsService.RecordTimerAsync("procesamiento_pedido_ms", stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, string>
                {
                    ["tipo"] = "express",
                    ["region"] = "norte"
                });
        }

        /// <summary>
        /// Ejemplo de métricas HTTP
        /// </summary>
        public async Task HttpMetricsExample()
        {
            var httpMetrics = new HttpMetrics
            {
                Method = "POST",
                Endpoint = "/api/usuarios",
                StatusCode = 201,
                DurationMs = 125.5,
                RequestSizeBytes = 1024,
                ResponseSizeBytes = 512,
                Labels = new Dictionary<string, string>
                {
                    ["version"] = "v1",
                    ["autenticado"] = "true"
                }
            };

            await _metricsService.RecordHttpMetricsAsync(httpMetrics);
        }

        /// <summary>
        /// Ejemplo de métricas de base de datos
        /// </summary>
        public async Task DatabaseMetricsExample()
        {
            var dbMetrics = new DatabaseMetrics
            {
                Operation = "INSERT",
                Table = "pedidos",
                Database = "ecommerce",
                DurationMs = 45.2,
                RecordsAffected = 1,
                IsSuccess = true,
                Labels = new Dictionary<string, string>
                {
                    ["conexion"] = "pool-1",
                    ["transaccion"] = "true"
                }
            };

            await _metricsService.RecordDatabaseMetricsAsync(dbMetrics);
        }

        /// <summary>
        /// Ejemplo de métricas de negocio
        /// </summary>
        public async Task BusinessMetricsExample()
        {
            var businessMetrics = new BusinessMetrics
            {
                Operation = "ProcesarVenta",
                MetricType = "Revenue",
                Value = 299.99,
                Category = "Ventas",
                DurationMs = 200.0,
                IsSuccess = true,
                Labels = new Dictionary<string, string>
                {
                    ["producto"] = "laptop",
                    ["cliente_tipo"] = "premium",
                    ["descuento"] = "10%"
                }
            };

            await _metricsService.RecordBusinessMetricsAsync(businessMetrics);
        }

        /// <summary>
        /// Ejemplo de métricas del sistema
        /// </summary>
        public async Task SystemMetricsExample()
        {
            var systemMetrics = new SystemMetrics
            {
                MetricType = "CPU_Usage",
                Value = 75.5,
                Unit = "percent",
                Instance = "web-server-01",
                Labels = new Dictionary<string, string>
                {
                    ["core"] = "0",
                    ["temperature"] = "65"
                }
            };

            await _metricsService.RecordSystemMetricsAsync(systemMetrics);
        }

        /// <summary>
        /// Ejemplo completo de uso en un servicio
        /// </summary>
        public async Task EjemploCompleto()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Simular procesamiento
                await Task.Delay(200);

                // Registrar métricas de éxito
                await _metricsService.RecordCounterAsync("operaciones_exitosas", 1,
                    new Dictionary<string, string> { ["servicio"] = "ejemplo" });

                stopwatch.Stop();

                await _metricsService.RecordTimerAsync("operacion_completa_ms", stopwatch.Elapsed.TotalMilliseconds,
                    new Dictionary<string, string> { ["resultado"] = "exito" });

                await _metricsService.RecordGaugeAsync("ultima_operacion_duracion", stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Registrar métricas de error
                await _metricsService.RecordCounterAsync("operaciones_fallidas", 1,
                    new Dictionary<string, string> 
                    { 
                        ["servicio"] = "ejemplo",
                        ["error"] = ex.GetType().Name
                    });

                await _metricsService.RecordTimerAsync("operacion_completa_ms", stopwatch.Elapsed.TotalMilliseconds,
                    new Dictionary<string, string> { ["resultado"] = "error" });

                throw;
            }
        }
    }
}
