# Guía de Configuración - JonjubNet.Metrics

Esta guía documenta todas las opciones de configuración disponibles en JonjubNet.Metrics.

## Tabla de Contenidos

1. [Configuración Básica](#configuración-básica)
2. [Configuración de Métricas](#configuración-de-métricas)
3. [Configuración de Sinks](#configuración-de-sinks)
4. [Configuración de Resiliencia](#configuración-de-resiliencia)
5. [Configuración de Seguridad](#configuración-de-seguridad)
6. [Configuración Avanzada](#configuración-avanzada)
7. [Hot-Reload](#hot-reload)

---

## Configuración Básica

### Opciones Principales

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
    "Mode": "InProcess"
  }
}
```

#### Descripción de Opciones

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Habilitar/deshabilitar métricas globalmente |
| `ServiceName` | `string` | `""` | Nombre del servicio (usado en tags) |
| `Environment` | `string` | `"Development"` | Entorno de ejecución (Development, Staging, Production) |
| `Version` | `string` | `"1.0.0"` | Versión del servicio |
| `QueueCapacity` | `int` | `10000` | Capacidad máxima de la cola interna |
| `BatchSize` | `int` | `200` | Tamaño del batch para exportación |
| `FlushIntervalMs` | `int` | `1000` | Intervalo de flush en milisegundos |
| `Mode` | `enum` | `InProcess` | Modo de operación (`InProcess` o `Sidecar`) |

---

## Configuración de Métricas

### Contadores (Counters)

```json
{
  "Metrics": {
    "Counter": {
      "Enabled": true,
      "DefaultIncrement": 1,
      "EnableLabels": true,
      "Services": {
        "HttpRequests": {
          "Enabled": true,
          "Increment": 1,
          "Labels": ["method", "endpoint", "status_code"]
        }
      }
    }
  }
}
```

### Gauges

```json
{
  "Metrics": {
    "Gauge": {
      "Enabled": true,
      "EnableLabels": true,
      "Services": {
        "SystemMetrics": {
          "Enabled": true,
          "Labels": ["metric_type", "instance"]
        }
      }
    }
  }
}
```

### Histogramas

```json
{
  "Metrics": {
    "Histogram": {
      "Enabled": true,
      "DefaultBuckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0],
      "EnableLabels": true,
      "Services": {
        "ResponseTime": {
          "Enabled": true,
          "Buckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0, 30.0],
          "Labels": ["endpoint", "method"]
        }
      }
    }
  }
}
```

**Nota:** Los buckets definen los límites superiores de los intervalos para la distribución de valores.

### Summaries (Percentiles Configurables)

```json
{
  "Metrics": {
    "Summary": {
      "Enabled": true,
      "DefaultQuantiles": [0.5, 0.95, 0.99, 0.999],
      "EnableLabels": true,
      "Services": {
        "RequestLatency": {
          "Enabled": true,
          "Quantiles": [0.5, 0.9, 0.95, 0.99, 0.999],
          "Labels": ["endpoint", "method"]
        }
      }
    }
  }
}
```

**Nota:** Los quantiles son valores entre 0.0 y 1.0 (ej: 0.5 = p50, 0.95 = p95, 0.99 = p99).

### Timers

```json
{
  "Metrics": {
    "Timer": {
      "Enabled": true,
      "DefaultBuckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0],
      "EnableLabels": true,
      "Services": {
        "HttpRequests": {
          "Enabled": true,
          "Buckets": [0.1, 0.5, 1.0, 2.5, 5.0, 10.0, 30.0],
          "Labels": ["method", "endpoint", "status_code"]
        }
      }
    }
  }
}
```

### Sliding Windows

```json
{
  "Metrics": {
    "SlidingWindow": {
      "Enabled": true,
      "DefaultWindowSizeSeconds": 300,
      "CleanupIntervalSeconds": 10,
      "Services": {
        "RequestLatencyWindow": {
          "Enabled": true,
          "WindowSizeSeconds": 300,
          "Quantiles": [0.5, 0.95, 0.99, 0.999],
          "Labels": ["endpoint", "method"]
        }
      }
    }
  }
}
```

### Agregación en Tiempo Real

```json
{
  "Metrics": {
    "Aggregation": {
      "Enabled": true,
      "DefaultAggregationType": "Average",
      "UpdateIntervalSeconds": 1,
      "Services": {
        "TotalRequests": {
          "Enabled": true,
          "AggregationType": "Sum",
          "Labels": ["endpoint", "method"]
        },
        "AverageResponseTime": {
          "Enabled": true,
          "AggregationType": "Average",
          "Labels": ["endpoint"]
        }
      }
    }
  }
}
```

**Tipos de agregación disponibles:** `Sum`, `Average`, `Min`, `Max`, `Count`, `Last`

---

## Configuración de Sinks

### Prometheus

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

### OpenTelemetry

```json
{
  "Metrics": {
    "OpenTelemetry": {
      "Enabled": true,
      "Endpoint": "https://localhost:4318",
      "Protocol": "HttpJson",
      "EnableCompression": true,
      "TimeoutSeconds": 30
    }
  }
}
```

**Protocolos disponibles:** `HttpJson`, `HttpProtobuf`, `Grpc`

### Kafka

```json
{
  "Metrics": {
    "Kafka": {
      "Enabled": true,
      "Broker": "localhost:9092",
      "Topic": "metrics",
      "EnableCompression": true
    }
  }
}
```

**Nota:** Requiere integración con `Confluent.Kafka` para producción.

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
      "EnableCompression": true,
      "TimeoutSeconds": 30
    }
  }
}
```

---

## Configuración de Resiliencia

### Circuit Breaker

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
        "Prometheus": {
          "Enabled": true,
          "FailureThreshold": 3,
          "OpenDurationSeconds": 60
        },
        "InfluxDB": {
          "Enabled": true,
          "FailureThreshold": 5,
          "OpenDurationSeconds": 30
        }
      }
    }
  }
}
```

#### Descripción

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Habilitar circuit breakers por sink |
| `Default.FailureThreshold` | `int` | `5` | Número de fallos antes de abrir el circuit breaker |
| `Default.OpenDurationSeconds` | `int` | `30` | Duración que el circuit breaker permanece abierto |
| `Sinks.{SinkName}.Enabled` | `bool` | `true` | Habilitar circuit breaker para un sink específico |
| `Sinks.{SinkName}.FailureThreshold` | `int?` | `null` | Threshold específico (usa Default si es null) |
| `Sinks.{SinkName}.OpenDurationSeconds` | `int?` | `null` | Duración específica (usa Default si es null) |

### Retry Policy

```json
{
  "Metrics": {
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

#### Descripción

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Habilitar retry policy |
| `MaxRetries` | `int` | `3` | Número máximo de reintentos |
| `InitialDelayMs` | `int` | `100` | Delay inicial en milisegundos |
| `BackoffMultiplier` | `double` | `2.0` | Multiplicador de backoff exponencial |
| `JitterPercent` | `double` | `0.1` | Porcentaje de jitter (0.0 a 1.0) |

**Ejemplo de cálculo de delay:**
- Intento 1: `InitialDelayMs` = 100ms
- Intento 2: `100 * 2.0 * (1 ± 0.1)` = 180-220ms
- Intento 3: `200 * 2.0 * (1 ± 0.1)` = 360-440ms

### Dead Letter Queue (DLQ)

```json
{
  "Metrics": {
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

#### Descripción

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Habilitar Dead Letter Queue |
| `MaxSize` | `int` | `10000` | Tamaño máximo de la DLQ |
| `EnableAutoProcessing` | `bool` | `true` | Habilitar procesamiento automático (reintentos periódicos) |
| `ProcessingIntervalMs` | `int` | `60000` | Intervalo de procesamiento en milisegundos (1 minuto) |
| `MaxRetryAttempts` | `int` | `3` | Número máximo de reintentos para métricas en DLQ |

---

## Configuración de Seguridad

### Encriptación

```json
{
  "Metrics": {
    "Encryption": {
      "EnableInTransit": true,
      "EnableAtRest": true,
      "EncryptionKeyBase64": null,
      "EncryptionIVBase64": null,
      "EnableTls": true,
      "ValidateCertificates": true
    }
  }
}
```

#### Descripción

| Opción | Tipo | Default | Descripción |
|--------|------|---------|-------------|
| `EnableInTransit` | `bool` | `false` | Habilitar encriptación en tránsito (para sinks HTTP) |
| `EnableAtRest` | `bool` | `false` | Habilitar encriptación en reposo (para DLQ) |
| `EncryptionKeyBase64` | `string?` | `null` | Clave de encriptación en Base64 (se genera automáticamente si es null) |
| `EncryptionIVBase64` | `string?` | `null` | IV de encriptación en Base64 (se genera automáticamente si es null) |
| `EnableTls` | `bool` | `true` | Habilitar TLS/SSL para conexiones HTTP |
| `ValidateCertificates` | `bool` | `true` | Validar certificados SSL |

**Nota:** Si `EncryptionKeyBase64` o `EncryptionIVBase64` son `null`, se generan automáticamente. Para producción, se recomienda proporcionar claves fijas.

**Generación de claves (ejemplo):**
```csharp
using System.Security.Cryptography;

var key = new byte[32]; // 256 bits
var iv = new byte[16];  // 128 bits
RandomNumberGenerator.Fill(key);
RandomNumberGenerator.Fill(iv);

var keyBase64 = Convert.ToBase64String(key);
var ivBase64 = Convert.ToBase64String(iv);
```

---

## Configuración Avanzada

### Middleware HTTP

```json
{
  "Metrics": {
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

### Exportación

```json
{
  "Metrics": {
    "Export": {
      "Enabled": true,
      "ExportIntervalSeconds": 30,
      "Formats": ["Prometheus", "JSON"],
      "Prometheus": {
        "Enabled": true,
        "Endpoint": "/metrics",
        "Port": 9090
      },
      "File": {
        "Enabled": false,
        "Path": "./metrics",
        "FileName": "metrics.json",
        "RotationEnabled": true,
        "MaxFileSizeMB": 100
      }
    }
  }
}
```

---

## Hot-Reload

La configuración puede recargarse sin reiniciar la aplicación usando `MetricsConfigurationManager`:

```csharp
using JonjubNet.Metrics.Shared.Configuration;

// Inyectar MetricsConfigurationManager
var configManager = serviceProvider.GetRequiredService<MetricsConfigurationManager>();

// Recargar configuración
configManager.Reload();
```

**Nota:** Los cambios se aplican automáticamente cuando se modifica `appsettings.json` si se usa `IOptionsMonitor`.

---

## Configuración Completa de Ejemplo

Ver `appsettings.example.json` para un ejemplo completo de configuración con todas las opciones disponibles.

---

## Referencias

- [README.md](README.md) - Guía de inicio rápido
- [EXAMPLES.md](EXAMPLES.md) - Ejemplos de código
- [INTEGRATION.md](INTEGRATION.md) - Guías de integración
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Resolución de problemas

