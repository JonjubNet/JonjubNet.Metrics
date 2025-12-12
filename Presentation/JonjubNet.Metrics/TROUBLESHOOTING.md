# Troubleshooting Guide - JonjubNet.Metrics

Esta guía ayuda a resolver problemas comunes con el componente de métricas.

## 🔍 Problemas Comunes

### 1. Las métricas no se están exportando

**Síntomas:**
- No aparecen métricas en Prometheus/OTLP/Kafka
- El endpoint `/metrics` está vacío

**Soluciones:**

1. **Verificar que las métricas estén habilitadas:**
   ```json
   {
     "Metrics": {
       "Enabled": true,
       "Prometheus": {
         "Enabled": true
       }
     }
   }
   ```

2. **Verificar el estado del scheduler:**
   ```csharp
   var healthCheck = serviceProvider.GetService<IMetricsHealthCheck>();
   var health = healthCheck?.GetOverallHealth();
   Console.WriteLine($"Scheduler running: {health?.SchedulerHealth.IsRunning}");
   ```

3. **Revisar logs:**
   - Buscar errores del `MetricFlushScheduler`
   - Verificar errores de conexión a sinks

4. **Verificar estado del Registry:**
   ```csharp
   var registry = serviceProvider.GetService<MetricRegistry>();
   var counters = registry?.GetAllCounters();
   var gauges = registry?.GetAllGauges();
   
   // Verificar que hay métricas registradas
   Console.WriteLine($"Counters: {counters?.Count ?? 0}, Gauges: {gauges?.Count ?? 0}");
   ```

### 2. Performance degradada o alta latencia

**Síntomas:**
- Alta latencia en la aplicación
- Alto uso de CPU/memoria
- Métricas no se exportan a tiempo

**Soluciones:**

1. **Aumentar frecuencia de flush:**
   ```json
   {
     "Metrics": {
       "FlushIntervalMs": 500  // Reducir de 1000 para exportar más frecuentemente
     }
   }
   ```

2. **Optimizar configuración de batch:**
   ```json
   {
     "Metrics": {
       "BatchSize": 500  // Ajustar según volumen de métricas
     }
   }
   ```

3. **Reducir cardinalidad de tags:**
   - Evitar tags con valores únicos (IDs, timestamps)
   - Usar tags con valores limitados (status, region)
   - Limitar número de combinaciones de tags

4. **Verificar estado del scheduler:**
   ```csharp
   var healthCheck = serviceProvider.GetService<IMetricsHealthCheck>();
   var schedulerHealth = healthCheck?.CheckSchedulerHealth();
   Console.WriteLine($"Scheduler running: {schedulerHealth?.IsRunning}");
   ```

### 3. Errores de conexión a sinks

**Síntomas:**
- Logs muestran errores de conexión
- Health check muestra sinks como unhealthy

**Soluciones:**

1. **Verificar conectividad:**
   ```bash
   # Para Prometheus
   curl http://localhost:9090/metrics
   
   # Para InfluxDB
   curl -X POST http://localhost:8086/api/v2/write
   ```

2. **Verificar configuración TLS:**
   ```json
   {
     "Metrics": {
       "InfluxDB": {
         "EnableTls": true,
         "ValidateCertificates": true
       }
     }
   }
   ```

3. **Verificar autenticación:**
   - Tokens de InfluxDB
   - Credenciales de Kafka
   - Certificados SSL

### 4. Métricas no aparecen en el Registry

**Síntomas:**
- Las métricas se registran pero no aparecen al consultar el Registry
- Health check muestra métricas pero no se exportan

**Soluciones:**

1. **Verificar que las métricas se están registrando:**
   ```csharp
   var client = serviceProvider.GetService<IMetricsClient>();
   client?.Increment("test_counter");
   
   var registry = serviceProvider.GetService<MetricRegistry>();
   var counters = registry?.GetAllCounters();
   var testCounter = counters?.GetValueOrDefault("test_counter");
   Console.WriteLine($"Test counter value: {testCounter?.GetValue()}");
   ```

2. **Verificar que el scheduler está corriendo:**
   ```csharp
   var healthCheck = serviceProvider.GetService<IMetricsHealthCheck>();
   var schedulerHealth = healthCheck?.CheckSchedulerHealth();
   if (!schedulerHealth?.IsRunning == true)
   {
       // El scheduler no está corriendo - verificar BackgroundService
   }
   ```

