# Deployment Guide

**Document Type**: Living Document - Deployment & Operations  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This guide provides comprehensive instructions for building, publishing, and deploying the Bellwood Global Mobile App to various platforms. It covers local development, CI/CD pipelines, and production app store deployments.

**Target Platforms**:
- ?? Android (Google Play Store)
- ?? iOS (Apple App Store)
- ?? Windows (Microsoft Store)
- ?? macOS (Mac App Store)

**Deployment Models**:
- Local development (debugging)
- Internal testing (Ad Hoc/TestFlight)
- App store production

---

## ??? Prerequisites

### Development Environment

**Required Software**:

| Software | Version | Purpose |
|----------|---------|---------|
| .NET SDK | 9.0+ | Runtime and build tools |
| Visual Studio 2022 | 17.8+ | IDE (Windows) |
| Xcode | 15+ | iOS/macOS builds (macOS only) |
| Android SDK | API 34+ | Android builds |
| Git | 2.x | Version control |

**Verification**:
```bash
dotnet --version
# Expected: 9.0.x

dotnet workload list
# Expected: maui, android, ios, maccatalyst
```

**Install MAUI Workload** (if missing):
```bash
dotnet workload install maui
```

---

### Platform-Specific Requirements

#### Android

**Required**:
- Android SDK (API Level 34)
- Android Emulator or physical device
- Java JDK 11+

**Setup**:
```bash
# Verify Android SDK
$env:ANDROID_HOME
# Expected: C:\Program Files\Android\android-sdk (or similar)

# Verify Java
java -version
# Expected: 11.x or higher
```

---

#### iOS/macOS

**Required** (macOS only):
- Xcode 15+
- Apple Developer Account
- iOS Simulator or physical device
- macOS 13+

**Setup**:
```bash
# Verify Xcode
xcode-select --version
# Expected: xcode-select version 2395 or higher

# Accept Xcode license
sudo xcodebuild -license accept
```

---

#### Windows

**Required**:
- Windows 11 SDK
- Windows 10 SDK (10.0.19041.0 or higher)

**Setup**:
```bash
# Verify Windows SDK
reg query "HKLM\SOFTWARE\Microsoft\Windows Kits\Installed Roots"
```

---

## ?? Local Development Setup

### Step 1: Clone Repository

```bash
git clone https://github.com/BidumanADT/BellwoodApp.git
cd BellwoodMobileApp/BellwoodGlobal.Mobile
```

---

### Step 2: Configure Settings

Create `appsettings.Development.json`:

```bash
cp appsettings.json appsettings.Development.json
```

Edit with your API keys:
```json
{
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "GooglePlacesApiKey": "AIzaSy...",
  "EnableDebugLogging": true
}
```

See `22-Configuration.md` for details.

---

### Step 3: Restore Dependencies

```bash
dotnet restore
```

---

### Step 4: Build & Run

**Android**:
```bash
dotnet build -t:Run -f net9.0-android
```

**iOS** (macOS only):
```bash
dotnet build -t:Run -f net9.0-ios
```

**Windows**:
```bash
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

**macOS** (macOS only):
```bash
dotnet build -t:Run -f net9.0-maccatalyst
```

---

## ?? Build for Release

### Android Release Build

**Step 1: Create Signing Key** (first time only)

```bash
# Generate keystore
keytool -genkey -v -keystore bellwood.keystore -alias bellwood -keyalg RSA -keysize 2048 -validity 10000

# Enter password and details when prompted
```

**Step 2: Configure Signing**

Edit `BellwoodGlobal.Mobile.csproj`:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>bellwood.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>bellwood</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>ENV:ANDROID_KEYSTORE_PASSWORD</AndroidSigningKeyPass>
  <AndroidSigningStorePass>ENV:ANDROID_KEYSTORE_PASSWORD</AndroidSigningStorePass>
</PropertyGroup>
```

**Step 3: Build APK/AAB**

```bash
# Set keystore password
export ANDROID_KEYSTORE_PASSWORD="your_password"

# Build APK (sideload)
dotnet build -c Release -f net9.0-android

# Build AAB (Google Play Store)
dotnet publish -c Release -f net9.0-android -p:AndroidPackageFormat=aab
```

