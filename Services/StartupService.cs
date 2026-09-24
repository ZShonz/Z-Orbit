using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CharacterLauncher.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CharacterLauncher";
    public static string ShortcutPath => Path.Combine(
        AppContext.GetData("CharacterLauncher.StartupDirectory") as string
            ?? Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "Z-Orbit.lnk");

    public static void Apply(bool enabled)
    {
        if (enabled) CreateStartupShortcut();
        else if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);

        var legacyShortcut = Path.Combine(Path.GetDirectoryName(ShortcutPath)!, "CharacterLauncher.lnk");
        if (File.Exists(legacyShortcut)) File.Delete(legacyShortcut);

        // Remove the previous registry-based startup entry to prevent duplicate launches.
        if (AppContext.GetData("CharacterLauncher.StartupDirectory") is null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static void CreateStartupShortcut()
    {
        var executable = Environment.ProcessPath
                         ?? throw new InvalidOperationException("无法确定启动器路径。");
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new InvalidOperationException("WScript.Shell 不可用。");
        object? shellObject = null;
        object? shortcutObject = null;
        try
        {
            shellObject = Activator.CreateInstance(shellType);
            dynamic shell = shellObject!;
            shortcutObject = shell.CreateShortcut(ShortcutPath);
            dynamic shortcut = shortcutObject;
            shortcut.TargetPath = executable;
            shortcut.IconLocation = executable + ",0";
            shortcut.Arguments = "--startup";
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.Description = "Z-Orbit";
            shortcut.Save();
        }
        finally
        {
            if (shortcutObject is not null && Marshal.IsComObject(shortcutObject))
                Marshal.FinalReleaseComObject(shortcutObject);
            if (shellObject is not null && Marshal.IsComObject(shellObject))
                Marshal.FinalReleaseComObject(shellObject);
        }
    }
}
