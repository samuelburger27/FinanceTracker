using System.Globalization;
using FinanceTracker.Models;

namespace FinanceTracker.Converters;

public class TransactionTypeToSignConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type == TransactionType.Income ? "+" : "−";
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
