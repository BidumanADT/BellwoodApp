# Configuration

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Bellwood Global Mobile App uses a flexible configuration system based on `appsettings.json` files, environment variables, and secure storage. This document covers all configuration options, setup procedures, and security best practices.

**Configuration Sources** (in priority order):
1. `appsettings.json` - Base configuration template
2. `appsettings.Development.json` - Local development overrides
3. Environment variables - Production deployment
4. Secure storage - Sensitive runtime data (tokens, etc.)

---

## ?? Configuration Files

### appsettings.json (Base Template)

**Location**: `BellwoodGlobal.Mobile/appsettings.json`

**Purpose**: Base configuration with environment variable placeholders

**Status**: ? **SAFE TO COMMIT** (contains no secrets)

**Contents**:
```json
{
  "AdminApiUrl": "ENV:ADMIN_API_URL",
  "AuthServerUrl": "ENV:AUTH_SERVER_URL",
  "GooglePlacesApiKey": "ENV:GOOGLE_PLACES_API_KEY",
  "LocationUpdateIntervalSeconds": 15,
  "QuotePollingIntervalSeconds": 30,
  "EnableDebugLogging": false
}
```

**Environment Variable Resolution**:
- Prefix `ENV:` indicates value should be loaded from environment variable
- Example: `ENV:ADMIN_API_URL` ? reads `$ADMIN_API_URL`
- Falls back to placeholder if environment variable not found

---

### appsettings.Development.json (Local Development)

**Location**: `BellwoodGlobal.Mobile/appsettings.Development.json`

**Purpose**: Local development configuration with actual values

**Status**: ?? **NEVER COMMIT** (contains secrets, in `.gitignore`)

**Contents**:
```json
{
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "GooglePlacesApiKey": "AIzaSyCDu1...",
  "EnableDebugLogging": true
}
```

**Setup for New Developers**:
```bash
# 1. Copy template
cp appsettings.json appsettings.Development.json

# 2. Edit with your actual values
# Replace ENV:GOOGLE_PLACES_API_KEY with your key

# 3. Verify it's in .gitignore
cat .gitignore | grep appsettings.Development.json
```

---

## ?? Configuration Options

### API Endpoints

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `AdminApiUrl` | string | Yes | - | AdminAPI base URL |
| `AuthServerUrl` | string | Yes | - | Authentication server URL |

**Production Values**:
```json
{
  "AdminApiUrl": "https://api.bellwood.com",
  "AuthServerUrl": "https://auth.bellwood.com"
}
```

**Development Values**:
```json
{
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001"
}
```

---

### Google Places API

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `GooglePlacesApiKey` | string | Yes | - | Google Places API key (restricted) |
| `GooglePlacesRegionCode` | string | No | `"US"` | Region bias for autocomplete |

**Security**:
- Key must be restricted by platform (Android package name, iOS bundle ID)
- Key must be restricted to Places API (New) only
- Never commit actual key to Git

See `10-Google-Places-Autocomplete.md` for API key setup.

---

### Performance Tuning

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `LocationUpdateIntervalSeconds` | int | No | 15 | GPS tracking polling interval |
| `QuotePollingIntervalSeconds` | int | No | 30 | Quote status polling interval |

**Recommended Values**:
- **Development**: 10-15 seconds (faster feedback)
- **Production**: 15-30 seconds (balance between freshness and battery/data)

**Example**:
```json
{
  "LocationUpdateIntervalSeconds": 15,
  "QuotePollingIntervalSeconds": 30
}
```

---

### Debugging

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `EnableDebugLogging` | bool | No | false | Enable verbose console logging |

**Usage**:
```json
{
  "EnableDebugLogging": true  // Development only
}
```

**Effect**:
```csharp
#if DEBUG
if (config.EnableDebugLogging)
{
    System.Diagnostics.Debug.WriteLine($"[QuoteDashboard] Refreshed: {quotes.Count} quotes");
}
#endif
```

---

## ?? Secure Storage

### Purpose

Store sensitive runtime data that shouldn't be in configuration files:
- JWT access tokens
- Refresh tokens
- User session data
- API keys (runtime)

### API

```csharp
// Store securely
await SecureStorage.SetAsync("access_token", token);

// Retrieve
string token = await SecureStorage.GetAsync("access_token");

// Remove
SecureStorage.Remove("access_token");

// Remove all
SecureStorage.RemoveAll();
```

