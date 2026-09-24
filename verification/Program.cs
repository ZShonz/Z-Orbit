using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CharacterLauncher;
using CharacterLauncher.Models;
using CharacterLauncher.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            var output = Path.Combine(root, "verification", "artifacts");
            var data = Path.Combine(output, "run-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(data);
            AppContext.SetData("CharacterLauncher.DataDirectory", data);
            var startupDirectory = Path.Combine(data, "startup");
            Directory.CreateDirectory(startupDirectory);
            AppContext.SetData("CharacterLauncher.StartupDirectory", startupDirectory);
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var original = Path.Combine(data, "original.png");
            File.Copy(Path.Combine(root, "assets", "avatars", "01-deepseek.png"), original);
            var localTarget = Path.Combine(data, "target.txt");
            File.WriteAllText(localTarget, "Test target. Not executed.");
            var config = new LauncherConfig();
            Check(!config.CleanMode, "First-run defaults to full interface");
            Check(!System.Text.Json.JsonSerializer.Deserialize<LauncherConfig>("{}")!.CleanMode,
                "Configuration without clean-mode field displays instructions");
            Check(System.Text.Json.JsonSerializer.Deserialize<LauncherConfig>("{\"CleanMode\":true}")!.CleanMode,
                "Explicit clean-mode preference is preserved");
            var manager = new AppManagerWindow(config, 0);
            manager.WindowStartupLocation = WindowStartupLocation.Manual;
            manager.Left = -20000; manager.Top = -20000; manager.ShowActivated = false;
            manager.Show();
            TextBox Input(string name) => (TextBox)manager.FindName(name);
            void Save() => ((Button)manager.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            void PendingImage(string path) => typeof(AppManagerWindow).GetField("_pendingImage", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(manager, path);
            Input("NameInput").Text = "示例应用";
            Input("TargetInput").Text = "https://example.com";
            Save();
            Check(config.Apps.Count == 0 && !File.Exists(ConfigService.ConfigPath), "Missing avatar does not persist");
            PendingImage(original);
            Input("TargetInput").Text = Path.Combine(data, "does-not-exist.exe");
            Save();
            Check(config.Apps.Count == 0, "Missing local target rejected");
            Input("TargetInput").Text = "https://example.com";
            Save();
            Check(config.Apps.Count == 1 && File.Exists(ConfigService.ConfigPath), "Add through manager persists");
            var savedAvatar = config.Apps[0].Avatar;
            File.Delete(original);
            Check(File.Exists(savedAvatar) && AvatarService.Load(savedAvatar).PixelWidth == 512, "Imported avatar survives original removal");
            Input("NameInput").Text = "已修改的应用";
            Input("TargetInput").Text = localTarget;
            Input("ArgumentsInput").Text = "--example";
            PendingImage(Path.Combine(root, "assets", "avatars", "04-Claude.png"));
            Save();
            var reloaded = ConfigService.Load();
            Check(reloaded.Apps.Count == 1 && reloaded.Apps[0].Name == "已修改的应用" && reloaded.Apps[0].Target == localTarget
                && reloaded.Apps[0].Arguments == "--example" && reloaded.Apps[0].Avatar != savedAvatar, "Edit name, local target, arguments and image survives reload");
            Check(File.Exists(ConfigService.ConfigPath + ".bak"), "Previous config backup exists");
            Check(TargetService.Validate("https://example.com/path?q=test") == "https://example.com/path?q=test", "Web address accepted");
            Check(TargetService.Validate(data) == data, "Local folder accepted");
            foreach (var invalid in new[] { "", "javascript:alert(1)", "ftp://example.com", "https://" })
            {
                bool rejected = false;
                try { TargetService.Validate(invalid); } catch { rejected = true; }
                Check(rejected, "Invalid target rejected: " + invalid);
            }
            ((Button)manager.FindName("AddButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Input("NameInput").Text = "网页入口";
            Input("TargetInput").Text = "https://example.com";
            PendingImage(Path.Combine(root, "assets", "avatars", "15-Grok.png"));
            Save();
            Check(config.Apps.Count == 2 && ((ListBox)manager.FindName("AppList")).SelectedIndex == 1, "Second app added and selected");
            var stage = new LauncherWindow { WindowState = WindowState.Normal, Width = 1280, Height = 720,
                Left = -20000, Top = -20000, ShowActivated = false, Topmost = false };
            stage.Show();
            var cards = (Dictionary<int, System.Windows.Controls.Border>)typeof(LauncherWindow).GetField("_slotCards", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(stage)!;
            Check(cards[0].Visibility == Visibility.Visible && (int)cards[0].Tag == 0,
                "Small carousel keeps selected portrait in center slot");
            var originalPortraitPixels = ReadPortraitPixels(cards[0]);
            Check(cards.Values.Where(card => card.Visibility == Visibility.Visible).All(card => card.Opacity == 1),
                "Portrait surfaces are opaque so theme background cannot tint them");
            Check(cards[0].Child is null && cards[-1].Child is System.Windows.Controls.Border shade
                && ((SolidColorBrush)shade.Background).Color == Colors.Black && shade.Opacity > 0,
                "Only unselected portraits receive neutral black shading");
            var originalSidePixels = ReadPortraitPixels(cards[-1]);
            manager.SettingsChanged += () => typeof(LauncherWindow).GetMethod("ApplyAppearance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(stage, null);
            Check(config.Language == "zh-CN" && LocalizationService.Normalize("unknown") == "zh-CN", "Language defaults to Chinese and unknown values fall back safely");
            Input("NameInput").Text = "Unsaved language-switch edit";
            var pendingPortrait = Path.Combine(root, "assets", "avatars", "15-Grok.png");
            PendingImage(pendingPortrait);
            ((RadioButton)manager.FindName("LanguageEnglish")).IsChecked = true;
            Check(config.Language == "en-US" && ConfigService.Load().Language == "en-US", "Language switch persists immediately");
            Check((string)((Button)manager.FindName("AddButton")).Content == "+ Add app"
                && ((TextBlock)manager.FindName("EditorTitle")).Text == "Edit app"
                && (string)((Button)manager.FindName("SaveButton")).Content == "Save changes", "English updates static and dynamic manager labels");
            Check((string)((Button)stage.FindName("ManageAppsButton")).Content == "Manage apps"
                && ((TextBlock)stage.FindName("UsageHint")).Text.Contains("Right-click"), "English updates main window and hotkey hints live");
            Check(Input("NameInput").Text == "Unsaved language-switch edit" && ConfigService.Load().Apps[1].Name == "网页入口"
                && (string?)typeof(AppManagerWindow).GetField("_pendingImage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager) == pendingPortrait,
                "Language change preserves pending fields and image without saving edits");
            Check(LocalizationService.F("启动 {0}", "示例 App") == "Launch 示例 App", "Translations preserve user-provided names");
            bool englishValidation = false;
            try { TargetService.Validate(""); } catch (ArgumentException ex) { englishValidation = ex.Message.StartsWith("Choose a local target"); }
            Check(englishValidation, "Validation errors follow selected language");
            Snapshot(manager, Path.Combine(output, "manager-english.png"));
            Snapshot(stage, Path.Combine(output, "stage-english.png"));
            manager.Width = 800; manager.Height = 620;
            Snapshot(manager, Path.Combine(output, "manager-english-small.png"));
            manager.Width = 1040; manager.Height = 800;
            var languageFailure = Path.Combine(data, "language-save-failure");
            File.WriteAllText(languageFailure, "x");
            AppContext.SetData("CharacterLauncher.DataDirectory", languageFailure);
            ((RadioButton)manager.FindName("LanguageChinese")).IsChecked = true;
            Check(config.Language == "en-US" && LocalizationService.Current == "en-US" && ((RadioButton)manager.FindName("LanguageEnglish")).IsChecked == true,
                "Failed language save retains active language and selection");
            AppContext.SetData("CharacterLauncher.DataDirectory", data);
            var englishReopened = new AppManagerWindow(ConfigService.Load(), 1);
            Check((string)((Button)englishReopened.FindName("SortButton")).Content == "Reorder", "Reopened manager restores English");
            englishReopened.Close();
            ((RadioButton)manager.FindName("LanguageChinese")).IsChecked = true;
            Check((string)((Button)manager.FindName("AddButton")).Content == "＋ 添加应用" && ConfigService.Load().Language == "zh-CN", "Switching back restores Chinese");
            PendingImage(savedAvatar);
            Input("NameInput").Text = "尚未保存的名称";
            foreach (var (control, id) in new[] { ("ThemeIce", "ice"), ("ThemeViolet", "violet"), ("ThemeCream", "cream"), ("ThemeTerracotta", "terracotta") })
            {
                ((RadioButton)manager.FindName(control)).IsChecked = true;
                Check(config.Theme == id && ConfigService.Load().Theme == id && ThemeService.Current == id,
                    "Theme applies immediately and persists: " + id);
                Check(Input("NameInput").Text == "尚未保存的名称" && ConfigService.Load().Apps[1].Name == "网页入口",
                    "Theme switch preserves unsaved editor without saving it: " + id);
                Check(ReferenceEquals(((System.Windows.Controls.Border)stage.FindName("BackgroundLayer")).Background,
                    Application.Current.Resources["StageBackground"]), "Main carousel receives live theme: " + id);
                Check(ReadPortraitPixels(cards[0]).SequenceEqual(originalPortraitPixels),
                    "Portrait rendered colors unchanged by theme: " + id);
                Check(ReadPortraitPixels(cards[-1]).SequenceEqual(originalSidePixels),
                    "Unselected portrait shading is independent of theme: " + id);
                Snapshot(manager, Path.Combine(output, "manager-" + id + ".png"));
                Snapshot(stage, Path.Combine(output, "stage-" + id + ".png"));
            }
            manager.Width = 800; manager.Height = 620;
            Snapshot(manager, Path.Combine(output, "manager-small.png"));
            manager.Width = 1040; manager.Height = 800;
            stage.Close();
            ThemeService.Apply("unknown-theme");
            Check(ThemeService.Current == "terracotta", "Unknown theme safely defaults to terracotta");
            Input("NameInput").Text = "网页入口";
            Save();
            var startupToggle = (CheckBox)manager.FindName("StartupToggle");
            startupToggle.IsChecked = true;
            startupToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Check(ConfigService.Load().StartWithWindows && File.Exists(StartupService.ShortcutPath), "Startup toggle persists and creates shortcut");
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            dynamic link = shell.CreateShortcut(StartupService.ShortcutPath);
            Check(string.Equals((string)link.TargetPath, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)
                && (string)link.Arguments == "--startup", "Startup shortcut points to current EXE with tray startup argument");
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            startupToggle.IsChecked = false;
            startupToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Check(!ConfigService.Load().StartWithWindows && !File.Exists(StartupService.ShortcutPath), "Disabling startup persists and removes shortcut");
            var failurePath = Path.Combine(data, "not-a-directory");
            File.WriteAllText(failurePath, "x");
            AppContext.SetData("CharacterLauncher.DataDirectory", failurePath);
            ((RadioButton)manager.FindName("ThemeIce")).IsChecked = true;
            Check(config.Theme == "terracotta" && ThemeService.Current == "terracotta"
                && ((RadioButton)manager.FindName("ThemeTerracotta")).IsChecked == true,
                "Failed theme save retains current palette and selection");
            Input("NameInput").Text = "This must not be committed";
            Save();
            Check(config.Apps[1].Name == "网页入口", "Failed disk save leaves live configuration intact");
            startupToggle.IsChecked = true;
            startupToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Check(!config.StartWithWindows && startupToggle.IsChecked == false && !File.Exists(StartupService.ShortcutPath),
                "Failed startup preference save rolls back shortcut and checkbox");
            AppContext.SetData("CharacterLauncher.DataDirectory", data);
            Input("NameInput").Text = "网页入口";
            Save();
            manager.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)manager.ActualWidth, (int)manager.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(manager);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(output, "app-manager.png"))) encoder.Save(file);
            manager.Close();
            var reopened = new AppManagerWindow(ConfigService.Load(), 1);
            Check(((TextBox)reopened.FindName("NameInput")).Text == "网页入口", "Reopened manager loads saved selection");
            reopened.Close();
            var deleting = new AppManagerWindow(ConfigService.Load(), 1);
            void Delete() => typeof(AppManagerWindow).GetMethod("DeleteSelected", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(deleting, null);
            var selectedAfterDelete = int.MinValue;
            deleting.Saved += index => selectedAfterDelete = index;
            AppContext.SetData("CharacterLauncher.DataDirectory", failurePath);
            Delete();
            Check(((ListBox)deleting.FindName("AppList")).Items.Count == 2 && selectedAfterDelete == int.MinValue,
                "Failed deletion save retains list and does not refresh carousel");
            AppContext.SetData("CharacterLauncher.DataDirectory", data);
            Delete();
            Check(ConfigService.Load().Apps.Count == 1 && selectedAfterDelete == 0,
                "Deletion persists and selects adjacent item");
            Check(File.Exists(localTarget) && File.Exists(savedAvatar), "Deletion preserves target files and backup images");
            Delete();
            Check(ConfigService.Load().Apps.Count == 0 && selectedAfterDelete == -1
                && !((Button)deleting.FindName("DeleteButton")).IsEnabled
                && ((TextBox)deleting.FindName("NameInput")).Text == "", "Deleting last item clears editor and persists empty list");
            ((TextBox)deleting.FindName("NameInput")).Text = "清空后新增";
            ((TextBox)deleting.FindName("TargetInput")).Text = "https://example.com";
            typeof(AppManagerWindow).GetField("_pendingImage", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(deleting,
                Path.Combine(root, "assets", "avatars", "15-Grok.png"));
            ((Button)deleting.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(ConfigService.Load().Apps.Count == 1 && selectedAfterDelete == 0
                && ((Button)deleting.FindName("DeleteButton")).IsEnabled, "Adding after deleting all restores selection");
            deleting.Close();
            var settingsConfig = ConfigService.Load();
            var settings = new AppManagerWindow(settingsConfig, 0);
            var clean = (CheckBox)settings.FindName("CleanModeToggle");
            clean.IsChecked = false;
            clean.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            Check(!ConfigService.Load().CleanMode, "Clean mode toggle persists");
            var fullStage = new LauncherWindow();
            Check(((FrameworkElement)fullStage.FindName("BrandHeader")).Visibility == Visibility.Visible, "Full mode restores header");
            fullStage.Close();
            clean.IsChecked = true;
            clean.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            var cleanStage = new LauncherWindow();
            Check(((FrameworkElement)cleanStage.FindName("BrandHeader")).Visibility == Visibility.Collapsed
                && ((FrameworkElement)cleanStage.FindName("HideButton")).Visibility == Visibility.Collapsed
                && ((FrameworkElement)cleanStage.FindName("UsageHint")).Visibility == Visibility.Collapsed
                && ((FrameworkElement)cleanStage.FindName("ManageAppsButton")).Visibility == Visibility.Visible, "Clean mode hides three decorations and retains management");
            var handle = new System.Windows.Interop.WindowInteropHelper(cleanStage).EnsureHandle();
            using (var registered = new HotkeyService())
            using (var conflict = new HotkeyService())
            {
                var hotkeyOk = registered.Register(handle, "Ctrl+Alt+Shift+F11");
                Console.WriteLine("HOTKEY_ERROR=" + System.Runtime.InteropServices.Marshal.GetLastWin32Error() + " HWND=" + handle);
                Check(hotkeyOk, "Custom hotkey registers");
                Check(!conflict.Register(handle, "Ctrl+Alt+Shift+F11"), "Conflicting hotkey rejected");
                var change = typeof(LauncherWindow).GetMethod("ChangeHotkey", BindingFlags.NonPublic | BindingFlags.Instance)!;
                change.Invoke(cleanStage, new object[] { "Ctrl+Alt+Shift+F10" });
                bool blocked = false;
                try { change.Invoke(cleanStage, new object[] { "Ctrl+Alt+Shift+F11" }); } catch { blocked = true; }
                Check(blocked && ConfigService.Load().Hotkey == "Ctrl+Alt+Shift+F10", "Conflict keeps saved hotkey");
                using var oldProbe = new HotkeyService();
                Check(!oldProbe.Register(handle, "Ctrl+Alt+Shift+F10"), "Old hotkey remains registered after conflict");
                AppContext.SetData("CharacterLauncher.DataDirectory", failurePath);
                try { change.Invoke(cleanStage, new object[] { "Ctrl+Alt+Shift+F9" }); } catch { }
                AppContext.SetData("CharacterLauncher.DataDirectory", data);
                using var releasedProbe = new HotkeyService();
                Check(releasedProbe.Register(handle, "Ctrl+Alt+Shift+F9") && ConfigService.Load().Hotkey == "Ctrl+Alt+Shift+F10", "Save failure releases candidate and retains preference");
            }
            cleanStage.Close();
            Check(HotkeyService.Parse("control+alt+l").Text == "Ctrl+Alt+L", "Hotkey input normalized");
            bool invalidHotkey = false;
            try { HotkeyService.Parse("L"); } catch { invalidHotkey = true; }
            Check(invalidHotkey, "Bare key rejected");
            var shortcutPath = Path.Combine(data, "Imported shortcut.lnk");
            dynamic importShell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            dynamic importLink = importShell.CreateShortcut(shortcutPath);
            importLink.TargetPath = Environment.ProcessPath;
            importLink.Arguments = "--example";
            importLink.WorkingDirectory = data;
            importLink.IconLocation = Path.Combine(root, "assets", "app-icon.ico");
            importLink.Save();
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(importLink);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(importShell);
            typeof(AppManagerWindow).GetMethod("ImportFile", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings, new object[] { shortcutPath, true });
            Check(((TextBox)settings.FindName("TargetInput")).Text == shortcutPath
                && ((TextBox)settings.FindName("NameInput")).Text == "Imported shortcut"
                && ((System.Windows.Controls.Image)settings.FindName("AvatarPreview")).Source is not null, "Drop fills shortcut path, name and original icon");
            ((Button)settings.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(settingsConfig.Apps.Count == 2 && File.Exists(settingsConfig.Apps[^1].Avatar), "Imported shortcut saves with extracted icon without custom image");
            var movedTarget = settingsConfig.Apps[1].Target;
            var otherTarget = settingsConfig.Apps[0].Target;
            var orderIndex = -1;
            settings.Saved += index => orderIndex = index;
            var sort = (Button)settings.FindName("SortButton");
            sort.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check((string)sort.Content == "完成排序", "Sort button enters drag mode");
            void Reorder(int from, int insertion) => typeof(AppManagerWindow).GetMethod("ReorderApp", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings, new object[] { from, insertion });
            ((TextBox)settings.FindName("NameInput")).Text = "Unsaved rename";
            Reorder(1, 0);
            Check(ConfigService.Load().Apps[0].Target == movedTarget && orderIndex == 0, "Drop at start persists order and selection");
            Check(((TextBox)settings.FindName("NameInput")).Text == "Unsaved rename" && ConfigService.Load().Apps[0].Name == "Imported shortcut", "Drag reorder preserves unsaved edit");
            AppContext.SetData("CharacterLauncher.DataDirectory", failurePath);
            Reorder(0, 2);
            Check(settingsConfig.Apps[0].Target == movedTarget && orderIndex == 0, "Failed drop save retains original order");
            AppContext.SetData("CharacterLauncher.DataDirectory", data);
            Reorder(0, 2);
            Check(ConfigService.Load().Apps[0].Target == otherTarget && orderIndex == 1, "Drop at end persists order");
            ((Button)settings.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(ConfigService.Load().Apps[1].Name == "Unsaved rename", "Edit after drag saves correct app");
            for (var i = 0; i < 30; i++) settingsConfig.Apps.Add(new AppEntry { Name = "Sort " + i, Target = movedTarget });
            ConfigService.Save(settingsConfig);
            var beforeOrder = settingsConfig.Apps.Select(x => x.Name).ToList();
            Reorder(0, 31);
            var expectedOrder = beforeOrder.ToList();
            var firstName = expectedOrder[0]; expectedOrder.RemoveAt(0); expectedOrder.Insert(30, firstName);
            Check(ConfigService.Load().Apps.Select(x => x.Name).SequenceEqual(expectedOrder), "Long-distance drag inserts rather than swaps intermediate apps");
            Reorder(30, 0);
            Check(ConfigService.Load().Apps.Select(x => x.Name).SequenceEqual(beforeOrder), "Long-distance reverse drag restores order");
            Reorder(0, 1);
            Check(ConfigService.Load().Apps.Select(x => x.Name).SequenceEqual(beforeOrder), "Drop on own position leaves order unchanged");
            sort.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check((string)sort.Content == "应用排序", "Done exits drag mode");            settings.Close();
            File.WriteAllText(ConfigService.ConfigPath, "{invalid");
            bool corruptRejected = false;
            try { ConfigService.Load(); } catch { corruptRejected = true; }
            Check(corruptRejected && File.ReadAllText(ConfigService.ConfigPath) == "{invalid", "Corrupt config reported without overwrite");
            app.Shutdown();
            Console.WriteLine("ALL CHECKS PASSED; artifacts=" + output);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        Console.WriteLine("PASS: " + label);
    }

    private static void Snapshot(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static byte[] ReadPortraitPixels(System.Windows.Controls.Border card)
    {
        card.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)card.ActualWidth, (int)card.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(card);
        var sampleSize = Math.Min(60, (int)card.ActualWidth / 3);
        var pixels = new byte[sampleSize * sampleSize * 4];
        bitmap.CopyPixels(new Int32Rect((int)card.ActualWidth / 3, (int)card.ActualHeight / 3, sampleSize, sampleSize), pixels, sampleSize * 4, 0);
        return pixels;
    }
}
