param([switch]$WithLocalDb)
$ErrorActionPreference = "Stop"
Push-Location (Join-Path $PSScriptRoot "..")
try {
    if ($WithLocalDb) {
        $env:APEXPOST_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
    }
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Tool restore failed." }
    dotnet restore ApexPost.slnx
    if ($LASTEXITCODE -ne 0) { throw "Package restore failed." }
    dotnet build ApexPost.slnx --no-restore -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    dotnet test ApexPost.slnx --no-build --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
    if (-not $env:APEXPOST_TEST_SQLSERVER) {
        Write-Host "SQL tests were skipped. Run this script with -WithLocalDb on your SQL desktop before submission."
    }
} finally { Pop-Location }
