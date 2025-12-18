# Guía de Implementación Paso a Paso - JonjubNet.Metrics

## 📋 Índice

1. [Nivel 1 - Básico (Implementación Mínima)](#nivel-1---básico-implementación-mínima)
2. [Nivel 2 - Métricas Intermedias](#nivel-2---métricas-intermedias)
3. [Nivel 3 - Múltiples Sinks](#nivel-3---múltiples-sinks)
4. [Nivel 4 - Funcionalidades Avanzadas](#nivel-4---funcionalidades-avanzadas)
5. [Nivel 5 - Resiliencia Enterprise](#nivel-5---resiliencia-enterprise)
6. [Nivel 6 - Seguridad](#nivel-6---seguridad)
7. [Nivel 7 - Configuración Dinámica](#nivel-7---configuración-dinámica)
8. [Infraestructura Necesaria por Sink](#-infraestructura-necesaria-por-sink)

---

## Nivel 1 - Básico (Implementación Mínima)

### Objetivo
Implementar la funcionalidad básica de métricas: Counter, Gauge y exportación a Prometheus.

### Paso 1.1: Instalar el Paquete NuGet

Agrega la referencia al proyecto en tu solución:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Componet\JonjubNet.Metrics\Presentation\JonjubNet.Metrics\JonjubNet.Metrics.csproj" />
</ItemGroup>
```

### Paso 1.2: Configurar en Program.cs (ASP.NET Core)

```csharp
using JonjubNet.Metrics;
using JonjubNet.Metrics.Hosting;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Agregar métricas
builder.Services.AddJonjubNetMetrics(builder.Configuration);

var app = builder.Build();

// Opcional: Agregar middleware para exponer métricas HTTP
app.UseMetricsMiddleware();

app.Run();
```

**Nota:** El método `AddJonjubNetMetrics` está en el namespace `JonjubNet.Metrics.Hosting`. Alternativamente, puedes usar `AddMetricsInfrastructure` que está en `JonjubNet.Metrics` y no requiere el using adicional.

### Paso 1.3: Configurar appsettings.json

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "Environment": "Development",
    "Version": "1.0.0",
    "FlushIntervalMs": 1000,
    "BatchSize": 200,
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    }
  }
}
```

### Paso 1.4: Implementar Counter (Contador)

**Ejemplo en un Controller:**

```csharp
using JonjubNet.Metrics.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMetricsClient _metrics;

    public OrdersController(IMetricsClient metrics)
    {
        _metrics = metrics;
    }

    [HttpPost]
    public IActionResult CreateOrder([FromBody] OrderDto order)
    {
        // Incrementar contador de órdenes creadas
        _metrics.Increment("orders_created_total");
        
        // Con tags (labels)
        _metrics.Increment("orders_created_total", 1.0, new Dictionary<string, string>
        {
            { "status", "success" },
            { "region", "us-east" }
        });

        return Ok();
    }
}
```

**Ejemplo con Counter reutilizable:**

```csharp
public class OrderService
{
    private readonly Counter _ordersCounter;

    public OrderService(IMetricsClient metrics)
    {
        _ordersCounter = metrics.CreateCounter("orders_created_total", "Total de órdenes creadas");
    }

    public void CreateOrder()
    {
        _ordersCounter.Inc(new Dictionary<string, string>
        {
            { "status", "success" }
        });
    }
}
```

### Paso 1.5: Implementar Gauge (Medidor)

**Ejemplo: Medir conexiones activas**

```csharp
public class ConnectionManager
{
    private readonly Gauge _activeConnections;

    public ConnectionManager(IMetricsClient metrics)
    {
        _activeConnections = metrics.CreateGauge("connections_active", "Conexiones activas actuales");
    }

    public void OnConnectionOpened()
    {
        _activeConnections.Set(GetActiveConnectionCount());
    }

    public void OnConnectionClosed()
    {
        _activeConnections.Set(GetActiveConnectionCount());
    }

    private double GetActiveConnectionCount()
    {
        // Tu lógica para contar conexiones
        return 42;
    }
}
```

**Ejemplo: Medir uso de memoria**

```csharp
public class SystemMonitor
{
    private readonly Gauge _memoryUsage;

    public SystemMonitor(IMetricsClient metrics)
    {
        _memoryUsage = metrics.CreateGauge("memory_usage_bytes", "Uso de memoria en bytes");
    }

    public void UpdateMemoryMetrics()
    {
        var memory = GC.GetTotalMemory(false);
        _memoryUsage.Set(memory);
    }
}
```

### Paso 1.6: Verificar Métricas en Prometheus

1. Inicia tu aplicación
2. Navega a: `http://localhost:5000/metrics`
3. Deberías ver las métricas en formato Prometheus:

```
# HELP orders_created_total Total de órdenes creadas
# TYPE orders_created_total counter
orders_created_total{status="success",region="us-east"} 5.0

# HELP connections_active Conexiones activas actuales
# TYPE connections_active gauge
connections_active 42.0
```

### ✅ Checklist Nivel 1

- [ ] Paquete NuGet agregado
- [ ] Servicios configurados en `Program.cs`
- [ ] `appsettings.json` configurado
- [ ] Counter implementado y funcionando
- [ ] Gauge implementado y funcionando
- [ ] Endpoint `/metrics` accesible
- [ ] Métricas visibles en formato Prometheus

---

### 🤖 Prompt para Cursor - Nivel 1

```
Necesito implementar métricas básicas en mi proyecto JonjubNet.CleanArch usando JonjubNet.Metrics.

Pasos a realizar:
1. Agregar referencia al proyecto JonjubNet.Metrics en mi proyecto principal
2. Configurar AddJonjubNetMetrics en Program.cs con la configuración básica
3. Crear appsettings.json con la sección Metrics configurada para Prometheus
4. Implementar un Counter en un servicio o controller para contar operaciones (ej: requests, orders, etc.)
5. Implementar un Gauge en un servicio para medir valores instantáneos (ej: conexiones activas, memoria)
6. Verificar que el endpoint /metrics exponga las métricas en formato Prometheus

Usa el patrón de inyección de dependencias estándar de ASP.NET Core. Los ejemplos deben ser prácticos y relacionados con un sistema de e-commerce o API REST.
```

---

## Nivel 2 - Métricas Intermedias

### Objetivo
Implementar Histogram, Summary, Timer y Tags/Labels para métricas más complejas.

### Paso 2.1: Implementar Histogram

**Histogram** mide distribuciones de valores (ej: tiempos de respuesta, tamaños de archivos).

```csharp
public class OrderService
{
    private readonly Histogram _orderValueHistogram;

    public OrderService(IMetricsClient metrics)
    {
        // Buckets personalizados para valores de órdenes
        var buckets = new[] { 10.0, 50.0, 100.0, 500.0, 1000.0, 5000.0 };
        _orderValueHistogram = metrics.CreateHistogram(
            "order_value_dollars",
            "Distribución de valores de órdenes en dólares",
            buckets
        );
    }

    public void ProcessOrder(Order order)
    {
        // Observar el valor de la orden
        _orderValueHistogram.Observe(order.TotalAmount);
        
        // Con tags
        _orderValueHistogram.Observe(
            order.TotalAmount,
            new Dictionary<string, string>
            {
                { "payment_method", order.PaymentMethod },
                { "region", order.Region }
            }
        );
    }
}
```

### Paso 2.2: Implementar Summary (Percentiles)

**Summary** calcula percentiles (P50, P90, P99) de valores observados.

```csharp
public class ApiController : ControllerBase
{
    private readonly Summary _responseTimeSummary;

    public ApiController(IMetricsClient metrics)
    {
        // Quantiles personalizados
        var quantiles = new[] { 0.5, 0.9, 0.95, 0.99 };
        _responseTimeSummary = metrics.CreateSummary(
            "http_request_duration_seconds",
            "Duración de peticiones HTTP",
            quantiles
        );
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var products = await _productService.GetAllAsync();
            stopwatch.Stop();
            
            // Observar tiempo de respuesta
            _responseTimeSummary.Observe(
                stopwatch.Elapsed.TotalSeconds,
                new Dictionary<string, string>
                {
                    { "method", "GET" },
                    { "endpoint", "/api/products" },
                    { "status", "200" }
                }
            );
            
            return Ok(products);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _responseTimeSummary.Observe(
                stopwatch.Elapsed.TotalSeconds,
                new Dictionary<string, string>
                {
                    { "method", "GET" },
                    { "endpoint", "/api/products" },
                    { "status", "500" }
                }
            );
            throw;
        }
    }
}
```

### Paso 2.3: Implementar Timer (Medición Automática)

**Timer** mide automáticamente la duración de operaciones usando `IDisposable`.

```csharp
public class DatabaseService
{
    private readonly IMetricsClient _metrics;

    public DatabaseService(IMetricsClient metrics)
    {
        _metrics = metrics;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        // Timer mide automáticamente la duración
        using (_metrics.StartTimer("database_query_duration_seconds", new Dictionary<string, string>
        {
            { "operation", "select" },
            { "table", "products" }
        }))
        {
            // Tu código de base de datos
            return await _dbContext.Products.ToListAsync();
        }
        // El timer se detiene automáticamente al salir del using
    }
}
```

**Ejemplo con try-catch:**

```csharp
public async Task SaveOrderAsync(Order order)
{
    using (var timer = _metrics.StartTimer("database_save_duration_seconds", new Dictionary<string, string>
    {
        { "operation", "insert" },
        { "table", "orders" }
    }))
    {
        try
        {
            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // El timer ya registró el tiempo, pero puedes agregar tags de error
            throw;
        }
    }
}
```

### Paso 2.4: Configurar Percentiles y Buckets en appsettings.json

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "Prometheus": {
      "Enabled": true
    },
    "Summary": {
      "DefaultQuantiles": [0.5, 0.9, 0.95, 0.99]
    },
    "Histogram": {
      "DefaultBuckets": [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
    }
  }
}
```

### Paso 2.5: Middleware para Métricas HTTP Automáticas

Crea un middleware que capture automáticamente métricas HTTP:

```csharp
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsClient _metrics;

    public MetricsMiddleware(RequestDelegate next, IMetricsClient metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var tags = new Dictionary<string, string>
            {
                { "method", context.Request.Method },
                { "route", context.Request.Path },
                { "status", context.Response.StatusCode.ToString() }
            };
            
            // Registrar tiempo de respuesta
            _metrics.ObserveHistogram(
                "http_request_duration_seconds",
                stopwatch.Elapsed.TotalSeconds,
                tags
            );
            
            // Contar peticiones
            _metrics.Increment("http_requests_total", 1.0, tags);
        }
    }
}
```

Registrar en `Program.cs`:

```csharp
app.UseMiddleware<MetricsMiddleware>();
```

### ✅ Checklist Nivel 2

- [ ] Histogram implementado para distribuciones
- [ ] Summary implementado con percentiles
- [ ] Timer implementado para medición automática
- [ ] Tags/Labels utilizados correctamente
- [ ] Configuración de buckets y quantiles en appsettings.json
- [ ] Middleware HTTP automático implementado (opcional)
- [ ] Métricas verificadas en `/metrics`

---

### 🤖 Prompt para Cursor - Nivel 2

```
Necesito implementar métricas intermedias en mi proyecto: Histogram, Summary y Timer.

Pasos a realizar:
1. Implementar un Histogram para medir distribuciones (ej: tiempos de respuesta HTTP, tamaños de archivos, valores de órdenes)
2. Implementar un Summary con percentiles (P50, P90, P99) para medir latencias de operaciones críticas
3. Implementar Timer usando StartTimer() para medir automáticamente la duración de operaciones de base de datos o llamadas a APIs externas
4. Agregar tags/labels apropiados a todas las métricas (método HTTP, endpoint, status code, etc.)
5. Configurar buckets personalizados para Histogram y quantiles para Summary en appsettings.json
6. Crear un middleware HTTP que capture automáticamente métricas de todas las peticiones (tiempo de respuesta, contador de peticiones)

Usa ejemplos prácticos relacionados con operaciones de base de datos, llamadas HTTP y procesamiento de órdenes.
```

---

## Nivel 3 - Múltiples Sinks (Configuración Genérica)

### 🎯 Concepto Fundamental: El Código NO Cambia

**IMPORTANTE:** El componente está diseñado para que **NO importe qué repositorio/sink uses** (Prometheus, InfluxDB, Kafka, StatsD, OpenTelemetry). 

**El código de tu servicio es IDÉNTICO para todos los sinks.** Solo cambia la configuración en `appsettings.json`.

### Arquitectura: Registry Central

```
Tu Servicio → IMetricsClient → MetricRegistry → MetricFlushScheduler → Sinks Habilitados
                (Código único)    (Punto central)    (Exporta automáticamente)
```

**Flujo:**
1. Tu código escribe métricas usando `IMetricsClient` (código idéntico siempre)
2. Las métricas se almacenan en `MetricRegistry` (punto central)
3. `MetricFlushScheduler` exporta automáticamente a **todos los sinks habilitados** en paralelo
4. Los sinks leen del Registry independientemente

### Objetivo
Configurar múltiples exportadores (sinks) mediante **solo configuración**. **NO requiere cambios de código** - solo ajustar `appsettings.json`.

### Paso 3.1: Código del Servicio (IDÉNTICO para Todos los Sinks)

**Este código funciona con Prometheus, InfluxDB, Kafka, StatsD, OpenTelemetry - TODOS:**

```csharp
// Tu servicio - CÓDIGO IDÉNTICO para todos los sinks
public class OrderService
{
    private readonly IMetricsClient _metrics;
    
    public OrderService(IMetricsClient metrics)
    {
        _metrics = metrics; // Mismo código siempre
    }
    
    public void CreateOrder(Order order)
    {
        // Este código es IDÉNTICO para Prometheus, InfluxDB, Kafka, etc.
        _metrics.Increment("orders_created_total", 1.0, new Dictionary<string, string>
        {
            { "status", "success" },
            { "region", order.Region }
        });
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Timer - mismo código para todos los sinks
        using (_metrics.StartTimer("order_processing_duration_seconds", new Dictionary<string, string>
        {
            { "order_type", order.Type }
        }))
        {
            await ValidateOrderAsync(order);
            await ChargePaymentAsync(order);
            await FulfillOrderAsync(order);
        }
    }
}
```

**Registro en Program.cs (UNA SOLA VEZ):**

```csharp
// Program.cs - Registra TODOS los sinks automáticamente
builder.Services.AddJonjubNetMetrics(builder.Configuration);
```

**¡Eso es todo!** No necesitas código adicional para cambiar de sink.

---

## Tabla de Referencia Rápida - Todos los Sinks

| Sink | Estado | Sección Config | Parámetros Clave | Protocolo | Código Requerido |
|------|--------|----------------|------------------|-----------|------------------|
| **Prometheus** | ✅ Completo | `Prometheus` | `Enabled`, `Endpoint` | HTTP (texto) | ❌ Solo config |
| **OpenTelemetry** | ✅ Completo | `OpenTelemetry` | `Enabled`, `Endpoint`, `Protocol` | HTTP JSON/Protobuf | ❌ Solo config |
| **InfluxDB** | ✅ Completo | `InfluxDB` | `Enabled`, `Url`, `Token`, `Bucket` | HTTP (Line Protocol) | ❌ Solo config |
| **StatsD** | ✅ Completo | `StatsD` | `Enabled`, `Host`, `Port` | UDP | ❌ Solo config |
| **Kafka** | ⚠️ Básico* | `Kafka` | `Enabled`, `Broker`, `Topic` | Kafka | ⚠️ Requiere librería |

*Kafka requiere integración con Confluent.Kafka para producción (actualmente usa logging fallback)

---

## Integración 1: Prometheus

### Estado de Implementación
✅ **Completo y funcional** - Formatter y exporter implementados. Performance optimizado (~5-15ns overhead).

### Configuración Básica

```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    }
  }
}
```

### Configuración Completa con Todos los Parámetros

```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics",
      "Port": null
    }
  }
}
```

**Parámetros:**
- `Enabled` (bool): Habilitar/deshabilitar Prometheus (default: `true`)
- `Endpoint` (string): Ruta del endpoint (default: `"/metrics"`)
- `Port` (int?, opcional): Puerto separado si se especifica (default: `null`)

### Verificación

1. Iniciar la aplicación
2. Navegar a: `http://localhost:5000/metrics`
3. Deberías ver métricas en formato Prometheus:

