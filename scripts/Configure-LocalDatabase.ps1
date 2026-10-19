param(
    [string]$DatabaseHost = 'localhost',
    [int]$Port = 5432,
    [string]$Database = 'atendia',
    [string]$Username = 'postgres'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../backend/src/Atendia.Api/Atendia.Api.csproj'
$dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
}

$password = Read-Host "PostgreSQL password for $Username (hidden)" -AsSecureString
$connection = New-Object System.Data.Common.DbConnectionStringBuilder
$settings = $null
$payload = $null

try {
    if ($password.Length -eq 0) {
        throw 'Password cannot be empty.'
    }

    $connection.set_Item('Host', $DatabaseHost)
    $connection.set_Item('Port', $Port)
    $connection.set_Item('Database', $Database)
    $connection.set_Item('Username', $Username)
    $connection.set_Item('Password', [System.Net.NetworkCredential]::new('', $password).Password)
    $settings = @{ ConnectionStrings = @{ AtendiaDb = $connection.get_ConnectionString() } }
    $payload = $settings | ConvertTo-Json -Depth 3 -Compress
    $payload | & $dotnet user-secrets set --project $project
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not save local database configuration.'
    }

    Write-Host 'Database connection saved in local .NET user secrets (Development only).'
    Write-Host 'User secrets are stored unencrypted outside the repository; use only for local development.'
}
finally {
    $connection.Clear()
    $password.Dispose()
    $settings = $null
    $payload = $null
}