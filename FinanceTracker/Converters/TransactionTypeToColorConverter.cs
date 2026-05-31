using System.Globalization;
using FinanceTracker.Models;
using FinanceTracker.Resources;

namespace FinanceTracker.Converters;

public class TransactionTypeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type == TransactionType.Income ? AppColors.Success : AppColors.Danger;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
