using BellwoodGlobal.Mobile.ViewModels;

namespace BellwoodGlobal.Mobile.Pages;

/// <summary>
/// Test page demonstrating LocationAutocompleteView component usage.
/// Shows how to integrate the component into any page.
/// </summary>
public partial class LocationAutocompleteTestPage : ContentPage
{
    public LocationAutocompleteTestPage()
    {
        InitializeComponent();
    }

    private void OnPickupLocationSelected(object? sender, LocationSelectedEventArgs e)
    {
        var location = e.Location;

        PickupLabel.Text = location.Label ?? "-";
        PickupAddress.Text = location.Address ?? "-";
        
        if (location.HasCoordinates)
        {
            PickupCoords.Text = $"{location.Latitude:F6}, {location.Longitude:F6}";
        }
        else
        {
            PickupCoords.Text = "No coordinates";
        }

        // Show success toast
        DisplayAlert("Pickup Selected", 
            $"{location.Label}\n{location.Address}", 
            "OK");
    }

    private void OnDropoffLocationSelected(object? sender, LocationSelectedEventArgs e)
    {
        var location = e.Location;

        DropoffLabel.Text = location.Label ?? "-";
        DropoffAddress.Text = location.Address ?? "-";
        
        if (location.HasCoordinates)
        {
            DropoffCoords.Text = $"{location.Latitude:F6}, {location.Longitude:F6}";
        }
        else
        {
            DropoffCoords.Text = "No coordinates";
        }

        // Show success toast
        DisplayAlert("Dropoff Selected", 
            $"{location.Label}\n{location.Address}", 
            "OK");
    }

    private void OnClearBoth(object sender, EventArgs e)
    {
        PickupAutocomplete.Clear();
        DropoffAutocomplete.Clear();

        PickupLabel.Text = "-";
        PickupAddress.Text = "-";
        PickupCoords.Text = "-";

        DropoffLabel.Text = "-";
        DropoffAddress.Text = "-";
        DropoffCoords.Text = "-";
    }
}
