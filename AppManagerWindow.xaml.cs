using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CharacterLauncher.Models;
using CharacterLauncher.Services;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace CharacterLauncher;

public partial class AppManagerWindow : Window
{
    private readonly LauncherConfig _config;
    private int _editingIndex = -1;
    private bool _loading = true;
    private bool _dirty;
    private string _avatar = "";
    private string? _pendingImage;
    private bool _changingTheme;
    private bool _changingLanguage;
    private System.Windows.Media.Imaging.BitmapSource? _pendingIcon;
    public event Action? SettingsChanged;
    public Action<string>? ChangeHotkey { get; set; }
    public event Action<int>? Saved;

    public AppManagerWindow(LauncherConfig config, int selectedIndex)
    {
        _config = config;
        LocalizationService.Apply(config.Language);
        ThemeService.Apply(config.Theme);
        InitializeComponent();
        InitializeSorting();
        SelectLanguage(config.Language);
        SelectTheme(config.Theme);
        StartupToggle.IsChecked = config.StartWithWindows;
        CleanModeToggle.IsChecked = config.CleanMode;
        HotkeyInput.Text = config.Hotkey;
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;
        AppList.ItemsSource = config.Apps;
        AppList.SelectedIndex = config.Apps.Count > 0 ? Math.Clamp(selectedIndex, 0, config.Apps.Count - 1) : -1;
        LoadEditor(AppList.SelectedIndex);
    }

    private void SelectLanguage(string id)
    {
        _changingLanguage = true;
        LanguageEnglish.IsChecked = LocalizationService.Normalize(id) == "en-US";
        LanguageChinese.IsChecked = !LanguageEnglish.IsChecked;
        _changingLanguage = false;
    }

