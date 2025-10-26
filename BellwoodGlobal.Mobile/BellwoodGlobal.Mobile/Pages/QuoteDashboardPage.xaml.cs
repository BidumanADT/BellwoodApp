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
            // Navigate to detail
            await Shell.Current.GoToAsync($"RideHistoryPage?quoteId={vm.Id}");
        }
    }

    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b) return;

        _filter = b.Text ?? "All";
        // quick visual toggle
        foreach (var btn in new[] { AllBtn, PendingBtn, PricedBtn, DeclinedBtn })
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

    // Map backend status -> customer-facing label
    private static readonly Dictionary<string, string> DisplayStatusMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Submitted"] = "Submitted",
            ["InReview"] = "Pending",
            ["Priced"] = "Priced",
            ["Sent"] = "Quoted",
            ["Closed"] = "Closed",
            ["Rejected"] = "Declined"
        };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Submitted";

        // If the API ever sends numeric enums, try to parse them defensively:
        if (int.TryParse(raw, out var n) && Enum.IsDefined(typeof(AdminStatus), n))
        {
            raw = ((AdminStatus)n).ToString();
        }

        return DisplayStatusMap.TryGetValue(raw!, out var friendly) ? friendly : raw!;
    }

    private enum AdminStatus { Submitted = 0, InReview = 1, Priced = 2, Sent = 3, Closed = 4, Rejected = 5 }

    // Color map for display labels
    private static Color StatusColorForDisplay(string display)
    {
        var d = (display ?? "").ToLowerInvariant();
        return d switch
        {
            "submitted" or "pending" => TryGetColor("ChipPending", Colors.Goldenrod),
            "priced" or "quoted" => TryGetColor("ChipPriced", Colors.SeaGreen),
            "declined" => TryGetColor("ChipDeclined", Colors.IndianRed),
            "closed" => TryGetColor("ChipOther", Colors.Gray),
            _ => TryGetColor("ChipOther", Colors.Gray),
        };
    }

    private static Color TryGetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
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

            return new RowVm
            {
                Id = q.Id ?? "",
                Title = $"{(string.IsNullOrWhiteSpace(q.PassengerName) ? "Passenger" : q.PassengerName)}  ·  {q.VehicleClass}",
                SubTitle = $"{q.PickupDateTime:g} — {q.PickupLocation}",
                Meta =
                    $"Booker: {q.BookerName}   •   " +
                    $"Drop: {(string.IsNullOrWhiteSpace(q.DropoffLocation) ? "As Directed" : q.DropoffLocation)}   •   " +
                    $"Created: {q.CreatedUtc.ToLocalTime():g}",
                Status = displayStatus,
                StatusColor = StatusColorForDisplay(displayStatus)
            };
        }
    }
}