### Platform Implementation

**Android**:
- Uses Android Keystore
- Hardware-backed encryption (if available)
- Survives app uninstall (optional)

**iOS**:
- Uses iOS Keychain
- Encrypted with device password
- iCloud sync (optional)

**Windows**:
- Uses Windows Credential Locker
- Per-user encrypted storage

---

## ?? Deployment Configuration

### Development Environment

**Setup**:
1. Create `appsettings.Development.json`
2. Add actual API keys
3. Build and run

**File**: `appsettings.Development.json`
```json
{
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "GooglePlacesApiKey": "AIzaSyCDu1...",
  "EnableDebugLogging": true
}
```

---

### Production Environment

**Option A: Environment Variables** ? **RECOMMENDED**

**Set before deployment**:
```bash
# Linux/macOS
export ADMIN_API_URL="https://api.bellwood.com"
export AUTH_SERVER_URL="https://auth.bellwood.com"
export GOOGLE_PLACES_API_KEY="AIzaSyProduction..."

# Windows PowerShell
$env:ADMIN_API_URL = "https://api.bellwood.com"
$env:AUTH_SERVER_URL = "https://auth.bellwood.com"
$env:GOOGLE_PLACES_API_KEY = "AIzaSyProduction..."
```

**appsettings.json** (committed to Git):
```json
{
  "AdminApiUrl": "ENV:ADMIN_API_URL",
  "AuthServerUrl": "ENV:AUTH_SERVER_URL",
  "GooglePlacesApiKey": "ENV:GOOGLE_PLACES_API_KEY"
}
```

App automatically resolves `ENV:` prefixes to environment variables.

---

**Option B: Azure App Configuration**

```csharp
// Load from Azure App Configuration
var azureAppConfigEndpoint = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_ENDPOINT");

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(azureAppConfigEndpoint)
           .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()));
});
```

**Advantages**:
- Centralized configuration
- Key Vault integration
- Dynamic updates without redeployment

---

**Option C: CI/CD Secrets Injection**

**GitHub Actions Example**:
```yaml
- name: Create appsettings.Development.json
  run: |
    echo '{
      "AdminApiUrl": "${{ secrets.ADMIN_API_URL }}",
      "AuthServerUrl": "${{ secrets.AUTH_SERVER_URL }}",
      "GooglePlacesApiKey": "${{ secrets.GOOGLE_PLACES_API_KEY }}"
    }' > BellwoodGlobal.Mobile/appsettings.Development.json

- name: Build
  run: dotnet build --configuration Release
```

**GitHub Secrets Setup**:
1. Repository ? Settings ? Secrets ? Actions
2. Add:
   - `ADMIN_API_URL`
   - `AUTH_SERVER_URL`
   - `GOOGLE_PLACES_API_KEY`

---

## ?? Security Best Practices

### ? DO

**1. Use Environment Variables in Production**
```bash
export GOOGLE_PLACES_API_KEY="AIzaSy..."
```

**2. Restrict API Keys by Platform**
- Android: Package name + SHA-1 fingerprint
- iOS: Bundle ID
- API restrictions: Places API (New) only

**3. Keep Development Config Out of Git**
```bash
# .gitignore
appsettings.Development.json
```

**4. Rotate Keys Regularly**
- Quarterly rotation schedule
- Immediate rotation if compromised
- Separate keys for dev/staging/prod

**5. Use Secure Storage for Tokens**
```csharp
await SecureStorage.SetAsync("access_token", token);
```

---

### ? DON'T

**1. Never Hardcode Secrets**
```csharp
// ? BAD
var apiKey = "AIzaSyCDu1..."; 

// ? GOOD
var apiKey = await _config.GetGooglePlacesApiKeyAsync();
```

**2. Never Commit appsettings.Development.json**
```bash
# Verify it's ignored
git check-ignore appsettings.Development.json
# Expected: appsettings.Development.json
```

**3. Never Share Keys in Chat/Email**
- Use secure password managers
- Use environment variables
- Use Azure Key Vault

**4. Never Use Production Keys in Development**
- Separate keys for each environment
- Prevents accidental quota exhaustion

---

## ?? Testing Configuration

### Verify Configuration Loads

