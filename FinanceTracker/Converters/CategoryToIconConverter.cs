using System.Globalization;
using FinanceTracker.Models;

namespace FinanceTracker.Converters;

public class CategoryToIconConverter : IValueConverter
{
    private static readonly Dictionary<string, string> IconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Housing", "\U0001F3E0" },
        { "Food", "\U0001F37D" },
        { "Groceries", "\U0001F6D2" },
        { "Salary", "\U0001F4B0" },
        { "Utilities", "\U0001F4A1" },
        { "Entertainment", "\U0001F3AC" },
        { "Transport", "\U0001F697" },
        { "Healthcare", "⚕️" },
        { "Shopping", "\U0001F6CD️" },
        { "Travel", "✈️" },
        { "Education", "\U0001F393" },
        { "Gifts", "\U0001F381" },
        { "Savings", "\U0001F3E6" },
        { "Other", "\U0001F4CC" },
    };

    // Stable fallback palette for user-defined categories.
    private static readonly string[] FallbackIcons =
    {
        "\U0001F3F7️", "\U0001F3AF", "\U0001F9E9", "⭐",
        "\U0001F516", "\U0001F4C2", "\U0001FA99", "\U0001F4D2",
        "\U0001F9F0", "\U0001F4E6", "\U0001F381", "\U0001F4BC",
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value as string ?? (value as Category)?.Name;
        if (string.IsNullOrEmpty(name)) return "\U0001F4CC";
        if (IconMap.TryGetValue(name, out var icon)) return icon;
        var hash = 0;
        foreach (var ch in name) hash = unchecked(hash * 31 + ch);
        var idx = (uint)hash % FallbackIcons.Length;
        return FallbackIcons[idx];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