**Output**:
- APK: `bin/Release/net9.0-android/com.bellwood.mobile-Signed.apk`
- AAB: `bin/Release/net9.0-android/publish/com.bellwood.mobile-Signed.aab`

---

### iOS Release Build

**Prerequisites**:
- Apple Developer Account
- Distribution Certificate
- Provisioning Profile

**Step 1: Configure Code Signing**

Edit `Info.plist`:
```xml
<key>CFBundleIdentifier</key>
<string>com.bellwood.mobile</string>
```

**Step 2: Archive for App Store**

```bash
# Build release archive
dotnet publish -c Release -f net9.0-ios -p:ArchiveOnBuild=true
```

**Step 3: Upload to App Store Connect**

```bash
# Option A: Xcode
# Open archive in Xcode ? Distribute App ? App Store Connect

# Option B: Command Line
xcrun altool --upload-app -f bin/Release/net9.0-ios/publish/BellwoodGlobal.ipa -u "your@apple.id" -p "app-specific-password"
```

---

### Windows Release Build

**Step 1: Create App Package**

```bash
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:GenerateAppxPackageOnBuild=true
```

**Step 2: Sign Package** (for Microsoft Store)

```bash
# Sign with certificate
signtool sign /fd SHA256 /a /f YourCertificate.pfx /p CertPassword bin/Release/net9.0-windows10.0.19041.0/publish/BellwoodGlobal.msix
```

---

### macOS Release Build

**Step 1: Build App Bundle**

```bash
dotnet publish -c Release -f net9.0-maccatalyst
```

**Step 2: Code Sign**

```bash
codesign --deep --force --verify --verbose --sign "Developer ID Application: Your Name" bin/Release/net9.0-maccatalyst/publish/BellwoodGlobal.app
```

---

## ?? CI/CD Pipeline

### GitHub Actions Workflow

**File**: `.github/workflows/mobile-build.yml`

```yaml
name: Mobile App Build

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-android:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Install MAUI workload
      run: dotnet workload install maui
    
    - name: Create appsettings.Development.json
      run: |
        echo '{
          "AdminApiUrl": "${{ secrets.ADMIN_API_URL }}",
          "AuthServerUrl": "${{ secrets.AUTH_SERVER_URL }}",
          "GooglePlacesApiKey": "${{ secrets.GOOGLE_PLACES_API_KEY }}"
        }' > BellwoodGlobal.Mobile/appsettings.Development.json
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build Android
      run: dotnet build -c Release -f net9.0-android
    
    - name: Publish APK
      run: dotnet publish -c Release -f net9.0-android
    
    - name: Upload APK artifact
      uses: actions/upload-artifact@v4
      with:
        name: android-apk
        path: bin/Release/net9.0-android/publish/*.apk

  build-ios:
    runs-on: macos-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Install MAUI workload
      run: dotnet workload install maui
    
    - name: Create appsettings.Development.json
      run: |
        echo '{
          "AdminApiUrl": "${{ secrets.ADMIN_API_URL }}",
          "AuthServerUrl": "${{ secrets.AUTH_SERVER_URL }}",
          "GooglePlacesApiKey": "${{ secrets.GOOGLE_PLACES_API_KEY }}"
        }' > BellwoodGlobal.Mobile/appsettings.Development.json
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build iOS
      run: dotnet build -c Release -f net9.0-ios
    
    - name: Archive
      run: dotnet publish -c Release -f net9.0-ios -p:ArchiveOnBuild=true
    
    - name: Upload IPA artifact
      uses: actions/upload-artifact@v4
      with:
        name: ios-ipa
        path: bin/Release/net9.0-ios/publish/*.ipa
```

---

### GitHub Secrets Setup

**Required Secrets** (Repository ? Settings ? Secrets ? Actions):

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `ADMIN_API_URL` | Production AdminAPI URL | `https://api.bellwood.com` |
| `AUTH_SERVER_URL` | Production AuthServer URL | `https://auth.bellwood.com` |
| `GOOGLE_PLACES_API_KEY` | Production Google Places API key | `AIzaSy...` |
| `ANDROID_KEYSTORE_PASSWORD` | Android signing keystore password | `secure_password` |
| `ANDROID_KEYSTORE_BASE64` | Base64-encoded keystore file | `MIIElQ...` |

