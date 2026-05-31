using FinanceTracker.Resources;
using Microsoft.Maui.Graphics;

namespace FinanceTracker.Controls;

public record ChartLinePoint(string Label, decimal Value);

public class LineChartView : GraphicsView
{
    public static readonly BindableProperty PointsProperty = BindableProperty.Create(
        nameof(Points), typeof(IEnumerable<ChartLinePoint>), typeof(LineChartView),
        propertyChanged: OnPointsChanged);

    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(
        nameof(LineColor), typeof(Color), typeof(LineChartView),
        AppColors.Primary,
        propertyChanged: OnLineColorChanged);

    public static readonly BindableProperty FillAreaProperty = BindableProperty.Create(
        nameof(FillArea), typeof(bool), typeof(LineChartView), true,
        propertyChanged: OnFillAreaChanged);

    public IEnumerable<ChartLinePoint>? Points
    {
        get => (IEnumerable<ChartLinePoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public bool FillArea
    {
        get => (bool)GetValue(FillAreaProperty);
        set => SetValue(FillAreaProperty, value);
    }

    private readonly LineDrawable _drawable = new();

    public LineChartView()
    {
        Drawable = _drawable;
    }

    private static void OnPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LineChartView v)
        {
            v._drawable.Points = (newValue as IEnumerable<ChartLinePoint>)?.ToList() ?? new List<ChartLinePoint>();
            v.Invalidate();
        }
    }

    private static void OnLineColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LineChartView v && newValue is Color c)
        {
            v._drawable.LineColor = c;
            v.Invalidate();
        }
    }

    private static void OnFillAreaChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LineChartView v && newValue is bool b)
        {
            v._drawable.FillArea = b;
            v.Invalidate();
        }
    }

    private class LineDrawable : IDrawable
    {
        public List<ChartLinePoint> Points { get; set; } = new();
        public Color LineColor { get; set; } = AppColors.Primary;
        public bool FillArea { get; set; } = true;

        private static readonly Color GridColor = AppColors.Gray200;
        private static readonly Color AxisColor = AppColors.Gray300;
        private static readonly Color LabelColor = AppColors.TextMuted;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Points.Count == 0) return;

            var padLeft = 40f;
            var padRight = 12f;
            var padTop = 10f;
            var padBottom = 28f;
            var plotLeft = dirtyRect.Left + padLeft;
            var plotRight = dirtyRect.Right - padRight;
            var plotTop = dirtyRect.Top + padTop;
            var plotBottom = dirtyRect.Bottom - padBottom;
            var plotWidth = plotRight - plotLeft;
            var plotHeight = plotBottom - plotTop;

            var maxValue = (float)Math.Max(1.0, Points.Max(p => (double)p.Value));
            var minValue = (float)Math.Min(0.0, Points.Min(p => (double)p.Value));
            var range = Math.Max(1f, maxValue - minValue);

            // gridlines (4 horizontal lines)
            canvas.StrokeColor = GridColor;
            canvas.StrokeSize = 1;
            canvas.FontColor = LabelColor;
            canvas.FontSize = 9;
            for (var i = 0; i <= 3; i++)
            {
                var y = plotBottom - (plotHeight * i / 3f);
                canvas.DrawLine(plotLeft, y, plotRight, y);
                var gridValue = minValue + range * i / 3f;
                canvas.DrawString(FormatShort(gridValue), dirtyRect.Left, y - 7, padLeft - 4, 14,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // map points to coordinates
            var stepX = Points.Count > 1 ? plotWidth / (Points.Count - 1) : plotWidth;
            var coords = new PointF[Points.Count];
            for (var i = 0; i < Points.Count; i++)
            {
                var x = Points.Count > 1 ? plotLeft + stepX * i : plotLeft + plotWidth / 2f;
                var v = (float)((double)Points[i].Value);
                var y = plotBottom - ((v - minValue) / range) * plotHeight;
                coords[i] = new PointF(x, y);
            }

            // area fill
            if (FillArea && coords.Length > 0)
            {
                var path = new PathF();
                path.MoveTo(coords[0].X, plotBottom);
                foreach (var p in coords) path.LineTo(p.X, p.Y);
                path.LineTo(coords[^1].X, plotBottom);
                path.Close();
                canvas.FillColor = LineColor.WithAlpha(0.18f);
                canvas.FillPath(path);
            }

            // line
            canvas.StrokeColor = LineColor;
            canvas.StrokeSize = 2.5f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            for (var i = 1; i < coords.Length; i++)
                canvas.DrawLine(coords[i - 1], coords[i]);

            // dots
            canvas.FillColor = LineColor;
            foreach (var p in coords)
                canvas.FillCircle(p.X, p.Y, 2.5f);

            // x-axis labels — show first, middle, last to avoid clutter
            canvas.FontColor = LabelColor;
            canvas.FontSize = 10;
            var labelIndices = new HashSet<int> { 0, Points.Count - 1 };
            if (Points.Count > 4) labelIndices.Add(Points.Count / 2);
            foreach (var i in labelIndices)
            {
                var lbl = Points[i].Label;
                canvas.DrawString(lbl, coords[i].X - 30, plotBottom + 6, 60, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }

            // baseline
            canvas.StrokeColor = AxisColor;
            canvas.StrokeSize = 1;
            canvas.DrawLine(plotLeft, plotBottom, plotRight, plotBottom);
        }

        private static string FormatShort(float v)
        {
            var sign = v < 0 ? "-" : string.Empty;
            v = Math.Abs(v);
            if (v >= 1_000_000) return $"{sign}{v / 1_000_000f:0.#}M";
            if (v >= 1_000) return $"{sign}{v / 1_000f:0.#}k";
            return $"{sign}{v:0}";
        }
    }
}
