using System.Collections.ObjectModel;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookingsPage : ContentPage
{
    private readonly IAdminApi _admin;
    private readonly ObservableCollection<RowVm> _rows = new();
    private string _filter = "All";
    private string _search = "";

    public BookingsPage()
    {
        InitializeComponent();
        _admin = ServiceHelper.GetRequiredService<IAdminApi>();
        List.ItemsSource = _rows;

        // Set initial filter button states
        AllBtn.BackgroundColor = (Color)Application.Current!.Resources["BellwoodGold"];
        AllBtn.TextColor = (Color)Application.Current!.Resources["BellwoodInk"];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Refresh.IsRefreshing = true;
            var items = await _admin.GetBookingsAsync(100);
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
            await DisplayAlert("Network", $"Couldn't load bookings: {ex.Message}", "OK");
        }
        finally
        {
            Refresh.IsRefreshing = false;
        }
    }

    // --- Filters & search ---
    private bool FilterFn(BookingListItem b)
    {
        if (_filter == "All") return true;
        var display = ToDisplayStatus(b.Status);
        return string.Equals(display, _filter, StringComparison.OrdinalIgnoreCase);
    }

    private bool SearchFn(BookingListItem b)
    {
        if (string.IsNullOrWhiteSpace(_search)) return true;
        var s = _search.Trim();
        return (b.Id?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (b.PassengerName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
            || (b.PickupLocation?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async void OnRefresh(object? sender, EventArgs e) => await LoadAsync();

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is RowVm vm)
        {
            ((CollectionView)sender!).SelectedItem = null;
            await Shell.Current.GoToAsync($"BookingDetailPage?id={vm.Id}");
        }
    }

    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b) return;

        _filter = b.Text ?? "All";

        // Visual toggle (same gold/charcoal logic as QuoteDashboard)
        foreach (var btn in new[] { AllBtn, RequestedBtn, ConfirmedBtn, CompletedBtn })
        {
            var isActive = btn == b;
            btn.BackgroundColor = isActive 
                ? (Color)Application.Current!.Resources["BellwoodGold"]
                : (Color)Application.Current!.Resources["BellwoodCharcoal"];
            btn.TextColor = isActive 
                ? (Color)Application.Current!.Resources["BellwoodInk"]
                : (Color)Application.Current!.Resources["BellwoodCream"];
        }

        await LoadAsync();
    }

    private async void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        _search = e.NewTextValue ?? "";
        await LoadAsync();
    }

    // Map backend status -> customer-facing label
    private static readonly Dictionary<string, string> DisplayStatusMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Requested"] = "Requested",
            ["Confirmed"] = "Confirmed",
            ["Scheduled"] = "Scheduled",
            ["InProgress"] = "In Progress",
            ["Completed"] = "Completed",
            ["Cancelled"] = "Cancelled",
            ["NoShow"] = "No Show"
        };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Requested";

        // Handle numeric enums defensively
        if (int.TryParse(raw, out var n) && Enum.IsDefined(typeof(BookingStatusEnum), n))
        {
            raw = ((BookingStatusEnum)n).ToString();
        }

        return DisplayStatusMap.TryGetValue(raw!, out var friendly) ? friendly : raw!;
    }

    // Mirror the enum from AdminAPI for defensive parsing
    private enum BookingStatusEnum 
    { 
        Requested = 0, Confirmed = 1, Scheduled = 2, 
        InProgress = 3, Completed = 4, Cancelled = 5, NoShow = 6 
    }

    // Color map for display labels (Bellwood Elite branding)
    private static Color StatusColorForDisplay(string display)
    {
        var d = (display ?? "").ToLowerInvariant();
        return d switch
        {
            "requested" => TryGetColor("ChipPending", Colors.Goldenrod),
            "confirmed" or "scheduled" => TryGetColor("ChipPriced", Colors.SeaGreen),
            "in progress" => TryGetColor("BellwoodGold", Colors.Gold),
            "completed" => TryGetColor("ChipOther", Colors.LightGray),
            "cancelled" or "no show" => TryGetColor("ChipDeclined", Colors.IndianRed),
            _ => TryGetColor("ChipOther", Colors.Gray),
        };
    }

    private static Color TryGetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
            await Shell.Current.GoToAsync("..");
        else
            await Shell.Current.GoToAsync("//MainPage");
    }

    // --- Lightweight row VM ---
    public sealed class RowVm
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string SubTitle { get; init; } = "";
        public string Meta { get; init; } = "";
        public string Status { get; init; } = "Requested";
        public Color StatusColor { get; init; } = Colors.Gray;

        public static RowVm From(BookingListItem b)
        {
            var displayStatus = ToDisplayStatus(b.Status);

            return new RowVm
            {
                Id = b.Id ?? "",
                Title = $"{(string.IsNullOrWhiteSpace(b.PassengerName) ? "Passenger" : b.PassengerName)}  ·  {b.VehicleClass}",
                SubTitle = $"{b.PickupDateTime:g} — {b.PickupLocation}",
                Meta =
                    $"Booker: {b.BookerName}   •   " +
                    $"Drop: {(string.IsNullOrWhiteSpace(b.DropoffLocation) ? "As Directed" : b.DropoffLocation)}   •   " +
                    $"Created: {b.CreatedUtc.ToLocalTime():g}",
                Status = displayStatus,
                StatusColor = StatusColorForDisplay(displayStatus)
            };
        }
    }
}