```
# HELP orders_created_total Total de órdenes creadas
# TYPE orders_created_total counter
orders_created_total{status="success",region="us-east"} 5.0
```

### Ejemplo de Uso en Producción

```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    }
  }
}
```

**Nota:** Prometheus es el único sink que expone un endpoint HTTP. Los demás exportan a backends externos.

---

## Integración 2: OpenTelemetry (OTLP)

### Estado de Implementación
✅ **Completo y funcional** - Conversión completa del Registry a formato OTLP. Soporta encriptación en tránsito y compresión.

### Configuración Básica

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "http://localhost:4318/v1/metrics",
      "Protocol": "HttpJson"
    }
  }
}
```

### Configuración Completa con Todos los Parámetros

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "http://otel-collector:4318/v1/metrics",
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

**Parámetros:**
- `Enabled` (bool): Habilitar/deshabilitar OpenTelemetry (default: `true`)
- `Endpoint` (string): URL del OTel Collector (ej: `"http://otel:4318/v1/metrics"`)
- `Protocol` (enum): `HttpJson`, `HttpProtobuf`, o `Grpc` (default: `HttpJson`)
- `EnableCompression` (bool): Habilitar compresión GZip (default: `true`)
- `TimeoutSeconds` (int): Timeout de conexión en segundos (default: `30`)

**Encriptación (opcional):**
- `Encryption.EnableInTransit`: Encriptar payloads antes de enviar (default: `false`)
- `Encryption.EnableTls`: Usar HTTPS/TLS (default: `true`)
- `Encryption.ValidateCertificates`: Validar certificados SSL (default: `true`)

### Configuración para Desarrollo

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "http://localhost:4318/v1/metrics",
      "Protocol": "HttpJson",
      "EnableCompression": false,
      "TimeoutSeconds": 10
    },
    "Encryption": {
      "EnableInTransit": false,
      "EnableTls": false
    }
  }
}
```

