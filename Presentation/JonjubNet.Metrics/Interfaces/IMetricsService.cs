using JonjubNet.Metrics.Models;

namespace JonjubNet.Metrics.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de métricas
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Registra una métrica de contador
        /// </summary>
        /// <param name="name">Nombre de la métrica</param>
        /// <param name="value">Valor a incrementar</param>
        /// <param name="labels">Etiquetas opcionales</param>
        Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? labels = null);

        /// <summary>
        /// Registra una métrica de gauge
        /// </summary>
        /// <param name="name">Nombre de la métrica</param>
        /// <param name="value">Valor actual</param>
        /// <param name="labels">Etiquetas opcionales</param>
        Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? labels = null);

        /// <summary>
        /// Registra una métrica de histograma
        /// </summary>
        /// <param name="name">Nombre de la métrica</param>
        /// <param name="value">Valor a registrar</param>
        /// <param name="labels">Etiquetas opcionales</param>
        Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? labels = null);

        /// <summary>
        /// Registra una métrica de timer
        /// </summary>
        /// <param name="name">Nombre de la métrica</param>
        /// <param name="duration">Duración en milisegundos</param>
        /// <param name="labels">Etiquetas opcionales</param>
        Task RecordTimerAsync(string name, double duration, Dictionary<string, string>? labels = null);

        /// <summary>
        /// Registra métricas HTTP
        /// </summary>
        /// <param name="metrics">Métricas HTTP a registrar</param>
        Task RecordHttpMetricsAsync(HttpMetrics metrics);

        /// <summary>
        /// Registra métricas de base de datos
        /// </summary>
        /// <param name="metrics">Métricas de base de datos a registrar</param>
        Task RecordDatabaseMetricsAsync(DatabaseMetrics metrics);

        /// <summary>
        /// Registra métricas de negocio
        /// </summary>
        /// <param name="metrics">Métricas de negocio a registrar</param>
        Task RecordBusinessMetricsAsync(BusinessMetrics metrics);

        /// <summary>
        /// Registra métricas del sistema
        /// </summary>
        /// <param name="metrics">Métricas del sistema a registrar</param>
        Task RecordSystemMetricsAsync(SystemMetrics metrics);
    }
}

