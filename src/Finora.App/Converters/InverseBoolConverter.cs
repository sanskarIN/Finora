using System.Globalization;

namespace Finora.App;

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool flag && !flag;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool flag && !flag;
}
