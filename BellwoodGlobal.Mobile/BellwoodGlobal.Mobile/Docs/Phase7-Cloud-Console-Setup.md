# Phase 7 - Google Cloud Console Setup Guide

**Date:** January 1, 2026  
**Purpose:** Configure API quotas and monitoring in Google Cloud Console  

---

## 🎯 **Overview**

This guide walks you through setting up Google Places API (New) quotas, restrictions, and monitoring in Google Cloud Console to ensure you stay within the free tier and prevent unexpected costs.

---

## 📋 **Prerequisites**

- ✅ Google Cloud account
- ✅ Project created in Google Cloud Console
- ✅ Places API (New) enabled
- ✅ API key generated (already in `AndroidManifest.xml`)

---

## 🔐 **STEP 1: API Key Restrictions**

### **Navigate to Credentials**

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to **APIs & Services** → **Credentials**
4. Find your API key (e.g., `KEY HERE`)
5. Click **Edit** (pencil icon)

---

### **Application Restrictions**

**For Android:**

1. Select **Android apps**
2. Add package name: `com.bellwoodglobal.mobile`
3. Add SHA-1 fingerprints:

**Debug Keystore:**
```bash
# Windows
keytool -list -v -keystore "%USERPROFILE%\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android

# Mac/Linux
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
```

**Release Keystore:**
```bash
keytool -list -v -keystore path/to/your/release.keystore -alias your-key-alias
```

Copy the SHA-1 fingerprint (looks like: `AA:BB:CC:DD:...`)

4. Add both debug and release SHA-1 fingerprints to the key restrictions

**For iOS:**

1. Select **iOS apps**
2. Add bundle ID: `com.bellwoodglobal.mobile`

---

### **API Restrictions**

**Limit key to specific APIs:**

1. Scroll to **API restrictions**
2. Select **Restrict key**
3. Check only:
   - ✅ **Places API (New)**
   - ✅ **Geocoding API** (if using fallback geocoding)
   - ✅ **Maps SDK for Android** (if using map views)
   - ✅ **Maps SDK for iOS** (if using map views)

4. Save changes

**Why restrict?**
- ✅ Prevents misuse if key is compromised
- ✅ Prevents accidental usage of expensive APIs
- ✅ Limits blast radius of leaked keys

---

## 📊 **STEP 2: Quota Configuration**

### **Navigate to Quotas**

1. In Google Cloud Console
2. Go to **APIs & Services** → **Enabled APIs & Services**
3. Click **Places API (New)**
4. Click **Quotas** tab

---

### **Recommended Quotas**

**Autocomplete (New):**
- **Free Tier Limit:** 10,000 sessions/month
- **Daily Calculation:** 10,000 ÷ 31 = 322 sessions/day
- **Recommended Cap:** 300 sessions/day (conservative, with safety margin)
- **Monitoring:** Enable alerts at 50% (150) and 80% (240)

**Place Details (New):**
- **Free Tier Limit:** Check current limits in Cloud Console (typically generous)
- **Recommended Cap:** 300 calls/day (conservative)
- **Monitoring:** Enable alerts at 50% (150) and 80% (240)

**How to Set:**

1. Find **Places API (New)** in quota list
2. Click quota item (e.g., "Autocomplete sessions per day")
3. Click **Edit Quotas**
4. Set your limit: **300** (or 322 if you want exact calculation)
5. Provide justification (e.g., "Cost control - aligning with 10k/month free tier")
6. Submit request (Google usually approves within 24 hours)

**Important:** The app-level tracker enforces 300/day. Cloud Console quota should match or be slightly higher (e.g., 320) to allow headroom.

---

## 🔔 **STEP 3: Budget Alerts**

### **Set Up Billing Budgets**

1. Go to **Billing** → **Budgets & alerts**
2. Click **Create Budget**

**Budget Settings:**

