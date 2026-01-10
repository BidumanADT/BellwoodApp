# How-To: Google Cloud Console Setup

**Task:** Configure Google Cloud for Places API  
**Last Updated:** January 10, 2026  
**Time Required:** ~20 minutes

---

## ?? Overview

This guide walks you through setting up Google Cloud Console for the Bellwood Elite mobile app's Places Autocomplete feature.

**What you'll configure:**
- Google Cloud Project
- Places API (New) enablement
- API key creation
- API key restrictions
- Quota monitoring

---

## ?? Prerequisites

- Google account with billing enabled
- Access to Google Cloud Console
- Package name (Android): `com.bellwoodglobal.mobile`
- Bundle ID (iOS): `com.bellwoodglobal.mobile`

---

## ?? Step-by-Step Instructions

### Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click **Select a project** dropdown ? **New Project**
3. Enter details:
   - **Project name:** `BellwoodElite-Mobile`
   - **Organization:** (leave as-is or select your org)
4. Click **Create**
5. Wait for project creation (~30 seconds)

**? Success:** Project created

---

### Step 2: Enable Places API (New)

1. In the Cloud Console, go to **APIs & Services** ? **Library**
2. Search for: `Places API (New)`
3. Click **Places API (New)** (not the old "Places API")
4. Click **Enable**
5. Wait for enablement (~10 seconds)

**? Success:** API enabled

**?? Important:** Make sure you enable **Places API (New)**, not the old deprecated API.

---

### Step 3: Create API Key

1. Go to **APIs & Services** ? **Credentials**
2. Click **+ Create Credentials** ? **API Key**
3. API key created (e.g., `AIzaSyB...`)
4. **Copy the key immediately** (you'll need it later)

**? Success:** API key created

**?? Security Note:** Restrict the key in the next step before using it.

---

### Step 4: Restrict API Key (Android)

1. On the API key screen, click **Edit API key**
2. **Application restrictions:**
   - Select: **Android apps**
   - Click **+ Add an item**
   - **Package name:** `com.bellwoodglobal.mobile`
   - **SHA-1 certificate fingerprint:** (see below)
3. **API restrictions:**
   - Select: **Restrict key**
   - Check: **Places API (New)**
   - Uncheck all others
4. Click **Save**

**? Success:** API key restricted for Android

---

#### How to Get SHA-1 Fingerprint (Android)

**Debug (Development):**
```bash
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
```

Look for: `SHA1: AB:CD:EF:...`

**Release (Production):**
```bash
keytool -list -v -keystore /path/to/your/release-keystore.jks -alias your-key-alias
```

---

### Step 5: Restrict API Key (iOS)

1. Click **Edit API key** again
2. **Application restrictions:**
   - Select: **iOS apps**
   - Click **+ Add an item**
   - **Bundle identifier:** `com.bellwoodglobal.mobile`
3. **API restrictions:**
   - Same as Android (Places API (New) only)
4. Click **Save**

**? Success:** API key restricted for iOS

**?? Tip:** You can add both Android and iOS restrictions to the same key.

---

### Step 6: Set Quota Limits (Optional but Recommended)

1. Go to **APIs & Services** ? **Quotas**
2. Search for: `Places API (New)`
3. Find: **Autocomplete (New) - Per Day**
4. Click **Edit Quotas**
5. Set daily limit (e.g., `100 requests/day`)
6. Click **Save**

**? Success:** Quota limit set

**?? Cost Management:** Free tier is 100 requests/day. Set a limit to avoid unexpected charges.

---

### Step 7: Enable Billing Alerts

1. Go to **Billing** ? **Budgets & alerts**
2. Click **Create budget**
3. Set budget (e.g., `$10/month`)
4. Set alert threshold (e.g., `50%`, `90%`, `100%`)
5. Add email for notifications
6. Click **Finish**

**? Success:** Billing alerts configured

---

### Step 8: Add API Key to Mobile App

1. Copy API key from Step 3
2. Open `appsettings.Development.json` (local, not committed)
3. Add:
   ```json
   {
     "GooglePlacesApiKey": "AIzaSyB...",
     "AdminApiUrl": "...",
     "AuthServerUrl": "...",
     "RidesApiUrl": "..."
   }
   ```
4. Save file
5. Build and run app

**? Success:** API key integrated

**?? Security:** Never commit `appsettings.Development.json` to Git. It's in `.gitignore`.

---

## ?? Verification Checklist

After setup, verify:
- [ ] Places API (New) enabled
- [ ] API key created
- [ ] Android restrictions set (package name + SHA-1)
- [ ] iOS restrictions set (bundle ID)
- [ ] API scope restricted to Places API (New) only
- [ ] Quota limit set (100/day or custom)
- [ ] Billing alerts configured
- [ ] API key added to app
- [ ] App builds successfully
- [ ] Autocomplete works in app

**All checked?** ? Setup complete!

---

## ?? Troubleshooting

### Problem: "API key not valid"

**Possible Causes:**
- API key not restricted correctly
- Package name/bundle ID mismatch
- SHA-1 fingerprint mismatch (Android)
- Places API (New) not enabled

**Solution:**
1. Verify package name matches exactly
2. Verify SHA-1 fingerprint is correct
3. Wait 5 minutes for restrictions to propagate
4. Try again

---

### Problem: "Quota exceeded"

**Possible Causes:**
- Exceeded daily limit (100/day free tier)
- Too many test searches

**Solution:**
1. Check quota usage in Cloud Console
2. Wait for daily reset (midnight UTC)
3. Increase quota limit if needed
4. Verify quota tracking in app (`Preferences`)

---

### Problem: "Places API (New) not found"

**Possible Causes:**
- Wrong API enabled (old "Places API" instead of "New")

**Solution:**
1. Go to **APIs & Services** ? **Library**
2. Search for **Places API (New)** (with "New")
3. Verify it's enabled
4. Disable old "Places API" if accidentally enabled

---

## ?? Cost Estimates

**Free Tier:**
- **Autocomplete:** 100 requests/day (free)
- **Place Details:** 100 requests/day (free)

**Paid Tier (if exceeded):**
- **Autocomplete:** $2.83 per 1,000 requests
- **Place Details:** $17 per 1,000 requests

**Typical Usage (Bellwood Elite):**
- **Autocomplete:** ~20-50 requests/day per user
- **Place Details:** ~10-20 requests/day per user

**Estimated Monthly Cost (100 users):**
- Free tier: **$0/month** (within limits)
- Paid tier: **$50-100/month** (if exceeded)

**?? Cost Optimization:**
- Use session tokens (already implemented)
- Cache frequent locations (future enhancement)
- Set quota limits
- Monitor usage

---

## ?? Related Documentation

- `Feature-GooglePlacesAutocomplete.md` - Implementation details
- `Guide-ConfigurationSecurity.md` - API key management
- `Testing-GooglePlacesAutocomplete.md` - Test scenarios

---

## ?? Support Resources

**Google Cloud Documentation:**
- [Places API (New) Overview](https://developers.google.com/maps/documentation/places/web-service/op-overview)
- [API Key Best Practices](https://cloud.google.com/docs/authentication/api-keys)
- [Quota Management](https://cloud.google.com/apis/docs/capping-api-usage)

**Bellwood Internal:**
- Check `Guide-ConfigurationSecurity.md` for secrets management
- Contact DevOps for production API keys

---

**Status:** ? Complete & Tested  
**Version:** 1.0  
**Maintainer:** Development Team