### Configuración para Producción

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector.prod:4318/v1/metrics",
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

### Verificación

1. Configurar OTel Collector para recibir en el endpoint especificado
2. Verificar logs de la aplicación: `"Exporting metrics to OpenTelemetry"`
3. Verificar en OTel Collector que recibe métricas
4. Consultar métricas en el backend configurado (ej: Jaeger, Grafana)

---

## Integración 3: InfluxDB

### Estado de Implementación
✅ **Completo y funcional** - HTTP client con formato Line Protocol. Soporta encriptación y compresión.

### Configuración Básica

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://localhost:8086",
      "Bucket": "metrics"
    }
  }
}
```

### Configuración Completa con Todos los Parámetros

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://influxdb:8086",
      "Token": "tu-token-influxdb",
      "Organization": "default",
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

**Parámetros:**
- `Enabled` (bool): Habilitar/deshabilitar InfluxDB (default: `true`)
- `Url` (string): URL del servidor InfluxDB (ej: `"http://influxdb:8086"`)
- `Token` (string?, opcional): Token de autenticación (InfluxDB 2.x)
- `Organization` (string): Organización de InfluxDB (default: `"default"`)
- `Bucket` (string): Bucket donde se almacenan las métricas (default: `"metrics"`)
- `EnableCompression` (bool): Habilitar compresión GZip (default: `true`)
- `TimeoutSeconds` (int): Timeout de conexión (default: `30`)

**Nota:** Para InfluxDB 1.x, usar `Database`, `Username` y `Password` en lugar de `Token`.

### Configuración para InfluxDB 1.x

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://influxdb:8086",
      "Database": "metrics",
      "Username": "admin",
      "Password": "password"
    }
  }
}
```

### Configuración para InfluxDB 2.x (Cloud/OSS)

```json
{
  "Metrics": {
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://us-east-1-1.aws.cloud2.influxdata.com",
      "Token": "${INFLUXDB_TOKEN}",
      "Organization": "my-org",
      "Bucket": "production-metrics"
    }
  }
}
```

### Verificación

1. Verificar conexión a InfluxDB:
   ```bash
   curl http://influxdb:8086/health
   ```

2. Consultar métricas en InfluxDB:
   ```sql
   -- InfluxDB 1.x
   SELECT * FROM "orders_created_total"
   
   -- InfluxDB 2.x (Flux)
   from(bucket: "metrics")
     |> range(start: -1h)
     |> filter(fn: (r) => r._measurement == "orders_created_total")
   ```

3. Verificar logs de la aplicación: `"Exporting metrics to InfluxDB"`

---

## Integración 4: StatsD

### Estado de Implementación
✅ **Completo y funcional** - UDP client con formato StatsD estándar. Fallback a logging si falla la conexión.

### Configuración Básica

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

### Configuración Completa con Todos los Parámetros

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

**Parámetros:**
- `Enabled` (bool): Habilitar/deshabilitar StatsD (default: `false`)
- `Host` (string): Hostname o IP del servidor StatsD (default: `"localhost"`)
- `Port` (int): Puerto UDP de StatsD (default: `8125`)

**Nota:** StatsD usa UDP, por lo que no hay autenticación ni encriptación a nivel de protocolo.

### Configuración para Desarrollo

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

### Configuración para Producción (Datadog, New Relic, etc.)

```json
{
  "Metrics": {
    "StatsD": {
      "Enabled": true,
      "Host": "statsd.datadog.svc.cluster.local",
      "Port": 8125
    }
  }
}
```

### Verificación

1. Verificar que el servidor StatsD esté escuchando:
   ```bash
   netstat -ulnp | grep 8125
   ```

2. Verificar logs de la aplicación: `"Exporting metrics to StatsD"`

3. Verificar en tu backend de StatsD (Datadog, New Relic, etc.) que las métricas lleguen

4. Si StatsD no está disponible, las métricas se registrarán en logs como fallback

---

## Integración 5: Kafka

### Estado de Implementación
⚠️ **Básico funcional** - Estructura lista para integración. Requiere librería Confluent.Kafka para producción.

### Configuración Básica

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

### Configuración Completa con Todos los Parámetros

```json
{
  "Metrics": {
    "Kafka": {
      "Enabled": true,
      "Broker": "kafka:9092",
      "Topic": "metrics",
      "EnableCompression": true,
      "BatchSize": 100,
      "TimeoutSeconds": 30
    }
  }
}
```

**Parámetros:**
- `Enabled` (bool): Habilitar/deshabilitar Kafka (default: `true`)
- `Broker` (string): Bootstrap servers de Kafka (ej: `"kafka1:9092,kafka2:9092"`)
- `Topic` (string): Tópico donde se publican las métricas (default: `"metrics"`)
- `EnableCompression` (bool): Habilitar compresión (default: `true`)
- `BatchSize` (int): Tamaño del batch de mensajes (default: `100`)
- `TimeoutSeconds` (int): Timeout de conexión (default: `30`)

**⚠️ Nota Importante:** 
- Actualmente usa logging como fallback
- Para producción, requiere integración con `Confluent.Kafka`
- Ver código en `KafkaMetricsSink.cs` para ver el TODO de integración

### Configuración para Desarrollo (Logging Fallback)

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

**Las métricas se registrarán en logs hasta que se integre Confluent.Kafka.**

### Verificación

1. Verificar logs de la aplicación: `"Kafka (logging fallback): Would send X messages to topic metrics"`
2. Para producción, integrar Confluent.Kafka según el TODO en el código

---

## Configuración Múltiple - Todos los Sinks Simultáneamente

### Ejemplo Completo: Habilitar Múltiples Sinks

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "Environment": "Production",
    "Version": "1.0.0",
    "FlushIntervalMs": 1000,
    "BatchSize": 200,
    
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    },
    
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector:4318/v1/metrics",
      "Protocol": "HttpJson",
      "EnableCompression": true,
      "TimeoutSeconds": 30
    },
    
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://influxdb:8086",
      "Token": "${INFLUXDB_TOKEN}",
      "Organization": "production",
      "Bucket": "metrics",
      "EnableCompression": true,
      "TimeoutSeconds": 30
    },
    
    "StatsD": {
      "Enabled": true,
      "Host": "statsd-server",
      "Port": 8125
    },
    
    "Kafka": {
      "Enabled": false,
      "Broker": "kafka:9092",
      "Topic": "metrics"
    }
  }
}
```

**Todas las métricas se exportarán simultáneamente a todos los sinks habilitados.**

### Cómo Habilitar/Deshabilitar Sinks

**Para habilitar un sink:** Cambiar `"Enabled": true` en su sección.

**Para deshabilitar un sink:** Cambiar `"Enabled": false` en su sección.

**Ejemplo: Habilitar solo Prometheus e InfluxDB:**

```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    },
    "OpenTelemetry": {
      "Enabled": false
    },
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://influxdb:8086",
      "Bucket": "metrics"
    },
    "StatsD": {
      "Enabled": false
    },
    "Kafka": {
      "Enabled": false
    }
  }
}
```

**El código de tu servicio NO cambia - solo la configuración.**

---

## Verificación Genérica de Sinks

### Pasos Comunes para Todos los Sinks

1. **Habilitar en appsettings.json:**
   ```json
   {
     "Metrics": {
       "[NombreSink]": {
         "Enabled": true,
         // ... parámetros específicos
       }
     }
   }
   ```

2. **Reiniciar la aplicación**

3. **Verificar logs:**
   - Buscar: `"Exporting metrics to [NombreSink]"`
   - No deberían aparecer errores de conexión

4. **Verificar en el backend:**
   - Prometheus: `http://localhost:5000/metrics`
   - OpenTelemetry: Verificar en OTel Collector
   - InfluxDB: Consultar con Flux/SQL
   - StatsD: Verificar en backend (Datadog, New Relic)
   - Kafka: Verificar en consumer (cuando esté integrado)

---

## ✅ Checklist Nivel 3 (Genérico)

- [ ] Entender que todos los sinks se configuran igual (solo `appsettings.json`)
- [ ] Entender que el código del servicio es IDÉNTICO para todos los sinks
- [ ] Prometheus configurado y endpoint `/metrics` accesible
- [ ] OpenTelemetry configurado y conectado a OTel Collector
- [ ] InfluxDB configurado y métricas almacenándose
- [ ] StatsD configurado y métricas llegando al servidor
- [ ] Kafka configurado (opcional, requiere integración)
- [ ] Múltiples sinks activos simultáneamente
- [ ] Verificación en cada backend de métricas
- [ ] Probar cambiar de sink solo modificando configuración (sin cambiar código)

