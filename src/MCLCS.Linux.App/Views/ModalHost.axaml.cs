using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System.ComponentModel;

namespace MCLCS.Linux.App.Views;

public partial class ModalHost : UserControl
{
    public ModalHost()
    {
        InitializeComponent();
        DataContext = DialogService.Instance;
        DialogService.Instance.PropertyChanged += OnDialogChanged;
    }

    private void OnDialogChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DialogService.CurrentDialog)) return;
        var dlg = DialogService.Instance.CurrentDialog;

        // 无对话框：隐藏并释放命中测试，避免遮挡下层 UI
        if (dlg is null)
        {
            IsHitTestVisible = false;
            Root.IsVisible = false;
            return;
        }

        Card.MaxWidth = dlg.Width ?? 560;

        IsHitTestVisible = true;
        Root.IsVisible = true;

        // 入场动画：scrim 淡入（scrimIn 150ms）
        Scrim.Opacity = 0;

        if (dlg.Anchor is Control anchorCtrl)
        {
            // 以控件为锚点：水平中线对齐控件中心、向下展开（弹出动画 scale 0.92→1 + 淡入）
            var p = anchorCtrl.TranslatePoint(
                new Point(anchorCtrl.Bounds.Width / 2, anchorCtrl.Bounds.Height / 2), this);
            var anchor = p ?? new Point(Bounds.Width / 2, Bounds.Height / 2);

            Card.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            Card.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            Card.RenderTransformOrigin = new RelativePoint(0.5, 0, RelativeUnit.Relative);
            Card.Margin = new Thickness(0);
            Card.Opacity = 0;
            Card.RenderTransform = TransformOperations.Parse("scale(0.92)");
            Dispatcher.UIThread.Post(() =>
            {
                Scrim.Opacity = 1;
                double cw = Card.Bounds.Width, ch = Card.Bounds.Height;
                double left = Math.Clamp(anchor.X - cw / 2, 8, Math.Max(8, Bounds.Width - cw - 8));
                double top = Math.Clamp(anchor.Y + anchorCtrl.Bounds.Height / 2 + 6,
                                        8, Math.Max(8, Bounds.Height - ch - 8));
                Card.Margin = new Thickness(left, top, 0, 0);
                Card.Opacity = 1;
                Card.RenderTransform = TransformOperations.Parse("scale(1)");
            }, DispatcherPriority.Render);
        }
        else
        {
            // 三种对齐变体（居中 / 右上 / 右下），对齐设计稿 scrim 的 flex 对齐 + padding
            var (ha, va, m) = dlg.Alignment switch
            {
                DialogAlignment.TopRight => (Avalonia.Layout.HorizontalAlignment.Right, Avalonia.Layout.VerticalAlignment.Top, new Thickness(0, 64, 20, 0)),
                DialogAlignment.BottomRight => (Avalonia.Layout.HorizontalAlignment.Right, Avalonia.Layout.VerticalAlignment.Bottom, new Thickness(0, 0, 16, 40)),
                _ => (Avalonia.Layout.HorizontalAlignment.Center, Avalonia.Layout.VerticalAlignment.Center, new Thickness(0))
            };
            Card.HorizontalAlignment = ha;
            Card.VerticalAlignment = va;
            Card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            Card.Margin = m;
            Card.Opacity = 0;
            Card.RenderTransform = TransformOperations.Parse("translateY(8px)");
            Dispatcher.UIThread.Post(() =>
            {
                Scrim.Opacity = 1;
                Card.Opacity = 1;
                Card.RenderTransform = TransformOperations.Parse("translateY(0px)");
            }, DispatcherPriority.Render);
        }
    }

    private void Scrim_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ReferenceEquals(e.Source, Scrim) && DialogService.Instance.CurrentDialog?.DismissOnScrim == true)
            DialogService.Instance.Complete(null);
    }

    private void CloseBtn_Click(object? sender, RoutedEventArgs e) =>
        DialogService.Instance.Complete(null);

    private void DialogButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: DialogButton db })
            DialogService.Instance.Complete(db.Result);
    }

    private void DialogButton_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is DialogButton db)
        {
            var cls = db.Kind switch
            {
                DialogButtonKind.Primary => "primary",
                DialogButtonKind.Ghost => "ghost",
                _ => "normal"
            };
            if (cls != "normal") b.Classes.Add(cls);
        }
    }
}
