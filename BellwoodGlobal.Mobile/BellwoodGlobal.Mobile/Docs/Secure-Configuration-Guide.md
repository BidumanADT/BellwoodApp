# ?? Secure Configuration Guide

**Date:** January 2, 2026  
**Purpose:** Protect API keys and sensitive configuration  
**Status:** ? **IMPLEMENTED**  

---

## ?? **Overview**

All sensitive configuration (API keys, URLs) has been moved out of source code into external configuration files that are **NOT committed to Git**.

---

## ?? **Configuration Files**

### **appsettings.json** ? **SAFE TO COMMIT**

**Location:** `BellwoodGlobal.Mobile/appsettings.json`

**Purpose:** Production template with environment variable placeholders

**Contents:**
```json
{
  "GooglePlacesApiKey": "ENV:GOOGLE_PLACES_API_KEY",
  "AdminApiUrl": "ENV:ADMIN_API_URL",
  "AuthServerUrl": "ENV:AUTH_SERVER_URL",
  "RidesApiUrl": "ENV:RIDES_API_URL"
}
```

**This file IS committed to Git** - it contains no actual secrets, only references to environment variables.

---

### **appsettings.Development.json** ?? **NEVER COMMIT**

**Location:** `BellwoodGlobal.Mobile/appsettings.Development.json`

**Purpose:** Local development configuration with actual keys

**Contents:**
```json
{
  "GooglePlacesApiKey": "AIzaSyCDu1jdljMdXvcl9tG7O6cJBw8f2h0sUIY",
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "RidesApiUrl": "https://localhost:5005"
}
```

**?? THIS FILE IS IN .GITIGNORE** - It contains actual API keys and should never be committed.

---

## ?? **Setup for Developers**

### **Step 1: Copy Template**

```bash
# After cloning the repo, create your local config file
cp appsettings.json appsettings.Development.json
```

### **Step 2: Add Your API Keys**

Edit `appsettings.Development.json`:

```json
{
  "GooglePlacesApiKey": "YOUR_ACTUAL_API_KEY_HERE",
  "AdminApiUrl": "https://localhost:5206",
  "AuthServerUrl": "https://localhost:5001",
  "RidesApiUrl": "https://localhost:5005"
}
```

### **Step 3: Build and Run**

```bash
dotnet build
dotnet run
```

The app will automatically load `appsettings.Development.json` and use your keys.

---

## ?? **Production Deployment**

### **Option A: Environment Variables** ? **RECOMMENDED**

**Set environment variables before running the app:**

```bash
# Linux/macOS
export GOOGLE_PLACES_API_KEY="your_production_key"
export ADMIN_API_URL="https://api.bellwood.com"
export AUTH_SERVER_URL="https://auth.bellwood.com"
export RIDES_API_URL="https://rides.bellwood.com"

# Windows PowerShell
$env:GOOGLE_PLACES_API_KEY="your_production_key"
$env:ADMIN_API_URL="https://api.bellwood.com"
$env:AUTH_SERVER_URL="https://auth.bellwood.com"
$env:RIDES_API_URL="https://rides.bellwood.com"
```

**The app will read from `appsettings.json`, see `ENV:` prefixes, and look up the environment variables.**

---

### **Option B: Azure App Configuration**

For Azure deployments:

1. Store secrets in **Azure Key Vault**
2. Reference them in **Azure App Configuration**
3. Update `ConfigurationService` to read from Azure SDK

---

### **Option C: GitHub Secrets (CI/CD)**

For automated builds:

1. Go to **Settings** ? **Secrets** ? **Actions**
2. Add secrets:
   - `GOOGLE_PLACES_API_KEY`
   - `ADMIN_API_URL`
   - `AUTH_SERVER_URL`
   - `RIDES_API_URL`

3. Update build workflow to inject secrets:

```yaml
- name: Create appsettings.Development.json
  run: |
    echo '{
      "GooglePlacesApiKey": "${{ secrets.GOOGLE_PLACES_API_KEY }}",
      "AdminApiUrl": "${{ secrets.ADMIN_API_URL }}",
      "AuthServerUrl": "${{ secrets.AUTH_SERVER_URL }}",
      "RidesApiUrl": "${{ secrets.RIDES_API_URL }}"
    }' > BellwoodGlobal.Mobile/appsettings.Development.json
```

