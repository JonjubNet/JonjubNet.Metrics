# JonjubNet.Metrics

[![NuGet Version](https://img.shields.io/nuget/v/JonjubNet.Metrics.svg)](https://www.nuget.org/packages/JonjubNet.Metrics/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JonjubNet.Metrics.svg)](https://www.nuget.org/packages/JonjubNet.Metrics/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Biblioteca de métricas para aplicaciones .NET con soporte para contadores, gauges, histogramas y timers, integración con Prometheus y exportación a múltiples formatos.

## 🚀 Características

- **Métricas Estándar**: Soporte completo para contadores, gauges, histogramas y timers
- **Integración Prometheus**: Exportación nativa a formato Prometheus
- **Middleware HTTP**: Captura automática de métricas HTTP
- **Métricas de Base de Datos**: Tracking automático de operaciones de BD
- **Métricas de Negocio**: Soporte para métricas personalizadas de negocio
- **Configuración Flexible**: Configuración completa via appsettings.json
- **Etiquetas Dinámicas**: Soporte para etiquetas personalizadas
- **Alto Rendimiento**: Basado en Prometheus.Client para máximo rendimiento
- **.NET 10.0**: Compatible con las últimas versiones de .NET

## 📦 Instalación

```bash
dotnet add package JonjubNet.Metrics
```

## 🔧 Configuración

### 1. Configurar en Program.cs

```csharp
using JonjubNet.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios
builder.Services.AddMetricsInfrastructure(builder.Configuration);

var app = builder.Build();

// Usar middleware de métricas HTTP
app.UseMetricsMiddleware();

// Configurar endpoint de Prometheus
app.UsePrometheusServer();

app.Run();
```

### 2. Configurar en appsettings.json

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiAplicacion",
    "Environment": "Development",
    "Counter": {
      "Enabled": true,
      "DefaultIncrement": 1,
      "EnableLabels": true
    },
    "Gauge": {
      "Enabled": true,
      "EnableLabels": true
    },
    "Histogram": {
      "Enabled": true,
      "DefaultBuckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0],
      "EnableLabels": true
    },
    "Timer": {
      "Enabled": true,
      "DefaultBuckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0],
      "EnableLabels": true
    },
    "Export": {
      "Enabled": true,
      "ExportIntervalSeconds": 30,
      "Formats": ["Prometheus", "JSON"],
      "Prometheus": {
        "Enabled": true,
        "Endpoint": "/metrics",
        "Port": 9090
      }
    },
    "Middleware": {
      "Enabled": true,
      "HttpMetrics": {
        "Enabled": true,
        "TrackRequestDuration": true,
        "TrackRequestSize": true,
        "TrackResponseSize": true,
        "TrackStatusCode": true,
        "ExcludePaths": ["/health", "/metrics", "/swagger"]
      }
    }
  }
}
```

## 📊 Uso

### Métricas Básicas

```csharp
public class MiServicio
{
    private readonly IMetricsService _metricsService;

    public MiServicio(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task ProcesarDatos()
    {
        // Contador
        await _metricsService.RecordCounterAsync("datos_procesados", 1, 
            new Dictionary<string, string> { ["tipo"] = "importante" });

        // Gauge
        await _metricsService.RecordGaugeAsync("memoria_utilizada", 1024.5,
            new Dictionary<string, string> { ["unidad"] = "MB" });

        // Histograma
        await _metricsService.RecordHistogramAsync("tiempo_procesamiento", 150.2,
            new Dictionary<string, string> { ["operacion"] = "transformacion" });

        // Timer
        var stopwatch = Stopwatch.StartNew();
        // ... operación ...
        stopwatch.Stop();
        await _metricsService.RecordTimerAsync("operacion_completa", stopwatch.Elapsed.TotalMilliseconds);
    }
}
```

### Métricas HTTP Automáticas

El middleware captura automáticamente:
- Duración de requests
- Códigos de estado HTTP
- Tamaño de requests y responses
- Método HTTP y endpoint

### Métricas de Base de Datos

```csharp
public async Task ConsultarUsuarios()
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var usuarios = await _dbContext.Usuarios.ToListAsync();
        stopwatch.Stop();

        await _metricsService.RecordDatabaseMetricsAsync(new DatabaseMetrics
        {
            Operation = "SELECT",
            Table = "Usuarios",
            Database = "MiBaseDatos",
            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
            RecordsAffected = usuarios.Count,
            IsSuccess = true
        });

        return usuarios;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        await _metricsService.RecordDatabaseMetricsAsync(new DatabaseMetrics
        {
            Operation = "SELECT",
            Table = "Usuarios",
            Database = "MiBaseDatos",
            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
            RecordsAffected = 0,
            IsSuccess = false
        });
        throw;
    }
}
```

### Métricas de Negocio

```csharp
public async Task ProcesarPedido(Pedido pedido)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        // Procesar pedido...
        stopwatch.Stop();

        await _metricsService.RecordBusinessMetricsAsync(new BusinessMetrics
        {
            Operation = "ProcesarPedido",
            MetricType = "Revenue",
            Value = pedido.Total,
            Category = "Ventas",
            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
            IsSuccess = true,
            Labels = new Dictionary<string, string>
            {
                ["cliente_tipo"] = pedido.Cliente.Tipo,
                ["moneda"] = pedido.Moneda
            }
        });
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        await _metricsService.RecordBusinessMetricsAsync(new BusinessMetrics
        {
            Operation = "ProcesarPedido",
            MetricType = "Error",
            Value = 1,
            Category = "Errores",
            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
            IsSuccess = false
        });
        throw;
    }
}
```

## 🔍 Endpoints de Métricas

### Prometheus
```
GET /metrics
```

### JSON
```
GET /metrics/json
```

## 🏗️ Construcción

```bash
# Windows
.\build-package.ps1

# Linux/Mac
./build-package.sh
```

## 📈 Monitoreo

### Grafana Dashboard

Ejemplo de consultas PromQL:

```promql
# Requests por segundo
rate(http_requests_total[5m])

# Latencia promedio
histogram_quantile(0.95, rate(http_request_duration_ms_bucket[5m]))

# Errores por minuto
rate(http_requests_total{status_code=~"5.."}[1m])

# Métricas de base de datos
rate(database_queries_total[5m])
```

## 🧪 Testing

```csharp
[Test]
public async Task Debe_Registrar_Metrica_Correctamente()
{
    // Arrange
    var metricsService = new MetricsService(logger, options);
    
    // Act
    await metricsService.RecordCounterAsync("test_counter", 1);
    
    // Assert
    // Verificar que la métrica se registró correctamente
}
```

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## 🆘 Soporte

- 📧 Email: support@jonjubnet.com
- 🐛 Issues: [GitHub Issues](https://github.com/jonjubnet/JonjubNet.Metrics/issues)
- 📖 Documentación: [Wiki](https://github.com/jonjubnet/JonjubNet.Metrics/wiki)

## 🔗 Enlaces Relacionados

- [JonjubNet.Logging](https://github.com/jonjubnet/JonjubNet.Logging) - Biblioteca de logging estructurado
- [JonjubNet.Resilience](https://github.com/jonjubnet/JonjubNet.Resilience) - Biblioteca de resiliencia
- [Prometheus](https://prometheus.io/) - Sistema de monitoreo
- [Grafana](https://grafana.com/) - Plataforma de visualización

---

**Desarrollado con ❤️ por [JonjubNet](https://github.com/jonjubnet)**