---

## 🤖 Prompts para Cursor - Por Integración

### Prompt 1: Integrar Prometheus

```
Necesito integrar Prometheus en mi proyecto JonjubNet.CleanArch usando JonjubNet.Metrics.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Agregar referencia al proyecto JonjubNet.Metrics en mi proyecto principal
2. Configurar AddJonjubNetMetrics(builder.Configuration) en Program.cs (UNA SOLA VEZ)
3. Configurar Prometheus en appsettings.json con "Enabled": true y "Endpoint": "/metrics"
4. Implementar métricas en mi servicio usando IMetricsClient (código idéntico para todos los sinks):
   - Inyectar IMetricsClient en el constructor
   - Usar _metrics.Increment() para contadores
   - Usar _metrics.SetGauge() para gauges
   - Usar _metrics.StartTimer() para timers
5. Verificar que el endpoint /metrics exponga las métricas en formato Prometheus
6. Probar accediendo a http://localhost:5000/metrics y verificar que se muestren las métricas

NO se necesita código adicional específico para Prometheus - solo configuración en appsettings.json.
El mismo código funcionará si cambio a InfluxDB, Kafka, etc. solo modificando la configuración.
```

### Prompt 2: Integrar OpenTelemetry

```
Necesito integrar OpenTelemetry (OTLP) en mi proyecto usando JonjubNet.Metrics.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Asegurar que AddJonjubNetMetrics(builder.Configuration) esté configurado en Program.cs
2. Configurar OpenTelemetry en appsettings.json con:
   - "Enabled": true
   - "Endpoint": "http://otel-collector:4318/v1/metrics"
   - "Protocol": "HttpJson"
   - "EnableCompression": true (opcional)
   - "TimeoutSeconds": 30
3. Si es producción, configurar encriptación en "Encryption.EnableInTransit": true y "EnableTls": true
4. Verificar que AddJonjubNetMetrics registre automáticamente el sink (NO se necesita código adicional)
5. Verificar logs para confirmar exportación: "Exporting metrics to OpenTelemetry"
6. Verificar en OTel Collector que recibe las métricas

El código de mi servicio NO cambia - uso el mismo IMetricsClient que ya tengo implementado.
Solo cambio la configuración en appsettings.json para habilitar OpenTelemetry.
```

### Prompt 3: Integrar InfluxDB

```
Necesito integrar InfluxDB en mi proyecto usando JonjubNet.Metrics.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Asegurar que AddJonjubNetMetrics(builder.Configuration) esté configurado en Program.cs
2. Configurar InfluxDB en appsettings.json con:
   - "Enabled": true
   - "Url": "http://influxdb:8086"
   - Para InfluxDB 2.x: "Token", "Organization", "Bucket"
   - Para InfluxDB 1.x: "Database", "Username", "Password"
   - "EnableCompression": true (opcional)
   - "TimeoutSeconds": 30
3. Si es producción, configurar encriptación y TLS
4. Verificar conexión a InfluxDB
5. Verificar logs: "Exporting metrics to InfluxDB"
6. Consultar métricas en InfluxDB para confirmar que se almacenan

El código de mi servicio NO cambia - uso el mismo IMetricsClient que ya tengo implementado.
Solo cambio la configuración en appsettings.json para habilitar InfluxDB.
```

### Prompt 4: Integrar StatsD

```
Necesito integrar StatsD en mi proyecto usando JonjubNet.Metrics.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Asegurar que AddJonjubNetMetrics(builder.Configuration) esté configurado en Program.cs
2. Configurar StatsD en appsettings.json con:
   - "Enabled": true
   - "Host": "statsd-server" (o IP del servidor)
   - "Port": 8125 (puerto UDP estándar)
3. Verificar que el servidor StatsD esté escuchando en el puerto 8125
4. Verificar logs: "Exporting metrics to StatsD"
5. Verificar en tu backend de StatsD (Datadog, New Relic, etc.) que las métricas lleguen
6. Si StatsD no está disponible, las métricas se registrarán en logs como fallback

El código de mi servicio NO cambia - uso el mismo IMetricsClient que ya tengo implementado.
Solo cambio la configuración en appsettings.json para habilitar StatsD.
```

### Prompt 5: Integrar Kafka

```
Necesito integrar Kafka en mi proyecto usando JonjubNet.Metrics.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Asegurar que AddJonjubNetMetrics(builder.Configuration) esté configurado en Program.cs
2. Configurar Kafka en appsettings.json con:
   - "Enabled": true
   - "Broker": "kafka:9092" (bootstrap servers)
   - "Topic": "metrics"
   - "EnableCompression": true (opcional)
   - "BatchSize": 100
3. NOTA: Actualmente usa logging como fallback - requiere integración con Confluent.Kafka para producción
4. Verificar logs: "Kafka (logging fallback): Would send X messages"
5. Para producción, integrar Confluent.Kafka según el TODO en KafkaMetricsSink.cs

El código de mi servicio NO cambia - uso el mismo IMetricsClient que ya tengo implementado.
Solo cambio la configuración en appsettings.json para habilitar Kafka.
La configuración básica está lista, pero requiere código adicional para producción (integración con Confluent.Kafka).
```

### Prompt 6: Configurar Múltiples Sinks Simultáneamente

```
Necesito configurar múltiples exportadores (sinks) de métricas simultáneamente en mi proyecto.

IMPORTANTE: El código del servicio es IDÉNTICO para todos los sinks. Solo cambia la configuración.

Pasos a realizar:
1. Asegurar que AddJonjubNetMetrics(builder.Configuration) esté configurado en Program.cs (UNA SOLA VEZ)
2. Configurar todos los sinks deseados en appsettings.json bajo la sección "Metrics"
3. Habilitar cada sink cambiando "Enabled": true
4. Configurar parámetros específicos de cada sink (URLs, credenciales, etc.)
5. Verificar que AddJonjubNetMetrics registre automáticamente todos los sinks configurados
6. Verificar que las métricas se exporten simultáneamente a todos los sinks habilitados
7. Probar habilitar/deshabilitar sinks cambiando solo "Enabled" sin modificar código

ENFATIZAR:
- El código de mi servicio es IDÉNTICO para todos los sinks
- Solo uso IMetricsClient con los mismos métodos (Increment, SetGauge, StartTimer, etc.)
- El cambio de sink es 100% basado en configuración - NO se necesita código adicional
- Todos los sinks funcionan en paralelo y exportan las mismas métricas
- Puedo cambiar de Prometheus a InfluxDB solo modificando appsettings.json, sin tocar el código del servicio
```

### Prompt 7: Cambiar de un Sink a Otro (Sin Cambiar Código)

```
Necesito cambiar de Prometheus a InfluxDB (o cualquier otro sink) en mi proyecto.

IMPORTANTE: El código del servicio NO cambia. Solo modifico la configuración.

Pasos a realizar:
1. Verificar que AddJonjubNetMetrics(builder.Configuration) esté en Program.cs
2. En appsettings.json, cambiar la configuración:
   - Deshabilitar el sink actual: "Prometheus": { "Enabled": false }
   - Habilitar el nuevo sink: "InfluxDB": { "Enabled": true, "Url": "...", "Bucket": "..." }
3. Reiniciar la aplicación
4. Verificar que las métricas se exporten al nuevo sink
5. Verificar logs: "Exporting metrics to InfluxDB"

DEMOSTRAR que:
- El código de mi servicio (OrderService, etc.) NO cambia
- Solo uso IMetricsClient con los mismos métodos
- El cambio es 100% en appsettings.json
- Las mismas métricas se exportan al nuevo sink automáticamente
```

---

## Nivel 4 - Funcionalidades Avanzadas

### Objetivo
Implementar SlidingWindowSummary, MetricAggregator y Health Checks.

### Paso 4.1: Implementar SlidingWindowSummary

**SlidingWindowSummary** calcula percentiles sobre una ventana de tiempo deslizante.

```csharp
public class OrderProcessingService
{
    private readonly SlidingWindowSummary _orderProcessingTime;

    public OrderProcessingService(IMetricsClient metrics)
    {
        // Ventana de 5 minutos, percentiles P50, P90, P99
        _orderProcessingTime = metrics.CreateSlidingWindowSummary(
            "order_processing_duration_seconds",
            "Tiempo de procesamiento de órdenes (ventana 5min)",
            TimeSpan.FromMinutes(5),
            new[] { 0.5, 0.9, 0.99 }
        );
    }

    public async Task ProcessOrderAsync(Order order)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await ValidateOrderAsync(order);
            await ChargePaymentAsync(order);
            await FulfillOrderAsync(order);
        }
        finally
        {
            stopwatch.Stop();
            _orderProcessingTime.Observe(
                new Dictionary<string, string>
                {
                    { "order_type", order.Type },
                    { "region", order.Region }
                },
                stopwatch.Elapsed.TotalSeconds
            );
        }
    }
}
```

