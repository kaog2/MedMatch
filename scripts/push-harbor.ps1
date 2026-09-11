param(
    [string]$Tag = "",
    [string]$Registry = "registry.example.com",
    [string]$Project = "medmatch"
)

$ErrorActionPreference = "Stop"
$versionTag = if ([string]::IsNullOrWhiteSpace($Tag) -or $Tag -eq "latest") { (Get-Date).ToUniversalTime().ToString("yyyy.MM.dd.HHmmss") } else { $Tag }
$backendImage = "$Registry/$Project/backend:$versionTag"
$frontendImage = "$Registry/$Project/frontend:$versionTag"
$backendLatestImage = "$Registry/$Project/backend:latest"
$frontendLatestImage = "$Registry/$Project/frontend:latest"

# No secrets are passed here: the Google OAuth client id is injected at
# container start (docker-entrypoint.sh -> /config.js), never baked into
# the image. See docker-compose.harbor.yml for the GOOGLE_CLIENT_ID env var.

Write-Host "Building and pushing multi-architecture $backendImage and $backendLatestImage"
docker buildx build --platform linux/amd64,linux/arm64 --push `
    -t $backendImage `
    -t $backendLatestImage ./backend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building and pushing multi-architecture $frontendImage and $frontendLatestImage"
docker buildx build --platform linux/amd64,linux/arm64 --push --target production `
    -t $frontendImage `
    -t $frontendLatestImage ./frontend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Pushed version tag $versionTag and latest for backend and frontend"
