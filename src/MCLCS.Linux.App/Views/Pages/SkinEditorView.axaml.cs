using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class SkinEditorView : UserControl
{
    private bool _dragging;

    public SkinEditorView()
    {
        InitializeComponent();
        DataContext = new SkinEditorViewModel();
    }

    /// <summary>2D 编辑 / 3D 预览切换。</summary>
    private void Mode_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb) return;
        var is2D = ReferenceEquals(tb, Mode2D);
        if (is2D == tb.IsChecked) return;
        // 仅当点击的按钮被选中时切换（避免互斥闪烁）
        Mode2D.IsChecked = is2D;
        Mode3D.IsChecked = !is2D;
        Editor2D.IsVisible = is2D;
        Preview3D.IsVisible = !is2D;
    }

    /// <summary>画布左键：画笔 / 橡皮；右键：取色。按下即落笔。</summary>
    private void FaceCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image img || DataContext is not SkinEditorViewModel vm) return;
        var p = e.GetPosition(img);
        if (e.GetCurrentPoint(img).Properties.IsRightButtonPressed)
        {
            vm.PickColorCommand.Execute(p);
            return;
        }
        _dragging = true;
        vm.BrushCommand.Execute(p);
        e.Pointer.Capture(img);
    }

    /// <summary>按住拖动连续绘制。</summary>
    private void FaceCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || sender is not Image img || DataContext is not SkinEditorViewModel vm) return;
        vm.BrushCommand.Execute(e.GetPosition(img));
    }

    private void FaceCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }
}
