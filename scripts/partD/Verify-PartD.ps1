param([switch]$WithLocalDb,[switch]$BuildWindows)
$ErrorActionPreference='Stop'
Push-Location (Resolve-Path "$PSScriptRoot/../..").Path
try {
    if($WithLocalDb) { $env:APEXPOST_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True' }
    New-Item -ItemType Directory -Force evidence/week4-partD | Out-Null
    & dotnet build ApexPost.slnx -m:1 --nologo 2>&1 | Tee-Object evidence/week4-partD/build-windows.txt
    if($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    & dotnet test ApexPost.slnx --no-build -m:1 --nologo 2>&1 | Tee-Object evidence/week4-partD/tests-windows.txt
    if($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    if($BuildWindows) {
        & dotnet build ApexPost.Courier/ApexPost.Courier.csproj -f net10.0-windows10.0.19041.0 -m:1 --nologo 2>&1 | Tee-Object evidence/week4-partD/maui-windows.txt
        if($LASTEXITCODE -ne 0) { throw 'MAUI Windows build failed. Share the first error.' }
    }
} finally { Pop-Location }
