@echo off
setlocal

set "ROOT=%~dp0"

where.exe dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Install the .NET SDK, then run start.bat again.
    exit /b 1
)

where.exe npm >nul 2>nul
if errorlevel 1 (
    echo npm was not found. Install Node.js, then run start.bat again.
    exit /b 1
)

if not exist "%ROOT%InsuranceClaimApi\InsuranceClaimApi.csproj" (
    echo Could not find InsuranceClaimApi\InsuranceClaimApi.csproj.
    exit /b 1
)

if not exist "%ROOT%node_modules" (
    echo Frontend dependencies are missing. Run npm install from the project folder first.
    exit /b 1
)

start "Claims API" /D "%ROOT%InsuranceClaimApi" "%ComSpec%" /k "dotnet run --project InsuranceClaimApi.csproj --launch-profile https"
start "Claims Frontend" /D "%ROOT%" "%ComSpec%" /k "npm start"

echo Started the claims API and Angular frontend in separate terminals.
echo Frontend: http://localhost:4200
echo API:      https://localhost:7197
