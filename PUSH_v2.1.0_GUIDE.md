# Push Guide for v2.1.0 Release

## Quick Start

Follow these steps to push your changes and create the v2.1.0 release:

### Step 1: Review Changes

Check what files have been modified:
```bash
git status
```

You should see:
- `.github/workflows/release.yml` (new)
- `RELEASE_GUIDE.md` (new)
- `INSTALLER_GUIDE.md` (updated)
- `README.md` (updated)
- `CHANGELOG.md` (updated)
- `setup.iss` (updated)

### Step 2: Stage All Changes

```bash
git add .
```

### Step 3: Commit Changes

```bash
git commit -m "Release v2.1.0: Add automated CI/CD and update documentation

- Add GitHub Actions workflow for automated releases
- Update installer to v2.1.0 with all current features
- Modernize installation documentation to use GitHub Releases
- Fix installer icon display for shortcuts
- Add comprehensive release guide
- Update CHANGELOG with v2.1.0 changes"
```

### Step 4: Push to GitHub

```bash
git push origin main
```

### Step 5: Create and Push Version Tag

```bash
# Create the tag
git tag v2.1.0

# Push the tag (this triggers the automated build!)
git push origin v2.1.0
```

### Step 6: Monitor the Build

1. Go to: https://github.com/MrShankarAryal/ShadowStrike/actions
2. You'll see "Build and Release" workflow running
3. Wait 5-10 minutes for it to complete
4. Check for any errors in the build log

### Step 7: Verify the Release

Once the build completes:

1. Go to: https://github.com/MrShankarAryal/ShadowStrike/releases
2. You should see a new release "v2.1.0"
3. Verify it contains:
   - `ShadowStrike-Setup-v2.1.0.exe` (installer)
   - `ShadowStrike-v2.1.0-Portable.zip` (portable version)

---

## Complete Command Sequence

Copy and paste these commands one by one:

```bash
# Check status
git status

# Stage all changes
git add .

# Commit
git commit -m "Release v2.1.0: Add automated CI/CD and update documentation"

# Push changes
git push origin main

# Create tag
git tag v2.1.0

# Push tag (triggers automated build)
git push origin v2.1.0
```

---

## What Happens After Pushing the Tag?

GitHub Actions will automatically:

1. ✅ Checkout your code
2. ✅ Setup .NET 8
3. ✅ Restore dependencies
4. ✅ Build and publish the application
5. ✅ Download and install Inno Setup
6. ✅ Build the installer
7. ✅ Create the portable ZIP
8. ✅ Create a GitHub Release
9. ✅ Upload both files to the release

**Total time**: ~5-10 minutes

---

## Troubleshooting

### If the build fails:

1. Check the Actions tab for error logs
2. Common issues:
   - Missing files in repository
   - Incorrect paths in setup.iss
   - .NET build errors

### If you need to fix and re-release:

```bash
# Make your fixes
git add .
git commit -m "Fix: [describe the fix]"
git push

# Delete the old tag locally and remotely
git tag -d v2.1.0
git push origin :refs/tags/v2.1.0

# Create and push the tag again
git tag v2.1.0
git push origin v2.1.0
```

---

## Future Releases

For future releases (v2.2.0, v2.3.0, etc.):

1. Make your changes
2. Update version in:
   - `setup.iss` (AppVersion and OutputBaseFilename)
   - `README.md` (version badge)
   - `CHANGELOG.md` (add new entry)
3. Follow the same push process with the new version tag

---

## That's It!

Your release process is now fully automated. Just push a tag and GitHub does the rest! 🚀
