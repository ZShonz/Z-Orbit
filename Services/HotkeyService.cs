using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace CharacterLauncher.Services;

public sealed class HotkeyService : IDisposable
{
    private static int _nextId = 0x434B;
    private readonly int _id = System.Threading.Interlocked.Increment(ref _nextId);
    private HwndSource? _source;
    public bool IsRegistered => _source is not null;
    public event EventHandler? Pressed;

    public static (uint Modifiers, uint Key, string Text) Parse(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        uint modifiers = 0;
        foreach (var part in parts.SkipLast(1))
            modifiers |= part.ToLowerInvariant() switch { "alt" => 1u, "ctrl" or "control" => 2u, "shift" => 4u, "win" or "windows" => 8u, _ => throw new ArgumentException(LocalizationService.T("请使用 Ctrl、Alt、Shift 或 Win 组合键。")) };
        if (parts.Length < 2 || modifiers == 0 || !Enum.TryParse<Key>(parts[^1], true, out var key)
            || key is Key.None or Key.System or Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            || KeyInterop.VirtualKeyFromKey(key) == 0)
            throw new ArgumentException(LocalizationService.T("请输入组合键，例如 Alt+Space 或 Ctrl+Alt+L。"));
        var labels = new List<string>();
        if ((modifiers & 2) != 0) labels.Add("Ctrl");
        if ((modifiers & 1) != 0) labels.Add("Alt");
        if ((modifiers & 4) != 0) labels.Add("Shift");
        if ((modifiers & 8) != 0) labels.Add("Win");
        labels.Add(key.ToString());
        return (modifiers, (uint)KeyInterop.VirtualKeyFromKey(key), string.Join("+", labels));
    }

    public bool Register(IntPtr handle, string text = "Alt+Space")
    {
        var key = Parse(text);
        if (!RegisterHotKey(handle, _id, key.Modifiers | 0x4000, key.Key)) return false;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        return true;
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == _id) { Pressed?.Invoke(this, EventArgs.Empty); handled = true; }
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if (_source is null) return;
        UnregisterHotKey(_source.Handle, _id);
        _source.RemoveHook(WndProc);
        _source = null;
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
