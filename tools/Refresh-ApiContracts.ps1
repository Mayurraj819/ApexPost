param([string]$BaseUrl = "https://localhost:7268")
$ErrorActionPreference = "Stop"
Push-Location (Join-Path $PSScriptRoot "..")
try {
    New-Item -ItemType Directory -Force contracts | Out-Null
    foreach ($version in @("v1", "v2")) {
        Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/openapi/$version.json" -OutFile "contracts/$version.json"
    }
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Tool restore failed." }
    dotnet nswag run nswag.json
    if ($LASTEXITCODE -ne 0) { throw "NSwag generation failed." }
    Write-Host "Review contracts and generated SDK diffs; build and run tests before committing."
} finally { Pop-Location }
