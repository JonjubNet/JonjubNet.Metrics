param(
    [string]$NuspecFile,
    [string]$TargetFramework,
    [string[]]$References,
    [string]$ReferencesFile
)

if (-not (Test-Path $NuspecFile)) {
    Write-Error "Nuspec file not found: $NuspecFile"
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($ReferencesFile) -and (Test-Path $ReferencesFile)) {
    $References = Get-Content $ReferencesFile | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

if ($null -eq $References -or $References.Count -eq 0) {
    Write-Error "No references provided"
    exit 1
}

try {
    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($NuspecFile)
    
    $ns = $xml.DocumentElement.NamespaceURI
    if ([string]::IsNullOrWhiteSpace($ns)) {
        $ns = $null
    }
    
    $package = $xml.DocumentElement
    $metadata = $package.SelectSingleNode("metadata")
    if ($null -eq $metadata) {
        Write-Error "Metadata not found"
        exit 1
    }
    
    $existingRefs = $metadata.SelectSingleNode("references")
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
    
    $refCount = 0
    foreach ($ref in $References) {
        $ref = $ref.Trim()
        if ([string]::IsNullOrWhiteSpace($ref)) {
            continue
        }
        
        if ($null -ne $ns) {
            $refElem = $xml.CreateElement("reference", $ns)
        } else {
            $refElem = $xml.CreateElement("reference")
        }
        $refElem.SetAttribute("file", $ref)
        $null = $group.AppendChild($refElem)
        $refCount++
    }
    
    if ($refCount -eq 0) {
        Write-Error "No valid references"
        exit 1
    }
    
    $null = $references.AppendChild($group)
    $null = $metadata.AppendChild($references)
    
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $writer = New-Object System.IO.StreamWriter($NuspecFile, $false, $utf8NoBom)
    $xml.Save($writer)
    $writer.Close()
    
    Write-Host "Added $refCount references"
    exit 0
}
catch {
    Write-Error "Error: $_"
    exit 1
}
