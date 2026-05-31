using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Controls;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAuthService _authService;

    private static readonly string[] SlicePalette =
    {
        "#6366F1", "#06B6D4", "#F59E0B", "#EF4444",
        "#10B981", "#8B5CF6", "#EC4899", "#14B8A6",
        "#F97316", "#84CC16", "#3B82F6", "#A855F7",
    };

    public enum Period { ThisMonth, LastMonth, Last6Months, AllTime }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsThisMonth))]
    [NotifyPropertyChangedFor(nameof(IsLastMonth))]
    [NotifyPropertyChangedFor(nameof(IsLast6Months))]
    [NotifyPropertyChangedFor(nameof(IsAllTime))]
    private Period _selectedPeriod = Period.ThisMonth;

    public bool IsThisMonth => SelectedPeriod == Period.ThisMonth;
    public bool IsLastMonth => SelectedPeriod == Period.LastMonth;
    public bool IsLast6Months => SelectedPeriod == Period.Last6Months;
    public bool IsAllTime => SelectedPeriod == Period.AllTime;

    [ObservableProperty] private string _periodLabel = "This Month";
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private decimal _netChange;
    [ObservableProperty] private int _transactionCount;
    [ObservableProperty] private decimal _savingsRate;
    [ObservableProperty] private bool _isSavingsPositive;
    [ObservableProperty] private string _topCategoryName = "—";
    [ObservableProperty] private decimal _topCategoryAmount;
    [ObservableProperty] private bool _hasExpenses;
    [ObservableProperty] private bool _hasIncome;
    [ObservableProperty] private bool _hasTrendData;
    [ObservableProperty] private bool _hasDailySeries;
    [ObservableProperty] private bool _hasDayOfWeekData;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    private ObservableCollection<ChartSlice> _expenseSlices = new();

    [ObservableProperty]
    private ObservableCollection<ChartSlice> _legendItems = new();

    [ObservableProperty]
    private ObservableCollection<ChartSlice> _incomeSlices = new();

    [ObservableProperty]
    private ObservableCollection<ChartSlice> _incomeLegendItems = new();

    [ObservableProperty]
    private ObservableCollection<ChartBarGroup> _trendGroups = new();

    [ObservableProperty]
    private ObservableCollection<ChartBarGroup> _dayOfWeekGroups = new();

    [ObservableProperty]
    private ObservableCollection<ChartLinePoint> _dailyBalancePoints = new();

    [ObservableProperty]
    private ObservableCollection<CategoryBreakdownItem> _topCategories = new();

    [ObservableProperty]
    private ObservableCollection<CategoryBreakdownItem> _topIncomeCategories = new();

    public StatisticsViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAuthService authService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_authService.CurrentUser == null) return;
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            var userId = _authService.CurrentUser.Id;
            var period = SelectedPeriod;

            var result = await Task.Run(async () =>
            {
                var all = await _transactionService.GetTransactionsAsync(userId);
                var cats = await _categoryService.GetCategoriesAsync(userId);
                var lookup = cats
                    .GroupBy(c => c.Id)
                    .ToDictionary(g => g.Key, g => g.First());
                return Compute(all, period, lookup);
            });

            ApplyResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetPeriod(string period)
    {
        if (Enum.TryParse<Period>(period, out var parsed))
        {
            SelectedPeriod = parsed;
            await LoadAsync();
        }
    }

    private static Color ColorForCategory(int categoryId)
    {
        var idx = (uint)categoryId % SlicePalette.Length;
        return Color.FromArgb(SlicePalette[idx]);
    }

    private static StatsResult Compute(IReadOnlyList<Transaction> all, Period period, Dictionary<int, Category> categoryLookup)
    {
        var (from, to, label) = ResolveRange(period);

        var filtered = all
            .Where(t => (from == null || t.Date >= from) && (to == null || t.Date <= to))
            .ToList();

        var income = filtered.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = filtered.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var net = income - expense;
        var rate = income > 0 ? Math.Round(net / income * 100, 1) : 0m;

        var (expSlices, expBreakdown, topName, topAmount) = BuildBreakdown(
            filtered.Where(t => t.Type == TransactionType.Expense), expense, categoryLookup);
        var (incSlices, incBreakdown, _, _) = BuildBreakdown(
            filtered.Where(t => t.Type == TransactionType.Income), income, categoryLookup);

        var trend = BuildMonthlyTrend(all);
        var hasTrend = trend.Any(g => g.Income > 0 || g.Expense > 0);

        var daily = BuildDailyBalance(filtered, from, to);
        var hasDaily = daily.Count >= 2;

        var dow = BuildDayOfWeek(filtered);
        var hasDow = dow.Any(g => g.Expense > 0);

        return new StatsResult(
            label, income, expense, net, filtered.Count, rate, rate >= 0,
            expSlices.Count > 0, incSlices.Count > 0,
            topName, topAmount,
            expSlices, expBreakdown, incSlices, incBreakdown,
            trend, hasTrend, daily, hasDaily, dow, hasDow);
    }

    private static (List<ChartSlice> Slices, List<CategoryBreakdownItem> Breakdown, string TopName, decimal TopAmount)
        BuildBreakdown(IEnumerable<Transaction> transactions, decimal totalForPercent, Dictionary<int, Category> categoryLookup)
    {
        var slices = new List<ChartSlice>();
        var breakdown = new List<CategoryBreakdownItem>();

        var grouped = transactions
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                Id = g.Key,
                Name = ResolveName(g.Key, g.First(), categoryLookup),
                Amount = g.Sum(x => x.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        if (grouped.Count == 0) return (slices, breakdown, "—", 0m);

        for (var i = 0; i < grouped.Count; i++)
        {
            var color = ColorForCategory(grouped[i].Id);
            slices.Add(new ChartSlice(grouped[i].Name, grouped[i].Amount, color));
            if (i < 6)
            {
                var pct = totalForPercent > 0
                    ? Math.Round(grouped[i].Amount / totalForPercent * 100, 1)
                    : 0m;
                breakdown.Add(new CategoryBreakdownItem(grouped[i].Name, grouped[i].Amount, pct, color));
            }
        }

        return (slices, breakdown, grouped[0].Name, grouped[0].Amount);
    }

    private static string ResolveName(int categoryId, Transaction sample, Dictionary<int, Category> lookup)
    {
        if (lookup.TryGetValue(categoryId, out var cat) && !string.IsNullOrWhiteSpace(cat.Name))
            return cat.Name;
        if (!string.IsNullOrWhiteSpace(sample.Category?.Name))
            return sample.Category!.Name;
        return $"Category #{categoryId}";
    }

    private static List<ChartBarGroup> BuildMonthlyTrend(IReadOnlyList<Transaction> all)
    {
        var now = DateTime.Now;
        var trend = new List<ChartBarGroup>();
        for (var i = 5; i >= 0; i--)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            decimal mIncome = 0, mExpense = 0;
            foreach (var t in all)
            {
                if (t.Date < monthStart || t.Date > monthEnd) continue;
                if (t.Type == TransactionType.Income) mIncome += t.Amount;
                else mExpense += t.Amount;
            }
            var lbl = monthStart.ToString("MMM", CultureInfo.InvariantCulture);
            trend.Add(new ChartBarGroup(lbl, mIncome, mExpense));
        }
        return trend;
    }

    private static List<ChartLinePoint> BuildDailyBalance(List<Transaction> filtered, DateTime? from, DateTime? to)
    {
        if (filtered.Count == 0) return new List<ChartLinePoint>();

        var rangeStart = (from ?? filtered.Min(t => t.Date)).Date;
        var rangeEnd = (to ?? filtered.Max(t => t.Date)).Date;
        if (rangeEnd < rangeStart) return new List<ChartLinePoint>();

        var totalDays = (rangeEnd - rangeStart).Days + 1;
        // Cap to ~120 buckets to keep rendering crisp on long ranges.
        var bucketDays = Math.Max(1, (int)Math.Ceiling(totalDays / 120.0));
        var bucketCount = (int)Math.Ceiling(totalDays / (double)bucketDays);

        var sums = new decimal[bucketCount];
        foreach (var t in filtered)
        {
            var dayIndex = (t.Date.Date - rangeStart).Days;
            if (dayIndex < 0 || dayIndex >= totalDays) continue;
            var bucket = dayIndex / bucketDays;
            sums[bucket] += t.Type == TransactionType.Income ? t.Amount : -t.Amount;
        }

        var points = new List<ChartLinePoint>(bucketCount);
        decimal cumulative = 0m;
        var fmt = totalDays > 60 ? "MMM d" : "MMM dd";
        for (var b = 0; b < bucketCount; b++)
        {
            cumulative += sums[b];
            var bucketDate = rangeStart.AddDays((long)b * bucketDays);
            points.Add(new ChartLinePoint(bucketDate.ToString(fmt, CultureInfo.InvariantCulture), cumulative));
        }
        return points;
    }

    private static List<ChartBarGroup> BuildDayOfWeek(List<Transaction> filtered)
    {
        // Mon..Sun for readability.
        var order = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        };
        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        var sums = new decimal[7];
        foreach (var t in filtered)
        {
            if (t.Type != TransactionType.Expense) continue;
            var idx = Array.IndexOf(order, t.Date.DayOfWeek);
            if (idx >= 0) sums[idx] += t.Amount;
        }

        var groups = new List<ChartBarGroup>(7);
        for (var i = 0; i < 7; i++)
            groups.Add(new ChartBarGroup(labels[i], 0m, sums[i]));
        return groups;
    }

    private void ApplyResult(StatsResult r)
    {
        PeriodLabel = r.PeriodLabel;
        TotalIncome = r.TotalIncome;
        TotalExpense = r.TotalExpense;
        NetChange = r.NetChange;
        TransactionCount = r.TransactionCount;
        SavingsRate = r.SavingsRate;
        IsSavingsPositive = r.IsSavingsPositive;
        HasExpenses = r.HasExpenses;
        HasIncome = r.HasIncome;
        TopCategoryName = r.TopCategoryName;
        TopCategoryAmount = r.TopCategoryAmount;
        ExpenseSlices = new ObservableCollection<ChartSlice>(r.ExpenseSlices);
        LegendItems = new ObservableCollection<ChartSlice>(r.ExpenseSlices);
        IncomeSlices = new ObservableCollection<ChartSlice>(r.IncomeSlices);
        IncomeLegendItems = new ObservableCollection<ChartSlice>(r.IncomeSlices);
        TopCategories = new ObservableCollection<CategoryBreakdownItem>(r.ExpenseBreakdown);
        TopIncomeCategories = new ObservableCollection<CategoryBreakdownItem>(r.IncomeBreakdown);
        TrendGroups = new ObservableCollection<ChartBarGroup>(r.TrendGroups);
        HasTrendData = r.HasTrendData;
        DailyBalancePoints = new ObservableCollection<ChartLinePoint>(r.DailyBalance);
        HasDailySeries = r.HasDailySeries;
        DayOfWeekGroups = new ObservableCollection<ChartBarGroup>(r.DayOfWeek);
        HasDayOfWeekData = r.HasDayOfWeekData;
    }

    private static (DateTime? From, DateTime? To, string Label) ResolveRange(Period period)
    {
        var now = DateTime.Now;
        switch (period)
        {
            case Period.ThisMonth:
                {
                    var from = new DateTime(now.Year, now.Month, 1);
                    var to = from.AddMonths(1).AddTicks(-1);
                    return (from, to, now.ToString("MMMM yyyy", CultureInfo.InvariantCulture));
                }
            case Period.LastMonth:
                {
                    var from = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    var to = from.AddMonths(1).AddTicks(-1);
                    return (from, to, from.ToString("MMMM yyyy", CultureInfo.InvariantCulture));
                }
            case Period.Last6Months:
                {
                    var from = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
                    return (from, null, "Last 6 Months");
                }
            case Period.AllTime:
            default:
                return (null, null, "All Time");
        }
    }

    private record StatsResult(
        string PeriodLabel,
        decimal TotalIncome,
        decimal TotalExpense,
        decimal NetChange,
        int TransactionCount,
        decimal SavingsRate,
        bool IsSavingsPositive,
        bool HasExpenses,
        bool HasIncome,
        string TopCategoryName,
        decimal TopCategoryAmount,
        List<ChartSlice> ExpenseSlices,
        List<CategoryBreakdownItem> ExpenseBreakdown,
        List<ChartSlice> IncomeSlices,
        List<CategoryBreakdownItem> IncomeBreakdown,
        List<ChartBarGroup> TrendGroups,
        bool HasTrendData,
        List<ChartLinePoint> DailyBalance,
        bool HasDailySeries,
        List<ChartBarGroup> DayOfWeek,
        bool HasDayOfWeekData);
}