- **Name:** "Places API Monthly Budget"
- **Time range:** Monthly
- **Projects:** Select your project
- **Services:** Select "Places API (New)"
- **Budget amount:** $20/month (safety net for free tier monitoring)

**Alert Thresholds:**

- ✅ 50% ($10) - Email alert (review usage patterns)
- ✅ 80% ($16) - Email alert + review usage immediately
- ✅ 100% ($20) - Email alert + investigate + consider disabling

**Note:** With proper quota limits (300/day), you should stay at $0. Budget alerts catch any misconfigurations or unexpected usage.

**Alert Recipients:**

- Add your email
- Add team emails
- Consider a Slack/Teams webhook

3. Save budget

---

## 📈 **STEP 4: Usage Monitoring**

### **Cloud Console Dashboard**

**Create Custom Dashboard:**

1. Go to **Monitoring** → **Dashboards**
2. Click **Create Dashboard**
3. Name: "Places API Usage"

**Add Charts:**

**Chart 1: Daily Autocomplete Sessions**
- Metric: `serviceruntime.googleapis.com/quota/rate/net_usage`
- Filter: `service="places-backend.googleapis.com"`, `quota_metric="AutocompleteSessions"`
- Group by: Date
- Chart type: Line

**Chart 2: Daily Place Details Calls**
- Metric: `serviceruntime.googleapis.com/quota/rate/net_usage`
- Filter: `service="places-backend.googleapis.com"`, `quota_metric="PlaceDetailsRequests"`
- Group by: Date
- Chart type: Line

**Chart 3: Error Rate**
- Metric: `serviceruntime.googleapis.com/api/request_count`
- Filter: `service="places-backend.googleapis.com"`, `response_code_class="5xx"`
- Chart type: Stacked area

4. Save dashboard

---

### **Log-Based Metrics**

**Create Alert for Quota Exceeded:**

1. Go to **Logging** → **Logs Explorer**
2. Query:
   ```
   resource.type="api"
   protoPayload.serviceName="places-backend.googleapis.com"
   protoPayload.status.code=8
   ```
   (Code 8 = RESOURCE_EXHAUSTED)

3. Click **Create Alert**
4. Name: "Places API Quota Exceeded"
5. Threshold: 1 occurrence in 1 hour
6. Save

---

## 🧪 **STEP 5: Testing Quota Limits**

### **Simulate Quota Exhaustion (DEV ONLY)**

**Option A: Temporary Lower Limit**

1. Set autocomplete quota to 10/day in Cloud Console
2. Use autocomplete 11 times
3. Verify app shows: "Address autocomplete is temporarily unavailable"
4. Reset quota to normal

**Option B: Manual Tracker Reset**

```csharp
// In DEBUG mode
var tracker = ServiceHelper.GetRequiredService<IPlacesUsageTracker>();

// Simulate hitting limit
Preferences.Set("PlacesUsage_20260101", JsonSerializer.Serialize(new PlacesUsageStats
{
    Date = "2026-01-01",
    AutocompleteSessions = 950,
    PlaceDetailsCalls = 475,
    IsDisabled = false
}));

// Next autocomplete will show warning
// Set to 1000 sessions to trigger disable
```

---

## 📊 **STEP 6: Monthly Review Process**

### **First Week of Each Month:**

1. **Export Usage Report**
   ```csharp
   var tracker = ServiceHelper.GetRequiredService<IPlacesUsageTracker>();
   var report = tracker.ExportUsageReport(30);
   // Save to file or email to team
   ```

2. **Review Cloud Console**
   - Check actual API usage vs. app-tracked usage
   - Verify free tier limits not exceeded
   - Review any unexpected spikes

3. **Check Billing**
   - Confirm $0.00 charges (staying in free tier)
   - Review budget alerts
   - Adjust quotas if needed

4. **Analyze Trends**
   - Average sessions per day
   - Average details per session
   - Error rate
   - Optimization opportunities

