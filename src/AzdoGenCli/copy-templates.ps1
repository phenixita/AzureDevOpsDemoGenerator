# Copy Templates folder from VstsDemoBuilder to AzdoGenCli
# Run this script from the repository root

$sourcePath = "src\VstsDemoBuilder\Templates"
$destinationPath = "src\AzdoGenCli\Templates"

Write-Host "Copying Templates folder from $sourcePath to $destinationPath..." -ForegroundColor Cyan

if (Test-Path $destinationPath) {
    Write-Host "Removing existing Templates folder..." -ForegroundColor Yellow
    Remove-Item -Path $destinationPath -Recurse -Force
}

Copy-Item -Path $sourcePath -Destination $destinationPath -Recurse -Force

Write-Host "Templates folder copied successfully!" -ForegroundColor Green
Write-Host "Total files copied: $(( Get-ChildItem -Path $destinationPath -Recurse -File | Measure-Object ).Count)" -ForegroundColor Green
