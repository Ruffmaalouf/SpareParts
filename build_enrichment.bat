@echo off
title Build Check - SpareParts.ImageEnrichment
cd /d D:\Ralph\SpareParts
echo Building SpareParts.ImageEnrichment...
dotnet build tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj --configuration Release
if %ERRORLEVEL% == 0 (
    echo.
    echo BUILD SUCCEEDED - ready to run add_missing_parts.bat
) else (
    echo.
    echo BUILD FAILED - check errors above
)
pause
