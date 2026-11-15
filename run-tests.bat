@echo off
cd /d "%~dp0"
dotnet run --project GameProject.Tests\GameProject.Tests.csproj
pause
