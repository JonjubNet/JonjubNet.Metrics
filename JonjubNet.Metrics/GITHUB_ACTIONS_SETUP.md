# GitHub Actions Setup para JonjubNet.Metrics

## 🚀 **Configuración de CI/CD**

Este documento describe cómo configurar GitHub Actions para el proyecto JonjubNet.Metrics.

## 📁 **Estructura de Workflows**

### **1. Workflow Principal (.github/workflows/ci.yml)**

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '8.0.x'
  PROJECT_NAME: 'JonjubNet.Metrics'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build project
      run: dotnet build --configuration Release --no-restore
      
    - name: Run tests
      run: dotnet test --configuration Release --no-build --verbosity normal
      
    - name: Generate coverage report
      run: dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ./coverage
      
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage/coverage.cobertura.xml
        flags: unittests
        name: codecov-umbrella

  package:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Pack NuGet package
      run: dotnet pack --configuration Release --no-build --output ./packages
      
    - name: Upload package artifact
      uses: actions/upload-artifact@v3
      with:
        name: nuget-package
        path: ./packages/*.nupkg

  publish:
    needs: package
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Download package artifact
      uses: actions/download-artifact@v3
      with:
        name: nuget-package
        path: ./packages
        
    - name: Publish to NuGet
      run: dotnet nuget push ./packages/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

### **2. Workflow de Release (.github/workflows/release.yml)**

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  create-release:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build project
      run: dotnet build --configuration Release --no-restore
      
    - name: Run tests
      run: dotnet test --configuration Release --no-build
      
    - name: Pack NuGet package
      run: dotnet pack --configuration Release --no-build --output ./packages
      
    - name: Create Release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: ${{ github.ref }}
        release_name: Release ${{ github.ref }}
        draft: false
        prerelease: false
        
    - name: Upload Release Asset
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ steps.create_release.outputs.upload_url }}
        asset_path: ./packages/*.nupkg
        asset_name: JonjubNet.Metrics.${{ github.ref_name }}.nupkg
        asset_content_type: application/octet-stream
```

## 🔧 **Configuración de Secrets**

### **Secrets Requeridos en GitHub:**

1. **NUGET_API_KEY**: API Key de NuGet.org para publicar paquetes
   - Obtener en: https://www.nuget.org/account/apikeys
   - Configurar en: Settings > Secrets and variables > Actions

### **Variables de Entorno Opcionales:**

- `CODECOV_TOKEN`: Token para Codecov (opcional)
- `SONAR_TOKEN`: Token para SonarCloud (opcional)

## 📋 **Configuración de Branch Protection**

### **Reglas Recomendadas:**

1. **main branch:**
   - Require pull request reviews
   - Require status checks to pass before merging
   - Require branches to be up to date before merging
   - Restrict pushes that create files

2. **develop branch:**
   - Require pull request reviews
   - Require status checks to pass before merging

## 🏷️ **Versionado y Tags**

### **Estrategia de Versionado:**

- Usar [Semantic Versioning](https://semver.org/)
- Formato: `v1.0.0`, `v1.0.1`, `v1.1.0`, etc.
- Tags se crean automáticamente en releases

### **Comandos para Crear Release:**

```bash
# Crear tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# O usar GitHub CLI
gh release create v1.0.0 --title "Release v1.0.0" --notes "Initial release"
```

## 📊 **Monitoreo y Métricas**

### **Integraciones Recomendadas:**

1. **Codecov**: Para cobertura de código
2. **SonarCloud**: Para análisis de calidad
3. **Dependabot**: Para actualizaciones de dependencias
4. **GitHub Security**: Para vulnerabilidades

### **Configuración de Dependabot (.github/dependabot.yml):**

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
```

## 🚨 **Notificaciones**

### **Configuración de Notificaciones:**

- Slack/Discord webhooks para notificaciones de build
- Email notifications para releases
- GitHub notifications para pull requests

## 📝 **Documentación Automática**

### **Generación de Documentación:**

- XML documentation comments
- GitHub Pages para documentación
- API documentation con DocFX

## 🔄 **Actualización del Workflow**

### **Cuándo Actualizar:**

1. **Cambios en dependencias**: Actualizar versiones de .NET
2. **Nuevas funcionalidades**: Agregar nuevos jobs o steps
3. **Cambios en estructura**: Modificar paths o configuraciones
4. **Nuevas herramientas**: Integrar nuevas herramientas de CI/CD

### **Proceso de Actualización:**

1. Modificar archivos `.yml` en `.github/workflows/`
2. Commit y push de cambios
3. Verificar que el workflow funcione correctamente
4. Actualizar este documento si es necesario

## 📚 **Recursos Adicionales**

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET GitHub Actions](https://github.com/actions/setup-dotnet)
- [NuGet Package Publishing](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Semantic Versioning](https://semver.org/)

---

**Nota**: Este archivo debe actualizarse manualmente cuando se modifiquen los workflows de GitHub Actions o se agreguen nuevas configuraciones de CI/CD.