**Uso directo sin crear instancia:**

```csharp
// Observar directamente
_metrics.ObserveSlidingWindowSummary(
    "api_response_time_seconds",
    TimeSpan.FromMinutes(1), // Ventana de 1 minuto
    responseTime,
    new Dictionary<string, string> { { "endpoint", "/api/products" } }
);
```

### Paso 4.2: Implementar MetricAggregator

**MetricAggregator** agrega valores en tiempo real (Sum, Average, Min, Max, Count, Last).

```csharp
public class SalesService
{
    private readonly IMetricsClient _metrics;

    public SalesService(IMetricsClient metrics)
    {
        _metrics = metrics;
    }

    public void RecordSale(Sale sale)
    {
        // Agregar valor de venta
        _metrics.AddToAggregator(
            "sales_amount_total",
            sale.Amount,
            new Dictionary<string, string>
            {
                { "product_category", sale.Category },
                { "region", sale.Region }
            }
        );
    }

    public SalesStats GetSalesStats(string category, string region)
    {
        var tags = new Dictionary<string, string>
        {
            { "product_category", category },
            { "region", region }
        };

        // Obtener estadísticas agregadas
        var stats = _metrics.GetAggregatedStats("sales_amount_total", tags);
        
        if (stats != null)
        {
            return new SalesStats
            {
                Total = stats.Sum ?? 0,
                Average = stats.Average ?? 0,
                Min = stats.Min ?? 0,
                Max = stats.Max ?? 0,
                Count = stats.Count,
                Last = stats.Last ?? 0
            };
        }

        return new SalesStats();
    }

    // Obtener valor específico
    public double? GetTotalSales(string category)
    {
        return _metrics.GetAggregatedValue(
            "sales_amount_total",
            AggregationType.Sum,
            new Dictionary<string, string> { { "product_category", category } }
        );
    }
}
```

**Ejemplo: Agregar métricas de CPU y Memoria**

```csharp
public class SystemMetricsCollector
{
    private readonly IMetricsClient _metrics;
    private readonly Timer _collectionTimer;

    public SystemMetricsCollector(IMetricsClient metrics)
    {
        _metrics = metrics;
        _collectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    private void CollectMetrics(object? state)
    {
        // Agregar uso de CPU
        var cpuUsage = GetCpuUsage();
        _metrics.AddToAggregator("system_cpu_percent", cpuUsage);

        // Agregar uso de memoria
        var memoryUsage = GetMemoryUsage();
        _metrics.AddToAggregator("system_memory_mb", memoryUsage);
    }

    public SystemStats GetCurrentStats()
    {
        return new SystemStats
        {
            CpuAverage = _metrics.GetAggregatedValue("system_cpu_percent", AggregationType.Average) ?? 0,
            CpuMax = _metrics.GetAggregatedValue("system_cpu_percent", AggregationType.Max) ?? 0,
            MemoryAverage = _metrics.GetAggregatedValue("system_memory_mb", AggregationType.Average) ?? 0,
            MemoryMax = _metrics.GetAggregatedValue("system_memory_mb", AggregationType.Max) ?? 0
        };
    }
}
```

### Paso 4.3: Implementar Health Checks

**Health Checks** monitorean el estado del sistema de métricas.

```csharp
// En Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<MetricsHealthCheckService>("metrics");

var app = builder.Build();

// Endpoint de health check
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/metrics", new HealthCheckOptions
{
    Predicate = check => check.Name == "metrics"
});
```

**Verificar estado programáticamente:**

```csharp
public class MetricsStatusService
{
    private readonly IMetricsHealthCheck _healthCheck;

    public MetricsStatusService(IMetricsHealthCheck healthCheck)
    {
        _healthCheck = healthCheck;
    }

    public async Task<HealthStatus> GetMetricsHealthAsync()
    {
        return await _healthCheck.CheckHealthAsync();
    }
}
```

**Endpoint personalizado de health:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetricsHealth()
    {
        var result = await _healthCheckService.CheckHealthAsync(
            check => check.Name == "metrics"
        );

        if (result.Status == HealthStatus.Healthy)
        {
            return Ok(new { status = "healthy", metrics = result.Entries });
        }

        return StatusCode(503, new { status = "unhealthy", metrics = result.Entries });
    }
}
```

### Paso 4.4: Configuración de Health Checks en appsettings.json

```json
{
  "Metrics": {
    "HealthCheck": {
      "Enabled": true,
      "CheckIntervalMs": 30000
    }
  }
}
```

### ✅ Checklist Nivel 4

- [ ] SlidingWindowSummary implementado con ventanas de tiempo
- [ ] MetricAggregator implementado para agregaciones en tiempo real
- [ ] Health Checks configurados y funcionando
- [ ] Endpoints de health check expuestos
- [ ] Verificación de estado programática implementada
- [ ] Métricas avanzadas probadas y funcionando

---

### 🤖 Prompt para Cursor - Nivel 4

```
Necesito implementar funcionalidades avanzadas de métricas: SlidingWindowSummary, MetricAggregator y Health Checks.

Pasos a realizar:
1. Implementar SlidingWindowSummary para calcular percentiles sobre ventanas de tiempo deslizantes (ej: tiempos de procesamiento de órdenes en los últimos 5 minutos)
2. Implementar MetricAggregator para agregar valores en tiempo real (Sum, Average, Min, Max, Count, Last) - ej: agregar ventas por categoría y región
3. Crear un servicio que use MetricAggregator para recolectar métricas de sistema (CPU, memoria) cada 5 segundos
4. Configurar Health Checks para monitorear el estado del sistema de métricas
5. Crear endpoints /health y /health/metrics para exponer el estado de salud
6. Implementar verificación programática del estado de métricas en un servicio

Usa ejemplos prácticos: procesamiento de órdenes con ventanas deslizantes, agregación de ventas, monitoreo de sistema.
```

---

## Nivel 5 - Resiliencia Enterprise

### Objetivo
Implementar Circuit Breaker, Dead Letter Queue y Retry Policy para alta disponibilidad.

### Paso 5.1: Configurar Dead Letter Queue (DLQ)

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "EnableAutoProcessing": true,
      "ProcessingIntervalMs": 60000,
      "MaxRetryAttempts": 3
    }
  }
}
```

**La DLQ se activa automáticamente cuando un sink falla.** Las métricas fallidas se almacenan y se reintentan periódicamente.

### Paso 5.2: Configurar Retry Policy

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "InitialDelayMs": 100,
      "BackoffMultiplier": 2.0,
      "JitterPercent": 0.1
    }
  }
}
```

**Comportamiento:**
- Reintento 1: 100ms + jitter
- Reintento 2: 200ms + jitter
- Reintento 3: 400ms + jitter

### Paso 5.3: Configurar Circuit Breaker

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenDurationSeconds": 30
      },
      "Sinks": {
        "InfluxDB": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenDurationSeconds": 60
        },
        "OpenTelemetry": {
          "Enabled": true,
          "FailureThreshold": 5,
          "OpenDurationSeconds": 30
        }
      }
    }
  }
}
```

**El Circuit Breaker:**
- Se abre después de N fallos consecutivos
- Permanece abierto por X segundos
- Luego intenta medio abrir (half-open)
- Si tiene éxito, se cierra; si falla, se vuelve a abrir

### Paso 5.4: Monitorear Estado de Resiliencia

```csharp
public class MetricsResilienceMonitor
{
    private readonly ISinkCircuitBreakerManager _circuitBreakerManager;
    private readonly DeadLetterQueue _dlq;

    public MetricsResilienceMonitor(
        ISinkCircuitBreakerManager circuitBreakerManager,
        DeadLetterQueue dlq)
    {
        _circuitBreakerManager = circuitBreakerManager;
        _dlq = dlq;
    }

    public ResilienceStatus GetStatus()
    {
        var circuitBreakers = _circuitBreakerManager.GetAllCircuitBreakers();
        var dlqSize = _dlq.Count;

        return new ResilienceStatus
        {
            CircuitBreakers = circuitBreakers.Select(cb => new CircuitBreakerStatus
            {
                SinkName = cb.Key,
                State = cb.Value.State.ToString(),
                FailureCount = cb.Value.FailureCount
            }).ToList(),
            DeadLetterQueueSize = dlqSize,
            DeadLetterQueueMaxSize = _dlq.MaxSize
        };
    }
}
```

### Paso 5.5: Endpoint de Monitoreo de Resiliencia

```csharp
[ApiController]
[Route("api/[controller]")]
public class MetricsResilienceController : ControllerBase
{
    private readonly MetricsResilienceMonitor _monitor;

    public MetricsResilienceController(MetricsResilienceMonitor monitor)
    {
        _monitor = monitor;
    }

    [HttpGet("status")]
    public IActionResult GetResilienceStatus()
    {
        var status = _monitor.GetStatus();
        return Ok(status);
    }
}
```

