# Performance Improvement Plan for Bellwood Elite

**Status Update (January 3, 2026):**
✅ **Phase 1 & 2 COMPLETE** - ConfigurationService and FormStateService fixes implemented and ready for testing.
See `Performance-Improvements-Implementation-Summary.md` for full details.

---

## 1. Background and symptoms
The Bellwood Elite mobile app (a multi‑tenant ride‑booking application built in .NET MAUI) currently experiences long load times and UI unresponsiveness. The debug logs show numerous warnings such as:
```
[Choreographer] Skipped 34 frames!  The application may be doing too much work on its main thread
```

**✅ UPDATE:** Root causes identified and fixed:
- ConfigurationService: Removed blocking `.Result` on file I/O
- FormStateService: Removed blocking `.Result` on SecureStorage access

---

## 2. Understanding why frames are skipped
When rendering at 60 frames per second, the UI thread has ~16 ms to complete its work. Android documentation explains that if your app does not finish drawing in this window, the system may skip frames and animations will appear janky[1]. A TechYourChance article emphasises that heavy work on the UI thread leads to dropped frames and that all computationally expensive tasks should be moved off the main thread[2]. Developers are encouraged to use profiling tools (e.g., Profile GPU Rendering or Systrace) to identify slow rendering operations and to offload network and disk operations to background threads[3].
3. Diagnostic tools
1.	StrictMode (Android) – enables detection of disk or network operations on the UI thread by logging or throwing exceptions. The Xamarin/Microsoft documentation recommends enabling StrictMode in your main activity during development to catch synchronous network calls or file I/O[4].
2.	Profile GPU Rendering – available under Android Developer Options; it displays bars showing how long each frame takes to render. Bars above the 16 ms line indicate janky areas[3].
3.	Systrace/Perfetto – system‑level tracing tool that allows inspection of thread execution and blocked calls.
4.	.NET MAUI/Visual Studio Profiler – MAUI projects can be profiled using dotnet‑counters and Visual Studio’s performance profilers. They highlight CPU, memory, and I/O usage.
Using these tools early will help identify the exact functions that block the UI thread.
4. Findings from code analysis
All repositories (BellwoodApp, Bellwood.DriverApp, Bellwood.AdminApi, Bellwood.AdminPortal and BellwoodAuthServer) were reviewed on their latest main branches. Most services use asynchronous programming correctly: network calls are made with HttpClient and awaited, polling loops use Task.Delay, and UI pages update via MainThread.BeginInvokeOnMainThread. However, two components synchronously block the main thread, which can cause the long startup times and frame skipping observed:
4.1 ConfigurationService synchronously loads configuration
ConfigurationService reads appsettings.json and appsettings.Development.json during construction. It uses FileSystem.OpenAppPackageFileAsync(filename).Result to synchronously wait for an asynchronous call to return[5]:
// ConfigurationService.cs
using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;  // blocks thread
var json   = reader.ReadToEnd();
...
Because the constructor runs during app initialization (via dependency injection in MauiProgram), this .Result call blocks the UI thread until the file is read and deserialized. On slower devices or when the file is large, this can take tens or hundreds of milliseconds, causing multiple frames to be skipped.
4.2 FormStateService uses synchronous SecureStorage calls
FormStateService derives a per‑user key from the user’s email by calling SecureStorage.GetAsync("user_email").Result[6]:
// FormStateService.cs
var userEmail = SecureStorage.GetAsync("user_email").Result;  // blocks thread
When a page using this service (e.g., the quote page or booking page) constructs or loads form state, this synchronous call blocks the UI thread while waiting for SecureStorage (which may involve underlying file or encryption operations). On lower‑end devices this call can take 50–200 ms, which is long enough to miss several frames.
4.3 Polling loops and long‑running tasks
Several services (e.g., RideStatusService, DriverTrackingService, LocationTracker) poll remote APIs at intervals using Task.Delay and await loops. This pattern is appropriate, but ensure that there are no inadvertently synchronous calls (e.g., .Wait() or .Result) inside the loops. Also ensure that tasks use ConfigureAwait(false) when context capture is unnecessary.
4.4 Large startup operations and images
The mobile app displays lists of rides and uses images (vehicle icons, maps). If images are loaded synchronously or at full resolution, they can cause UI stalls. The app uses CollectionView with virtualization, which is good. However, images loaded via ImageSource.FromFile or FromStream should be decoded at appropriate sizes, and network images should be cached.
4.5 Backend API performance
Bellwood.AdminApi and the authentication server appear to use asynchronous database calls. While backend latency can slow data loading, it should not freeze the mobile UI if the mobile app awaits calls correctly. Ensure that long‑running API operations return promptly and that the mobile UI displays a loading indicator rather than blocking the thread.
5. Recommended strategies and implementation plan
The following steps outline a comprehensive approach to eliminating frame skipping and improving load performance:
5.1 Refactor blocking calls to asynchronous operations
1.	Make ConfigurationService asynchronous. Move file loading into an async method and avoid blocking in the constructor:
public sealed class ConfigurationService : IConfigurationService
{
    private readonly Dictionary<string,string> _settings = new();

