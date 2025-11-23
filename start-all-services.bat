@echo off
echo Starting all microservices...
echo.

cd /d "%~dp0"

echo Starting AuthService (https://localhost:7268)...
start "AuthService" cmd /k "cd AuthService\AuthService && dotnet run"

timeout /t 3 /nobreak > nul

echo Starting ProductService (https://localhost:7181)...
start "ProductService" cmd /k "cd ProductService\ProductService && dotnet run"

timeout /t 3 /nobreak > nul

echo Starting OrderService (https://localhost:7182)...
start "OrderService" cmd /k "cd OrderService\OrderService && dotnet run"

timeout /t 3 /nobreak > nul

echo Starting ReportService (https://localhost:5003)...
start "ReportService" cmd /k "cd ReportService && dotnet run"

timeout /t 3 /nobreak > nul

echo Starting EcommerceClientUI...
start "EcommerceClientUI" cmd /k "cd EcommerceClientUI\EcommerceClientUI && dotnet run"

echo.
echo All services are starting in separate windows...
echo.
echo Services URLs:
echo   - AuthService:     https://localhost:7268/swagger
echo   - ProductService:  https://localhost:7181/swagger
echo   - OrderService:    https://localhost:7182/swagger
echo   - ReportService:   https://localhost:5003/swagger
echo   - Client UI:       Check the terminal window
echo.
pause

