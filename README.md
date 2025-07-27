# Resolution Launcher

A lightweight, beautifully designed Windows application that allows you to launch games and applications at specific resolutions, with automatic resolution restoration when the application exits.

## Directory Structure

- `*.xaml`, `*.cs`, `*.csproj` - Modern WPF application source code
- `build-release.bat` - Optimized build script  
- `README.md` - This documentation

## Features

- **Modern UI**: Clean, Windows 11-inspired design with Mica effects and system accent colors
- **Dual App Support**: Launch both desktop applications (.exe, .lnk) and UWP/Store applications
- **Smart Resolution Management**: Automatically detects and restores your original resolution
- **Desktop Shortcuts**: Create clickable shortcuts for instant game launching
- **Lightweight**: Optimized for minimal performance impact while gaming
- **System Integration**: Uses Windows accent colors and proper DPI scaling
- **Curated Resolutions**: Practical gaming resolutions (4K, 1440p, 1080p, etc.)

## UWP Application Support

**Now Supported!** The application can launch any UWP/Store app by entering its App ID.

**How to find UWP App IDs:**
1. Right-click any UWP app shortcut on your desktop
2. Select "Properties"
3. Copy the "Target" field (this is the App ID)

**Examples:**
- **Minecraft Bedrock**: `Microsoft.MinecraftUWP_8wekyb3d8bbwe!App`
- **Forza Horizon 5**: `Microsoft.624F8B84B80_8wekyb3d8bbwe!App`
- **Sea of Thieves**: `Microsoft.SeaofThieves_8wekyb3d8bbwe!App`

**Note**: UWP apps require manual resolution restoration due to their sandboxed nature. The app will prompt you when to restore your original resolution.

## How to Use

### Main Interface
1. **Select Application Type**: Choose between Desktop Application or UWP/Store Application
2. **Choose Your App**: 
   - For desktop apps: Click "Browse..." to select an executable file or shortcut
   - For UWP apps: Enter the UWP App ID (found in shortcut properties)
3. **Select Resolution**: Choose your desired resolution from the dropdown menu
4. **Launch Options**:
   - **Launch Now**: Immediately run the application at the selected resolution
   - **Create Shortcut**: Create a desktop shortcut for future use

### Desktop Shortcuts
- Shortcuts created by Resolution Launcher will automatically:
  - Change to the specified resolution
  - Launch the target application
  - Monitor the application and restore the original resolution when it exits (desktop apps)
  - Prompt for manual restoration (UWP apps)

## Technical Details

- **Framework**: .NET Framework 4.8 Windows Forms
- **Resolution Management**: Uses Windows Display Settings API (user32.dll)
- **Process Monitoring**: Lightweight background thread monitoring for automatic resolution restoration
- **UWP Integration**: Uses Windows shell integration for UWP app launching
- **Executable Size**: ~24KB (single file, no dependencies)

## System Requirements

- Windows 10 or later (for UWP support)
- .NET Framework 4.8 (pre-installed on Windows 10/11)

## Building from Source

Simply run the included build script:
```batch
build-release.bat
```

Or compile manually:
```batch
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:ResolutionLauncher.exe /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ResolutionLauncher_Simple.cs
```

## Notes

- The application requires appropriate permissions to change display settings
- Desktop applications: Resolution automatically restores when the app exits
- UWP applications: Manual restoration prompt due to sandboxed execution model
- Some fullscreen applications may override system resolution settings
- The process monitoring is designed to be lightweight and should not impact game performance