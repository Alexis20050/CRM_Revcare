param([int]$Port = 5197, [switch]$Cloud)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
$backend = $null
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousRevcareEnv = $env:REVCARE_ENVIRONMENT
$previousApi = $env:REVCARE_API_BASE_URL
try {
    if ($Cloud) {
        if ([string]::IsNullOrWhiteSpace($env:REVCARE_CLOUD_PLATFORM_CONNECTION) -and [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('REVCARE_CLOUD_PLATFORM_CONNECTION', 'User'))) {
            throw 'Cloud launch requires REVCARE_CLOUD_PLATFORM_CONNECTION (db81188.public.databaseasp.net,1433). Refusing silent LocalDB fallback.'
        }
        if ([string]::IsNullOrWhiteSpace($env:REVCARE_CLOUD_TENANT_CONNECTION) -and [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('REVCARE_CLOUD_TENANT_CONNECTION', 'User'))) {
            throw 'Cloud launch requires REVCARE_CLOUD_TENANT_CONNECTION (db80735.public.databaseasp.net,1433). Refusing silent LocalDB fallback.'
        }
    }
    if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
        throw "Port $Port is already in use. Close the existing Tenant A session or choose another -Port."
    }
    dotnet build CRM.api -c Release --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
    dotnet build CRM_Revcare -c Release --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Desktop build failed.' }
    $env:ASPNETCORE_ENVIRONMENT = if ($Cloud) { 'Production' } else { 'Development' }
    if ($Cloud) { $env:REVCARE_ENVIRONMENT = 'CLOUD' }
    $env:REVCARE_API_BASE_URL = "http://127.0.0.1:$Port"
    $logDirectory = Join-Path $env:LOCALAPPDATA 'RevCareAutoHub\logs'
    [IO.Directory]::CreateDirectory($logDirectory) | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $apiArgs = if ($Cloud) { @('--cloud', '--urls', $env:REVCARE_API_BASE_URL) } else { @('--urls', $env:REVCARE_API_BASE_URL) }
    $backend = Start-Process -FilePath (Join-Path $PSScriptRoot 'CRM.api\bin\Release\net10.0\CRM.api.exe') `
        -ArgumentList $apiArgs -WorkingDirectory (Join-Path $PSScriptRoot 'CRM.api') `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logDirectory "$stamp-api.log") `
        -RedirectStandardError (Join-Path $logDirectory "$stamp-api-error.log")
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($backend.HasExited) { throw "Backend stopped. See $logDirectory." }
        try {
            $health = Invoke-RestMethod -Uri ($env:REVCARE_API_BASE_URL + '/health') -TimeoutSec 2
            if ($health.status -eq 'running') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (!$ready) { throw 'Backend did not become ready.' }
    Start-Process -FilePath (Join-Path $PSScriptRoot 'CRM_Revcare\bin\Release\net10.0-windows\CRM_Revcare.exe') `
        -ArgumentList '--tenant' -WorkingDirectory $PSScriptRoot -WindowStyle Normal -Wait
} finally {
    if ($backend -and !$backend.HasExited) { Stop-Process -Id $backend.Id }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:REVCARE_ENVIRONMENT = $previousRevcareEnv
    $env:REVCARE_API_BASE_URL = $previousApi
    Pop-Location
}
