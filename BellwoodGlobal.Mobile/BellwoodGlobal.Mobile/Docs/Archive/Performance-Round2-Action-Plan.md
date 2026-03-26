# Performance Round 2 - Quick Action Plan

**Date:** January 7, 2026  
**Status:** Ready for Testing  

---

## ?? What We Just Fixed

### Problem
Even with async/await, config loading was taking **914ms** and causing **188 frames skipped** during app startup.

### Root Cause
`FileSystem.OpenAppPackageFileAsync()` was still blocking the UI thread internally, even though we were `await`ing it.

### Solution Applied
1. ? Wrapped file I/O in `Task.Run()` to move to background thread
2. ? Added `Task.Yield()` in SplashPage to allow first frame to render
3. ? Added thread-safe `lock` for dictionary access

---

## ?? Testing Checklist (10 minutes)

### Step 1: Build & Deploy
```bash
# Clean build
dotnet clean
dotnet build -f net9.0-android

# Deploy to emulator
dotnet build -t:Run -f net9.0-android
```

**Expected:** Build successful ?

---

### Step 2: Watch Logs
```bash
# Open new terminal
adb logcat | grep -E "ConfigurationService|Choreographer|SplashPage"
```

---

### Step 3: Cold Start App

**Action:** Close app completely, then relaunch

**Watch for:**
1. Config load time in logs: `[ConfigurationService] Initialization complete in Xms`
2. Splash animation (should be smooth)
3. Any "Choreographer: Skipped" messages

**Success Criteria:**
- ? Config loads in **<200ms**
- ? **NO** "Skipped 188 frames" warning
- ? Splash animation smooth

---

### Step 4: Navigate Around

**Action:**
1. Login as alice/password
2. Go to Quote page
3. Go to Bookings page
4. Back to Main page

**Watch for:**
- Frame skip warnings in logcat
- Any jank/stuttering

**Success Criteria:**
- ? **Fewer** frame skips than before (was 32-60 frames)
- ? **No** frame skips during splash

---

### Step 5: Profile GPU Rendering (Optional)

**Enable:**
1. Settings ? Developer Options ? Profile GPU Rendering ? On screen as bars

**Run:**
1. Cold start app
2. Watch bars during splash

**Success Criteria:**
- ? Most bars **below 16ms line** (green)
- ? No huge spikes during splash

---

## ?? Expected Results

### Before Round 2
```
07:28:02:290 [Choreographer] Skipped 188 frames! ?
07:28:03:914 [ConfigurationService] Initialization complete in 914ms ?
```

### After Round 2 (Expected)
```
[ConfigurationService] Starting async initialization...
[SplashPage] Configuration initialized in 87ms ?
// NO "Skipped 188 frames" message ?
```

---

## ?? What If It's Still Slow?

### Scenario 1: Still seeing 188 frames skipped

**Possible Causes:**
- Task.Run() not working as expected
- DI container initialization heavy

**Next Steps:**
- Enable StrictMode to see exact violations
- Profile with Android Studio

### Scenario 2: Config still >500ms

**Possible Causes:**
- appsettings.json very large
- File system slow on emulator

**Next Steps:**
- Check file sizes
- Test on physical device

### Scenario 3: Different frame skip count

**Possible Causes:**
- ProfileService or other services
- Page navigation heavy

**Next Steps:**
- Phase 3: Lazy-load ProfileService
- Add loading indicators

---

## ? Success Definition

**Phase 1 & 2 SUCCESSFUL if:**
1. ? Config loads in <200ms
2. ? NO "Skipped 188 frames" during startup
3. ? Splash animation smooth
4. ? Total frame skips **significantly reduced**

**Phase 1 & 2 PARTIAL if:**
- Config fast but still some frame skips ? Proceed to Phase 3

**Phase 1 & 2 FAILED if:**
- Still 914ms config load
- Still 188 frames skipped
- ? Investigate further

---

## ?? Rollback Plan

**If testing shows regression:**

```bash
# Revert changes
git diff HEAD~2 HEAD

# If needed, hard reset
git reset --hard HEAD~2
```

**Files to restore:**
- ConfigurationService.cs
- SplashPage.xaml.cs

---

## ?? Quick Debug

**See Config Timing:**
```bash
adb logcat | grep ConfigurationService
```

**Count Frame Skips:**
```bash
adb logcat | grep "Skipped.*frames" | wc -l
```

**See All Performance Logs:**
```bash
adb logcat -s ConfigurationService:D Choreographer:W SplashPage:D
```

---

## ?? Next Actions

**If Test Passes:**
1. ? Mark Phase 1 & 2 complete
2. ? Document results
3. ? Move to Phase 3 (ProfileService optimization)

**If Test Fails:**
1. ? Capture full logcat
2. ? Profile with Android Studio
3. ? Investigate specific violations

---

## ?? Phase 3 Preview

**What's Left:**
- Lazy-load ProfileService data (eliminate 32-60 frame skips)
- Add loading indicators to pages
- Optimize PlacesUsageTracker Preferences access

**Estimated Impact:**
- Should eliminate **remaining** frame skips
- Total session: **0 frame skips** ??

---

**Ready to test!** Run the build, watch the logs, and let me know what you see. ??

---

**Version:** 1.0  
**Author:** AI Assistant  
**Status:** ? Awaiting Test Results
