using Microsoft.AspNetCore.Http;

namespace JonjubNet.Metrics.Interfaces
{
    /// <summary>
    /// Interfaz para middleware de métricas HTTP
    /// </summary>
    public interface IMetricsMiddleware
    {
        /// <summary>
        /// Procesa una solicitud HTTP y registra métricas
        /// </summary>
        /// <param name="context">Contexto de la solicitud HTTP</param>
        /// <param name="next">Siguiente middleware en el pipeline</param>
        Task InvokeAsync(HttpContext context, RequestDelegate next);
    }
}