### Paso 5.6: Configuración Completa de Resiliencia

**appsettings.json completo:**

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "FlushIntervalMs": 1000,
    "BatchSize": 200,
    
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "EnableAutoProcessing": true,
      "ProcessingIntervalMs": 60000,
      "MaxRetryAttempts": 3
    },
    
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "InitialDelayMs": 100,
      "BackoffMultiplier": 2.0,
      "JitterPercent": 0.1
    },
    
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenDurationSeconds": 30
      },
      "Sinks": {
        "InfluxDB": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenDurationSeconds": 60
        }
      }
    },
    
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://influxdb:8086",
      "Database": "metrics"
    }
  }
}
```

### ✅ Checklist Nivel 5

- [ ] Dead Letter Queue configurada y habilitada
- [ ] Retry Policy configurada con exponential backoff
- [ ] Circuit Breaker configurado globalmente y por sink
- [ ] Monitoreo de estado de resiliencia implementado
- [ ] Endpoint de monitoreo expuesto
- [ ] Pruebas de fallo y recuperación realizadas
- [ ] DLQ procesando métricas fallidas automáticamente

---

### 🤖 Prompt para Cursor - Nivel 5

```
Necesito implementar resiliencia enterprise para el sistema de métricas: Circuit Breaker, Dead Letter Queue y Retry Policy.

Pasos a realizar:
1. Configurar Dead Letter Queue (DLQ) en appsettings.json con tamaño máximo, procesamiento automático e intervalo de reintentos
2. Configurar Retry Policy con exponential backoff, jitter y número máximo de reintentos
3. Configurar Circuit Breaker global y por sink individual (ej: InfluxDB con umbral de 3 fallos, OpenTelemetry con umbral de 5)
4. Crear un servicio MetricsResilienceMonitor que monitoree el estado de circuit breakers y el tamaño de la DLQ
5. Crear un endpoint /api/metrics-resilience/status que exponga el estado de resiliencia
6. Probar escenarios de fallo: desconectar InfluxDB, verificar que el circuit breaker se abra, verificar que las métricas vayan a la DLQ
7. Verificar que el procesamiento automático de DLQ reintente las métricas fallidas cuando el sink se recupere

Incluye configuración completa en appsettings.json y código para monitoreo y visualización del estado.
```

---

## Nivel 6 - Seguridad

### Objetivo
Implementar encriptación en tránsito, encriptación en reposo y validación de tags.

### Paso 6.1: Configurar Encriptación en Tránsito (TLS/SSL)

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "Encryption": {
      "EnableInTransit": true,
      "EnableTls": true,
      "ValidateCertificates": true
    },
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector:4318/v1/metrics"
    },
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://influxdb:8086"
    }
  }
}
```

**La encriptación TLS/SSL se aplica automáticamente a todos los sinks HTTP cuando `EnableTls: true`.**

### Paso 6.2: Configurar Encriptación en Reposo (AES)

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "Encryption": {
      "EnableAtRest": true,
      "EncryptionKeyBase64": "tu-clave-base64-aqui",
      "EncryptionIVBase64": "tu-iv-base64-aqui"
    },
    "DeadLetterQueue": {
      "Enabled": true
    }
  }
}
```

**Generar claves (ejemplo en C#):**

```csharp
using System.Security.Cryptography;

// Generar clave y IV
using (var aes = Aes.Create())
{
    aes.GenerateKey();
    aes.GenerateIV();
    
    var keyBase64 = Convert.ToBase64String(aes.Key);
    var ivBase64 = Convert.ToBase64String(aes.IV);
    
    Console.WriteLine($"EncryptionKeyBase64: {keyBase64}");
    Console.WriteLine($"EncryptionIVBase64: {ivBase64}");
}
```

**Nota:** En producción, almacena las claves en Azure Key Vault, AWS Secrets Manager o similar.

### Paso 6.3: Configurar Encriptación Completa

**appsettings.json (Producción):**

```json
{
  "Metrics": {
    "Enabled": true,
    "Encryption": {
      "EnableInTransit": true,
      "EnableAtRest": true,
      "EnableTls": true,
      "ValidateCertificates": true,
      "EncryptionKeyBase64": "${METRICS_ENCRYPTION_KEY}",
      "EncryptionIVBase64": "${METRICS_ENCRYPTION_IV}"
    },
    "DeadLetterQueue": {
      "Enabled": true
    },
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector:4318/v1/metrics"
    },
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://influxdb:8086"
    }
  }
}
```

### Paso 6.4: Implementar Validación de Tags

**SecureTagValidator** se aplica automáticamente, pero puedes usarlo manualmente:

```csharp
public class OrderService
{
    private readonly SecureTagValidator _tagValidator;
    private readonly IMetricsClient _metrics;

    public OrderService(SecureTagValidator tagValidator, IMetricsClient metrics)
    {
        _tagValidator = tagValidator;
        _metrics = metrics;
    }

    public void RecordOrder(Order order)
    {
        var tags = new Dictionary<string, string>
        {
            { "order_id", order.Id.ToString() }, // ⚠️ Puede contener PII
            { "customer_email", order.CustomerEmail } // ⚠️ PII - será sanitizado
        };

        // Validar y sanitizar tags
        var safeTags = _tagValidator.ValidateAndSanitize(tags);

        // Usar tags seguros
        _metrics.Increment("orders_created_total", 1.0, safeTags);
    }
}
```

**Configuración de validación (si está disponible):**

```json
{
  "Metrics": {
    "Security": {
      "MaxTagKeyLength": 100,
      "MaxTagValueLength": 500,
      "AllowPiiInTags": false
    }
  }
}
```

### Paso 6.5: Configuración de Seguridad en Producción

**Usando Variables de Entorno:**

```bash
# .env o configuración de Kubernetes
METRICS_ENCRYPTION_KEY=base64-encoded-key
METRICS_ENCRYPTION_IV=base64-encoded-iv
```

**Program.cs con configuración desde Key Vault (ejemplo):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Cargar claves desde Azure Key Vault o AWS Secrets Manager
var encryptionKey = builder.Configuration["Metrics:Encryption:EncryptionKeyBase64"] 
    ?? Environment.GetEnvironmentVariable("METRICS_ENCRYPTION_KEY");
var encryptionIV = builder.Configuration["Metrics:Encryption:EncryptionIVBase64"] 
    ?? Environment.GetEnvironmentVariable("METRICS_ENCRYPTION_IV");

builder.Services.AddJonjubNetMetrics(builder.Configuration, options =>
{
    if (!string.IsNullOrEmpty(encryptionKey) && !string.IsNullOrEmpty(encryptionIV))
    {
        options.Encryption.EnableAtRest = true;
        options.Encryption.EncryptionKeyBase64 = encryptionKey;
        options.Encryption.EncryptionIVBase64 = encryptionIV;
    }
    
    options.Encryption.EnableInTransit = true;
    options.Encryption.EnableTls = true;
    options.Encryption.ValidateCertificates = true;
});
```

### Paso 6.6: Verificar Encriptación

**Verificar TLS/SSL:**

```csharp
// Las conexiones HTTP usan HTTPS automáticamente cuando EnableTls: true
// Verificar en logs que las conexiones sean HTTPS
```

**Verificar encriptación en reposo:**

```csharp
// Las métricas en DLQ están encriptadas cuando EnableAtRest: true
// Verificar que los datos almacenados estén encriptados
```

### ✅ Checklist Nivel 6

- [ ] Encriptación en tránsito (TLS/SSL) configurada
- [ ] Encriptación en reposo (AES) configurada
- [ ] Claves de encriptación almacenadas de forma segura
- [ ] Validación de tags implementada
- [ ] PII no incluido en tags
- [ ] Certificados SSL validados
- [ ] Configuración de producción con variables de entorno

---

### 🤖 Prompt para Cursor - Nivel 6

```
Necesito implementar seguridad completa para el sistema de métricas: encriptación en tránsito, en reposo y validación de tags.

Pasos a realizar:
1. Configurar encriptación en tránsito (TLS/SSL) para todos los sinks HTTP (OpenTelemetry, InfluxDB) en appsettings.json
2. Configurar encriptación en reposo (AES) para Dead Letter Queue con claves Base64
3. Crear un script o método para generar claves de encriptación AES (key e IV) y convertirlas a Base64
4. Configurar las claves de encriptación desde variables de entorno o Azure Key Vault (ejemplo)
5. Implementar validación de tags usando SecureTagValidator para prevenir PII en tags
6. Crear un servicio que demuestre el uso seguro de tags (sin PII, sanitizados)
7. Configurar validación de certificados SSL para conexiones HTTPS
8. Verificar que las métricas en DLQ estén encriptadas cuando EnableAtRest: true

Incluye ejemplos de configuración para desarrollo y producción, con manejo seguro de claves.
```

