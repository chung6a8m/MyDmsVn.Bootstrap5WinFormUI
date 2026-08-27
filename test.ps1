[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$testProject = Join-Path $repositoryRoot "tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj"

function Assert-NativeSuccess([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

if (-not $SkipBuild) {
    & (Join-Path $repositoryRoot "build.ps1") -Configuration $Configuration
}

Push-Location $repositoryRoot
try {
    foreach ($framework in @("net48", "net8.0-windows")) {
        dotnet test $testProject -c $Configuration -f $framework --no-build --no-restore
        Assert-NativeSuccess "dotnet test -f $framework"
    }
}
finally {
    Pop-Location
}
