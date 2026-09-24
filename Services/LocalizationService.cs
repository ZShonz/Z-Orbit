using System.IO;
using System.Text.Json;
using System.Windows;

namespace CharacterLauncher.Services;

public static class LocalizationService
{
    public sealed record Entry(string Key, string Zh, string En);
    public static IReadOnlyList<Entry> Entries { get; } = Load();
    private static readonly Dictionary<string, Entry> ByChinese = Entries.ToDictionary(e => e.Zh);
    public static string Current { get; private set; } = "zh-CN";
    public static string Normalize(string? language) => language == "en-US" ? "en-US" : "zh-CN";
    public static string T(string chinese) => Current == "en-US" && ByChinese.TryGetValue(chinese, out var entry) ? entry.En : chinese;
    public static string F(string chinese, params object[] values) => string.Format(T(chinese), values);
    public static void Apply(string? language)
    {
        Current = Normalize(language);
        foreach (var entry in Entries)
            System.Windows.Application.Current.Resources[entry.Key] = Current == "en-US" ? entry.En : entry.Zh;
    }
    private static List<Entry> Load()
    {
        using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream("CharacterLauncher.Localization.Strings.json")
            ?? throw new InvalidDataException("Missing localization catalog.");
        return JsonSerializer.Deserialize<List<Entry>>(stream)!;
    }
}