    private void Language_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading || _changingLanguage || sender is not System.Windows.Controls.RadioButton button) return;
        var id = (string)button.Tag;
        try
        {
            var draft = ConfigService.Clone(_config);
            draft.Language = id;
            ConfigService.Save(draft);
        }
        catch (Exception ex)
        {
            SelectLanguage(_config.Language);
            ThemeStatus.Text = LocalizationService.T("语言保存失败，保留原语言：") + ex.Message;
            return;
        }
        _config.Language = id;
        LocalizationService.Apply(id);
        EditorTitle.Text = LocalizationService.T(_editingIndex < 0 ? "添加应用" : "编辑应用");
        SaveButton.Content = LocalizationService.T(_editingIndex < 0 ? "添加到轮播" : "保存更改");
        SortButton.Content = LocalizationService.T(_sorting ? "完成排序" : "应用排序");
        StartupStatus.Text = _config.StartWithWindows
            ? LocalizationService.F("已开启：登录后在托盘运行，{0} 呼出。", _config.Hotkey)
            : LocalizationService.T("已关闭开机自动启动。");
        ThemeStatus.Text = LocalizationService.T("语言已保存，界面已切换。");
        StatusText.Text = "";
        SettingsChanged?.Invoke();
    }

    private void SelectTheme(string id)
    {
        _changingTheme = true;
        var normalized = ThemeService.Normalize(id);
        foreach (var radio in new[] { ThemeTerracotta, ThemeIce, ThemeViolet, ThemeCream })
            radio.IsChecked = (string)radio.Tag == normalized;
        _changingTheme = false;
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (_changingTheme || _loading || sender is not System.Windows.Controls.RadioButton radio) return;
        var id = (string)radio.Tag;
        try
        {
            var draft = ConfigService.Clone(_config);
            draft.Theme = id;
            ConfigService.Save(draft);
            _config.Theme = id;
            ThemeService.Apply(id);
            ThemeStatus.Text = LocalizationService.T("配色已保存，主界面与管理窗口同步更新。");
        }
        catch (Exception ex)
        {
            SelectTheme(_config.Theme);
            ThemeStatus.Text = LocalizationService.T("配色保存失败，保留原主题：") + ex.Message;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void LoadEditor(int index)
    {
        _loading = true;
        _editingIndex = index;
        DeleteButton.IsEnabled = index >= 0;

        var app = index >= 0 ? _config.Apps[index] : new AppEntry { Name = "" };
        NameInput.Text = app.Name;
        TargetInput.Text = app.Target;
        ArgumentsInput.Text = app.Arguments;
        _avatar = app.Avatar;
        _pendingImage = null;
        _pendingIcon = null;
        EditorTitle.Text = index < 0 ? LocalizationService.T("添加应用") : LocalizationService.T("编辑应用");
        SaveButton.Content = index < 0 ? LocalizationService.T("添加到轮播") : LocalizationService.T("保存更改");
        StatusText.Text = "";
        AvatarPreview.Source = null;
        AvatarPlaceholder.Visibility = Visibility.Visible;
        if (!string.IsNullOrWhiteSpace(_avatar))
        {
            try { ShowImage(_avatar); }
            catch { ShowStatus(LocalizationService.T("原头像无法读取，可以重新选择图片。"), false); }
        }
        _dirty = false;
        _loading = false;
    }

    private void ShowImage(string path)
    {
        AvatarPreview.Source = AvatarService.Load(path);
        AvatarPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void StartupToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = StartupToggle.IsChecked == true;
        var previous = _config.StartWithWindows;
        try
        {
            var draft = ConfigService.Clone(_config);
            draft.StartWithWindows = enabled;
            StartupService.Apply(enabled);
            ConfigService.Save(draft);
            _config.StartWithWindows = enabled;
            StartupStatus.Text = enabled ? LocalizationService.F("已开启：登录后在托盘运行，{0} 呼出。", _config.Hotkey) : LocalizationService.T("已关闭开机自动启动。");
        }
        catch (Exception ex)
        {
            StartupToggle.IsChecked = previous;
            var rollbackError = "";
            try { StartupService.Apply(previous); }
            catch (Exception rollback) { rollbackError = LocalizationService.T(" 恢复启动项也失败：") + rollback.Message; }
            StartupStatus.Text = LocalizationService.T("设置失败，配置未更改：") + ex.Message + rollbackError;
        }
    }

    private void ShowStatus(string message, bool success)
    {
        StatusText.SetResourceReference(TextBlock.ForegroundProperty, success ? "Success" : "Danger");
        StatusText.Text = message;
    }

    private bool CanDiscard() => !_dirty || System.Windows.MessageBox.Show(this,
        LocalizationService.T("当前修改尚未保存，是否放弃这些修改？"), LocalizationService.T("未保存的修改"),
        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    private void AppList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (!CanDiscard())
        {
            _loading = true;
            AppList.SelectedIndex = _editingIndex;
            _loading = false;
            return;
        }
        LoadEditor(AppList.SelectedIndex);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!CanDiscard()) return;
        _loading = true;
        AppList.SelectedIndex = -1;
        LoadEditor(-1);
        NameInput.Focus();
    }

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _dirty = true;
        StatusText.Text = "";
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_editingIndex < 0 || _editingIndex >= _config.Apps.Count) return;
        var name = _config.Apps[_editingIndex].Name;
        var message = LocalizationService.F("确定从轮播中删除“{0}”吗？\n\n将移除这个条目的图片、名称、目标地址和参数，不会卸载软件或删除原始图片。", name);
        if (_dirty) message += LocalizationService.T("\n当前条目尚未保存的修改也会放弃。");
        if (System.Windows.MessageBox.Show(this, message, LocalizationService.T("删除应用"), MessageBoxButton.YesNo,
            MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        DeleteSelected();
    }

    private void DeleteSelected()
    {
        if (_editingIndex < 0 || _editingIndex >= _config.Apps.Count) return;
        try
        {
            var draft = ConfigService.Clone(_config);
            draft.Apps.RemoveAt(_editingIndex);
            ConfigService.Save(draft);
            var nextIndex = Math.Min(_editingIndex, draft.Apps.Count - 1);
            _loading = true;
            _config.Apps.Clear();
            _config.Apps.AddRange(draft.Apps);
            AppList.Items.Refresh();
            AppList.SelectedIndex = nextIndex;
            LoadEditor(nextIndex);
            Saved?.Invoke(nextIndex);
            ShowStatus(nextIndex < 0 ? LocalizationService.T("已删除，轮播已清空。可以添加新的应用。") : LocalizationService.T("已删除，轮播已更新。"), true);
        }
        catch (Exception ex) { ShowStatus(LocalizationService.T("删除失败，条目未移除：") + ex.Message, false); }
    }

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = LocalizationService.T("选择应用头像"), Filter = LocalizationService.T("图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.ico"), CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            ShowImage(picker.FileName);
            _pendingImage = picker.FileName;
            _pendingIcon = null;
            _dirty = true;
            StatusText.Text = "";
        }
        catch (Exception ex) { ShowStatus(LocalizationService.T("无法读取图片：") + ex.Message, false); }
    }

    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = LocalizationService.T("选择要打开的应用或文件"), Filter = LocalizationService.T("应用和快捷方式|*.exe;*.lnk;*.url|所有文件|*.*"), DereferenceLinks = false, CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        ImportFile(picker.FileName, false);
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = LocalizationService.T("选择要打开的文件夹") };
        if (picker.ShowDialog(this) != true) return;
        TargetInput.Text = picker.FolderName;
        if (string.IsNullOrWhiteSpace(NameInput.Text)) NameInput.Text = Path.GetFileName(picker.FolderName);
    }

    private void TestOpen_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TargetService.Open(TargetInput.Text, ArgumentsInput.Text);
            ShowStatus(LocalizationService.T("已发送打开请求。确认打开正确后，点击保存。"), true);
        }
        catch (Exception ex) { ShowStatus(ex.Message, false); TargetInput.Focus(); }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string? imported = null;
        try
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                ShowStatus(LocalizationService.T("请填写应用名称。"), false);
                NameInput.Focus();
                return;
            }
            string target;
            try { target = TargetService.Validate(TargetInput.Text); }
            catch (Exception ex) { ShowStatus(ex.Message, false); TargetInput.Focus(); return; }
            if (_editingIndex < 0 && _pendingImage is null && _pendingIcon is null)
            {
                ShowStatus(LocalizationService.T("请先选择一张应用头像。"), false);
                return;
            }
            var draft = ConfigService.Clone(_config);
            var entry = new AppEntry
            {
                Name = NameInput.Text.Trim(), Target = target, Arguments = ArgumentsInput.Text.Trim(),
                Avatar = _pendingImage is not null ? (imported = AvatarService.Import(_pendingImage)) : _pendingIcon is not null ? (imported = AvatarService.Import(_pendingIcon)) : _avatar,
                Accent = _editingIndex >= 0 ? _config.Apps[_editingIndex].Accent : "#8B7CFF"
            };
            var index = _editingIndex < 0 ? draft.Apps.Count : _editingIndex;
            if (_editingIndex < 0) draft.Apps.Add(entry); else draft.Apps[index] = entry;
            ConfigService.Save(draft);
            // Only update the live carousel after the durable save succeeds.
            imported = null;
            _config.Apps.Clear();
            _config.Apps.AddRange(draft.Apps);
            _loading = true;
            AppList.Items.Refresh();
            AppList.SelectedIndex = index;
            LoadEditor(index);
            Saved?.Invoke(index);
            ShowStatus(LocalizationService.T("已保存，轮播已更新。"), true);
        }
        catch (Exception ex)
        {
            if (imported is not null) { try { File.Delete(imported); } catch { } }
            ShowStatus(LocalizationService.T("保存失败：") + ex.Message, false);
        }
    }

    private void CleanMode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var draft = ConfigService.Clone(_config);
            draft.CleanMode = CleanModeToggle.IsChecked == true;
            ConfigService.Save(draft);
            _config.CleanMode = draft.CleanMode;
            SettingsChanged?.Invoke();
            ThemeStatus.Text = draft.CleanMode ? LocalizationService.T("洁净版已开启，主界面保留轮播和应用管理入口。") : LocalizationService.T("已恢复完整界面和操作提示。");
        }
        catch (Exception ex) { CleanModeToggle.IsChecked = _config.CleanMode; ThemeStatus.Text = LocalizationService.T("保存失败：") + ex.Message; }
    }

    private void ApplyHotkey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ChangeHotkey is null) throw new InvalidOperationException(LocalizationService.T("请从启动器打开管理窗口后设置快捷键。"));
            ChangeHotkey(HotkeyInput.Text);
            HotkeyInput.Text = _config.Hotkey;
            ThemeStatus.Text = LocalizationService.T("呼出快捷键已保存：") + _config.Hotkey;
        }
        catch (Exception ex) { ThemeStatus.Text = ex.Message; }
    }

    private void Usage_Click(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show(this,
        LocalizationService.T("鼠标滚轮：切换选中的图标\n右键点击任意图标：直接打开对应程序\n左键点击图标：选中；再次点击选中图标：打开\n点击空白处：隐藏启动器，返回桌面\n\n方向键：切换图标\nEnter：打开选中的应用\nEsc：隐藏启动器\n呼出 / 隐藏快捷键：") + _config.Hotkey +
        LocalizationService.T("\n可在外观与配色旁输入新组合键并点击“应用快捷键”。\n\n管理窗口：Ctrl+S 保存，Esc 关闭\n拖入快捷方式或 EXE：新建应用并读取原图标\n拖入图片：更换当前编辑条目的图片\n\n洁净版仅隐藏主界面装饰和提示，操作方式不变。"),
        LocalizationService.T("使用说明"), MessageBoxButton.OK, MessageBoxImage.Information);

    private static bool IsImage(string path) => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".ico" }.Contains(Path.GetExtension(path).ToLowerInvariant());
    private static bool CanImport(string path) => IsImage(path) || new[] { ".lnk", ".url", ".exe" }.Contains(Path.GetExtension(path).ToLowerInvariant());
    private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (HandleSortDrag(e, false)) return;
        e.Effects = e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths && paths.Length == 1 && CanImport(paths[0])
            ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }
    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        e.Handled = true;
        if (HandleSortDrag(e, true)) return;
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] paths || paths.Length != 1 || !CanImport(paths[0]))
        { ShowStatus(LocalizationService.T("每次请拖入一个快捷方式、EXE 或图片。"), false); return; }
        ImportFile(paths[0], true);
    }
    private void ImportFile(string path, bool createNew)
    {
        try
        {
            if (IsImage(path))
            {
                ShowImage(path);
                _pendingImage = path;
                _pendingIcon = null;
                _dirty = true;
                ShowStatus(LocalizationService.T("图片已替换，点击保存后生效。"), true);
                return;
            }
            TargetService.Validate(path);
            if (createNew)
            {
                if (!CanDiscard()) return;
                _loading = true;
                AppList.SelectedIndex = -1;
                LoadEditor(-1);
            }
            TargetInput.Text = path;
            ArgumentsInput.Text = "";
            if (createNew || string.IsNullOrWhiteSpace(NameInput.Text)) NameInput.Text = Path.GetFileNameWithoutExtension(path);
            _dirty = true;
            // Launch the shortcut itself to retain its arguments, working directory and shell settings.
            if (createNew || (string.IsNullOrWhiteSpace(_avatar) && _pendingImage is null))
            {
                _pendingIcon = ShortcutImportService.ReadIcon(path);
                AvatarPreview.Source = _pendingIcon;
                AvatarPlaceholder.Visibility = Visibility.Collapsed;
            }
            ShowStatus(LocalizationService.T("已填入目标地址，确认后点击保存。快捷方式按原有设置启动；已有自定义图片会保留。"), true);
        }
        catch (Exception ex) { ShowStatus(LocalizationService.T("导入失败：") + ex.Message, false); }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_Closing(object? sender, CancelEventArgs e) { if (!CanDiscard()) e.Cancel = true; }
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; Close(); }
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        { e.Handled = true; Save_Click(sender, e); }
    }
}
