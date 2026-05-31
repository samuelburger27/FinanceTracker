using FinanceTracker.Resources;

namespace FinanceTracker.Controls;

public class BarChartView : GraphicsView
{
    public static readonly BindableProperty GroupsProperty = BindableProperty.Create(
        nameof(Groups), typeof(IEnumerable<ChartBarGroup>), typeof(BarChartView),
        propertyChanged: OnGroupsChanged);

    public IEnumerable<ChartBarGroup>? Groups
    {
        get => (IEnumerable<ChartBarGroup>?)GetValue(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    private readonly BarDrawable _drawable = new();

    public BarChartView()
    {
        Drawable = _drawable;
    }

    private static void OnGroupsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BarChartView v)
        {
            v._drawable.Groups = (newValue as IEnumerable<ChartBarGroup>)?.ToList() ?? new List<ChartBarGroup>();
            v.Invalidate();
        }
    }

    private class BarDrawable : IDrawable
    {
        public List<ChartBarGroup> Groups { get; set; } = new();

        private static readonly Color IncomeColor = AppColors.Success;
        private static readonly Color ExpenseColor = AppColors.Danger;
        private static readonly Color AxisColor = AppColors.Gray300;
        private static readonly Color LabelColor = AppColors.TextMuted;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Groups.Count == 0) return;

            var padLeft = 36f;
            var padRight = 12f;
            var padTop = 10f;
            var padBottom = 28f;
            var plotLeft = dirtyRect.Left + padLeft;
            var plotRight = dirtyRect.Right - padRight;
            var plotTop = dirtyRect.Top + padTop;
            var plotBottom = dirtyRect.Bottom - padBottom;
            var plotWidth = plotRight - plotLeft;
            var plotHeight = plotBottom - plotTop;

            var maxValue = (float)Math.Max(
                Groups.Max(g => (double)g.Income),
                Groups.Max(g => (double)g.Expense));
            if (maxValue <= 0) maxValue = 1;

            // gridlines (4 lines)
            canvas.StrokeColor = AppColors.Gray200;
            canvas.StrokeSize = 1;
            canvas.FontColor = LabelColor;
            canvas.FontSize = 9;
            for (int i = 0; i <= 3; i++)
            {
                var y = plotBottom - (plotHeight * i / 3f);
                canvas.DrawLine(plotLeft, y, plotRight, y);
                var gridValue = maxValue * i / 3f;
                canvas.DrawString(FormatShort(gridValue), dirtyRect.Left, y - 7, padLeft - 4, 14,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // bars
            var groupSlot = plotWidth / Groups.Count;
            var barWidth = Math.Min(18f, groupSlot * 0.35f);
            var groupGap = 2f;

            for (int i = 0; i < Groups.Count; i++)
            {
                var g = Groups[i];
                var slotCenter = plotLeft + groupSlot * i + groupSlot / 2f;

                var incomeHeight = (float)((double)g.Income / maxValue) * plotHeight;
                var expenseHeight = (float)((double)g.Expense / maxValue) * plotHeight;

                var incomeX = slotCenter - barWidth - groupGap / 2f;
                var expenseX = slotCenter + groupGap / 2f;

                canvas.FillColor = IncomeColor;
                if (incomeHeight > 0)
                    canvas.FillRoundedRectangle(incomeX, plotBottom - incomeHeight, barWidth, incomeHeight, 3);

                canvas.FillColor = ExpenseColor;
                if (expenseHeight > 0)
                    canvas.FillRoundedRectangle(expenseX, plotBottom - expenseHeight, barWidth, expenseHeight, 3);

                // x-axis label
                canvas.FontColor = LabelColor;
                canvas.FontSize = 10;
                canvas.DrawString(g.Label, slotCenter - groupSlot / 2f, plotBottom + 6,
                    groupSlot, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
            }

            // baseline
            canvas.StrokeColor = AxisColor;
            canvas.StrokeSize = 1;
            canvas.DrawLine(plotLeft, plotBottom, plotRight, plotBottom);
        }

        private static string FormatShort(float v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000f:0.#}M";
            if (v >= 1_000) return $"{v / 1_000f:0.#}k";
            return $"{v:0}";
        }
    }
}
