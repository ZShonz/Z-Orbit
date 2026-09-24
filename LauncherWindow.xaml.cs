using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CharacterLauncher.Models;
using CharacterLauncher.Services;
using Forms = System.Windows.Forms;

namespace CharacterLauncher;

public partial class LauncherWindow : Window
{
    private HotkeyService _hotkey = new();
    private readonly List<AppEntry> _apps;
    private readonly LauncherConfig _config;
    private AppManagerWindow? _manager;
    private readonly Dictionary<string, ImageSource> _avatarCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ImageBrush> _avatarBrushCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Border> _slotCards = new();
    private Forms.NotifyIcon? _tray;
    private System.Drawing.Icon? _trayIcon;
    private int _selected;
    private bool _carouselReady;

    public LauncherWindow()
    {
        _config = ConfigService.Load();
        ThemeService.Apply(_config.Theme);
        InitializeComponent();
        _apps = _config.Apps;
        ApplyAppearance();
        try { StartupService.Apply(_config.StartWithWindows); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("启动器仍可使用，但开机启动设置未生效：\n" + ex.Message,
                "开机启动设置", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Loaded += (_, _) => RenderCarousel(animate: false);
        Closing += (_, _) => _hotkey.Dispose();
    }

    public void InitializeShell(bool startHidden = false)
    {
        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();
        BackdropService.Enable(helper.Handle);
        _hotkey.Pressed += (_, _) => Dispatcher.Invoke(Toggle);
        var hotkeyRegistered = _hotkey.Register(helper.Handle, _config.Hotkey);
        CreateTrayIcon();
        if (!startHidden) ShowLauncher();

        if (!hotkeyRegistered)
        {
            _tray!.Text = "Z-Orbit — 快捷键被占用";
            if (!startHidden) System.Windows.MessageBox.Show(
                $"启动器已经打开，但 {_config.Hotkey} 被其他程序或 Windows 占用。\n\n你仍可通过托盘图标打开启动器。",
                "Z-Orbit",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void CreateTrayIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Z-Orbit;component/assets/app-icon.ico"));
        using (var stream = resource!.Stream)
        using (var icon = new System.Drawing.Icon(stream))
            _trayIcon = (System.Drawing.Icon)icon.Clone();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示启动器", null, (_, _) => Dispatcher.Invoke(ShowLauncher));
        menu.Items.Add("退出", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _tray = new Forms.NotifyIcon
        {
            Text = "Z-Orbit — " + _config.Hotkey,
            Icon = _trayIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowLauncher);
    }

    private void Toggle()
    {
        if (_manager is not null) { _manager.Activate(); return; }
        if (IsVisible) Hide(); else ShowLauncher();
    }

    private void ManageApps_Click(object sender, RoutedEventArgs e)
    {
        if (_manager is not null) { _manager.Activate(); return; }
        _manager = new AppManagerWindow(_config, _selected) { Owner = this };
        _manager.SettingsChanged += ApplyAppearance;
        _manager.ChangeHotkey = ChangeHotkey;
        _manager.Saved += index =>
        {
            _selected = index;
            _avatarCache.Clear();
            _avatarBrushCache.Clear();
            RenderCarousel(animate: false);
        };
        Topmost = false;
        try { _manager.ShowDialog(); }
        finally { _manager = null; Topmost = true; ManageAppsButton.Focus(); }
    }

    private void ShowLauncher()
    {
        if (_manager is not null) { _manager.Activate(); return; }
        Show();
        WindowState = WindowState.Maximized;
        Activate();
        Focus();
        if (!_carouselReady && IsLoaded) RenderCarousel(animate: false);
    }

    private void ExitApplication()
    {
        _tray?.Dispose();
        _trayIcon?.Dispose();
        _hotkey.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void RenderCarousel(bool animate = true)
    {
        if (_apps.Count == 0)
        {
            _selected = 0;
            SelectionFrame.Visibility = Visibility.Collapsed;
            foreach (var card in _slotCards.Values)
            {
                card.Visibility = Visibility.Collapsed;
                card.Tag = null;
                card.Background = System.Windows.Media.Brushes.Transparent;
                card.Child = null;
            }
            SelectedName.Text = "点击右下角“应用管理”添加应用";
            return;
        }

        _selected = Math.Clamp(_selected, 0, _apps.Count - 1);
        SelectionFrame.Visibility = Visibility.Visible;
        SelectedName.Text = _apps[_selected].Name;
        if (animate) AnimateSelectedLabel();
        var center = Math.Max(0, CarouselCanvas.ActualWidth / 2);
        EnsureCardSlots();
        var visibleIndexes = new HashSet<int>();
        foreach (var offset in new[] { 0, -1, 1, -2, 2, -3, 3 })
        {
            var index = Wrap(_selected + offset, _apps.Count);
            var card = _slotCards[offset];
            if (!visibleIndexes.Add(index))
            {
                card.Visibility = Visibility.Collapsed;
                continue;
            }
            card.Visibility = Visibility.Visible;
            UpdateCard(card, _apps[index], index, offset);
            var targetLeft = center + Position(offset) - card.Width / 2;
            var targetTop = offset == 0 ? 24d : 92 + Math.Abs(offset) * 12;
            Canvas.SetLeft(card, targetLeft);
            Canvas.SetTop(card, targetTop);
            System.Windows.Controls.Panel.SetZIndex(card, 10 - Math.Abs(offset));
            if (animate && offset == 0)
            {
                var duration = TimeSpan.FromMilliseconds(140);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                if (card.RenderTransform is TransformGroup group && group.Children[2] is ScaleTransform focusScale)
                {
                    focusScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
                    focusScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                        new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
                }
            }
        }
        _carouselReady = true;
    }

    private void EnsureCardSlots()
    {
        if (_slotCards.Count != 0) return;
        for (var offset = -3; offset <= 3; offset++)
        {
            var card = BuildCardSlot(offset);
            _slotCards[offset] = card;
            CarouselCanvas.Children.Add(card);
        }
    }

    private Border BuildCardSlot(int offset)
    {
        var selected = offset == 0;
        var size = selected ? 250d : Math.Max(96, 168 - Math.Abs(offset) * 18);
        var cardRadius = selected ? 22d : 14d;
        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(cardRadius),
            BorderThickness = new Thickness(selected ? 2 : 0),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            Background = System.Windows.Media.Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Opacity = 1,
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            CacheMode = new BitmapCache()
        };
        var scaleX = selected ? 1 : Math.Max(0.55, 0.82 - Math.Abs(offset) * 0.07);
        border.RenderTransform = new TransformGroup
        {
            Children = new TransformCollection
            {
                new ScaleTransform(scaleX, 1),
                new SkewTransform(offset == 0 ? 0 : -Math.Sign(offset) * 7, 0),
                new ScaleTransform(1, 1)
            }
        };

        border.MouseEnter += (_, _) => AnimateHover(border, true);
        border.MouseLeave += (_, _) => AnimateHover(border, false);
        border.MouseLeftButtonDown += (_, _) => AnimatePress(border, true);
        border.MouseLeftButtonUp += (_, e) =>
        {
            AnimatePress(border, false);
            e.Handled = true;
            if (border.Tag is not int index) return;
            if (index == _selected) LaunchSelected();
            else { _selected = index; RenderCarousel(); }
        };
        border.MouseRightButtonUp += (_, e) =>
        {
            e.Handled = true;
            if (border.Tag is not int index) return;
            _selected = index;
            RenderCarousel(false);
            LaunchSelected();
        };
        return border;
    }

    private void UpdateCard(Border border, AppEntry app, int index, int offset)
    {
        border.Tag = index;
        border.ToolTip = app.Name;
        if (offset == 0) border.SetResourceReference(Border.BorderBrushProperty, "Accent");
        else border.BorderBrush = System.Windows.Media.Brushes.Transparent;
        System.Windows.Automation.AutomationProperties.SetName(border, $"启动 {app.Name}");

        var avatarPath = ResolveAvatar(app.Avatar);
        if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
        {
            border.Background = LoadAvatarBrush(avatarPath);
            // Neutral shading only on unselected portraits; never blend with the theme background.
            border.Child = offset == 0 ? null : new Border
            {
                Background = System.Windows.Media.Brushes.Black,
                Opacity = Math.Min(0.68, 0.32 + Math.Abs(offset) * 0.12),
                CornerRadius = border.CornerRadius,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(border, BitmapScalingMode.HighQuality);
            return;
        }

        border.Background = System.Windows.Media.Brushes.Transparent;
        border.Child = new TextBlock
        {
            Text = Initials(app.Name),
            Foreground = ThemeService.GetBrush("Text"),
            FontSize = offset == 0 ? 58 : 36,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        ((TextBlock)border.Child).SetResourceReference(TextBlock.ForegroundProperty, "Text");
    }

    private void ApplyAppearance()
    {
        BrandHeader.Visibility = HideButton.Visibility = UsageHint.Visibility = _config.CleanMode ? Visibility.Collapsed : Visibility.Visible;
        UsageHint.Text = $"鼠标滚轮 / ← →：切换    右键图标 / Enter：打开    左键：选中，再次点击打开\n点击空白处 / Esc：隐藏并返回桌面    呼出快捷键：{_config.Hotkey}";
    }

    private void ChangeHotkey(string text)
    {
        var normalized = HotkeyService.Parse(text).Text;
        if (normalized == _config.Hotkey && _hotkey.IsRegistered) return;
        var candidate = new HotkeyService();
        try
        {
            if (!candidate.Register(new WindowInteropHelper(this).Handle, normalized))
                throw new InvalidOperationException("这个快捷键已被占用，请换一个组合；原快捷键仍然有效。");
            var draft = ConfigService.Clone(_config);
            draft.Hotkey = normalized;
            ConfigService.Save(draft);
        }
        catch { candidate.Dispose(); throw; }
        _hotkey.Dispose();
        _hotkey = candidate;
        _hotkey.Pressed += (_, _) => Dispatcher.Invoke(Toggle);
        _config.Hotkey = normalized;
        if (_tray is not null) _tray.Text = "Z-Orbit — " + normalized;
        ApplyAppearance();
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();

    private static void AnimateHover(Border card, bool active)
    {
        if (card.RenderTransform is not TransformGroup group || group.Children.Count < 3) return;
        if (group.Children[2] is not ScaleTransform hover) return;
        var target = active ? 1.12 : 1;
        var duration = TimeSpan.FromMilliseconds(90);
        hover.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(target, duration));
        hover.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(target, duration));
    }

    private static void AnimatePress(Border card, bool pressed)
    {
        if (card.RenderTransform is not TransformGroup group || group.Children.Count < 3) return;
        if (group.Children[2] is not ScaleTransform scale) return;
        var target = pressed ? 1.04 : 1.12;
        var duration = TimeSpan.FromMilliseconds(70);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(target, duration) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(target, duration) { EasingFunction = ease });
    }

    private void AnimateSelectedLabel()
    {
        var duration = TimeSpan.FromMilliseconds(120);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        SelectedName.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0.58, 1, duration) { EasingFunction = ease });
        if (SelectedName.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.96, 1, duration) { EasingFunction = ease });
        }
    }

    private void Move(int delta)
    {
        if (_apps.Count == 0) return;
        _selected = Wrap(_selected + delta, _apps.Count);
        RenderCarousel();
    }

    private void LaunchSelected()
    {
        if (_apps.Count == 0) return;
        var app = _apps[_selected];
        if (string.IsNullOrWhiteSpace(app.Target)) return;
        try
        {
            var target = ResolveTarget(app.Target);
            if (!LooksLikeUri(target) && !File.Exists(target) && !Directory.Exists(target))
                throw new FileNotFoundException("应用快捷方式或程序不存在。", target);
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                Arguments = app.Arguments,
                UseShellExecute = true
            });
            Hide();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法启动 {app.Name}\n\n{ex.Message}", "Z-Orbit",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Hide(); e.Handled = true; return; }
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        if (e.Key is Key.Left or Key.Down) Move(-1);
        else if (e.Key is Key.Right or Key.Up) Move(1);
        else if (e.Key == Key.Enter) LaunchSelected();
        else if (e.Key == Key.Escape) Hide();
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e) => Move(e.Delta > 0 ? -1 : 1);

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Border) return;
    }

    private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var node = e.OriginalSource as DependencyObject;
        while (node is not null && node != Backdrop)
        {
            if (node is System.Windows.Controls.Primitives.ButtonBase || node is Border { Tag: int }) return;
            node = VisualTreeHelper.GetParent(node);
        }
        Hide();
    }

    private static int Wrap(int value, int count) => (value % count + count) % count;

    private static double Position(int offset) => offset switch
    {
        -3 => -490,
        -2 => -360,
        -1 => -220,
        0 => 0,
        1 => 220,
        2 => 360,
        3 => 490,
        _ => 0
    };

    private static string ResolveAvatar(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var expanded = Environment.ExpandEnvironmentVariables(value);
        return Path.IsPathRooted(expanded) ? expanded : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
    }

    private static string ResolveTarget(string value)
    {
        var expanded = Environment.ExpandEnvironmentVariables(value);
        return LooksLikeUri(expanded) || Path.IsPathRooted(expanded)
            ? expanded
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
    }

    private static bool LooksLikeUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme.Length > 1;

    private ImageSource LoadAvatar(string path)
    {
        if (_avatarCache.TryGetValue(path, out var cached)) return cached;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 512;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        _avatarCache[path] = image;
        return image;
    }

    private ImageBrush LoadAvatarBrush(string path)
    {
        if (_avatarBrushCache.TryGetValue(path, out var cached)) return cached;
        var brush = new ImageBrush(LoadAvatar(path))
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
        brush.Freeze();
        _avatarBrushCache[path] = brush;
        return brush;
    }

    private static string Initials(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 1 ? string.Concat(words.Take(2).Select(x => x[0])).ToUpperInvariant()
            : value[..Math.Min(2, value.Length)].ToUpperInvariant();
    }

    private static System.Windows.Media.Brush BrushFrom(string color, double opacity)
    {
        try
        {
            var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            brush.Opacity = opacity;
            return brush;
        }
        catch
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(255 * opacity), 139, 124, 255));
        }
    }

}