---

## ?? **Security Best Practices**

### ? **DO:**
- Keep `appsettings.Development.json` in `.gitignore`
- Use environment variables for production
- Rotate API keys regularly
- Use separate keys for dev/staging/production
- Store production keys in Azure Key Vault or similar

### ? **DON'T:**
- Commit `appsettings.Development.json` to Git
- Hardcode keys in source code
- Share keys in Slack/email/chat
- Use production keys in development
- Commit `.env` files

---

## ?? **Testing**

### **Verify Configuration Loads**

```csharp
// In debug console, check loaded values
var config = ServiceHelper.GetRequiredService<IConfigurationService>();
var apiKey = config.GetPlacesApiKey();
Console.WriteLine($"API Key loaded: {apiKey.Substring(0, 10)}...");
```

**Expected Output:**
```
API Key loaded: AIzaSyCDu1...
```

---

## ?? **Troubleshooting**

### **Error: "Google Places API key not found"**

**Cause:** `appsettings.Development.json` is missing or empty

**Fix:**
1. Copy `appsettings.json` to `appsettings.Development.json`
2. Replace `ENV:GOOGLE_PLACES_API_KEY` with your actual key
3. Rebuild the app

---

### **Error: "Environment variable 'GOOGLE_PLACES_API_KEY' not found"**

**Cause:** Production app expects environment variable but it's not set

**Fix:**
```bash
# Set the environment variable
export GOOGLE_PLACES_API_KEY="your_key_here"
```

---

### **Build Error: "Could not load appsettings.json"**

**Cause:** File not marked as `MauiAsset` in `.csproj`

**Fix:**
Verify in `BellwoodGlobal.Mobile.csproj`:
```xml
<ItemGroup>
  <MauiAsset Include="appsettings.json" />
  <MauiAsset Include="appsettings.Development.json" />
</ItemGroup>
```

---

## ?? **Configuration Priority**

**Load Order (later overrides earlier):**

1. `appsettings.json` (template with `ENV:` placeholders)
2. `appsettings.Development.json` (local dev keys, if exists)
3. Environment variables (if referenced with `ENV:` prefix)

**Example:**

```json
// appsettings.json
{
  "GooglePlacesApiKey": "ENV:GOOGLE_PLACES_API_KEY"
}
```

```json
// appsettings.Development.json (overrides)
{
  "GooglePlacesApiKey": "AIzaSyCDu1jdljMdXvcl9tG7O6cJBw8f2h0sUIY"
}
```

**Result:** Development uses the actual key from `appsettings.Development.json`.

---

## ?? **Migrating Existing Code**

### **Before (? Insecure):**

```csharp
// MauiProgram.cs
c.DefaultRequestHeaders.Add("X-Goog-Api-Key", "AIzaSyCDu...");
```

### **After (? Secure):**

```csharp
// MauiProgram.cs
var configService = serviceProvider.GetRequiredService<IConfigurationService>();
var apiKey = configService.GetPlacesApiKey();
c.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
```

---

## ?? **Checklist for New Developers**

- [ ] Clone repository
- [ ] Create `appsettings.Development.json` from template
- [ ] Add your API keys
- [ ] Verify `.gitignore` excludes `appsettings.Development.json`
- [ ] Build and run app
- [ ] Confirm autocomplete works
- [ ] **NEVER commit `appsettings.Development.json`**

---

## ?? **Summary**

| File | Committed to Git? | Contains Secrets? | Purpose |
|------|-------------------|-------------------|---------|
| `appsettings.json` | ? Yes | ? No | Template with `ENV:` placeholders |
| `appsettings.Development.json` | ? NO | ? Yes | Local dev keys (gitignored) |
| Environment Variables | N/A | ? Yes | Production deployment |

**Your API keys are now secure!** ??

---

**Last Updated:** January 2, 2026  
**Status:** ? **PRODUCTION READY**
