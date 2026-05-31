namespace FinanceTracker.Resources;

// Code-side mirror of Resources/Styles/Colors.xaml. XAML still pulls from the .xaml dictionary;
// converters and IDrawable controls (which run before/outside the XAML resource scope) read here.
// Keep names and values aligned with Colors.xaml.
internal static class AppColors
{
    public static readonly Color Primary = Color.FromArgb("#6366F1");
    public static readonly Color Success = Color.FromArgb("#10B981");
    public static readonly Color SuccessSoft = Color.FromArgb("#D1FAE5");
    public static readonly Color Danger = Color.FromArgb("#EF4444");
    public static readonly Color DangerSoft = Color.FromArgb("#FEE2E2");
    public static readonly Color Surface = Color.FromArgb("#FFFFFF");
    public static readonly Color TextMuted = Color.FromArgb("#64748B");
    public static readonly Color Gray200 = Color.FromArgb("#E2E8F0");
    public static readonly Color Gray300 = Color.FromArgb("#CBD5E1");
}
