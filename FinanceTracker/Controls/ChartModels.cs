namespace FinanceTracker.Controls;

public record ChartSlice(string Label, decimal Value, Color Color);

public record ChartBarGroup(string Label, decimal Income, decimal Expense);

public record CategoryBreakdownItem(string Label, decimal Amount, decimal Percentage, Color Color);
