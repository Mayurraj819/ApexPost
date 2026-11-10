param([string]$ConnectionString)
. "$PSScriptRoot/Secrets.ps1"
$values=Read-ApiSecrets
foreach($name in @('Jwt:SigningKey','DevelopmentTokens:BootstrapKey','Partners:ApiKeys:0')) {
    if(-not $values[$name]) { Set-ApiSecret $name (New-RandomSecret) }
}
if(-not $values['Jwt:Issuer']) { Set-ApiSecret 'Jwt:Issuer' 'ApexPostDevelopment' }
if(-not $values['Jwt:Audience']) { Set-ApiSecret 'Jwt:Audience' 'ApexPostCourier' }
if($ConnectionString) { Set-ApiSecret 'ConnectionStrings:ApexPost' $ConnectionString }
elseif(-not $values['ConnectionStrings:ApexPost']) { throw 'Security secrets saved. Run again with -ConnectionString for your existing ApexPost database. See PARTD-START-HERE.md.' }
& dotnet dev-certs https --trust
if($LASTEXITCODE -ne 0) { throw 'Trust the ASP.NET development certificate before continuing.' }
Write-Host 'Part D configured. Existing keys preserved; no credentials printed.'
