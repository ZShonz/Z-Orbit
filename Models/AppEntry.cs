namespace CharacterLauncher.Models;

public sealed class AppEntry
{
    public string Name { get; set; } = "App";
    public string Avatar { get; set; } = "";
    public string Target { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string Accent { get; set; } = "#8B7CFF";
}

public sealed class LauncherConfig
{
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "terracotta";
    public bool CleanMode { get; set; } = false;
    public string Hotkey { get; set; } = "Alt+Space";
    public bool StartWithWindows { get; set; }
    public List<AppEntry> Apps { get; set; } = [];
}
