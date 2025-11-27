# GitHub Push Guide for ShadowStrike

## Restructuring Complete

Your project has been successfully restructured:

```
ShadowStrike/                 ← Root directory (ready for GitHub)
├── ShadowStrike.Core/        ← Core business logic
├── ShadowStrike.UI/          ← WPF user interface  
├── ShadowStrike.sln          ← Solution file
├── README.md                 ← Comprehensive documentation
├── LICENSE                   ← MIT License
└── .gitignore                ← Git ignore rules
```

## Push to GitHub - Step by Step

### 1. Navigate to Project Directory
```powershell
cd "C:\Users\Shankar Aryal\OneDrive\Desktop\ShadowStrike"
```

### 2. Initialize Git Repository
```powershell
git init
```

### 3. Add All Files
```powershell
git add .
```

### 4. Create Initial Commit
```powershell
git commit -m "Initial commit - ShadowStrike v2.0

- Complete .NET 8 rewrite with WPF UI
- OSINT & reconnaissance engine
- DDoS attack modules (HTTP, SYN, UDP Flood)
- Injection testing (SQL, File Upload)
- Persistent logging & history
- Comprehensive help documentation
- Material Design dark theme UI"
```

### 5. Add Remote Repository
```powershell
git remote add origin https://github.com/MrShankarAryal/ShadowStrike.git
```

### 6. Push to GitHub
```powershell
# If this is a fresh repo or you want to overwrite
git push -u origin main --force

# OR if you want to push to a new branch first
git branch -M main
git push -u origin main
```

## After Pushing

### Create a Release on GitHub
1. Go to: https://github.com/MrShankarAryal/ShadowStrike/releases
2. Click "Create a new release"
3. Tag version: `v2.0.0`
4. Release title: `ShadowStrike v2.0 - Complete Rewrite`
5. Description: Copy from README.md
6. Publish release

### Update Repository Settings
1. Go to repository Settings
2. Add description: "Advanced Security Testing & Penetration Testing Platform"
3. Add website: Your personal site (optional)
4. Add topics: 
   - `security`
   - `penetration-testing`
   - `dotnet`
   - `csharp`
   - `wpf`
   - `osint`
   - `ddos`
   - `sql-injection`
   - `cybersecurity`
   - `ethical-hacking`

### Enable Features
- Issues
- Discussions (optional)
- Projects (optional)
- Wiki (optional)

## Verify Everything Works

### Test Clone and Build
```powershell
# In a different directory
git clone https://github.com/MrShankarAryal/ShadowStrike.git
cd ShadowStrike
dotnet restore
dotnet build --configuration Release
dotnet run --project ShadowStrike.UI/ShadowStrike.UI.csproj
```

## Add Screenshots (Optional)

Create `docs/screenshots/` folder and add:
- `dashboard.png`
- `ddos.png`
- `injection.png`
- `logs.png`

Then update README.md image paths.

## Important Reminders

- No API keys or secrets in code
- No personal data in logs
- Legal disclaimer present
- MIT License included
- .gitignore configured

## You're Ready!

Your project is now properly structured and ready to push to GitHub!

---

**Repository**: https://github.com/MrShankarAryal/ShadowStrike
**Author**: Shankar Aryal
**Version**: 2.0
**License**: MIT
