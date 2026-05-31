using System.Globalization;
using FinanceTracker.Resources;

namespace FinanceTracker.Converters;

public class BoolToPrimaryTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = value is bool b && b;
        return isSelected ? Colors.White : AppColors.TextMuted;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
