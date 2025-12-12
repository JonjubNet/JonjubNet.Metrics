# JonjubNet.Metrics

[![NuGet Version](https://img.shields.io/nuget/v/JonjubNet.Metrics.svg)](https://www.nuget.org/packages/JonjubNet.Metrics/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Biblioteca de métricas para aplicaciones .NET con soporte para contadores, gauges, histogramas y timers, integración con múltiples backends (Prometheus, OpenTelemetry, Kafka, StatsD, InfluxDB) y arquitectura Hexagonal (Ports & Adapters).

## 🚀 Características

- **Métricas Estándar**: Soporte completo para contadores, gauges, histogramas, summaries y timers
- **Multi-Backend**: Exportación a Prometheus, OpenTelemetry, Kafka, StatsD, InfluxDB
- **Arquitectura Hexagonal**: Diseño pluggable con Ports & Adapters
- **Alto Rendimiento**: Overhead ~5-15ns por métrica (comparable o mejor que Prometheus), zero allocations en hot path
- **Thread-Safe**: Registro thread-safe con ConcurrentDictionary
- **Configuración Flexible**: Configuración completa via appsettings.json con hot-reload
- **Seguridad**: Validación y sanitización de tags, encriptación en tránsito (TLS/SSL, AES) y en reposo (DLQ)
- **Resiliencia**: Circuit breakers por sink individual, retry con exponential backoff y jitter, Dead Letter Queue con auto-processing
- **Health Checks**: Health checks integrados para observabilidad
- **TLS/SSL**: Soporte para conexiones seguras en sinks HTTP
- **.NET 10.0**: Compatible con las últimas versiones de .NET

## 📦 Instalación

```bash
dotnet add package JonjubNet.Metrics
```

## 🔧 Configuración Rápida

### 1. Configurar en Program.cs

```csharp
using JonjubNet.Metrics;
using JonjubNet.Metrics.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios de métricas
builder.Services.AddJonjubNetMetrics(builder.Configuration);

// Agregar health checks (opcional pero recomendado)
builder.Services.AddHealthChecks()
    .AddCheck<MetricsHealthCheckService>("metrics");

var app = builder.Build();

// Usar middleware de métricas HTTP (opcional)
app.UseMetricsMiddleware();

// Configurar endpoint de health
app.MapHealthChecks("/health");

app.Run();
```

### 2. Configurar en appsettings.json

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MyService",
    "Environment": "Production",
    "Version": "1.0.0",
    "QueueCapacity": 10000,
    "BatchSize": 200,
    "FlushIntervalMs": 1000,
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  }
}
```

## 📖 Ejemplos de Uso

### Uso Básico con IMetricsClient

```csharp
using JonjubNet.Metrics.Core.Interfaces;

public class MyService
{
    private readonly IMetricsClient _metricsClient;

    public MyService(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public void ProcessOrder()
    {
        // Incrementar contador
        _metricsClient.Increment("orders_processed_total", 1.0, 
            new Dictionary<string, string> { ["status"] = "success" });

        // Registrar gauge
        _metricsClient.SetGauge("active_orders", 42.0);

        // Registrar histograma
        _metricsClient.ObserveHistogram("order_processing_duration_seconds", 0.5);

        // Usar timer
        using var timer = _metricsClient.StartTimer("operation_duration_seconds");
        // ... operación ...
    }
}
```

### Uso con IMetricsService (Alto Nivel)

```csharp
using JonjubNet.Metrics.Interfaces;

public class OrderService
{
    private readonly IMetricsService _metricsService;

    public OrderService(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Procesar orden...
            
            stopwatch.Stop();

            // Registrar métricas
            await _metricsService.RecordCounterAsync("orders_processed_total", 1.0,
                new Dictionary<string, string> 
                { 
                    ["status"] = "success",
                    ["region"] = order.Region 
                });

            await _metricsService.RecordTimerAsync("order_processing_duration_ms", 
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            await _metricsService.RecordCounterAsync("orders_processed_total", 1.0,
                new Dictionary<string, string> 
                { 
                    ["status"] = "error",
                    ["error_type"] = ex.GetType().Name 
                });
            
            throw;
        }
    }
}
```

### Métricas HTTP Automáticas

El middleware captura automáticamente métricas HTTP:

```csharp
// En Program.cs
app.UseMetricsMiddleware();
```

Esto registra automáticamente:
- `http_requests_total` - Contador de requests
- `http_request_duration_seconds` - Histograma de duración
- `http_request_size_bytes` - Tamaño de request (si está habilitado)
- `http_response_size_bytes` - Tamaño de respuesta (si está habilitado)

### Métricas de Base de Datos

```csharp
await _metricsService.RecordDatabaseMetricsAsync(new DatabaseMetrics
{
    Operation = "SELECT",
    Table = "users",
    Database = "mydb",
    DurationMs = 45.2,
    RecordsAffected = 10,
    IsSuccess = true,
    Labels = new Dictionary<string, string>
    {
        ["connection"] = "pool-1"
    }
});
```

### Métricas de Negocio

```csharp
await _metricsService.RecordBusinessMetricsAsync(new BusinessMetrics
{
    Operation = "ProcessPayment",
    MetricType = "Revenue",
    Value = 299.99,
    Category = "Sales",
    DurationMs = 200.0,
    IsSuccess = true,
    Labels = new Dictionary<string, string>
    {
        ["product"] = "laptop",
        ["customer_type"] = "premium"
    }
});
```

## 🔌 Configuración de Adapters

### Prometheus (Por Defecto)

```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  }
}
```

El endpoint `/metrics` expone las métricas en formato Prometheus.

### OpenTelemetry

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://localhost:4318",
      "Protocol": "HttpProtobuf",
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

**Nota:** Para conexiones seguras, usar `https://` y habilitar `EnableTls: true`.