---

## ?? App Store Deployment

### Google Play Store (Android)

**Step 1: Create App Listing**

1. Go to [Google Play Console](https://play.google.com/console)
2. Create App ? Fill app details
3. Upload app icon, screenshots, descriptions

**Step 2: Upload AAB**

```bash
# Build signed AAB
dotnet publish -c Release -f net9.0-android -p:AndroidPackageFormat=aab

# Upload to Play Console
# ? Release ? Production ? Create new release ? Upload AAB
```

**Step 3: Submit for Review**

- Complete all required sections
- Review rollout percentage (start with 10-20%)
- Submit for review

**Review Time**: 1-3 days

---

### Apple App Store (iOS)

**Step 1: Create App in App Store Connect**

1. Go to [App Store Connect](https://appstoreconnect.apple.com)
2. My Apps ? + ? New App
3. Fill app details (Bundle ID: `com.bellwood.mobile`)

**Step 2: Upload Build**

```bash
# Archive and upload
dotnet publish -c Release -f net9.0-ios -p:ArchiveOnBuild=true
xcrun altool --upload-app -f BellwoodGlobal.ipa -u "apple@id.com" -p "app-password"
```

**Step 3: Submit for Review**

- Select build in App Store Connect
- Complete app information
- Screenshots for all device sizes
- Privacy policy URL
- Submit for review

**Review Time**: 1-7 days

---

### Microsoft Store (Windows)

**Step 1: Create App Submission**

1. Go to [Partner Center](https://partner.microsoft.com/dashboard)
2. Create new app ? Reserve app name
3. Fill app details

**Step 2: Upload Package**

```bash
# Build MSIX
dotnet publish -c Release -f net9.0-windows10.0.19041.0 -p:GenerateAppxPackageOnBuild=true

# Upload to Partner Center
# ? Packages ? Upload MSIX
```

**Step 3: Submit**

- Complete certification requirements
- Submit for certification

**Review Time**: 1-5 days

---

## ? Pre-Deployment Checklist

### Code Quality

- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] No build warnings
- [ ] Code coverage >80%

### Configuration

- [ ] `appsettings.Development.json` NOT in Git
- [ ] Production URLs configured
- [ ] API keys stored securely
- [ ] SSL certificates valid

### Testing

- [ ] Tested on Android emulator
- [ ] Tested on iOS simulator
- [ ] Tested on physical device
- [ ] All features working
- [ ] No crash reports

### Security

- [ ] API keys restricted (platform + API)
- [ ] HTTPS only
- [ ] JWT authentication working
- [ ] Data isolation verified

### App Store Requirements

- [ ] App icon (1024x1024)
- [ ] Screenshots (all device sizes)
- [ ] Privacy policy URL
- [ ] Description & keywords
- [ ] App version incremented

---

## ?? Troubleshooting

### Build Failures

**Issue**: "MAUI workload not found"

**Solution**:
```bash
dotnet workload install maui
```

---

**Issue**: "Android SDK not found"

**Solution**:
```bash
# Set ANDROID_HOME environment variable
export ANDROID_HOME="/path/to/android-sdk"
```

---

**Issue**: "iOS build requires macOS"

**Solution**:
- iOS builds only work on macOS
- Use GitHub Actions with `macos-latest` runner

---

### Signing Issues

**Issue**: "Keystore password incorrect"

**Solution**:
```bash
# Verify keystore password
keytool -list -v -keystore bellwood.keystore
```

---

**Issue**: "Provisioning profile not found"

**Solution**:
- Download provisioning profile from Apple Developer
- Install in Xcode: Settings ? Account ? Download Manual Profiles

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing before deployment
- **[22-Configuration.md](22-Configuration.md)** - Production configuration
- **[23-Security-Model.md](23-Security-Model.md)** - Security requirements
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common deployment issues

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
