$ErrorActionPreference = 'Stop'
$script:ApiProject = Join-Path $PSScriptRoot '../../ApexPost.Api/ApexPost.Api.csproj'
function Read-ApiSecrets {
    $raw = (& dotnet user-secrets list --project $script:ApiProject --json | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Could not read API User Secrets.' }
    $start=$raw.IndexOf('{'); $end=$raw.LastIndexOf('}')
    if ($start -lt 0 -or $end -lt $start) { return @{} }
    $obj=$raw.Substring($start,$end-$start+1) | ConvertFrom-Json
    $values=@{}; foreach($property in $obj.PSObject.Properties) { $values[$property.Name]=$property.Value }; return $values
}
function Set-ApiSecret([string]$Name,[string]$Value) {
    @{ $Name=$Value } | ConvertTo-Json -Compress | & dotnet user-secrets set --project $script:ApiProject | Out-Null
    if($LASTEXITCODE -ne 0) { throw "Could not save User Secret: $Name" }
}
function New-RandomSecret {
    $bytes=New-Object byte[] 64; $rng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes); return [Convert]::ToBase64String($bytes) } finally { $rng.Dispose() }
}
