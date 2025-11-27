# Release Guide

## Automated Release Process

ShadowStrike uses GitHub Actions to automatically build and release installers. No manual building required!

## How to Create a New Release

### Method 1: Tag-Based Release (Recommended)

1. **Update version numbers** in your code if needed

2. **Commit and push your changes:**
   ```bash
   git add .
   git commit -m "Prepare for release v2.1.0"
   git push origin main
   ```

3. **Create and push a version tag:**
   ```bash
   git tag v2.1.0
   git push origin v2.1.0
   ```

4. **That's it!** GitHub Actions will automatically:
   - Build the application
   - Create the Inno Setup installer
   - Create the portable ZIP
   - Publish a new release with both files

### Method 2: Manual Trigger

1. Go to: https://github.com/MrShankarAryal/ShadowStrike/actions
2. Click on "Build and Release" workflow
3. Click "Run workflow"
4. Select branch and click "Run workflow"

## What Gets Built Automatically

When you push a tag, GitHub Actions creates:

1. **Installer**: `ShadowStrike-Setup-v2.0.exe`
   - Full Windows installer with shortcuts
   - Includes all dependencies
   - Professional installation experience

2. **Portable ZIP**: `ShadowStrike-v2.1.0-Portable.zip`
   - Extract and run
   - No installation needed
   - Perfect for portable use

## Version Numbering

Use semantic versioning: `vMAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (v2.0.0 → v3.0.0)
- **MINOR**: New features (v2.0.0 → v2.1.0)
- **PATCH**: Bug fixes (v2.0.0 → v2.0.1)

Examples:
```bash
git tag v2.0.1  # Bug fix release
git tag v2.1.0  # New feature release
git tag v3.0.0  # Major version with breaking changes
```

## Monitoring the Build

1. Go to: https://github.com/MrShankarAryal/ShadowStrike/actions
2. Click on the running workflow
3. Watch the build progress in real-time
4. Build takes approximately 5-10 minutes

## After Release

The release will appear at:
https://github.com/MrShankarAryal/ShadowStrike/releases

Users can download:
- The installer (`.exe`)
- The portable version (`.zip`)

## Troubleshooting

### Build Fails
- Check the Actions tab for error logs
- Ensure all dependencies are in the repository
- Verify `setup.iss` paths are correct

### Release Not Created
- Ensure you pushed the tag: `git push origin v2.1.0`
- Check that the tag starts with `v` (e.g., `v2.1.0` not `2.1.0`)
- Verify GitHub Actions is enabled in repository settings

## Local Testing (Optional)

If you want to test locally before releasing:

```bash
# Build
dotnet publish -c Release -r win-x64 --self-contained true -o publish

# Create installer (requires Inno Setup installed)
iscc.exe setup.iss

# Test the installer
.\installer\ShadowStrike-Setup-v2.0.exe
```

## Professional Workflow Summary

```
1. Make changes → Commit → Push
2. Create tag → Push tag
3. GitHub Actions builds everything automatically
4. Release appears with installer + portable ZIP
5. Users download and install
```

**No manual building required!** 🚀
