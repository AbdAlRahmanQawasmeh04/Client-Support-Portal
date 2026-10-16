$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'Backend/Client Support Ticketing System/Client Support Ticketing System'
$targetPath = Join-Path $projectPath 'appsettings.Development.json'
if (Test-Path -LiteralPath $targetPath) { throw 'Configuration already exists. Review it manually rather than overwriting it.' }
$connection = Read-Host 'Enter your local SQL Server connection string (do not paste it into chat)'
if ([string]::IsNullOrWhiteSpace($connection)) { throw 'A connection string is required.' }
$bytes = New-Object byte[] 32
$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$generator.GetBytes($bytes)
$generator.Dispose()
$config = @{
 ConnectionStrings = @{ DefaultConnection = $connection }
 Jwt = @{ Key = [Convert]::ToBase64String($bytes); Issuer = 'SupportPortal'; Audience = 'SupportPortal'; ExpiryMinutes = 60 }
 Cors = @{ AllowedOrigins = @('http://localhost:4200') }
 Logging = @{ LogLevel = @{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning' } }
 AllowedHosts = 'localhost'
}
$config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $targetPath -Encoding utf8
Write-Output 'Local development configuration created. Do not share it.'
