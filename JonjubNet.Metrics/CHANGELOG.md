# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

### Agregado
- Implementación inicial del servicio de métricas reutilizable
- Soporte completo para métricas: Contadores, Gauges, Histogramas, Timers
- Integración nativa con Prometheus para exportación de métricas
- Middleware HTTP automático para captura de métricas de requests
- Configuración flexible via appsettings.json
- Interfaz genérica IMetricsService para fácil integración
- Implementación completa del servicio de métricas
- Soporte para métricas HTTP automáticas con middleware
- Soporte para métricas de base de datos estructuradas
- Soporte para métricas de negocio personalizadas
- Soporte para métricas del sistema
- Documentación completa con ejemplos de uso
- Scripts de build para Windows y Linux/Mac
- Configuración de GitHub Actions para CI/CD
- Icono para paquete NuGet

### Características Técnicas
- Basado en .NET 10.0 para máximo rendimiento
- Integración con Prometheus.Client para métricas estándar
- Configuración via IConfiguration con validación
- Inyección de dependencias nativa con ServiceExtensions
- Logging estructurado integrado con Microsoft.Extensions.Logging
- Configuración granular por tipo de métrica
- Etiquetas dinámicas para métricas personalizadas
- Buckets configurables para histogramas y timers
- Exportación a múltiples formatos (Prometheus, JSON)
- Middleware configurable con exclusiones de rutas
- Auto-detección de endpoints con normalización de parámetros

### Configuración
- Sección de configuración: "Metrics"
- Tipos de métricas: Counter, Gauge, Histogram, Timer
- Configuración por servicio específico con etiquetas
- Buckets personalizables para histogramas
- Configuración de exportación Prometheus
- Configuración de middleware HTTP
- Configuración de métricas de base de datos
- Exclusiones de rutas configurables

### Interfaces y Modelos
- IMetricsService: Interfaz principal para registro de métricas
- IMetricsMiddleware: Interfaz para middleware personalizado
- HttpMetrics: Modelo para métricas HTTP
- DatabaseMetrics: Modelo para métricas de base de datos
- BusinessMetrics: Modelo para métricas de negocio
- SystemMetrics: Modelo para métricas del sistema

### Documentación
- README.md completo con ejemplos de uso
- Comentarios XML en todo el código
- Ejemplos de implementación en carpeta Examples/
- Guía de configuración detallada
- Instrucciones de construcción y publicación
- Configuración de GitHub Actions
- Licencia MIT incluida
