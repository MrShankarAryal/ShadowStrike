# Installation Guide

## For End Users

### Option 1: Installer (Recommended)

The easiest way to install ShadowStrike is using the automated installer.

1. **Download** the latest installer:
   - Go to [Releases](https://github.com/MrShankarAryal/ShadowStrike/releases)
   - Download `ShadowStrike-Setup-v2.1.0.exe`

2. **Run** the installer:
   - Double-click the downloaded file
   - Accept the legal disclaimer
   - Follow the installation wizard
   - Choose installation location (default: `C:\Program Files\ShadowStrike`)
   - Select shortcuts (Desktop, Start Menu)

3. **Launch** ShadowStrike:
   - From Start Menu: Search for "ShadowStrike"
   - From Desktop: Double-click the ShadowStrike icon
   - Right-click and "Run as Administrator" for full functionality

**What You Get:**
- ✅ Automatic installation to Program Files
- ✅ Start Menu shortcut with eagle icon
- ✅ Optional Desktop shortcut
- ✅ Clean uninstall via Windows Settings
- ✅ All dependencies bundled (no .NET installation needed)

### Option 2: Portable Version

For users who prefer portable applications:

1. **Download** the portable version:
   - Go to [Releases](https://github.com/MrShankarAryal/ShadowStrike/releases)
   - Download `ShadowStrike-v2.1.0-Portable.zip`

2. **Extract** the ZIP file:
   - Right-click → Extract All
   - Choose any location (USB drive, Documents, etc.)

3. **Run** the application:
   - Navigate to the extracted folder
   - Double-click `ShadowStrike.UI.exe`
   - Right-click and "Run as Administrator" for full functionality

**What You Get:**
- ✅ No installation required
- ✅ Run from any location
- ✅ Perfect for USB drives
- ✅ Easy to move or delete

---

## For Developers

### Building from Source

If you want to modify the code or build from source:

**Prerequisites:**
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Windows 10/11
- Visual Studio 2022 or VS Code (optional)

**Steps:**

1. **Clone the repository:**
   ```bash
   git clone https://github.com/MrShankarAryal/ShadowStrike.git
   cd ShadowStrike
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run the application:**
   ```bash
   dotnet run --project ShadowStrike.UI/ShadowStrike.UI.csproj
   ```

### Creating Your Own Installer

See [RELEASE_GUIDE.md](RELEASE_GUIDE.md) for instructions on:
- Building the installer locally
- Using the automated CI/CD pipeline
- Creating releases

---

## System Requirements

- **Operating System**: Windows 10/11 (64-bit or 32-bit)
- **RAM**: 4GB minimum, 8GB recommended
- **Disk Space**: 200MB
- **Permissions**: Administrator privileges (for raw socket operations)
- **.NET Runtime**: Bundled with installer (no separate installation needed)

---

## Troubleshooting

### Installer Issues

**"Windows protected your PC" message:**
- Click "More info" → "Run anyway"
- This is normal for unsigned installers

**Installation fails:**
- Run installer as Administrator
- Disable antivirus temporarily
- Check disk space

### Application Issues

**App won't start:**
- Right-click → "Run as Administrator"
- Check Windows Event Viewer for errors

**Missing features:**
- Ensure running as Administrator
- Check firewall settings

**Antivirus blocks the app:**
- Add exception for ShadowStrike
- This is a security testing tool, false positives are common

---

## Uninstallation

### If Installed via Installer:
1. Open Windows Settings
2. Go to Apps → Installed apps
3. Find "ShadowStrike"
4. Click Uninstall

### If Using Portable Version:
- Simply delete the extracted folder

---

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/MrShankarAryal/ShadowStrike/issues)
- **Documentation**: See README.md
- **Email**: ShadowStrikeContact@shankararyal404.com.np
