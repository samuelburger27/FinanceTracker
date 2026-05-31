using System.Globalization;
using FinanceTracker.Resources;

namespace FinanceTracker.Converters;

public class BoolToSurfaceColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = value is bool b && b;
        return isSelected ? AppColors.Primary : Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
