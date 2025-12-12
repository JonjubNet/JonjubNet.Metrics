# Ejemplos de Uso - JonjubNet.Metrics

Este documento contiene ejemplos detallados de uso del componente de métricas.

## Tabla de Contenidos

1. [Configuración Básica](#configuración-básica)
2. [Uso de Métricas](#uso-de-métricas)
3. [Health Checks](#health-checks)
4. [Seguridad](#seguridad)
5. [Performance](#performance)
6. [Casos de Uso Avanzados](#casos-de-uso-avanzados)

---

## Configuración Básica

### Configuración Mínima

```csharp
// Program.cs
using JonjubNet.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Agregar métricas
builder.Services.AddJonjubNetMetrics(builder.Configuration);

var app = builder.Build();

// Usar middleware (opcional)
app.UseMetricsMiddleware();

app.Run();
```

### Configuración Completa en appsettings.json

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
    },
    "OpenTelemetry": {
      "Enabled": false,
      "Endpoint": "http://localhost:4318",
      "Protocol": "HttpProtobuf"
    },
    "Kafka": {
      "Enabled": false,
      "Broker": "localhost:9092",
      "Topic": "metrics"
    },
    "StatsD": {
      "Enabled": false,
      "Host": "localhost",
      "Port": 8125
    },
    "InfluxDB": {
      "Enabled": false,
      "Url": "http://localhost:8086",
      "Token": "your-token",
      "Organization": "my-org",
      "Bucket": "metrics"
    }
  }
}
```

---

## Uso de Métricas

### Ejemplo 1: Contador Simple

```csharp
public class OrderService
{
    private readonly IMetricsClient _metricsClient;

    public OrderService(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Incrementar contador
        _metricsClient.Increment("orders_processed_total", 1.0, 
            new Dictionary<string, string> 
            { 
                ["status"] = "success",
                ["region"] = order.Region 
            });
    }
}
```

### Ejemplo 2: Gauge para Métricas de Sistema

```csharp
public class SystemMonitor
{
    private readonly IMetricsClient _metricsClient;

    public SystemMonitor(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public void UpdateMemoryUsage()
    {
        var memoryUsage = GC.GetTotalMemory(false) / 1024.0 / 1024.0; // MB
        
        _metricsClient.SetGauge("system_memory_usage_mb", memoryUsage,
            new Dictionary<string, string>
            {
                ["host"] = Environment.MachineName
            });
    }
}
```

### Ejemplo 3: Histograma para Latencia

```csharp
public class ApiService
{
    private readonly IMetricsClient _metricsClient;

    public ApiService(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public async Task<string> CallExternalApiAsync(string url)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            stopwatch.Stop();
            
            // Registrar latencia
            _metricsClient.ObserveHistogram("external_api_duration_seconds", 
                stopwatch.Elapsed.TotalSeconds,
                new Dictionary<string, string>
                {
                    ["endpoint"] = url,
                    ["status"] = "success"
                });
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _metricsClient.ObserveHistogram("external_api_duration_seconds",
                stopwatch.Elapsed.TotalSeconds,
                new Dictionary<string, string>
                {
                    ["endpoint"] = url,
                    ["status"] = "error",
                    ["error_type"] = ex.GetType().Name
                });
            
            throw;
        }
    }
}
```

### Ejemplo 4: Timer Automático

```csharp
public class DatabaseService
{
    private readonly IMetricsClient _metricsClient;

    public DatabaseService(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        using var timer = _metricsClient.StartTimer("database_query_duration_seconds",
            new Dictionary<string, string>
            {
                ["operation"] = "SELECT",
                ["table"] = "users"
            });
        
        // La operación se mide automáticamente
        return await _dbContext.Users.ToListAsync();
    }
}
```

### Ejemplo 5: Uso con IMetricsService (Alto Nivel)

```csharp
public class PaymentService
{
    private readonly IMetricsService _metricsService;

    public PaymentService(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Procesar pago...
            var result = await ProcessPaymentInternalAsync(payment);
            stopwatch.Stop();
            
            // Registrar métricas de negocio
            await _metricsService.RecordBusinessMetricsAsync(new BusinessMetrics
            {
                Operation = "ProcessPayment",
                MetricType = "Revenue",
                Value = payment.Amount,
                Category = "Sales",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                IsSuccess = true,
                Labels = new Dictionary<string, string>
                {
                    ["payment_method"] = payment.Method,
                    ["currency"] = payment.Currency
                }
            });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            await _metricsService.RecordCounterAsync("payments_failed_total", 1.0,
                new Dictionary<string, string>
                {
                    ["error_type"] = ex.GetType().Name
                });
            
            throw;
        }
    }
}
```

---

## Health Checks

### Configuración de Health Checks

```csharp
// Program.cs
using JonjubNet.Metrics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJonjubNetMetrics(builder.Configuration);

// Agregar health check de métricas
builder.Services.AddHealthChecks()
    .AddMetricsHealthCheck("metrics");

var app = builder.Build();

// Endpoint de health checks
app.MapHealthChecks("/health");

app.Run();
```

### Uso Programático de Health Checks

```csharp
public class MetricsMonitor
{
    private readonly IMetricsHealthCheck _healthCheck;

    public MetricsMonitor(IMetricsHealthCheck healthCheck)
    {
        _healthCheck = healthCheck;
    }

    public void CheckHealth()
    {
        var status = _healthCheck.GetHealthStatus();
        var info = _healthCheck.GetHealthInfo();
        
        Console.WriteLine($"Status: {status}");
        Console.WriteLine($"Queue Utilization: {info.BusQueueUtilization:P2}");
        Console.WriteLine($"Active Sinks: {info.ActiveSinks}/{info.TotalSinks}");
        
        foreach (var sink in info.SinkHealth)
        {
            Console.WriteLine($"Sink {sink.Key}: {(sink.Value.IsHealthy ? "Healthy" : "Unhealthy")}");
        }
    }
}
```

---

## Seguridad

### Encriptación de Métricas

```csharp
using JonjubNet.Metrics.Shared.Security;

public class SecureMetricsService
{
    private readonly EncryptionService _encryption;
    private readonly IMetricsClient _metricsClient;

    public SecureMetricsService(EncryptionService encryption, IMetricsClient metricsClient)
    {
        _encryption = encryption;
        _metricsClient = metricsClient;
    }

    public void RecordSensitiveMetric(string metricName, string sensitiveValue)
    {
        // Encriptar valor sensible antes de enviarlo
        var encrypted = _encryption.EncryptString(sensitiveValue);
        
        _metricsClient.Increment(metricName, 1.0,
            new Dictionary<string, string>
            {
                ["encrypted_value"] = encrypted
            });
    }
}
```

### Configuración de Claves de Encriptación

```csharp
// En producción, las claves deben venir de configuración segura
var key = Convert.FromBase64String(configuration["Metrics:Encryption:Key"]);
var iv = Convert.FromBase64String(configuration["Metrics:Encryption:IV"]);

var encryptionService = new EncryptionService(key, iv, logger);
```

---

## Performance

### Benchmarking

```bash
# Ejecutar benchmarks
cd Tests/JonjubNet.Metrics.Benchmarks
dotnet run -c Release
```

### Optimizaciones Implementadas

El componente incluye varias optimizaciones de performance:

1. **Object Pooling**: Reutilización de listas y diccionarios para reducir allocations
2. **JSON Serializer Cache**: Reutilización de instancias de JsonSerializerOptions
3. **Procesamiento Paralelo**: Los sinks se procesan en paralelo usando `Task.WhenAll`
4. **Compresión GZip**: Compresión automática para batches grandes (>50 métricas) en adapters HTTP
5. **Pre-allocation**: Capacidad pre-asignada en colecciones para evitar re-allocations

### Optimizaciones Recomendadas para Usuarios

1. **Usar tags eficientemente**: Evitar crear diccionarios nuevos en cada llamada
   ```csharp
   // ❌ Malo
   _metricsClient.Increment("counter", 1.0, new Dictionary<string, string> { ["tag"] = "value" });
   
   // ✅ Bueno
   private static readonly Dictionary<string, string> Tags = new() { ["tag"] = "value" };
   _metricsClient.Increment("counter", 1.0, Tags);
   ```

2. **Reutilizar instancias de métricas**
   ```csharp
   // ✅ Bueno - reutilizar contador
   private readonly Counter _orderCounter;
   
   public MyService(IMetricsClient metricsClient)
   {
       _orderCounter = metricsClient.CreateCounter("orders_total", "Total orders");
   }
   
   public void ProcessOrder()
   {
       _orderCounter.Inc();
   }
   ```

3. **Usar batching automático**
   ```csharp
   // Las métricas se batching automáticamente en el MetricBus
   // Configurar batchSize y flushInterval según necesidades:
   // - BatchSize mayor = menos llamadas pero más latencia
   // - BatchSize menor = más llamadas pero menor latencia
   ```

4. **Habilitar compresión para adapters remotos**
   ```json
   {
     "Metrics": {
       "OpenTelemetry": {
         "EnableCompression": true
       },
       "InfluxDB": {
         "EnableCompression": true
       }
     }
   }
   ```

---

## Casos de Uso Avanzados

### Métricas Personalizadas por Endpoint

```csharp
public class CustomMetricsMiddleware
{
    private readonly IMetricsClient _metricsClient;
    private readonly RequestDelegate _next;

    public CustomMetricsMiddleware(IMetricsClient metricsClient, RequestDelegate next)
    {
        _metricsClient = metricsClient;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await _next(context);
            stopwatch.Stop();
            
            _metricsClient.ObserveHistogram("http_request_duration_seconds",
                stopwatch.Elapsed.TotalSeconds,
                new Dictionary<string, string>
                {
                    ["method"] = context.Request.Method,
                    ["path"] = context.Request.Path,
                    ["status"] = context.Response.StatusCode.ToString()
                });
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _metricsClient.Increment("http_requests_errors_total", 1.0);
            throw;
        }
    }
}
```

### Métricas de Base de Datos con EF Core

```csharp
public class DbContextWithMetrics : DbContext
{
    private readonly IMetricsService _metricsService;

    public DbContextWithMetrics(DbContextOptions options, IMetricsService metricsService)
        : base(options)
    {
        _metricsService = metricsService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();
            
            await _metricsService.RecordDatabaseMetricsAsync(new DatabaseMetrics
            {
                Operation = "SaveChanges",
                Database = Database.GetDbConnection().Database ?? "unknown",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                RecordsAffected = result,
                IsSuccess = true
            });
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            await _metricsService.RecordDatabaseMetricsAsync(new DatabaseMetrics
            {
                Operation = "SaveChanges",
                Database = Database.GetDbConnection().Database ?? "unknown",
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                RecordsAffected = 0,
                IsSuccess = false
            });
            
            throw;
        }
    }
}
```

---

## Más Información

- Ver [README.md](README.md) para documentación general
- Ver [project_structure.md](../../project_structure.md) para arquitectura
- Ver [EVALUACION_PRODUCCION.md](../../EVALUACION_PRODUCCION.md) para análisis de producción
