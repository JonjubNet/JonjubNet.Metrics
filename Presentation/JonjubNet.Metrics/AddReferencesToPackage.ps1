param(
    [string]$PackageFile,
    [string]$TargetFramework
)

$refs = @(
    "JonjubNet.Metrics.dll",
    "JonjubNet.Metrics.Core.dll",
    "JonjubNet.Metrics.Shared.dll",
    "JonjubNet.Metrics.Prometheus.dll",
    "JonjubNet.Metrics.OpenTelemetry.dll",
    "JonjubNet.Metrics.InfluxDB.dll"
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$tempDir = [System.IO.Path]::GetTempPath() + [System.Guid]::NewGuid().ToString()
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($PackageFile, $tempDir)
    
    $nuspecFile = Join-Path $tempDir "JonjubNet.Metrics.nuspec"
    if (-not (Test-Path $nuspecFile)) {
        Write-Error "Nuspec not found in package"
        exit 1
    }
    
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($nuspecFile)
    
    $ns = $xml.DocumentElement.NamespaceURI
    if ([string]::IsNullOrWhiteSpace($ns)) { $ns = $null }
    
    $package = $xml.DocumentElement
    if ($null -eq $package) {
        Write-Error "Package element not found"
        exit 1
    }
    
    $nsManager = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    if (-not [string]::IsNullOrWhiteSpace($ns)) {
        $nsManager.AddNamespace("n", $ns)
        $metadata = $package.SelectSingleNode("n:metadata", $nsManager)
        $existingRefs = $metadata.SelectSingleNode("n:references", $nsManager) -as [System.Xml.XmlElement]
    } else {
        $metadata = $package.SelectSingleNode("metadata")
        $existingRefs = $metadata.SelectSingleNode("references") -as [System.Xml.XmlElement]
    }
    
    if ($null -eq $metadata) {
        Write-Error "Metadata not found"
        exit 1
    }
    if ($null -ne $existingRefs) {
        $metadata.RemoveChild($existingRefs) | Out-Null
    }
    
    if ($null -ne $ns) {
        $references = $xml.CreateElement("references", $ns)
        $group = $xml.CreateElement("group", $ns)
    } else {
        $references = $xml.CreateElement("references")
        $group = $xml.CreateElement("group")
    }
    
    $group.SetAttribute("targetFramework", $TargetFramework)
    
    foreach ($ref in $refs) {
        if ($null -ne $ns) {
            $refElem = $xml.CreateElement("reference", $ns)
        } else {
            $refElem = $xml.CreateElement("reference")
        }
        $refElem.SetAttribute("file", $ref)
        $null = $group.AppendChild($refElem)
    }
    
    $null = $references.AppendChild($group)
    $null = $metadata.AppendChild($references)
    
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $writer = New-Object System.IO.StreamWriter($nuspecFile, $false, $utf8NoBom)
    $xml.Save($writer)
    $writer.Close()
    
    Remove-Item $PackageFile -Force
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $PackageFile)
    
    Write-Host "Added references to package"
    exit 0
}
catch {
    Write-Error "Error: $_"
    exit 1
}
finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
