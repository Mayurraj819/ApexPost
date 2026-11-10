param([ValidateSet('Courier','Admin')][string]$Role='Courier',[string]$CourierId='COURIER-01',[string]$BaseUrl='https://localhost:7268')
. "$PSScriptRoot/Secrets.ps1"
if(-not $BaseUrl.StartsWith('https://localhost:')) { throw 'Use the trusted local HTTPS API address.' }
$values=Read-ApiSecrets
if(-not $values['DevelopmentTokens:BootstrapKey']) { throw 'Run Setup-PartD.ps1 first.' }
$body=@{courierId=$CourierId;role=$Role} | ConvertTo-Json
try { $result=Invoke-RestMethod -Method Post -Uri "$BaseUrl/dev/token" -Headers @{'X-Development-Key'=$values['DevelopmentTokens:BootstrapKey']} -ContentType 'application/json' -Body $body }
catch { throw 'Cannot obtain token. Check that the API is running with the https Development profile and a trusted certificate.' }
Set-Clipboard -Value $result.accessToken
Write-Host "$Role token copied; expires $($result.expiresAtUtc). Paste into Login or Swagger Authorize. Mask the token in evidence."
