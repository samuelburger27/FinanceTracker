using System.Globalization;
using FinanceTracker.Models;
using FinanceTracker.Resources;

namespace FinanceTracker.Converters;

public class TransactionTypeToSoftColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type == TransactionType.Income ? AppColors.SuccessSoft : AppColors.DangerSoft;
        }
        return AppColors.Gray200;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
