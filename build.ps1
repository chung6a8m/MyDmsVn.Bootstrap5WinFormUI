[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = $PSScriptRoot
$solution = Join-Path $repositoryRoot "MyDmsVn.Bootstrap5WinFormUI.sln"

$projects = @(
    "src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj",
    "tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj",
    "demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj"
)

function Assert-NativeSuccess([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repositoryRoot
try {
    dotnet restore $solution
    Assert-NativeSuccess "dotnet restore"

    foreach ($framework in @("net48", "net8.0-windows")) {
        foreach ($project in $projects) {
            dotnet build $project -c $Configuration -f $framework --no-restore
            Assert-NativeSuccess "dotnet build $project -f $framework"
        }
    }
}
finally {
    Pop-Location
}
