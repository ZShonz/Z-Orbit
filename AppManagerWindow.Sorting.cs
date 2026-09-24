using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using CharacterLauncher.Models;
using CharacterLauncher.Services;

namespace CharacterLauncher;

public partial class AppManagerWindow
{
    private bool _sorting;
    private System.Windows.Point _dragOrigin;
    private AppEntry? _dragCandidate, _draggedApp;
    private const string SortFormat = "CharacterLauncher.AppOrder";
    private readonly System.Windows.Threading.DispatcherTimer _sortScrollTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private int _scrollDirection;
    private System.Windows.Point _sortPointer;
    private SortInsertionAdorner? _insertion;

    private void InitializeSorting()
    {
        _sortScrollTimer.Tick += (_, _) =>
        {
            var scroll = FindVisual<ScrollViewer>(AppList);
            if (_scrollDirection < 0) scroll?.LineUp(); else if (_scrollDirection > 0) scroll?.LineDown();
            AppList.UpdateLayout();
            ShowInsertion();
        };
        AppList.DragLeave += (_, _) => ClearInsertion();
        Closed += (_, _) => ClearInsertion();
    }

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        _sorting = !_sorting;
        SortButton.Content = _sorting ? "完成排序" : "应用排序";
        AppList.Cursor = _sorting ? System.Windows.Input.Cursors.SizeAll : null;
        ShowStatus(_sorting ? "拖动头像到目标位置，松开自动保存；拖到列表上下边缘可滚动。" : "已退出排序模式。", true);
    }

    private void AppList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragCandidate = null;
        if (!_sorting) return;
        var item = ItemsControl.ContainerFromElement(AppList, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (item?.Content is not AppEntry app) return;
        AppList.SelectedItem = app;
        if (!ReferenceEquals(AppList.SelectedItem, app)) { e.Handled = true; return; }
        _dragCandidate = app;
        _dragOrigin = e.GetPosition(AppList);
    }

    private void AppList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_sorting || _dragCandidate is null || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(AppList);
        if (Math.Abs(point.X - _dragOrigin.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(point.Y - _dragOrigin.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _draggedApp = _dragCandidate;
        _dragCandidate = null;
        try { System.Windows.DragDrop.DoDragDrop(AppList, new System.Windows.DataObject(SortFormat, _draggedApp), System.Windows.DragDropEffects.Move); }
        finally { _draggedApp = null; ClearInsertion(); }
        e.Handled = true;
    }

    private void ClearInsertion()
    {
        _sortScrollTimer.Stop();
        _scrollDirection = 0;
        if (_insertion is not null) AdornerLayer.GetAdornerLayer(AppList)?.Remove(_insertion);
        _insertion = null;
    }

    private int InsertionIndex(System.Windows.Point point, out double lineY)
    {
        var lastVisible = -1;
        lineY = 0;
        for (var i = 0; i < AppList.Items.Count; i++)
        {
            if (AppList.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem row) continue;
            var top = row.TranslatePoint(new System.Windows.Point(), AppList).Y;
            if (top + row.ActualHeight < 0 || top > AppList.ActualHeight) continue;
            if (point.Y < top + row.ActualHeight / 2) { lineY = Math.Max(2, top); return i; }
            lastVisible = i;
            lineY = Math.Min(AppList.ActualHeight - 2, top + row.ActualHeight);
        }
        return lastVisible < 0 ? 0 : lastVisible + 1;
    }

    private void ShowInsertion()
    {
        InsertionIndex(_sortPointer, out var y);
        var layer = AdornerLayer.GetAdornerLayer(AppList);
        if (layer is null) return;
        if (_insertion is null) { _insertion = new SortInsertionAdorner(AppList) { IsHitTestVisible = false }; layer.Add(_insertion); }
        _insertion.LineY = y;
        _insertion.InvalidateVisual();
    }

    private bool HandleSortDrag(System.Windows.DragEventArgs e, bool drop)
    {
        if (!e.Data.GetDataPresent(SortFormat)) return false;
        _sortPointer = e.GetPosition(AppList);
        var inside = _sorting && _draggedApp is not null && _sortPointer.X >= 0 && _sortPointer.X <= AppList.ActualWidth
            && _sortPointer.Y >= 0 && _sortPointer.Y <= AppList.ActualHeight;
        e.Effects = inside ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
        e.Handled = true;
        if (!inside) { ClearInsertion(); return true; }
        if (drop)
        {
            ReorderApp(_config.Apps.IndexOf(_draggedApp!), InsertionIndex(_sortPointer, out _));
            ClearInsertion();
            return true;
        }
        _scrollDirection = _sortPointer.Y < 30 ? -1 : _sortPointer.Y > AppList.ActualHeight - 30 ? 1 : 0;
        if (_scrollDirection != 0) _sortScrollTimer.Start(); else _sortScrollTimer.Stop();
        ShowInsertion();
        return true;
    }

    private static T? FindVisual<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            if (FindVisual<T>(child) is T nested) return nested;
        }
        return null;
    }

    private void ReorderApp(int from, int insertion)
    {
        if (from < 0 || from >= _config.Apps.Count || insertion < 0 || insertion > _config.Apps.Count) return;
        var to = insertion > from ? insertion - 1 : insertion;
        if (from == to) return;
        try
        {
            var draft = ConfigService.Clone(_config);
            var moved = draft.Apps[from];
            draft.Apps.RemoveAt(from);
            draft.Apps.Insert(to, moved);
            ConfigService.Save(draft);
        }
        catch (Exception ex) { ShowStatus("顺序保存失败，原顺序未改变：" + ex.Message, false); return; }
        var editing = _editingIndex >= 0 ? _config.Apps[_editingIndex] : null;
        _loading = true;
        try
        {
            var moved = _config.Apps[from];
            _config.Apps.RemoveAt(from);
            _config.Apps.Insert(to, moved);
            _editingIndex = editing is null ? -1 : _config.Apps.IndexOf(editing);
            AppList.Items.Refresh();
            AppList.SelectedIndex = _editingIndex;
            if (AppList.SelectedItem is not null) AppList.ScrollIntoView(AppList.SelectedItem);
        }
        finally { _loading = false; }
        Saved?.Invoke(_editingIndex);
        ShowStatus(_dirty ? "顺序已保存。当前名称、地址或图片的修改仍需点击保存。" : "顺序已保存，可继续拖动，或点击“完成排序”。", true);
    }

    private sealed class SortInsertionAdorner(UIElement element) : Adorner(element)
    {
        public double LineY { get; set; }
        protected override void OnRender(DrawingContext context) => context.DrawLine(
            new System.Windows.Media.Pen(ThemeService.GetBrush("Accent"), 3),
            new System.Windows.Point(4, LineY), new System.Windows.Point(Math.Max(4, ActualWidth - 4), LineY));
    }
}
