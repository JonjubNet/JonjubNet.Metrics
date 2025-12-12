using JonjubNet.Metrics.Interfaces;
using JonjubNet.Metrics.Models;
using JonjubNet.Metrics.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace JonjubNet.Metrics.Services
{
    /// <summary>
    /// Middleware para capturar métricas HTTP automáticamente
    /// Compatible con la nueva arquitectura
    /// </summary>
    public class HttpMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMetricsService _metricsService;
        private readonly MetricsConfiguration _configuration;

        public HttpMetricsMiddleware(
            RequestDelegate next,
            IMetricsService metricsService,
            IOptions<MetricsConfiguration> configuration)
        {
            _next = next;
            _metricsService = metricsService;
            _configuration = configuration.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_configuration.Enabled || !_configuration.Middleware.Enabled || !_configuration.Middleware.HttpMetrics.Enabled)
            {
                await _next(context);
                return;
            }

            // Verificar si la ruta debe ser excluida
            if (ShouldExcludePath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                // Interceptar el stream de respuesta para medir el tamaño
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                stopwatch.Stop();

                // Obtener información de la solicitud
                var requestSize = await GetRequestSizeAsync(context.Request);
                var responseSize = responseBodyStream.Length;

                // Copiar la respuesta al stream original
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);

                // Registrar métricas
                await RecordHttpMetricsAsync(context, stopwatch.Elapsed.TotalMilliseconds, requestSize, responseSize);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                
                // En caso de error, restaurar el stream original
                context.Response.Body = originalBodyStream;
                throw;
            }
        }

        private bool ShouldExcludePath(PathString path)
        {
            var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;
            return _configuration.Middleware.HttpMetrics.ExcludePaths.Any(excludedPath => 
                pathString.StartsWith(excludedPath.ToLowerInvariant()));
        }

        private async Task<long> GetRequestSizeAsync(HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                return request.ContentLength.Value;
            }

            // Si no hay ContentLength, intentar leer el body
            if (request.Body.CanSeek)
            {
                var originalPosition = request.Body.Position;
                request.Body.Seek(0, SeekOrigin.Begin);
                var size = request.Body.Length;
                request.Body.Seek(originalPosition, SeekOrigin.Begin);
                return size;
            }

            return 0;
        }

        private async Task RecordHttpMetricsAsync(HttpContext context, double durationMs, long requestSize, long responseSize)
        {
            try
            {
                var metrics = new HttpMetrics
                {
                    Method = context.Request.Method,
                    Endpoint = GetEndpoint(context.Request.Path),
                    StatusCode = context.Response.StatusCode,
                    DurationMs = durationMs,
                    RequestSizeBytes = requestSize,
                    ResponseSizeBytes = responseSize,
                    Labels = new Dictionary<string, string>
                    {
                        ["service"] = _configuration.ServiceName,
                        ["environment"] = _configuration.Environment,
                        ["version"] = _configuration.Version
                    }
                };

                await _metricsService.RecordHttpMetricsAsync(metrics);
            }
            catch (Exception)
            {
                // No queremos que las métricas rompan la aplicación
                // El logging se maneja en el servicio de métricas
            }
        }

        private string GetEndpoint(PathString path)
        {
            var pathValue = path.Value ?? string.Empty;
            
            // Normalizar rutas con parámetros (ej: /api/users/123 -> /api/users/{id})
            var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var normalizedSegments = new List<string>();

            foreach (var segment in segments)
            {
                if (int.TryParse(segment, out _) || Guid.TryParse(segment, out _))
                {
                    normalizedSegments.Add("{id}");
                }
                else
                {
                    normalizedSegments.Add(segment);
                }
            }

            return "/" + string.Join("/", normalizedSegments);
        }
    }
}
