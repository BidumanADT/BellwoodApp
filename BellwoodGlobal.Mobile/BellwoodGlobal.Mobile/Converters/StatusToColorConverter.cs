using System.Globalization;

namespace BellwoodGlobal.Mobile.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.ToLowerInvariant() ?? "";
        return s switch
        {
            "created" => Application.Current!.Resources["ChipPending"],
            "confirmed" => Application.Current!.Resources["ChipPriced"],
            "completed" => Application.Current!.Resources["ChipOther"],
            "cancelled" => Application.Current!.Resources["ChipDeclined"],
            _ => Application.Current!.Resources["ChipOther"],
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
