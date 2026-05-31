using FinanceTracker.Resources;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.Controls;

public class DonutChartView : GraphicsView
{
    public static readonly BindableProperty SlicesProperty = BindableProperty.Create(
        nameof(Slices), typeof(IEnumerable<ChartSlice>), typeof(DonutChartView),
        propertyChanged: OnSlicesChanged);

    public static readonly BindableProperty CenterLabelProperty = BindableProperty.Create(
        nameof(CenterLabel), typeof(string), typeof(DonutChartView), string.Empty,
        propertyChanged: OnAnyChanged);

    public static readonly BindableProperty CenterValueProperty = BindableProperty.Create(
        nameof(CenterValue), typeof(string), typeof(DonutChartView), string.Empty,
        propertyChanged: OnAnyChanged);

    public IEnumerable<ChartSlice>? Slices
    {
        get => (IEnumerable<ChartSlice>?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public string CenterLabel
    {
        get => (string)GetValue(CenterLabelProperty);
        set => SetValue(CenterLabelProperty, value);
    }

    public string CenterValue
    {
        get => (string)GetValue(CenterValueProperty);
        set => SetValue(CenterValueProperty, value);
    }

    private readonly DonutDrawable _drawable = new();

    public DonutChartView()
    {
        Drawable = _drawable;
    }

    private static void OnSlicesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DonutChartView v)
        {
            v._drawable.Slices = (newValue as IEnumerable<ChartSlice>)?.ToList() ?? new List<ChartSlice>();
            v.Invalidate();
        }
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DonutChartView v)
        {
            v._drawable.CenterLabel = v.CenterLabel;
            v._drawable.CenterValue = v.CenterValue;
            v.Invalidate();
        }
    }

    private class DonutDrawable : IDrawable
    {
        public List<ChartSlice> Slices { get; set; } = new();
        public string CenterLabel { get; set; } = string.Empty;
        public string CenterValue { get; set; } = string.Empty;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var cx = dirtyRect.Center.X;
            var cy = dirtyRect.Center.Y;
            var outerRadius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 8;
            var innerRadius = outerRadius * 0.62f;
            var thickness = outerRadius - innerRadius;
            var midRadius = (outerRadius + innerRadius) / 2f;

            var rect = new RectF(cx - midRadius, cy - midRadius, midRadius * 2, midRadius * 2);
            var total = (float)Slices.Sum(s => (double)s.Value);

            if (total <= 0)
            {
                canvas.StrokeColor = AppColors.Gray200;
                canvas.StrokeSize = thickness;
                canvas.StrokeLineCap = LineCap.Butt;
                canvas.DrawCircle(cx, cy, midRadius);
            }
            else
            {
                canvas.StrokeSize = thickness;
                canvas.StrokeLineCap = LineCap.Butt;

                // If a single slice covers (essentially) the entire ring, DrawArc
                // collapses to 0° because start and end angles coincide. Draw a
                // full circle in that case.
                var nonZeroSlices = Slices.Where(s => s.Value > 0).ToList();
                if (nonZeroSlices.Count == 1)
                {
                    canvas.StrokeColor = nonZeroSlices[0].Color;
                    canvas.DrawCircle(cx, cy, midRadius);
                }
                else
                {
                    var startAngle = 90f;
                    foreach (var slice in nonZeroSlices)
                    {
                        var sweep = (float)((double)slice.Value / total) * 360f;
                        canvas.StrokeColor = slice.Color;
                        var endAngle = startAngle - sweep;
                        canvas.DrawArc(rect.Left, rect.Top, rect.Width, rect.Height,
                            startAngle, endAngle, true, false);
                        startAngle = endAngle;
                    }
                }
            }

            if (!string.IsNullOrEmpty(CenterValue))
            {
                canvas.FontColor = AppColors.Surface;
                canvas.FontSize = 18;
                canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                canvas.DrawString(CenterValue, cx - 100, cy - 14, 200, 22,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            if (!string.IsNullOrEmpty(CenterLabel))
            {
                canvas.FontColor = AppColors.TextMuted;
                canvas.FontSize = 11;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                canvas.DrawString(CenterLabel, cx - 100, cy + 10, 200, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
    }
}
