using BellwoodGlobal.Mobile.Models.Places;
using BellwoodGlobal.Mobile.Services;
using BellwoodGlobal.Mobile.ViewModels;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.Components;

/// <summary>
/// Reusable autocomplete component for location selection.
/// Provides real-time address suggestions via Google Places API.
/// </summary>
public partial class LocationAutocompleteView : ContentView
{
    private readonly LocationAutocompleteViewModel _viewModel;

    // Bindable properties
    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(
            nameof(Placeholder),
            typeof(string),
            typeof(LocationAutocompleteView),
            "Search for an address...");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // Events
    public event EventHandler<LocationSelectedEventArgs>? LocationSelected;

    public LocationAutocompleteView()
    {
        InitializeComponent();

        // Initialize ViewModel
        var placesService = ServiceHelper.GetRequiredService<IPlacesAutocompleteService>();
        _viewModel = new LocationAutocompleteViewModel(placesService);
        
        // Subscribe to ViewModel events
        _viewModel.LocationSelected += OnViewModelLocationSelected;

        // Set binding context
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Clears the search and predictions.
    /// </summary>
    public void Clear()
    {
        _viewModel.ClearSearch();
    }

    /// <summary>
    /// Gets the current search text.
    /// </summary>
    public string SearchText => _viewModel.SearchText;

    /// <summary>
    /// Gets whether a search is in progress.
    /// </summary>
    public bool IsBusy => _viewModel.IsBusy;

    private async void OnPredictionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is AutocompletePrediction prediction)
        {
            await _viewModel.SelectPredictionAsync(prediction);
        }
    }

    private void OnViewModelLocationSelected(object? sender, LocationSelectedEventArgs e)
    {
        // Bubble up the event
        LocationSelected?.Invoke(this, e);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // Cleanup when removed from visual tree
        if (Handler == null)
        {
            _viewModel.LocationSelected -= OnViewModelLocationSelected;
        }
    }
}
