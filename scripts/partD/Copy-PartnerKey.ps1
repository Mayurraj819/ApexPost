. "$PSScriptRoot/Secrets.ps1"
$values=Read-ApiSecrets
if(-not $values['Partners:ApiKeys:0']) { throw 'Run Setup-PartD.ps1 first.' }
Set-Clipboard -Value $values['Partners:ApiKeys:0']
Write-Host 'Partner key copied. Paste into Swagger PartnerKey, never into committed files.'