3. **Revisar logs del scheduler:**
   - Buscar "MetricFlushScheduler started" en logs
   - Verificar errores de exportación

### 5. Errores de validación de tags

**Síntomas:**
- Tags se eliminan automáticamente
- Errores de "invalid tag key"

**Soluciones:**

1. **Verificar formato de tags:**
   ```csharp
   // ✅ Correcto
   var tags = new Dictionary<string, string> 
   { 
       ["env"] = "prod",
       ["service"] = "api"
   };
   
   // ❌ Incorrecto
   var tags = new Dictionary<string, string> 
   { 
       ["invalid-key"] = "value",  // Guiones no permitidos
       ["password"] = "secret"     // Blacklisted
   };
   ```

2. **Revisar SecureTagValidator:**
   - Tags deben usar `snake_case` o `camelCase`
   - No usar claves en blacklist (password, secret, etc.)
   - No incluir PII en valores

### 6. Health checks fallan

**Síntomas:**
- `/health` endpoint muestra unhealthy
- Scheduler no está corriendo

**Soluciones:**

1. **Verificar registro de servicios:**
   ```csharp
   services.AddJonjubNetMetrics(configuration);
   services.AddHealthChecks()
       .AddCheck<MetricsHealthCheckService>("metrics");
   ```

2. **Verificar BackgroundService:**
   - Asegurar que `MetricsBackgroundService` esté registrado
   - Verificar que la aplicación tenga `IHost`

3. **Revisar logs del scheduler:**
   ```csharp
   // Buscar en logs:
   // "MetricFlushScheduler started"
   // "Error in MetricFlushScheduler"
   ```

## 🛠️ Debugging

### Habilitar logging detallado

```json
{
  "Logging": {
    "LogLevel": {
      "JonjubNet.Metrics": "Debug"
    }
  }
}
```

### Inspeccionar estado interno

```csharp
// Verificar Registry (fuente única de verdad - todos los sinks leen de aquí)
var registry = serviceProvider.GetService<MetricRegistry>();
var counters = registry?.GetAllCounters();
var gauges = registry?.GetAllGauges();
var histograms = registry?.GetAllHistograms();
var summaries = registry?.GetAllSummaries();

// Verificar métricas registradas
foreach (var counter in counters ?? new Dictionary<string, Counter>())
{
    Console.WriteLine($"Counter: {counter.Key}, Value: {counter.Value.GetValue()}");
}

// Verificar estado del scheduler
var scheduler = serviceProvider.GetService<MetricFlushScheduler>();
var dlqStats = scheduler?.GetDeadLetterQueueStats();
if (dlqStats != null)
{
    Console.WriteLine($"DLQ size: {dlqStats.CurrentSize}, Failed metrics: {dlqStats.TotalFailed}");
}
```

### Verificar exportación

```csharp
// Para Prometheus
var exporter = serviceProvider.GetService<PrometheusExporter>();
var formatter = serviceProvider.GetService<PrometheusFormatter>();
var registry = serviceProvider.GetService<MetricRegistry>();
var text = formatter?.FormatRegistry(registry);
Console.WriteLine(text);
```

## 📊 Métricas de Diagnóstico

El componente expone métricas internas para diagnóstico a través de health checks:

- **Scheduler Health:**
  - `IsRunning` - Indica si el scheduler está activo
  - `LastFlushTime` - Última vez que se exportaron métricas
  - `TotalBatchesProcessed` - Total de batches procesados
  - `TotalMetricsExported` - Total de métricas exportadas

- **Sink Health (por cada sink):**
  - `IsHealthy` - Estado de salud del sink
  - `IsEnabled` - Si el sink está habilitado
  - `LastExportTime` - Última exportación exitosa
  - `ExportCount` - Total de exportaciones exitosas
  - `ErrorCount` - Total de errores de exportación

- **Registry State:**
  - Número de contadores, gauges, histograms y summaries registrados
  - Valores actuales de métricas (consultables directamente del Registry)

**Nota:** El MetricBus fue eliminado en la arquitectura optimizada. Todos los sinks ahora leen directamente del Registry, eliminando overhead innecesario.

## 🔗 Recursos Adicionales

- [README Principal](README.md)
- [Arquitectura del Componente](../project_structure.md)
- [Evaluación de Producción](../../EVALUACION_PRODUCCION.md)

## 💬 Soporte

Si el problema persiste:
1. Revisar logs detallados
2. Ejecutar health checks
3. Verificar configuración
4. Consultar documentación de arquitectura

