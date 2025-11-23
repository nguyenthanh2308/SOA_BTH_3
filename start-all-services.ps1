# Script to start all microservices
Write-Host "Starting all microservices..." -ForegroundColor Green

# Function to start a service
function Start-Service {
    param(
        [string]$ServicePath,
        [string]$ServiceName
    )
    
    Write-Host "Starting $ServiceName..." -ForegroundColor Yellow
    Start-Process dotnet -ArgumentList "run --project $ServicePath" -WindowStyle Normal
    Start-Sleep -Seconds 2
}

# Get the base directory
$baseDir = $PSScriptRoot

# Start all services
Start-Service -ServicePath "$baseDir\AuthService\AuthService\AuthService.csproj" -ServiceName "AuthService (https://localhost:7268)"
Start-Service -ServicePath "$baseDir\ProductService\ProductService\ProductService.csproj" -ServiceName "ProductService (https://localhost:7181)"
Start-Service -ServicePath "$baseDir\OrderService\OrderService\OrderService.csproj" -ServiceName "OrderService (https://localhost:7182)"
Start-Service -ServicePath "$baseDir\ReportService\ReportService.csproj" -ServiceName "ReportService (https://localhost:5003)"
Start-Service -ServicePath "$baseDir\EcommerceClientUI\EcommerceClientUI\EcommerceClientUI.csproj" -ServiceName "EcommerceClientUI"

Write-Host "`nAll services are starting..." -ForegroundColor Green
Write-Host "Please wait for all services to be ready before testing." -ForegroundColor Cyan
Write-Host "`nServices URLs:" -ForegroundColor Cyan
Write-Host "  - AuthService:     https://localhost:7268/swagger" -ForegroundColor White
Write-Host "  - ProductService:  https://localhost:7181/swagger" -ForegroundColor White
Write-Host "  - OrderService:    https://localhost:7182/swagger" -ForegroundColor White
Write-Host "  - ReportService:   https://localhost:5003/swagger" -ForegroundColor White
Write-Host "  - Client UI:       https://localhost:xxxx (check terminal)" -ForegroundColor White

