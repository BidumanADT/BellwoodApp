using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BellwoodGlobal.Mobile.Models.Places;
using BellwoodGlobal.Mobile.Services;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.ViewModels;

/// <summary>
/// ViewModel for LocationAutocompleteView component.
/// Handles autocomplete search logic, debouncing, and place selection.
/// </summary>
public sealed class LocationAutocompleteViewModel : INotifyPropertyChanged
{
    private readonly IPlacesAutocompleteService _placesService;
    private CancellationTokenSource? _searchCts;
    
    // Backing fields
    private string _searchText = string.Empty;
    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private bool _hasPredictions;
    
    // Session management
    private string _sessionToken = string.Empty;
    
    // Debounce settings
    private const int DebounceDelayMs = 300;
    private const int MinimumSearchLength = 3;

    public LocationAutocompleteViewModel(IPlacesAutocompleteService placesService)
    {
        _placesService = placesService;
        Predictions = new ObservableCollection<AutocompletePrediction>();
        
        // Initialize session token
        RegenerateSessionToken();
        
        // Commands
        ClearCommand = new Command(OnClear);
        
#if DEBUG
        Debug.WriteLine("[LocationAutocompleteViewModel] Initialized");
#endif
    }

    // ========== Properties ==========

    /// <summary>
    /// Current search text entered by user.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                OnSearchTextChanged();
            }
        }
    }

    /// <summary>
    /// Collection of autocomplete predictions.
    /// </summary>
    public ObservableCollection<AutocompletePrediction> Predictions { get; }

    /// <summary>
    /// Indicates if an API request is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Error message to display (empty if no error).
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// True if there's an error message.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    /// <summary>
    /// True if predictions are available.
    /// </summary>
    public bool HasPredictions
    {
        get => _hasPredictions;
        private set
        {
            if (_hasPredictions != value)
            {
                _hasPredictions = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Current session token for API requests.
    /// </summary>
    public string SessionToken
    {
        get => _sessionToken;
        private set
        {
            if (_sessionToken != value)
            {
                _sessionToken = value;
                OnPropertyChanged();
            }
        }
    }

    // ========== Commands ==========

    public ICommand ClearCommand { get; }

    // ========== Events ==========

    /// <summary>
    /// Raised when a location is selected from predictions.
    /// </summary>
    public event EventHandler<LocationSelectedEventArgs>? LocationSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ========== Public Methods ==========

    /// <summary>
    /// Handles selection of a prediction.
    /// Fetches place details and raises LocationSelected event.
    /// </summary>
    public async Task SelectPredictionAsync(AutocompletePrediction prediction)
    {
        if (prediction == null) return;

#if DEBUG
        Debug.WriteLine($"[LocationAutocompleteViewModel] Selecting prediction: {prediction.Description}");
#endif

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var location = await _placesService.GetLocationFromPlaceIdAsync(prediction.PlaceId);

            if (location != null)
            {
                // Raise LocationSelected event
                LocationSelected?.Invoke(this, new LocationSelectedEventArgs(location));

                // Clear search and predictions
                ClearSearch();

                // Regenerate session token after selection
                RegenerateSessionToken();

#if DEBUG
                Debug.WriteLine($"[LocationAutocompleteViewModel] Location selected: {location.Address}");
#endif
            }
            else
            {
                ErrorMessage = "Unable to get location details. Please try another address.";
#if DEBUG
                Debug.WriteLine($"[LocationAutocompleteViewModel] Failed to get place details for {prediction.PlaceId}");
#endif
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading location details.";
#if DEBUG
            Debug.WriteLine($"[LocationAutocompleteViewModel] Error selecting prediction: {ex.Message}");
#endif
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears search text, predictions, and errors.
    /// </summary>
    public void ClearSearch()
    {
        SearchText = string.Empty;
        Predictions.Clear();
        ErrorMessage = string.Empty;
        HasPredictions = false;
        
        // Cancel any in-flight search
        _searchCts?.Cancel();
        
#if DEBUG
        Debug.WriteLine("[LocationAutocompleteViewModel] Search cleared");
#endif
    }

    // ========== Private Methods ==========

    private void OnSearchTextChanged()
    {
        // Cancel previous search
        _searchCts?.Cancel();

        // Clear error
        ErrorMessage = string.Empty;

        // If text is empty, clear predictions
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Predictions.Clear();
            HasPredictions = false;
            return;
        }

        // If text is too short, don't search yet
        if (SearchText.Length < MinimumSearchLength)
        {
            Predictions.Clear();
            HasPredictions = false;
            return;
        }

        // Debounce: wait before searching
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelayMs, ct);
                await SearchAsync(SearchText, ct);
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled, ignore
            }
        }, ct);
    }

    private async Task SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

#if DEBUG
        Debug.WriteLine($"[LocationAutocompleteViewModel] Searching for: '{query}'");
#endif

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var predictions = await _placesService.GetPredictionsAsync(
                query, 
                SessionToken, 
                ct);

            // Update predictions on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Predictions.Clear();
                
                if (predictions.Length > 0)
                {
                    foreach (var prediction in predictions)
                    {
                        Predictions.Add(prediction);
                    }
                    HasPredictions = true;
                    
#if DEBUG
                    Debug.WriteLine($"[LocationAutocompleteViewModel] Found {predictions.Length} predictions");
#endif
                }
                else
                {
                    HasPredictions = false;
                    ErrorMessage = "No suggestions found. Try a different address.";
                    
#if DEBUG
                    Debug.WriteLine("[LocationAutocompleteViewModel] No predictions found");
#endif
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Search cancelled, ignore
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ErrorMessage = "Unable to search addresses. Check your connection.";
                HasPredictions = false;
                
#if DEBUG
                Debug.WriteLine($"[LocationAutocompleteViewModel] Search error: {ex.Message}");
#endif
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsBusy = false;
            });
        }
    }

    private void OnClear()
    {
        ClearSearch();
    }

    private void RegenerateSessionToken()
    {
        SessionToken = _placesService.GenerateSessionToken();
        
#if DEBUG
        Debug.WriteLine($"[LocationAutocompleteViewModel] New session token: {SessionToken[..8]}...");
#endif
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Event args for LocationSelected event.
/// </summary>
public sealed class LocationSelectedEventArgs : EventArgs
{
    public LocationSelectedEventArgs(AppLocation location)
    {
        Location = location;
    }

    /// <summary>
    /// The selected location with label, address, and coordinates.
    /// </summary>
    public AppLocation Location { get; }
}
