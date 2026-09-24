using System.IO;
using System.Text.Json;
using CharacterLauncher.Models;

namespace CharacterLauncher.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string DataDirectory => AppContext.GetData("CharacterLauncher.DataDirectory") as string
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CharacterLauncher");
    public static string ConfigPath => Path.Combine(DataDirectory, "apps.json");

    public static LauncherConfig Load()
    {
        var path = File.Exists(ConfigPath) ? ConfigPath : Path.Combine(AppContext.BaseDirectory, "apps.json");
        if (!File.Exists(path)) return new LauncherConfig();
        var config = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("应用配置为空，请检查：" + path);
        if (config.Apps is null || config.Apps.Any(app => app is null))
            throw new InvalidDataException("应用列表格式不正确，请检查：" + path);
        foreach (var app in config.Apps)
        {
            app.Avatar = ResolvePath(app.Avatar);
            app.Target = TargetService.Resolve(app.Target);
        }
        return config;
    }

    public static LauncherConfig Clone(LauncherConfig config) =>
        JsonSerializer.Deserialize<LauncherConfig>(JsonSerializer.Serialize(config, JsonOptions), JsonOptions)!;

    public static void Save(LauncherConfig config)
    {
        Directory.CreateDirectory(DataDirectory);
        var temp = Path.Combine(DataDirectory, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(config, JsonOptions));
            if (File.Exists(ConfigPath)) File.Replace(temp, ConfigPath, ConfigPath + ".bak");
            else File.Move(temp, ConfigPath);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static string ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        path = Environment.ExpandEnvironmentVariables(path);
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
