using System.Globalization;

namespace DemoMobileApp.Converters;

public class IsAuthenticatedConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<string> claims)
            return GetValue(false);

        return GetValue(claims.Any());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private bool GetValue(bool value) =>
        Invert ? !value : value;
}
