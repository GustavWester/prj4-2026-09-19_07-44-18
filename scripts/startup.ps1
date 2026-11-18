[CmdletBinding()]
param()

# Starts the game backend: SQL Server (Docker), EF migrations and GameBackend.Api.
# The game itself runs as a WebGL build inside the website, so this script does not start Unity.
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File scripts\startup.ps1

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$backendDirectory = Join-Path $repoRoot "GameBackend"
$apiDirectory = Join-Path $backendDirectory "GameBackend.Api"
$composeFile = Join-Path $backendDirectory "docker-compose.yml"
$envFile = Join-Path $backendDirectory ".env"
$containerName = "game-sqlserver"
$websiteApiPort = 5032
$healthTimeoutSeconds = 180

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install it and run this script again."
    }
}

# Runs a native command without letting its stderr turn into a terminating error
# (Windows PowerShell 5.1 does that when $ErrorActionPreference is "Stop").
function Invoke-Native {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @()
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $FilePath @Arguments 2>&1 | ForEach-Object { "$_" }
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = (($output | Out-String).Trim())
        }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Test-TcpPort {
    param([string]$HostName = "localhost", [int]$Port, [int]$TimeoutMs = 1000)

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connection = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $connection.AsyncWaitHandle.WaitOne($TimeoutMs)) {
            return $false
        }
        $client.EndConnect($connection)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

# --- Prerequisites ----------------------------------------------------------

Write-Host "Checking prerequisites..." -ForegroundColor Cyan

Assert-Command "docker"
Assert-Command "dotnet"

if ((Invoke-Native "docker" @("info")).ExitCode -ne 0) {
    throw "Docker is not running. Start Docker Desktop and run this script again."
}

if (-not (Test-Path $composeFile)) {
    throw "Could not find docker-compose.yml in $backendDirectory."
}

if (-not (Test-Path $envFile) -or -not (Select-String -Path $envFile -Pattern '^\s*MSSQL_SA_PASSWORD\s*=\s*\S' -Quiet)) {
    throw "Missing MSSQL_SA_PASSWORD in $envFile. Create the file with MSSQL_SA_PASSWORD=<password> before starting the backend."
}

# Run dotnet checks inside the API folder: the repo root holds several Unity .csproj files,
# which makes 'dotnet ef' fail with "contains more than one project file".
Push-Location $apiDirectory
try {
    if ((Invoke-Native "dotnet" @("ef", "--version")).ExitCode -ne 0) {
        throw "dotnet-ef is not installed. Run 'dotnet tool install --global dotnet-ef' and try again."
    }

    $secrets = Invoke-Native "dotnet" @("user-secrets", "list")
}
finally {
    Pop-Location
}

$missingSecrets = @("ConnectionStrings:GameDb", "Jwt:Key") | Where-Object {
    $secrets.Output -notmatch ("(?m)^" + [regex]::Escape($_) + "\s*=")
}
if ($missingSecrets) {
    throw "Missing user secret(s) for GameBackend.Api: $($missingSecrets -join ', '). See step 2 in README.md."
}

# --- Database ---------------------------------------------------------------

Write-Host "Starting game SQL Server..." -ForegroundColor Cyan
Push-Location $backendDirectory
try {
    & docker compose up -d
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose could not start the game SQL Server."
    }
}
finally {
    Pop-Location
}

Write-Host "Waiting for SQL Server to become healthy (up to $healthTimeoutSeconds seconds)..." -ForegroundColor Cyan
$health = ""
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while ($stopwatch.Elapsed.TotalSeconds -lt $healthTimeoutSeconds) {
    $health = (Invoke-Native "docker" @("inspect", "--format", "{{.State.Health.Status}}", $containerName)).Output

    if ($health -eq "healthy") {
        break
    }

    if ($health -eq "unhealthy") {
        throw "SQL Server reported an unhealthy state. Check 'docker logs $containerName'."
    }

    Write-Progress -Activity "Waiting for $containerName" -Status "Status: $health ($([int]$stopwatch.Elapsed.TotalSeconds)s)"
    Start-Sleep -Seconds 2
}
Write-Progress -Activity "Waiting for $containerName" -Completed

if ($health -ne "healthy") {
    throw "SQL Server did not become healthy within $healthTimeoutSeconds seconds. Check 'docker logs $containerName'."
}

Write-Host "Applying database migrations..." -ForegroundColor Cyan
Push-Location $apiDirectory
try {
    & dotnet ef database update
    if ($LASTEXITCODE -ne 0) {
        throw "Database migrations failed (see the EF output above). Common causes: the password in ConnectionStrings:GameDb does not match MSSQL_SA_PASSWORD in GameBackend\.env, or the model has changes without a migration ('dotnet ef migrations add <Name>')."
    }
}
finally {
    Pop-Location
}

# --- API --------------------------------------------------------------------

if (-not (Test-TcpPort -Port $websiteApiPort)) {
    Write-Warning "The website API (http://localhost:$websiteApiPort) is not running. Login from the game will fail until it is started."
}

Write-Host "Launching Game API at https://localhost:7188 (Swagger: https://localhost:7188/swagger)..." -ForegroundColor Green
Start-Process powershell.exe -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location -LiteralPath '$($apiDirectory -replace "'", "''")'; dotnet run --launch-profile https"
)

Write-Host "Game backend started. Close the Game API PowerShell window to stop it." -ForegroundColor Green