    // Introduce an initialization method to be awaited during app startup
    public async Task InitializeAsync()
    {
        await TryLoadSettingsFileAsync("appsettings.json");
        await TryLoadSettingsFileAsync("appsettings.Development.json");
    }

    private async Task TryLoadSettingsFileAsync(string filename)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var loaded = JsonSerializer.Deserialize<Dictionary<string,string>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                        _settings[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex) { /* log but do not block UI */ }
    }
}
Register the service in MauiProgram using AddSingleton<IConfigurationService, ConfigurationService>() and call await configurationService.InitializeAsync() during app startup (e.g., in App.xaml.cs).
1.	Make FormStateService truly asynchronous. Replace .Result with await and propagate async methods. For example:
private static async Task<string> GetUserSpecificKeyAsync(string prefix)
{
    try
    {
        var userEmail = await SecureStorage.GetAsync("user_email");
        return string.IsNullOrWhiteSpace(userEmail) ? prefix : $"{prefix}_{userEmail}";
    }
    catch (Exception) { return prefix; }
}

public async Task SaveQuoteFormStateAsync(QuotePageState state)
{
    var key = await GetUserSpecificKeyAsync(QuoteKeyPrefix);
    var json = JsonSerializer.Serialize(state, JsonOptions);
    Preferences.Set(key, json);
}