### Kafka

```json
{
  "Metrics": {
    "Kafka": {
      "Enabled": true,
      "Broker": "localhost:9092",
      "Topic": "metrics"
    }
  }
}
```

**Nota:** Para producción, integra con `Confluent.Kafka` o similar.

### StatsD

```json
{
  "Metrics": {
    "StatsD": {
      "Enabled": true,
      "Host": "localhost",
      "Port": 8125
    }
  }
}
```

### InfluxDB

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://localhost:8086",
      "Token": "your-token",
      "Organization": "my-org",
      "Bucket": "metrics",
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

**Nota:** Para conexiones seguras, usar `https://` y habilitar `EnableTls: true`.

## 🏗️ Arquitectura

El componente sigue una arquitectura Hexagonal (Ports & Adapters) optimizada para performance:

```
Application
    ↓
IMetricsClient (Port)
    ↓
MetricRegistry (Core - escritura directa, ~5-15ns overhead)
    ↓
MetricFlushScheduler (lee del Registry periódicamente)
    ↓
IMetricsSink (Port - lee directamente del Registry)
    ↓
Adapters (Prometheus, OTel, Kafka, etc.)
```

**Optimizaciones implementadas:**
- ✅ Eliminación del Bus: Sinks leen directamente del Registry (85% reducción en overhead)
- ✅ Fast path: `Interlocked.Add()` para contadores sin tags (~5-10ns)
- ✅ Zero allocations en hot path
- ✅ Circuit breakers por sink individual
- ✅ Encriptación integrada automáticamente en todos los sinks HTTP

### Estructura de Proyectos

- **Core/JonjubNet.Metrics.Core**: Lógica central sin dependencias
- **Infrastructure/JonjubNet.Metrics.Shared**: Resiliencia, seguridad, configuración
- **Infrastructure/JonjubNet.Metrics.***: Adapters para diferentes backends
- **Presentation/JonjubNet.Metrics**: Integración con ASP.NET Core

## 🔒 Seguridad

El componente incluye validación y sanitización automática de tags:

- Validación de claves (formato, blacklist)
- Detección de PII (emails, tarjetas, SSN)
- Sanitización de valores sensibles
- Prevención de metric injection

## 🩺 Health Checks

El componente incluye health checks integrados:

```csharp
// En Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<MetricsHealthCheckService>("metrics");

// Endpoint de health
app.MapHealthChecks("/health");
```

El health check verifica:
- Estado del scheduler (último flush, ejecución)
- Estado de los sinks (disponibilidad, errores)
- Estado de la Dead Letter Queue (métricas fallidas)

### Uso Programático

```csharp
var healthCheck = serviceProvider.GetService<IMetricsHealthCheck>();
var health = healthCheck?.GetOverallHealth();

if (health?.IsHealthy == true)
{
    Console.WriteLine("Metrics system is healthy");
    Console.WriteLine($"Scheduler running: {health.SchedulerHealth.IsRunning}");
    Console.WriteLine($"DLQ size: {health.DlqHealth?.QueueSize ?? 0}");
}
```

## 🔒 Seguridad Avanzada

### Encriptación en Tránsito y Reposo

El componente soporta encriptación completa:

```json
{
  "Metrics": {
    "Encryption": {
      "EnableInTransit": true,
      "EnableAtRest": true,
      "EnableTls": true,
      "ValidateCertificates": true,
      "EncryptionKeyBase64": "optional-base64-key",
      "EncryptionIVBase64": "optional-base64-iv"
    }
  }
}
```