---

## Nivel 7 - Configuración Dinámica

### Objetivo
Implementar hot-reload de configuración sin reiniciar la aplicación.

### Paso 7.1: Habilitar Hot-Reload

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "HotReload": {
      "Enabled": true,
      "ReloadOnChange": true
    }
  }
}
```

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Habilitar hot-reload de configuración
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddJonjubNetMetrics(builder.Configuration);
```

### Paso 7.2: Usar IOptionsMonitor para Cambios Dinámicos

```csharp
public class MetricsConfigurationService
{
    private readonly IOptionsMonitor<MetricsOptions> _optionsMonitor;
    private readonly ILogger<MetricsConfigurationService> _logger;

    public MetricsConfigurationService(
        IOptionsMonitor<MetricsOptions> optionsMonitor,
        ILogger<MetricsConfigurationService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        
        // Suscribirse a cambios
        _optionsMonitor.OnChange(options =>
        {
            _logger.LogInformation("Configuración de métricas actualizada");
            OnConfigurationChanged(options);
        });
    }

    private void OnConfigurationChanged(MetricsOptions options)
    {
        _logger.LogInformation($"Métricas habilitadas: {options.Enabled}");
        _logger.LogInformation($"Intervalo de flush: {options.FlushIntervalMs}ms");
        _logger.LogInformation($"Tamaño de batch: {options.BatchSize}");
    }

    public MetricsOptions GetCurrentOptions()
    {
        return _optionsMonitor.CurrentValue;
    }
}
```

### Paso 7.3: Cambiar Configuración en Tiempo de Ejecución

**Endpoint para actualizar configuración (ejemplo):**

```csharp
[ApiController]
[Route("api/[controller]")]
public class MetricsConfigController : ControllerBase
{
    private readonly MetricsConfigurationManager _configManager;
    private readonly ILogger<MetricsConfigController> _logger;

    public MetricsConfigController(
        MetricsConfigurationManager configManager,
        ILogger<MetricsConfigController> logger)
    {
        _configManager = configManager;
        _logger = logger;
    }

    [HttpPost("flush-interval")]
    public IActionResult UpdateFlushInterval([FromBody] int intervalMs)
    {
        try
        {
            _configManager.UpdateFlushInterval(intervalMs);
            _logger.LogInformation($"Flush interval actualizado a {intervalMs}ms");
            return Ok(new { message = "Configuración actualizada", flushIntervalMs = intervalMs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar configuración");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("enable-sink")]
    public IActionResult EnableSink([FromBody] EnableSinkRequest request)
    {
        try
        {
            _configManager.EnableSink(request.SinkName, request.Enabled);
            _logger.LogInformation($"Sink {request.SinkName} {(request.Enabled ? "habilitado" : "deshabilitado")}");
            return Ok(new { message = "Sink actualizado", sink = request.SinkName, enabled = request.Enabled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar sink");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("current")]
    public IActionResult GetCurrentConfig()
    {
        var config = _configManager.GetCurrentConfiguration();
        return Ok(config);
    }
}

public class EnableSinkRequest
{
    public string SinkName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
```

### Paso 7.4: Configuración por Sink Individual

**appsettings.json:**

```json
{
  "Metrics": {
    "Enabled": true,
    "FlushIntervalMs": 1000,
    "BatchSize": 200,
    
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    },
    
    "InfluxDB": {
      "Enabled": true,
      "Url": "http://influxdb:8086",
      "Database": "metrics",
      "BatchSize": 100,
      "FlushIntervalMs": 5000
    },
    
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "http://otel:4318/v1/metrics",
      "BatchSize": 50,
      "FlushIntervalMs": 2000
    }
  }
}
```

**Cada sink puede tener su propia configuración de batch size y flush interval.**

### Paso 7.5: Monitorear Cambios de Configuración

```csharp
public class MetricsConfigWatcher : BackgroundService
{
    private readonly IOptionsMonitor<MetricsOptions> _optionsMonitor;
    private readonly ILogger<MetricsConfigWatcher> _logger;

    public MetricsConfigWatcher(
        IOptionsMonitor<MetricsOptions> optionsMonitor,
        ILogger<MetricsConfigWatcher> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _optionsMonitor.OnChange(options =>
        {
            _logger.LogInformation("=== Configuración de Métricas Cambiada ===");
            _logger.LogInformation($"Habilitado: {options.Enabled}");
            _logger.LogInformation($"Flush Interval: {options.FlushIntervalMs}ms");
            _logger.LogInformation($"Batch Size: {options.BatchSize}");
            _logger.LogInformation($"Prometheus Enabled: {options.Prometheus?.Enabled ?? false}");
            _logger.LogInformation($"InfluxDB Enabled: {options.InfluxDB?.Enabled ?? false}");
        });

        return Task.CompletedTask;
    }
}
```

**Registrar en Program.cs:**

```csharp
builder.Services.AddHostedService<MetricsConfigWatcher>();
```

### ✅ Checklist Nivel 7

- [ ] Hot-reload habilitado en appsettings.json
- [ ] IOptionsMonitor configurado para detectar cambios
- [ ] Servicio de monitoreo de cambios implementado
- [ ] Endpoints para actualizar configuración en tiempo de ejecución
- [ ] Configuración por sink individual funcionando
- [ ] Cambios de configuración aplicándose sin reiniciar
- [ ] Logs de cambios de configuración funcionando

---

### 🤖 Prompt para Cursor - Nivel 7

```
Necesito implementar configuración dinámica (hot-reload) para el sistema de métricas sin reiniciar la aplicación.

Pasos a realizar:
1. Habilitar hot-reload en appsettings.json y Program.cs (reloadOnChange: true)
2. Crear un servicio MetricsConfigurationService que use IOptionsMonitor para detectar cambios de configuración
3. Implementar un endpoint /api/metrics-config que permita actualizar configuración en tiempo de ejecución (ej: flush interval, habilitar/deshabilitar sinks)
4. Crear un BackgroundService MetricsConfigWatcher que monitoree y registre cambios de configuración
5. Configurar opciones individuales por sink (batch size, flush interval) en appsettings.json
6. Probar cambiar la configuración en appsettings.json y verificar que se aplique sin reiniciar
7. Probar actualizar configuración mediante el endpoint API y verificar que se refleje

Incluye ejemplos de endpoints REST para actualizar configuración y servicios para monitorear cambios.
```

---

## 🎯 Resumen de Implementación

### Orden Recomendado de Implementación

1. **Nivel 1** → Funcionalidad básica (Counter, Gauge, Prometheus)
2. **Nivel 2** → Métricas intermedias (Histogram, Summary, Timer)
3. **Nivel 3** → Múltiples sinks (OpenTelemetry, InfluxDB, StatsD)
4. **Nivel 4** → Funcionalidades avanzadas (SlidingWindow, Aggregator, Health Checks)
5. **Nivel 5** → Resiliencia (DLQ, Retry, Circuit Breaker)
6. **Nivel 6** → Seguridad (Encriptación, Validación)
7. **Nivel 7** → Configuración dinámica (Hot-reload)

### Configuración Completa de Ejemplo

**appsettings.json (Producción):**

```json
{
  "Metrics": {
    "Enabled": true,
    "ServiceName": "MiServicio",
    "Environment": "Production",
    "Version": "1.0.0",
    "FlushIntervalMs": 1000,
    "BatchSize": 200,
    "QueueCapacity": 10000,
    
    "DeadLetterQueue": {
      "Enabled": true,
      "MaxSize": 10000,
      "EnableAutoProcessing": true,
      "ProcessingIntervalMs": 60000,
      "MaxRetryAttempts": 3
    },
    
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "InitialDelayMs": 100,
      "BackoffMultiplier": 2.0,
      "JitterPercent": 0.1
    },
    
    "CircuitBreaker": {
      "Enabled": true,
      "Default": {
        "FailureThreshold": 5,
        "OpenDurationSeconds": 30
      },
      "Sinks": {
        "InfluxDB": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenDurationSeconds": 60
        }
      }
    },
    
    "Encryption": {
      "EnableInTransit": true,
      "EnableAtRest": true,
      "EnableTls": true,
      "ValidateCertificates": true,
      "EncryptionKeyBase64": "${METRICS_ENCRYPTION_KEY}",
      "EncryptionIVBase64": "${METRICS_ENCRYPTION_IV}"
    },
    
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    },
    
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://otel-collector:4318/v1/metrics",
      "Protocol": "HttpJson",
      "BatchSize": 50,
      "FlushIntervalMs": 2000
    },
    
    "InfluxDB": {
      "Enabled": true,
      "Url": "https://influxdb:8086",
      "Database": "metrics",
      "Username": "${INFLUXDB_USER}",
      "Password": "${INFLUXDB_PASSWORD}",
      "BatchSize": 100,
      "FlushIntervalMs": 5000
    },
    
    "StatsD": {
      "Enabled": false,
      "Host": "statsd-server",
      "Port": 8125,
      "Prefix": "myservice"
    },
    
    "Kafka": {
      "Enabled": false,
      "BootstrapServers": "kafka:9092",
      "Topic": "metrics",
      "CompressionType": "gzip"
    },
    
    "Summary": {
      "DefaultQuantiles": [0.5, 0.9, 0.95, 0.99]
    },
    
    "Histogram": {
      "DefaultBuckets": [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
    }
  }
}
```

