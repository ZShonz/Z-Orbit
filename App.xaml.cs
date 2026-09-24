using System.Windows;

namespace CharacterLauncher;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstance;
    private bool _ownsSingleInstance;
    private LauncherWindow? _launcher;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new Mutex(initiallyOwned: true, "CharacterLauncher.SingleInstance", out _ownsSingleInstance);
        if (!_ownsSingleInstance)
        {
            if (!e.Args.Contains("--startup")) System.Windows.MessageBox.Show("Z-Orbit 已经在运行，请按 Alt + Space 或双击托盘图标。",
                "Z-Orbit", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        try
        {
            _launcher = new LauncherWindow();
            _launcher.InitializeShell(e.Args.Contains("--startup"));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("无法加载启动器。配置文件未被覆盖。\n\n" + ex.Message,
                "Z-Orbit", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsSingleInstance)
            _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
