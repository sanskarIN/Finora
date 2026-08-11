using System.Globalization;
using Finora.Application;
using Finora.Domain;

namespace Finora.App;

public sealed class PrivacyMoneyConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not long minor || values[1] is not string currency || string.IsNullOrWhiteSpace(currency))
            return "—";

        try
        {
            var settings = ServiceHelper.Get<IAppSettingsService>();
            if (settings.PrivacyMode || settings.HideAmountsOnLaunch) return "••••";
            return new Money(minor, currency).Format(culture);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            return "—";
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => targetTypes.Select(_ => BindableProperty.UnsetValue).ToArray();
}
