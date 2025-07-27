# Resolution Launcher Modern

A modern Windows 11 themed resolution launcher built with WPF and .NET 9.

## Features

- **Modern Windows 11 Design**: Frosted glass background with Windows 11 aesthetic
- **Light/Dark Mode**: Automatic theme switching based on Windows system theme
- **Accent Color Integration**: Uses Windows system accent color throughout the UI
- **Modern Controls**: Updated with modern WPF styling and animations
- **Same Core Functionality**: All original features preserved (desktop/UWP app launching, resolution selection, shortcut creation)

## Requirements

- .NET 9.0 or later
- Windows 10/11
- Visual Studio 2022 or later (for development)

## Building and Running

### From Command Line
```bash
dotnet build ResolutionLauncherModern.csproj
dotnet run --project ResolutionLauncherModern.csproj
```

### From Visual Studio
1. Open the solution in Visual Studio
2. Set ResolutionLauncherModern as the startup project
3. Press F5 to run

## Architecture

- **WPF (Windows Presentation Foundation)**: Modern UI framework
- **MVVM Pattern**: Clean separation of concerns
- **Modern Styling**: Windows 11 design language with frosted glass effects
- **System Integration**: Uses Windows APIs for theme detection and accent colors

## Files Structure

- `App.xaml` - Application resources and theme definitions
- `MainWindow.xaml` - Main application window with modern styling
- `MainWindow.xaml.cs` - Code-behind with application logic
- `ResolutionLauncherModern.csproj` - Project configuration

## Theme Features

The application automatically adapts to Windows system theme settings:
- **Light Mode**: Clean white background with subtle shadows
- **Dark Mode**: Dark gray background with frosted glass effects
- **Accent Color**: Windows system accent color used for highlights and interactive elements
- **Frosted Glass**: Acrylic blur effects for modern Windows 11 appearance

## Migration from Simple Version

This modern version replaces the original `ResolutionLauncher_Simple.cs` with:
- Better user experience
- Modern Windows 11 design
- Theme support
- Improved visual feedback
- Same core functionality
