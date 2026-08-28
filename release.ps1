[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?$')]
    [string]$Version = "1.0.0-rc.1",

    [string]$OutputDirectory = "artifacts/release",

    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$coreProject = Join-Path $repositoryRoot "src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj"

function Assert-NativeSuccess([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Resolve-OutputDirectory([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
}

function Assert-PackageEntry([string[]]$EntryNames, [string]$Pattern, [string]$Description) {
    if (-not ($EntryNames | Where-Object { $_ -match $Pattern })) {
        throw "Package is missing $Description (pattern: $Pattern)."
    }
}

if (-not $SkipBuild) {
    & (Join-Path $repositoryRoot "build.ps1") -Configuration $Configuration
}

if (-not $SkipTests) {
    & (Join-Path $repositoryRoot "test.ps1") -Configuration $Configuration -SkipBuild
}

$outputPath = Resolve-OutputDirectory $OutputDirectory
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath | Out-Null

Push-Location $repositoryRoot
try {
    dotnet pack $coreProject -c $Configuration --no-restore -p:PackageVersion=$Version -p:Version=$Version -o $outputPath
    Assert-NativeSuccess "dotnet pack"

    $packages = @(Get-ChildItem -Path $outputPath -Filter "*.nupkg" -File | Where-Object { $_.Name -notlike "*.symbols.nupkg" })
    $symbolPackages = @(Get-ChildItem -Path $outputPath -Filter "*.snupkg" -File)

    if ($packages.Count -ne 1) {
        throw "Expected exactly one .nupkg, found $($packages.Count)."
    }
    if ($symbolPackages.Count -ne 1) {
        throw "Expected exactly one .snupkg, found $($symbolPackages.Count)."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($packages[0].FullName)
    try {
        $entryNames = @($archive.Entries | ForEach-Object { $_.FullName })
        Assert-PackageEntry $entryNames '^lib/net48/MyDmsVn\.Bootstrap5WinFormUI\.dll$' 'the net48 assembly'
        Assert-PackageEntry $entryNames '^lib/net48/MyDmsVn\.Bootstrap5WinFormUI\.xml$' 'the net48 XML documentation'
        Assert-PackageEntry $entryNames '^lib/net8\.0-windows[^/]*/MyDmsVn\.Bootstrap5WinFormUI\.dll$' 'the net8.0-windows assembly'
        Assert-PackageEntry $entryNames '^lib/net8\.0-windows[^/]*/MyDmsVn\.Bootstrap5WinFormUI\.xml$' 'the net8.0-windows XML documentation'
        Assert-PackageEntry $entryNames '^README\.md$' 'the package README'

        $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -match '\.nuspec$' } | Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "Package is missing its .nuspec metadata."
        }

        $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $idNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
        $versionNode = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")
        if ($null -eq $idNode -or $idNode.InnerText -ne "MyDmsVn.Bootstrap5WinFormUI") {
            throw "Unexpected package id '$($idNode.InnerText)'."
        }
        if ($null -eq $versionNode -or $versionNode.InnerText -ne $Version) {
            throw "Unexpected package version '$($versionNode.InnerText)'; expected '$Version'."
        }
    }
    finally {
        $archive.Dispose()
    }

    $artifacts = @($packages + $symbolPackages)
    $checksumLines = foreach ($artifact in $artifacts) {
        $hash = Get-FileHash -Path $artifact.FullName -Algorithm SHA256
        "$($hash.Hash.ToLowerInvariant())  $($artifact.Name)"
    }
    Set-Content -Path (Join-Path $outputPath "SHA256SUMS.txt") -Value $checksumLines -Encoding utf8

    $sourceCommit = (git rev-parse HEAD).Trim()
    Assert-NativeSuccess "git rev-parse HEAD"
    $sdkVersion = (dotnet --version).Trim()
    Assert-NativeSuccess "dotnet --version"

    $manifest = [ordered]@{
        packageId = "MyDmsVn.Bootstrap5WinFormUI"
        version = $Version
        configuration = $Configuration
        sourceCommit = $sourceCommit
        dotnetSdk = $sdkVersion
        targetFrameworks = @("net48", "net8.0-windows")
        artifacts = @($artifacts | ForEach-Object { $_.Name })
    }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $outputPath "release-manifest.json") -Encoding utf8

    Write-Host "Release candidate package verified: $($packages[0].Name)"
    Write-Host "Symbols package verified: $($symbolPackages[0].Name)"
    Write-Host "Artifacts: $outputPath"
}
finally {
    Pop-Location
}
