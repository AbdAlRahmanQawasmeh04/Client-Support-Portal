$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot 'Backend\Client Support Ticketing System\Client Support Ticketing System\Client Support Ticketing System.csproj'
if (!(Test-Path -LiteralPath $apiProject)) {
    $apiProject = Join-Path $PSScriptRoot 'Client Support Ticketing System\Client Support Ticketing System.csproj'
}
if (!(Test-Path -LiteralPath $apiProject)) { throw 'Place this script next to the backend solution or in the project root.' }
$smtpHostName = 'smtp.gmail.com'
$smtpPortNumber = '587'
$smtpUserName = (Read-Host 'Project Gmail address').Trim()
$smtpFromAddress = $smtpUserName
$portalAddress = Read-Host 'Frontend URL (press Enter for http://localhost:4200)'
if (!$portalAddress) { $portalAddress = 'http://localhost:4200' }
$smtpSecret = Read-Host 'SMTP app password (hidden; do not paste it into chat)' -AsSecureString
$secretPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($smtpSecret)
try {
    $values = @{
        'RecoveryMail:Enabled'='true'; 'RecoveryMail:Host'=$smtpHostName
        'RecoveryMail:Port'=$smtpPortNumber; 'RecoveryMail:Username'=$smtpUserName
        'RecoveryMail:From'=$smtpFromAddress; 'RecoveryMail:FrontendUrl'=$portalAddress
        'RecoveryMail:Password'=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($secretPointer).Replace(' ', '')
    }
    $values | ConvertTo-Json -Compress | dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'Could not save local email settings.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($secretPointer)
    if ($values) { $values.Clear() }
    $smtpSecret.Dispose()
}
Write-Host 'Email settings saved in local development user secrets. Restart the backend, then test delivery.'