---

## ⚠️ **Troubleshooting**

### **"API key not valid" Error**

**Causes:**
- API key restrictions blocking requests
- Wrong package name or SHA-1 fingerprint
- API not enabled

**Fix:**
1. Check API key restrictions match app package/bundle ID
2. Verify SHA-1 fingerprints are correct
3. Ensure Places API (New) is enabled

---

### **"Quota Exceeded" Error**

**Causes:**
- Daily quota hit
- Monthly quota hit
- Sudden spike in usage

**Fix:**
1. Check app-level tracker: `GetTodayStats()`
2. Check Cloud Console quotas
3. Verify alert thresholds are set
4. Review recent usage patterns
5. Temporarily disable feature or increase quota

---

### **Unexpected Charges**

**Investigate:**
1. Review Cloud Console usage dashboard
2. Export app usage report
3. Compare app tracking vs. Cloud Console
4. Look for:
   - Missing rate limiting
   - Leaked API key
   - Abuse or bot traffic
   - Misconfigured quota limits

**Emergency Response:**
1. Disable API key temporarily
2. Review security logs
3. Generate new API key
4. Update app with new key
5. Add stricter restrictions

---

## 📋 **Configuration Checklist**

### **Security:**
- [ ] API key restricted to specific apps (package name + SHA-1)
- [ ] API key restricted to specific APIs (Places, Geocoding only)
- [ ] Debug and release fingerprints both added
- [ ] API key stored securely (not in public repo)

### **Quotas:**
- [ ] Autocomplete daily limit set (recommended: 1,000)
- [ ] Place Details daily limit set (recommended: 500)
- [ ] Budget alerts configured ($50/month)
- [ ] Alert thresholds set (50%, 80%, 100%)

### **Monitoring:**
- [ ] Custom dashboard created
- [ ] Usage charts added
- [ ] Error alerts configured
- [ ] Quota exceeded alerts configured

### **Process:**
- [ ] Monthly review scheduled
- [ ] Team members added to alerts
- [ ] Escalation process documented
- [ ] Emergency contact list created

---

## 🎯 **Recommended Limits Summary**

| Metric | Free Tier | Our Cap | Alert At (80%) | Notes |
|--------|-----------|---------|----------------|-------|
| Autocomplete Sessions/Day | 322 (10k/month ÷ 31) | 300 | 240 | Safety margin |
| Place Details Calls/Day | Varies by SKU | 300 | 240 | Conservative |
| Monthly Budget | $0 (free tier) | $20 | $10 (50%) | Safety net |
| Error Rate | N/A | <2% | >5% | Quality metric |

**Rationale:**
- **300/day cap** provides ~9,300/month usage (well within 10k free tier)
- **Safety margin** of ~700 sessions/month for unexpected spikes
- **Most users have saved locations** in LimoAnywhere, reducing autocomplete dependency
- **Budget alert at $20** catches any unexpected charges early

---

## 📝 **Notes**

### **Free Tier Changes:**

Google occasionally updates free tier allowances. Always check:
- [Google Maps Platform Pricing](https://mapsplatform.google.com/pricing/)
- Your Cloud Console quota page
- Billing alerts in your account

### **Production Deployment:**

Before going live:
1. ✅ Set production API key (separate from debug)
2. ✅ Configure production restrictions
3. ✅ Set conservative quotas
4. ✅ Enable all monitoring
5. ✅ Test quota limits
6. ✅ Document escalation process

---

## 🎉 **You're All Set!**

Your Google Places API is now:
- ✅ Secured with app + API restrictions
- ✅ Protected with quota limits
- ✅ Monitored with dashboards and alerts
- ✅ Budgeted to stay in free tier
- ✅ Ready for production use

**Next:** Monitor usage for first month and adjust limits as needed.

---

**Last Updated:** January 1, 2026  
**Status:** ✅ **READY FOR PRODUCTION**
