using System.Collections.ObjectModel;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class QuoteDashboardPage : ContentPage
{
    private readonly IAdminApi _admin;
    private readonly ObservableCollection<RowVm> _rows = new();
    private string _filter = "All";
    private string _search = "";
    private System.Timers.Timer? _pollingTimer;

    public QuoteDashboardPage()
    {
        InitializeComponent();
        _admin = ServiceHelper.GetRequiredService<IAdminApi>();
        List.ItemsSource = _rows;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
        StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopPolling();
    }

    private void StartPolling()
    {
        // Poll every 30 seconds for quote status updates
        _pollingTimer = new System.Timers.Timer(30_000);
        _pollingTimer.Elapsed += async (s, e) => await RefreshQuotesAsync();
        _pollingTimer.Start();

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[QuoteDashboard] Polling started (30s interval)");
#endif
    }

    private void StopPolling()
    {
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
        _pollingTimer = null;

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[QuoteDashboard] Polling stopped");
#endif
    }

    private async Task RefreshQuotesAsync()
    {
        try
        {
            var items = await _admin.GetQuotesAsync(100);
            var vms = items
                .Where(FilterFn)
                .Where(SearchFn)
                .Select(RowVm.From)
                .ToList();

            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _rows.Clear();
                foreach (var vm in vms) _rows.Add(vm);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[QuoteDashboard] Refreshed: {vms.Count} quotes displayed");
#endif
            });
        }
        catch (Exception ex)
        {
            // Log error but don't interrupt user with alerts during polling
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuoteDashboard] Polling refresh failed: {ex.Message}");
#endif
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            var items = await _admin.GetQuotesAsync(100);
            var vms = items
                .Where(FilterFn)
                .Where(SearchFn)
                .Select(RowVm.From)
                .ToList();

            _rows.Clear();
            foreach (var vm in vms) _rows.Add(vm);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", $"Couldn't load quotes: {ex.Message}", "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    // --- Filters & search ---
    private bool FilterFn(QuoteListItem q)
    {
        if (_filter == "All") return true;
        var display = ToDisplayStatus(q.Status);
        return string.Equals(display, _filter, StringComparison.OrdinalIgnoreCase);
    }

    private bool SearchFn(QuoteListItem q)
    {
        if (string.IsNullOrWhiteSpace(_search)) return true;
        var s = _search.Trim();
        return (q.Id?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (q.PassengerName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (q.PickupLocation?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async void OnRefresh(object? sender, EventArgs e) => await LoadAsync();

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is RowVm vm)
        {
            ((CollectionView)sender!).SelectedItem = null;
            await Shell.Current.GoToAsync($"QuoteDetailPage?id={vm.Id}");
        }
    }

    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b) return;

        _filter = b.Text ?? "All";
        // quick visual toggle
        foreach (var btn in new[] { AllBtn, AwaitingBtn, RespondedBtn, CancelledBtn })
        {
            var isActive = btn == b;
            btn.BackgroundColor = isActive ? (Color)Application.Current.Resources["BellwoodGold"]
                                           : (Color)Application.Current.Resources["BellwoodCharcoal"];
            btn.TextColor = isActive ? (Color)Application.Current.Resources["BellwoodInk"]
                                     : (Color)Application.Current.Resources["BellwoodCream"];
        }

        await LoadAsync();
    }

    private async void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        _search = e.NewTextValue ?? "";
        await LoadAsync();
    }

    // Map backend status -> customer-facing label (Phase Alpha + backward compatibility)
    private static readonly Dictionary<string, string> DisplayStatusMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Phase Alpha statuses (new)
            ["Pending"] = "Awaiting Response",
            ["Acknowledged"] = "Under Review",
            ["Responded"] = "Response Received",
            ["Accepted"] = "Booking Created",
            ["Cancelled"] = "Cancelled",
            
            // Legacy statuses (backward compatibility)
            ["Submitted"] = "Awaiting Response",      // Map to Pending
            ["InReview"] = "Under Review",            // Map to Acknowledged
            ["Priced"] = "Response Received",         // Map to Responded
            ["Sent"] = "Response Received",           // Map to Responded
            ["Closed"] = "Booking Created",           // Map to Accepted
            ["Rejected"] = "Cancelled"                // Map to Cancelled
        };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Awaiting Response";

        // If the API ever sends numeric enums, try to parse them defensively:
        if (int.TryParse(raw, out var n) && Enum.IsDefined(typeof(AdminStatus), n))
        {
            raw = ((AdminStatus)n).ToString();
        }

        return DisplayStatusMap.TryGetValue(raw!, out var friendly) ? friendly : raw!;
    }

    private enum AdminStatus { Submitted = 0, InReview = 1, Priced = 2, Sent = 3, Closed = 4, Rejected = 5 }

    // Color map for display labels (Phase Alpha colors)
    private static Color StatusColorForDisplay(string display)
    {
        var d = (display ?? "").ToLowerInvariant();
        return d switch
        {
            "awaiting response" => Colors.Orange,          // Pending
            "under review" => Colors.Blue,                 // Acknowledged
            "response received" => Colors.Green,           // Responded
            "booking created" => Colors.Gray,              // Accepted
            "cancelled" => Colors.Red,                     // Cancelled
            
            // Legacy fallbacks
            "submitted" or "pending" => Colors.Orange,
            "priced" or "quoted" => Colors.Green,
            "declined" => Colors.Red,
            "closed" => Colors.Gray,
            
            _ => Colors.Gray,
        };
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
            await Shell.Current.GoToAsync("..");
        else
            await Shell.Current.GoToAsync("//MainPage"); 
    }

    // --- lightweight row VM ---
    public sealed class RowVm
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string SubTitle { get; init; } = "";
        public string Meta { get; init; } = "";
        public string Status { get; init; } = "Pending";
        public Color StatusColor { get; init; } = Colors.Gray;

        public static RowVm From(QuoteListItem q)
        {
            var displayStatus = ToDisplayStatus(q.Status);
            
            // Show estimated price for responded quotes
            var statusWithPrice = displayStatus;
            if (displayStatus == "Response Received" && q.EstimatedPrice.HasValue)
            {
                statusWithPrice = $"{displayStatus} - ${q.EstimatedPrice.Value:F2}";
            }

            return new RowVm
            {
                Id = q.Id ?? "",
                Title = $"{(string.IsNullOrWhiteSpace(q.PassengerName) ? "Passenger" : q.PassengerName)}  ·  {q.VehicleClass}",
                SubTitle = $"{q.PickupDateTime:g} — {q.PickupLocation}",
                Meta =
                    $"Booker: {q.BookerName}   •   " +
                    $"Drop: {(string.IsNullOrWhiteSpace(q.DropoffLocation) ? "As Directed" : q.DropoffLocation)}   •   " +
                    $"Created: {q.CreatedUtc.ToLocalTime():g}",
                Status = statusWithPrice,
                StatusColor = StatusColorForDisplay(displayStatus)
            };
        }
    }
}