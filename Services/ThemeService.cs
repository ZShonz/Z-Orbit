using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace CharacterLauncher.Services;

public static class ThemeService
{
    public static readonly string[] ThemeIds = ["terracotta", "ice", "violet", "cream"];
    public static string Current { get; private set; } = "terracotta";

    public static string Normalize(string? id) => ThemeIds.Contains(id) ? id! : "terracotta";

    public static void Apply(string? id)
    {
        Current = Normalize(id);
        var resources = System.Windows.Application.Current.Resources;
        if (!resources.MergedDictionaries.Any(d => d.Source?.OriginalString.Contains("ThemeStyles.xaml") == true))
            resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/Z-Orbit;component/ThemeStyles.xaml", UriKind.Relative) });
        var light = Current == "cream";
        var accent = Current switch { "ice" => "#89C7EA", "violet" => "#BCABF1", "cream" => "#955031", _ => "#D59A74" };
        var values = new Dictionary<string, string>
        {
            ["AppBackground"] = light ? "#F2EDE5" : "#141414",
            ["Panel"] = light ? "#FFFCF7" : "#1D1D1F",
            ["Input"] = light ? "#F8F4EE" : "#242427",
            ["Hover"] = light ? "#E9DFD3" : "#333336",
            ["Border"] = light ? "#C4B8A9" : "#505054",
            ["Text"] = light ? "#30271F" : "#F4F1ED",
            ["Muted"] = light ? "#6D5F50" : "#B6B3AF",
            ["Accent"] = accent,
            ["AccentText"] = light ? "#FFFFFF" : "#211B17",
            ["Selection"] = Current switch { "ice" => "#233642", "violet" => "#342F46", "cream" => "#EFDDCB", _ => "#403127" },
            ["Danger"] = light ? "#A22E2A" : "#F18E87",
            ["Success"] = light ? "#28603C" : "#92D6A5"
        };
        foreach (var (key, hex) in values)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            resources[key] = brush;
        }
        var stage = new RadialGradientBrush
        {
            Center = new System.Windows.Point(0.5, 0.4), GradientOrigin = new System.Windows.Point(0.5, 0.4),
            RadiusX = 0.62, RadiusY = 0.85
        };
        var centerColor = Current switch { "ice" => "#222C32", "violet" => "#292430", "cream" => "#FFFCF7", _ => "#292623" };
        stage.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(centerColor), 0));
        stage.GradientStops.Add(new GradientStop(((SolidColorBrush)resources["AppBackground"]).Color, 1));
        stage.Freeze();
        resources["StageBackground"] = stage;
    }

    public static Brush GetBrush(string key) => (Brush)System.Windows.Application.Current.Resources[key];
}
