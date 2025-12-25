using System.Collections.ObjectModel;
using System.Diagnostics;
using BellwoodGlobal.Mobile.Models.Places;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

/// <summary>
/// Test page for Google Places Autocomplete API.
/// Allows manual testing of autocomplete and place details functionality.
/// </summary>
public partial class PlacesTestPage : ContentPage
{
    private readonly IPlacesAutocompleteService _placesService;
    private string _sessionToken = string.Empty;
    private readonly ObservableCollection<AutocompletePrediction> _predictions = new();
    private CancellationTokenSource? _searchCts;

    public PlacesTestPage()
    {
        InitializeComponent();

        _placesService = ServiceHelper.GetRequiredService<IPlacesAutocompleteService>();
        
        PredictionsList.ItemsSource = _predictions;

        InitializeSession();
        UpdateQuotaStatus();
    }

    private void InitializeSession()
    {
        _sessionToken = _placesService.GenerateSessionToken();
        SessionTokenLabel.Text = _sessionToken;
        Log($"New session started: {_sessionToken[..8]}...");
        
        // Show current location bias
        _ = UpdateLocationBiasDisplayAsync();
    }

    private void UpdateQuotaStatus()
    {
        var dateKey = DateTime.Today.ToString("yyyyMMdd");
        var storedDate = Preferences.Get("PlacesQuota_Date", string.Empty);
        var autocompleteCount = Preferences.Get("PlacesQuota_AutocompleteCount", 0);
        var detailsCount = Preferences.Get("PlacesQuota_DetailsCount", 0);

        if (storedDate != dateKey)
        {
            QuotaStatusLabel.Text = "? Quota reset (new day)";
        }
        else
        {
            QuotaStatusLabel.Text = $"Autocomplete: {autocompleteCount}/1000 | Details: {detailsCount}/500";
        }
    }

    private void OnNewSession(object? sender, EventArgs e)
    {
        InitializeSession();
        _predictions.Clear();
        ClearDetails();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Cancel previous search
        _searchCts?.Cancel();

        if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Length < 3)
        {
            _predictions.Clear();
            return;
        }

        // Debounce: wait 300ms after last keystroke
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, ct);
                await SearchAsync(e.NewTextValue, ct);
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled, ignore
            }
        }, ct);
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        var query = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlert("Error", "Please enter a search query", "OK");
            return;
        }

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await SearchAsync(query, _searchCts.Token);
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            Log($"Searching for: '{query}'");

            var stopwatch = Stopwatch.StartNew();
            var results = await _placesService.GetPredictionsAsync(query, _sessionToken, ct);
            stopwatch.Stop();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _predictions.Clear();
                foreach (var prediction in results)
                {
                    _predictions.Add(prediction);
                }

                Log($"? Found {results.Length} predictions in {stopwatch.ElapsedMilliseconds}ms");
                UpdateQuotaStatus();
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Log($"? Error: {ex.Message}");
            });
        }
    }

    private async void OnPredictionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not AutocompletePrediction prediction)
            return;

        Log($"Selected: {prediction.Description}");
        Log($"Fetching details for place ID: {prediction.PlaceId}");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var location = await _placesService.GetLocationFromPlaceIdAsync(prediction.PlaceId);
            stopwatch.Stop();

            if (location != null)
            {
                DetailsLabelLabel.Text = location.Label;
                DetailsAddressLabel.Text = location.Address;
                DetailsCoordinatesLabel.Text = location.HasCoordinates 
                    ? $"{location.Latitude:F6}, {location.Longitude:F6}"
                    : "No coordinates";
                DetailsPlaceIdLabel.Text = location.PlaceId ?? "N/A";

                Log($"? Place details retrieved in {stopwatch.ElapsedMilliseconds}ms");
                Log($"   Label: {location.Label}");
                Log($"   Address: {location.Address}");
                if (location.HasCoordinates)
                {
                    Log($"   Coordinates: {location.Latitude:F6}, {location.Longitude:F6}");
                }

                UpdateQuotaStatus();

                // Start new session after selection
                InitializeSession();
            }
            else
            {
                Log("? Failed to retrieve place details");
                await DisplayAlert("Error", "Could not retrieve place details", "OK");
            }
        }
        catch (Exception ex)
        {
            Log($"? Error fetching details: {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void ClearDetails()
    {
        DetailsLabelLabel.Text = string.Empty;
        DetailsAddressLabel.Text = string.Empty;
        DetailsCoordinatesLabel.Text = string.Empty;
        DetailsPlaceIdLabel.Text = string.Empty;
    }

    private void OnClearLog(object? sender, EventArgs e)
    {
        LogEditor.Text = string.Empty;
    }

    private async void OnRefreshLocationBias(object? sender, EventArgs e)
    {
        Log("Refreshing location bias...");
        await UpdateLocationBiasDisplayAsync();
    }

    private async Task UpdateLocationBiasDisplayAsync()
    {
        try
        {
            // Access the location picker service directly
            var locationPicker = ServiceHelper.GetRequiredService<ILocationPickerService>();
            var currentLoc = await locationPicker.GetCurrentLocationAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (currentLoc?.HasCoordinates == true)
                {
                    BiasLocationLabel.Text = $"? {currentLoc.Latitude:F4}, {currentLoc.Longitude:F4}\n{currentLoc.Address}";
                    BiasLocationLabel.TextColor = Colors.LightGreen;
                    Log($"Location bias: {currentLoc.Latitude:F4}, {currentLoc.Longitude:F4}");
                }
                else
                {
                    BiasLocationLabel.Text = "?? Region-only (US)\nLocation unavailable";
                    BiasLocationLabel.TextColor = Colors.Orange;
                    Log("Location bias: Region-only (no GPS)");
                }
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BiasLocationLabel.Text = "? Error getting location";
                BiasLocationLabel.TextColor = Colors.Red;
                Log($"Error getting location bias: {ex.Message}");
            });
        }
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}\n";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogEditor.Text += logMessage;

            // Auto-scroll to bottom
            if (LogEditor.Handler?.PlatformView != null)
            {
                // Scroll is handled differently per platform, this is a simple approach
                LogEditor.Focus();
            }
        });

#if DEBUG
        Debug.WriteLine($"[PlacesTestPage] {message}");
#endif
    }
}