```csharp
// ConfigurationService test
[Fact]
public async Task Configuration_LoadsSuccessfully()
{
    var config = new ConfigurationService();
    
    var adminApiUrl = await config.GetAdminApiUrlAsync();
    
    Assert.NotNull(adminApiUrl);
    Assert.StartsWith("https://", adminApiUrl);
}
```

---

### Verify Secrets Not Exposed

```csharp
// Ensure production config doesn't contain actual secrets
[Fact]
public void AppsettingsJson_DoesNotContainSecrets()
{
    var json = File.ReadAllText("appsettings.json");
    
    // All sensitive values should use ENV: prefix
    Assert.Contains("ENV:GOOGLE_PLACES_API_KEY", json);
    Assert.DoesNotContain("AIzaSy", json); // Not actual key
}
```

---

### Manual Verification

**Check loaded values in debug console**:
```csharp
#if DEBUG
var config = ServiceHelper.GetRequiredService<IConfigurationService>();
var apiKey = await config.GetGooglePlacesApiKeyAsync();

System.Diagnostics.Debug.WriteLine($"API Key loaded: {apiKey.Substring(0, 10)}...");
// Expected: API Key loaded: AIzaSyCDu1...
#endif
```

---

## ?? Troubleshooting

### Issue: "Configuration file not found"

**Symptoms**:
- App crashes on startup
- Error: "Could not load appsettings.json"

**Cause**: File not marked as `MauiAsset`

**Solution**:
```xml
<!-- BellwoodGlobal.Mobile.csproj -->
<ItemGroup>
  <MauiAsset Include="appsettings.json" />
  <MauiAsset Include="appsettings.Development.json" />
</ItemGroup>
```

---

### Issue: "Google Places API key not found"

**Symptoms**:
- Autocomplete doesn't work
- Error: "API key is null or empty"

**Cause**: `appsettings.Development.json` missing or ENV variable not set

**Solution**:

**Development**:
```bash
# Create appsettings.Development.json with actual key
echo '{"GooglePlacesApiKey": "AIzaSy..."}' > appsettings.Development.json
```

**Production**:
```bash
# Set environment variable
export GOOGLE_PLACES_API_KEY="AIzaSy..."
```

---

### Issue: "Environment variable not found"

**Symptoms**:
- Production app can't connect to API
- Error: "Environment variable 'ADMIN_API_URL' not found"

**Cause**: Environment variable not set before app starts

**Solution**:
```bash
# Verify variable is set
echo $ADMIN_API_URL
# Expected: https://api.bellwood.com

# Set if missing
export ADMIN_API_URL="https://api.bellwood.com"
```

---

### Issue: "Invalid API key" (403 Forbidden)

**Symptoms**:
- Google Places API returns 403
- Error: "REQUEST_DENIED"

**Cause**: API key restrictions don't match app

**Solution**:
1. Google Cloud Console ? Credentials
2. Edit API key restrictions
3. Verify:
   - **Android**: Package name = `com.bellwood.mobile`, SHA-1 correct
   - **iOS**: Bundle ID = `com.bellwood.mobile`
   - **APIs**: Places API (New) enabled

---

## ?? Configuration Priority

**Load Order** (later overrides earlier):

1. **appsettings.json** (base template)
2. **appsettings.Development.json** (local dev overrides, if exists)
3. **Environment Variables** (if `ENV:` prefix used)
4. **Secure Storage** (runtime secrets)

**Example**:

```json
// appsettings.json
{
  "AdminApiUrl": "ENV:ADMIN_API_URL",
  "EnableDebugLogging": false
}
```

```json
// appsettings.Development.json (overrides)
{
  "AdminApiUrl": "https://localhost:5206",
  "EnableDebugLogging": true
}
```

```bash
# Environment variable (overrides both files if ENV: prefix)
export ADMIN_API_URL="https://staging.bellwood.com"
```

**Final Result**:
- `AdminApiUrl` = `"https://localhost:5206"` (from Development.json)
- `EnableDebugLogging` = `true` (from Development.json)

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** - Google Places setup
- **[20-API-Integration.md](20-API-Integration.md)** - AdminAPI configuration
- **[23-Security-Model.md](23-Security-Model.md)** - Security best practices
- **[30-Deployment-Guide.md](30-Deployment-Guide.md)** - Production deployment
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common configuration issues

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
