using System.Globalization;

namespace FinanceTracker.Converters;

public class PercentToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is decimal pct ? (double)(pct * 1.8m) : 0.0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
