@echo off
echo ===================================
echo Building Resolution Launcher v1.0.0
echo ===================================
echo.

REM Create output directory if it doesn't exist
if not exist "bin" mkdir "bin"

REM Delete old executable to force rebuild
if exist "bin\ResolutionLauncher.exe" (
    echo Removing old executable...
    del "bin\ResolutionLauncher.exe"
)

REM Build the application using .NET Framework compiler
echo Compiling with .NET Framework 4.8...
echo.

"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" /target:winexe /out:bin\ResolutionLauncher.exe /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ResolutionLauncher_Simple.cs

echo.
REM Check if build was successful
if exist "bin\ResolutionLauncher.exe" (
    echo [SUCCESS] Build complete!
    echo.
    echo Executable: bin\ResolutionLauncher.exe
    dir "bin\ResolutionLauncher.exe"
    echo.
    echo The application is ready to run!
) else (
    echo [ERROR] Build failed!
    echo Check the error messages above.
)

echo.
pause