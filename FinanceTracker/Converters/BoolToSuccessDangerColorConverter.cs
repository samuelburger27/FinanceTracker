using System.Globalization;
using FinanceTracker.Resources;

namespace FinanceTracker.Converters;

public class BoolToSuccessDangerColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? AppColors.Success : AppColors.Danger;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