---

## 🏗️ Infraestructura Necesaria por Sink

### Resumen: ¿Qué Necesitas Instalar?

| Sink | ¿Requiere Instalación? | Qué Instalar | Dificultad | Visualización |
|------|------------------------|--------------|------------|---------------|
| **Prometheus** | Opcional | Prometheus Server + Grafana | Fácil | Grafana Dashboards |
| **OpenTelemetry** | Sí | OTel Collector | Media | Jaeger, Grafana, etc. |
| **InfluxDB** | Sí | InfluxDB Server | Fácil | InfluxDB UI, Grafana |
| **StatsD** | Sí | StatsD Server o Agent | Fácil | Datadog, New Relic, etc. |
| **Kafka** | Sí | Kafka Cluster | Avanzada | Consumer personalizado |

---

### 1. Prometheus

#### Opción A: Solo Ver Métricas (Sin Instalación)
Tu aplicación expone el endpoint `/metrics`. Puedes acceder directamente:
- URL: `http://localhost:5000/metrics`
- Formato: Texto plano Prometheus
- **No necesitas instalar nada adicional**

#### Opción B: Monitoreo Completo (Recomendado)

**Instalación con Docker:**
```bash
# Prometheus Server
docker run -d -p 9090:9090 \
  -v /path/to/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus

# Grafana (opcional, para visualización)
docker run -d -p 3000:3000 \
  -e GF_SECURITY_ADMIN_PASSWORD=admin \
  grafana/grafana
```

**Configuración `prometheus.yml`:**
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'mi-servicio'
    static_configs:
      - targets: ['host.docker.internal:5000']  # Tu aplicación
```

**Pasos:**
1. Instalar Prometheus Server
2. Configurar scraping de tu aplicación
3. (Opcional) Instalar Grafana y conectarlo a Prometheus
4. Crear dashboards en Grafana

---

### 2. OpenTelemetry (OTLP)

**Requiere:** OpenTelemetry Collector

**Instalación con Docker:**
```bash
docker run -d -p 4318:4318 \
  -v /path/to/otel-collector-config.yaml:/etc/otel-collector-config.yaml \
  otel/opentelemetry-collector:latest \
  --config=/etc/otel-collector-config.yaml
```

**Configuración básica `otel-collector-config.yaml`:**
```yaml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318

exporters:
  # Opción 1: Exportar a Prometheus
  prometheus:
    endpoint: "0.0.0.0:8889"
  
  # Opción 2: Exportar a Jaeger
  jaeger:
    endpoint: jaeger:14250
  
  # Opción 3: Exportar a otros backends
  logging:

service:
  pipelines:
    metrics:
      receivers: [otlp]
      exporters: [prometheus, logging]
```

**Backends compatibles:**
- Prometheus
- Jaeger
- Grafana Cloud
- Datadog
- New Relic
- Azure Monitor
- AWS CloudWatch

---

### 3. InfluxDB

**Requiere:** Servidor InfluxDB

**Instalación con Docker:**

**InfluxDB 2.x (Recomendado):**
```bash
docker run -d -p 8086:8086 \
  -e DOCKER_INFLUXDB_INIT_MODE=setup \
  -e DOCKER_INFLUXDB_INIT_USERNAME=admin \
  -e DOCKER_INFLUXDB_INIT_PASSWORD=password \
  -e DOCKER_INFLUXDB_INIT_ORG=my-org \
  -e DOCKER_INFLUXDB_INIT_BUCKET=metrics \
  influxdb:2.7
```

**InfluxDB 1.x (Legacy):**
```bash
docker run -d -p 8086:8086 \
  -e INFLUXDB_DB=metrics \
  -e INFLUXDB_ADMIN_USER=admin \
  -e INFLUXDB_ADMIN_PASSWORD=password \
  influxdb:1.8
```

**Visualización:**
- **InfluxDB UI (incluida):** `http://localhost:8086`
- **Grafana (recomendado):** Conectar InfluxDB como fuente de datos

**Consultar métricas:**
```sql
-- InfluxDB 1.x
SELECT * FROM "orders_created_total"

-- InfluxDB 2.x (Flux)
from(bucket: "metrics")
  |> range(start: -1h)
  |> filter(fn: (r) => r._measurement == "orders_created_total")
```

---

### 4. StatsD

**Requiere:** Servidor StatsD o Agent compatible

**Opción A: StatsD Standalone**
```bash
docker run -d -p 8125:8125/udp -p 8126:8126 \
  prom/statsd-exporter
```

**Opción B: Integración con Servicios Cloud**

**Datadog:**
```bash
docker run -d --name datadog-agent \
  -e DD_API_KEY=tu-api-key \
  -e DD_SITE=datadoghq.com \
  -p 8125:8125/udp \
  datadog/agent:latest
```

**New Relic:**
```bash
docker run -d \
  -e NEW_RELIC_LICENSE_KEY=tu-license-key \
  -p 8125:8125/udp \
  newrelic/infrastructure:latest
```

**Grafana Cloud Agent:**
```bash
docker run -d -p 8125:8125/udp \
  -e GRAFANA_CLOUD_API_KEY=tu-api-key \
  grafana/agent:latest
```

---

### 5. Kafka

**Requiere:** Cluster Kafka

**Instalación con Docker Compose:**
```yaml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181

  kafka:
    image: confluentinc/cp-kafka:latest
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
```

**Consumidores recomendados:**
- Kafka Connect
- Fluentd
- Logstash
- Aplicaciones personalizadas

**⚠️ Nota:** Actualmente Kafka usa logging como fallback. Para producción, requiere integración con `Confluent.Kafka`.

---

## 🎯 Recomendaciones por Escenario

### Escenario 1: Desarrollo Local (Más Simple)
```json
{
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Endpoint": "/metrics"
    }
  }
}
```
- **No instales nada adicional**
- Accede a `http://localhost:5000/metrics` para ver métricas

### Escenario 2: Producción Simple (Prometheus + Grafana)
1. Instalar Prometheus Server
2. Instalar Grafana
3. Configurar Prometheus para hacer scraping de tu servicio
4. Crear dashboards en Grafana

### Escenario 3: Producción Enterprise (OpenTelemetry)
1. Instalar OpenTelemetry Collector
2. Configurar exportadores (Prometheus, Jaeger, etc.)
3. Visualizar en Grafana/Jaeger

### Escenario 4: Cloud Managed (Más Fácil)
- **AWS:** CloudWatch (StatsD o OTLP)
- **Azure:** Azure Monitor (OTLP)
- **GCP:** Cloud Monitoring (OTLP)
- **Datadog:** Datadog Agent (StatsD)
- **New Relic:** New Relic Agent (StatsD)

---

## 🐳 Stack Completo Recomendado (Docker Compose)

```yaml
version: '3.8'
services:
  # Tu aplicación (ya configurada)
  mi-servicio:
    build: .
    ports:
      - "5000:5000"
    environment:
      - Metrics__Prometheus__Enabled=true
      - Metrics__InfluxDB__Enabled=true
      - Metrics__InfluxDB__Url=http://influxdb:8086

  # Prometheus
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  # Grafana
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin

  # InfluxDB
  influxdb:
    image: influxdb:2.7
    ports:
      - "8086:8086"
    environment:
      - DOCKER_INFLUXDB_INIT_MODE=setup
      - DOCKER_INFLUXDB_INIT_USERNAME=admin
      - DOCKER_INFLUXDB_INIT_PASSWORD=password
      - DOCKER_INFLUXDB_INIT_ORG=my-org
      - DOCKER_INFLUXDB_INIT_BUCKET=metrics
```

---

## 📚 Recursos Adicionales

- **Documentación del Componente:** Ver `evaluacion_produccion.md`
- **Ejemplos de Código:** Ver proyecto `Presentation/JonjubNet.Metrics/Examples`
- **Tests:** Ver proyecto `Tests/JonjubNet.Metrics.Core.Tests`

---

## ✅ Verificación Final

Antes de considerar la implementación completa, verifica:

- [ ] Todos los niveles implementados según tus necesidades
- [ ] Métricas exportándose correctamente a todos los sinks configurados
- [ ] Health checks funcionando
- [ ] Resiliencia probada (circuit breakers, DLQ, retry)
- [ ] Seguridad implementada (encriptación, validación)
- [ ] Configuración dinámica funcionando
- [ ] Logs y monitoreo en lugar
- [ ] Documentación actualizada

---

**¡Feliz implementación! 🚀**

