# Guías de Integración - JonjubNet.Metrics

Esta guía proporciona instrucciones detalladas para integrar JonjubNet.Metrics en diferentes tipos de aplicaciones y escenarios.

## Tabla de Contenidos

1. [Integración con ASP.NET Core](#integración-con-aspnet-core)
2. [Integración con Worker Services](#integración-con-worker-services)
3. [Integración con Console Apps](#integración-con-console-apps)
4. [Integración con Kubernetes](#integración-con-kubernetes)
5. [Integración con Prometheus](#integración-con-prometheus)
6. [Integración con OpenTelemetry](#integración-con-opentelemetry)
7. [Integración con InfluxDB](#integración-con-influxdb)
8. [Integración con Kafka](#integración-con-kafka)
9. [Integración con StatsD](#integración-con-statsd)
10. [Integración con Health Checks](#integración-con-health-checks)
11. [Integración con Logging](#integración-con-logging)

---

## Integración con ASP.NET Core

### Configuración Básica

```csharp
// Program.cs
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

// Configurar endpoints
app.MapHealthChecks("/health");
app.MapGet("/", () => "Hello World!");

app.Run();
```

### Configuración en appsettings.json

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MyWebApi",
    "Environment": "Production",
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  }
}
```

### Uso en Controladores

```csharp
using JonjubNet.Metrics.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMetricsClient _metricsClient;

    public OrdersController(IMetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        using var timer = _metricsClient.StartTimer("order_creation_duration_seconds");
        
        try
        {
            // Crear orden...
            
            _metricsClient.Increment("orders_created_total", 1.0,
                new Dictionary<string, string> 
                { 
                    ["status"] = "success",
                    ["region"] = order.Region 
                });
            
            return Ok(order);
        }
        catch (Exception ex)
        {
            _metricsClient.Increment("orders_created_total", 1.0,
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

---

## Integración con Worker Services

### Configuración Básica

```csharp
// Program.cs
using JonjubNet.Metrics;
using JonjubNet.Metrics.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Agregar servicios de métricas
        services.AddJonjubNetMetrics(context.Configuration);
        
        // Agregar worker service
        services.AddHostedService<MyWorkerService>();
    })
    .Build();

await host.RunAsync();
```

### Worker Service con Métricas

```csharp
using JonjubNet.Metrics.Core.Interfaces;

public class MyWorkerService : BackgroundService
{
    private readonly IMetricsClient _metricsClient;
    private readonly ILogger<MyWorkerService> _logger;

    public MyWorkerService(
        IMetricsClient metricsClient,
        ILogger<MyWorkerService> logger)
    {
        _metricsClient = metricsClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var timer = _metricsClient.StartTimer("worker_cycle_duration_seconds");
            
            try
            {
                // Procesar trabajo...
                
                _metricsClient.Increment("worker_cycles_total", 1.0,
                    new Dictionary<string, string> { ["status"] = "success" });
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _metricsClient.Increment("worker_cycles_total", 1.0,
                    new Dictionary<string, string> 
                    { 
                        ["status"] = "error",
                        ["error_type"] = ex.GetType().Name 
                    });
                
                _logger.LogError(ex, "Error processing work");
            }
        }
    }
}
```

---

## Integración con Console Apps

### Configuración con Host

```csharp
// Program.cs
using JonjubNet.Metrics;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Agregar servicios de métricas
        services.AddJonjubNetMetrics(context.Configuration);
    })
    .Build();

// Obtener IMetricsClient
var metricsClient = host.Services.GetRequiredService<IMetricsClient>();

// Usar métricas
metricsClient.Increment("console_app_operations_total", 1.0);

await host.RunAsync();
```

**Nota:** Console Apps simples sin `IHost` requieren configuración manual del scheduler.

---

## Integración con Kubernetes

### Configuración de Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-service
spec:
  replicas: 3
  template:
    metadata:
      labels:
        app: my-service
    spec:
      containers:
      - name: my-service
        image: my-service:latest
        ports:
        - containerPort: 8080
        - containerPort: 9090  # Prometheus metrics endpoint
        env:
        - name: Metrics__ServiceName
          value: "my-service"
        - name: Metrics__Environment
          value: "Production"
        - name: Metrics__Prometheus__Enabled
          value: "true"
        - name: Metrics__Prometheus__Path
          value: "/metrics"
```

### Service para Prometheus Scraping

```yaml
apiVersion: v1
kind: Service
metadata:
  name: my-service
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "9090"
    prometheus.io/path: "/metrics"
spec:
  selector:
    app: my-service
  ports:
  - name: http
    port: 8080
  - name: metrics
    port: 9090
```

### ServiceMonitor para Prometheus Operator

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: my-service
spec:
  selector:
    matchLabels:
      app: my-service
  endpoints:
  - port: metrics
    path: /metrics
    interval: 30s
```

---

## Integración con Prometheus

### Configuración de Prometheus

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'my-service'
    scrape_interval: 30s
    metrics_path: /metrics
    static_configs:
      - targets: ['my-service:9090']
        labels:
          service: 'my-service'
          environment: 'production'
```

### Exponer Endpoint de Métricas

El componente expone automáticamente el endpoint `/metrics` cuando Prometheus está habilitado:

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

### Verificación

```bash
# Verificar que el endpoint funciona
curl http://localhost:9090/metrics

# Debería mostrar métricas en formato Prometheus
# http_requests_total{method="GET",endpoint="/api/orders",status_code="200"} 42
```

---

## Integración con OpenTelemetry

### Configuración

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector:4318",
      "Protocol": "HttpJson",
      "EnableCompression": true,
      "TimeoutSeconds": 30
    },
    "Encryption": {
      "EnableInTransit": true,
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

### OpenTelemetry Collector Configuration

```yaml
# otel-collector-config.yml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
  
  # O exportar a otros backends
  # influxdb:
  #   endpoint: "http://influxdb:8086"
  #   token: "${INFLUXDB_TOKEN}"

service:
  pipelines:
    metrics:
      receivers: [otlp]
      exporters: [prometheus]
```

---

## Integración con InfluxDB

### Configuración

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://influxdb.example.com:8086",
      "Token": "${INFLUXDB_TOKEN}",
      "Organization": "my-org",
      "Bucket": "metrics",
      "EnableCompression": true,
      "TimeoutSeconds": 30
    },
    "Encryption": {
      "EnableInTransit": true,
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

### Variables de Entorno

```bash
# .env o configuración de Kubernetes
INFLUXDB_TOKEN=your-secret-token-here
```

### Verificación

```bash
# Verificar conexión a InfluxDB
curl -X POST "https://influxdb.example.com:8086/api/v2/write?org=my-org&bucket=metrics" \
  -H "Authorization: Token ${INFLUXDB_TOKEN}" \
  -H "Content-Type: text/plain; charset=utf-8" \
  -d "test,host=server01 value=1.0"
```

---

## Integración con Kafka

### Configuración

```json
{
  "Metrics": {
    "Kafka": {
      "Enabled": true,
      "Broker": "kafka-broker:9092",
      "Topic": "metrics",
      "EnableCompression": true
    }
  }
}
```

### Integración con Confluent.Kafka

**Nota:** El componente proporciona la estructura básica. Para producción, integra con `Confluent.Kafka`:

```csharp
using Confluent.Kafka;

// En tu implementación personalizada de KafkaMetricsSink
var config = new ProducerConfig
{
    BootstrapServers = "kafka-broker:9092",
    CompressionType = CompressionType.Gzip
};

using var producer = new ProducerBuilder<string, string>(config).Build();
// ... enviar métricas a Kafka
```

---

## Integración con StatsD

### Configuración

```json
{
  "Metrics": {
    "StatsD": {
      "Enabled": true,
      "Host": "statsd-server",
      "Port": 8125
    }
  }
}
```

### Ejecutar StatsD Server

```bash
# Docker
docker run -d \
  --name statsd \
  -p 8125:8125/udp \
  -p 8126:8126 \
  graphiteapp/graphite-statsd
```

### Verificación

```bash
# Enviar métrica de prueba
echo "test.metric:1|c" | nc -u -w1 statsd-server 8125
```

---

## Integración con Health Checks

### Configuración Básica

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<MetricsHealthCheckService>("metrics");
```

### Endpoint de Health

```csharp
app.MapHealthChecks("/health");
```

### Health Check Detallado

```csharp
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

### Uso Programático

```csharp
using JonjubNet.Metrics.Shared.Health;

var healthCheck = serviceProvider.GetRequiredService<IMetricsHealthCheck>();
var health = healthCheck?.GetOverallHealth();

if (health?.IsHealthy == true)
{
    Console.WriteLine("Metrics system is healthy");
    Console.WriteLine($"Scheduler running: {health.SchedulerHealth.IsRunning}");
    Console.WriteLine($"DLQ size: {health.DLQHealth?.CurrentSize ?? 0}");
}
```

---

## Integración con Logging

### Configuración con ILogger Estándar

El componente usa `ILogger` estándar de `Microsoft.Extensions.Logging`. Funciona automáticamente con cualquier proveedor de logging configurado:

```csharp
// Program.cs
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    // Agregar cualquier proveedor de logging
    // builder.AddSerilog();
    // builder.AddNLog();
});
```

### Configuración de Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "JonjubNet.Metrics": "Warning"
    }
  }
}
```

### Eventos Registrados

El componente registra los siguientes eventos:
- **Information**: Inicio/detención del scheduler, cambios de configuración
- **Warning**: Circuit breakers abiertos, validaciones fallidas, DLQ llena
- **Error**: Errores de exportación, fallos críticos
- **Debug**: Operaciones de métricas (si está habilitado)

---

## Referencias

- [README.md](README.md) - Guía de inicio rápido
- [CONFIGURATION.md](CONFIGURATION.md) - Documentación completa de configuración
- [EXAMPLES.md](EXAMPLES.md) - Ejemplos de código
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Resolución de problemas

