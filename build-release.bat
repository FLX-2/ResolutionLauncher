@echo off
echo Building optimized Resolution Launcher...

echo Cleaning all build artifacts...
dotnet clean --configuration Release --verbosity quiet
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "Release" rmdir /s /q "Release"

echo Building release version...
dotnet publish --configuration Release --runtime win-x64 --self-contained false --output "Release" --verbosity minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo SUCCESS: Build complete! 
    echo Optimized executable: Release\ResolutionLauncher.exe
    echo File size and performance optimized for gaming use.
) else (
    echo.
    echo ERROR: Build failed. Check output above.
)
pause