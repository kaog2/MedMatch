param(
    [string]$Tag = "",
    [string]$Registry = "registry.example.com",
    [string]$Project = "medmatch",
    [string]$GoogleClientId = $env:GOOGLE_CLIENT_ID
)

$ErrorActionPreference = "Stop"
$versionTag = if ([string]::IsNullOrWhiteSpace($Tag) -or $Tag -eq "latest") { (Get-Date).ToUniversalTime().ToString("yyyy.MM.dd.HHmmss") } else { $Tag }
$backendImage = "$Registry/$Project/backend:$versionTag"
$frontendImage = "$Registry/$Project/frontend:$versionTag"
$backendLatestImage = "$Registry/$Project/backend:latest"
$frontendLatestImage = "$Registry/$Project/frontend:latest"

if ([string]::IsNullOrWhiteSpace($GoogleClientId)) {
    throw "Set GOOGLE_CLIENT_ID before building the frontend image."
}

Write-Host "Building and pushing multi-architecture $backendImage and $backendLatestImage"
docker buildx build --platform linux/amd64,linux/arm64 --push `
    -t $backendImage `
    -t $backendLatestImage ./backend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building and pushing multi-architecture $frontendImage and $frontendLatestImage"
docker buildx build --platform linux/amd64,linux/arm64 --push --target production `
    --build-arg VITE_API_URL= `
    --build-arg VITE_GOOGLE_CLIENT_ID=$GoogleClientId `
    -t $frontendImage `
    -t $frontendLatestImage ./frontend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Pushed version tag $versionTag and latest for backend and frontend"
