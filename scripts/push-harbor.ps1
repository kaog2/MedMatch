param(
    [string]$Tag = "latest",
    [string]$Registry = "harbor.kevdevs.org",
    [string]$Project = "medmatch",
    [string]$GoogleClientId = $env:GOOGLE_CLIENT_ID
)

$ErrorActionPreference = "Stop"
$backendImage = "$Registry/$Project/backend:$Tag"
$frontendImage = "$Registry/$Project/frontend:$Tag"

if ([string]::IsNullOrWhiteSpace($GoogleClientId)) {
    throw "Set GOOGLE_CLIENT_ID before building the frontend image."
}

Write-Host "Building $backendImage"
docker build --platform linux/amd64 -t $backendImage ./backend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building $frontendImage"
docker build --platform linux/amd64 --target production `
    --build-arg VITE_API_URL= `
    --build-arg VITE_GOOGLE_CLIENT_ID=$GoogleClientId `
    -t $frontendImage ./frontend
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Pushing images"
docker push $backendImage
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
docker push $frontendImage
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Pushed $backendImage and $frontendImage"