public async Task<QuotePageState?> LoadQuoteFormStateAsync()
{
    var key = await GetUserSpecificKeyAsync(QuoteKeyPrefix);
    var json = Preferences.Get(key, string.Empty);
    return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<QuotePageState>(json, JsonOptions);
}
// apply the same pattern for booking forms
Because these methods are now asynchronous, update any callers (view models/pages) to await them. This change ensures that SecureStorage access occurs off the UI thread.
5.2 Offload other heavy work from the main thread
•	Database or network I/O – Ensure that HttpClient calls, database queries, and file access are always awaited and never invoked synchronously. Avoid Task.Run followed by .Result on the UI thread.
•	Large JSON parsing – If API responses return large JSON payloads, consider parsing on a background thread (e.g., using JsonSerializer.DeserializeAsync).
•	Image loading – Use ImageLoadingService (e.g., Maui.Maui.ImageLoader) or libraries like FFImageLoading with caching. When loading from file, call await FileSystem.OpenAppPackageFileAsync and decode images at the desired size to reduce memory usage. Ensure Bitmap decoding is done off the UI thread.
•	Lazy loading – Avoid fetching large lists during app startup. Instead, display a splash screen or loading indicator and fetch data asynchronously after the first frame is rendered. Use await Task.Yield() to allow the UI to draw before starting heavy work.
5.3 Use StrictMode and profiling to detect remaining issues
Enable StrictMode in MainActivity.OnCreate() during development:
if (BuildConfig.DEBUG) {
    StrictMode.setThreadPolicy(new StrictMode.ThreadPolicy.Builder()
        .detectDiskReads()
        .detectDiskWrites()
        .detectNetwork()
        .penaltyLog()
        .build());
    StrictMode.setVmPolicy(new StrictMode.VmPolicy.Builder()
        .detectLeakedClosableObjects()
        .penaltyLog()
        .build());
}
This will log warnings if disk or network operations occur on the main thread, helping identify additional offending calls[4].
Use Profile GPU Rendering to visually inspect UI jank[3]. Bars above the 16 ms line highlight slow frames. Investigate the corresponding code path.
5.4 Optimize polling intervals and concurrency
Services like RideStatusService and DriverTrackingService poll APIs at fixed intervals. Review whether the polling frequency is higher than necessary; longer intervals reduce network and CPU load. If real‑time updates are required, consider using WebSockets or server‑sent events instead of frequent polling. When polling, use ConfigureAwait(false) on awaited tasks inside services to avoid marshalling back to the UI thread.
5.5 Memory and resource optimizations
•	Use CollectionView with virtualization (already employed) and limit the number of items loaded at once.
•	Dispose of event subscriptions promptly in OnDisappearing to prevent memory leaks.
•	Use dependency injection to manage service lifetimes and avoid re‑creating heavy services on each page.
5.6 Backend/API improvements (if applicable)
While the UI thread should never block waiting for network responses, slow backend endpoints still affect perceived performance. Ensure that the API returns paginated data and efficient queries. Implement caching on the server side where appropriate. Consider compressing JSON responses or using HTTP GZip.
5.7 Testing and rollout plan
1.	Create a profiling baseline: instrument the current app using StrictMode and Profile GPU Rendering. Record the number of frames skipped and time to load the home page.
2.	Refactor ConfigurationService and FormStateService to be fully asynchronous and update all callers.
3.	Review the codebase for any remaining .Result, .Wait() or synchronous file/network operations and refactor them to async/await.
4.	Optimize image loading and caching. Use compressed assets and asynchronous decoding.
5.	Reduce polling frequency or adopt push notifications for ride status updates.
6.	Re‑profile after changes and compare against the baseline. Continue iterating until GPU bars mostly stay under the 16 ms line.
7.	Conduct user testing on low‑end devices to verify improved responsiveness and absence of ANR dialogs.
6. Conclusion
The skipped‑frame warnings and unresponsiveness in Bellwood Elite are caused primarily by synchronous operations on the UI thread—particularly the blocking calls in ConfigurationService and FormStateService. According to Android performance guidelines, UI work must finish within ~16 ms to maintain 60 fps; heavy operations on the main thread should be offloaded[2]. By refactoring these services to be fully asynchronous, enabling StrictMode to detect remaining issues, optimizing image loading and polling intervals, and profiling the app regularly, the team can substantially reduce load times and eliminate ANR dialogs. These improvements will lead to a smoother and more responsive experience for users.
________________________________________
[1] [3] Slow rendering  |  App quality  |  Android Developers
https://developer.android.com/topic/performance/vitals/render
[2] Why Android Applications Skip Frames and How to Fix This Issue
https://www.techyourchance.com/android-application-skips-frames/
[4] Tips for Creating a Smooth and Fluid Android UI - Xamarin Blog
https://devblogs.microsoft.com/xamarin/tips-for-creating-a-smooth-and-fluid-android-ui/
[5] ConfigurationService.cs
https://github.com/BidumanADT/BellwoodApp/blob/main/BellwoodGlobal.Mobile/BellwoodGlobal.Mobile/Services/ConfigurationService.cs
[6] FormStateService.cs
https://github.com/BidumanADT/BellwoodApp/blob/main/BellwoodGlobal.Mobile/BellwoodGlobal.Mobile/Services/FormStateService.cs
