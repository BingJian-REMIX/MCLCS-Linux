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

    /// <summary>2D 编辑 / 3D 预览切换。IsChecked 已绑 VM（Mode2D=IsEditing2D, Mode3D=Inv(IsEditing2D)），
    /// 此处只同步 VM，binding 自动驱动 IsChecked + IsVisible + Editor2D/Preview3D。</summary>
    private void Mode_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb || DataContext is not SkinEditorViewModel vm) return;
        var want2D = ReferenceEquals(tb, Mode2D);
        if (vm.IsEditing2D != want2D) vm.IsEditing2D = want2D;
    }

    /// <summary>公共：切换到 3D 预览（供外部如截屏工程调用，绕过用户点击）。
    /// 全部走 VM → binding 自动同步 IsChecked + IsVisible。</summary>
    public void Show3D()
    {
        if (DataContext is SkinEditorViewModel vm && vm.IsEditing2D) vm.IsEditing2D = false;
    }

    /// <summary>公共：切换到 2D 编辑。全部走 VM。</summary>
    public void Show2D()
    {
        if (DataContext is SkinEditorViewModel vm && !vm.IsEditing2D) vm.IsEditing2D = true;
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