**Características:**
- **Encriptación en tránsito**: AES para payloads HTTP, TLS/SSL para conexiones
- **Encriptación en reposo**: AES para métricas almacenadas en Dead Letter Queue
- **Integración automática**: Todos los sinks HTTP se registran automáticamente con encriptación
- **Configuración centralizada**: Una sola configuración para todos los sinks

### TLS/SSL para Sinks HTTP

```json
{
  "Metrics": {
    "InfluxDB": {
      "EnableTls": true,
      "ValidateCertificates": true
    },
    "OpenTelemetry": {
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

### Certificados Personalizados

```csharp
// Configurar validación personalizada de certificados
var factory = new SecureHttpClientFactory();
var client = factory.CreateSecureClient(
    validateCertificates: true,
    customCertificateValidation: (request, cert, chain, errors) =>
    {
        // Lógica personalizada de validación
        return errors == SslPolicyErrors.None;
    });
```

### Validación de Certificados

Por defecto, el componente valida certificados SSL estrictamente. Para desarrollo/testing, se puede deshabilitar (NO recomendado para producción):

```json
{
  "Metrics": {
    "InfluxDB": {
      "EnableTls": true,
      "ValidateCertificates": false  // Solo para desarrollo
    }
  }
}
```

## ⚡ Performance y Benchmarking

### Ejecutar Benchmarks

```bash
cd Tests/JonjubNet.Metrics.Benchmarks
dotnet run -c Release
```

Los benchmarks miden:
- Overhead de incremento de contadores (~5-15ns)
- Throughput (~100M+ métricas/segundo)
- Latencia de escritura/lectura
- Allocations de memoria (0 en hot path)

### Optimizaciones Recomendadas

1. **Aumentar batch size** para reducir overhead:
   ```json
   {
     "Metrics": {
       "BatchSize": 500
     }
   }
   ```

2. **Ajustar flush interval** según volumen:
   ```json
   {
     "Metrics": {
       "FlushIntervalMs": 2000  // Para bajo volumen
     }
   }
   ```

3. **Limitar cardinalidad de tags:**
   - Evitar tags con valores únicos
   - Usar agregación cuando sea posible

4. **Configurar circuit breakers por sink** para aislar fallos:
   ```json
   {
     "Metrics": {
       "CircuitBreaker": {
         "Enabled": true,
         "Default": {
           "FailureThreshold": 5,
           "OpenDurationSeconds": 30
         }
       }
     }
   }
   ```

## ⚙️ Configuración Avanzada

### Hot-Reload

La configuración puede recargarse sin reiniciar la aplicación:

```csharp
// Cambiar nivel de log en runtime
_configurationManager.SetMinimumLevel("Information");

// Habilitar/deshabilitar sinks
_configurationManager.SetSinkEnabled("Prometheus", true);
```

### Resiliencia

```json
{
  "Metrics": {
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenDurationSeconds": 30
      },
      "Sinks": {
        "OTLPExporter": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenDurationSeconds": 60
        }
      }
    },
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "InitialDelayMs": 100,
      "BackoffMultiplier": 2.0,
      "JitterPercent": 0.1
    },
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "EnableAutoProcessing": true,
      "ProcessingIntervalMs": 60000
    }
  }
}
```

## 📊 Métricas Disponibles

### Tipos de Métricas

1. **Counter**: Solo incrementa (ej: requests totales)
2. **Gauge**: Puede subir o bajar (ej: conexiones activas)
3. **Histogram**: Distribución de valores (ej: latencia)
4. **Summary**: Percentiles calculados (ej: tiempo de procesamiento)
5. **Timer**: Medición de duración (wrapper sobre histogram)

## 🧪 Testing

```csharp
// Mock IMetricsClient para tests
var mockMetricsClient = new Mock<IMetricsClient>();
mockMetricsClient.Setup(m => m.Increment(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<Dictionary<string, string>>()));

// Usar en tests
var service = new MyService(mockMetricsClient.Object);
```

## 🔍 Troubleshooting

Ver la guía completa de troubleshooting en [TROUBLESHOOTING.md](TROUBLESHOOTING.md) para resolver problemas comunes:

- Métricas no se exportan
- Circuit breakers abiertos
- Errores de conexión a sinks
- Performance degradada
- Errores de validación de tags
- Dead Letter Queue llena

## 📚 Más Información

- Ver `project_structure.md` para detalles de arquitectura
- Ver `EVALUACION_PRODUCCION.md` para análisis de producción
- Ver `EXAMPLES.md` para ejemplos detallados de uso
- Ver `TROUBLESHOOTING.md` para resolución de problemas
- Ver ejemplos en `Examples/UsageExample.cs`

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:
1. Fork el proyecto
2. Crea una rama para tu feature
3. Commit tus cambios
4. Push a la rama
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo LICENSE para más detalles.
