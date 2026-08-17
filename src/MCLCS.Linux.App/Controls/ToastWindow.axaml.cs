using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 右下角 Toast 提示（Avalonia 版 ToastService 载体，对齐 WPF ToastService）：
/// 无边框置顶窗口，挂到 owner 右下角，2.5s 后淡出关闭。
/// </summary>
public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(2500) };

    public ToastWindow()
    {
        InitializeComponent();
    }

    public ToastWindow(string title, string message, ToastKind kind) : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
        KindBar.Background = kind switch
        {
            ToastKind.Success => new SolidColorBrush(Color.Parse("#27AE60")),
            ToastKind.Error => new SolidColorBrush(Color.Parse("#E74C3C")),
            _ => new SolidColorBrush(Color.Parse("#3B82F6"))
        };

        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            FadeOut();
        };
        Opened += (_, _) => _timer.Start();
    }

    /// <summary>250ms 渐隐后关闭（Avalonia 无便捷 Animation.Setter API，用定时器递减透明度）。</summary>
    private void FadeOut()
    {
        var fade = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        fade.Tick += (_, _) =>
        {
            Opacity -= 0.1;
            if (Opacity <= 0)
            {
                fade.Stop();
                Close();
            }
        };
        fade.Start();
    }

    /// <summary>定位到 owner 窗口右下角并显示。</summary>
    public void ShowAt(Window owner)
    {
        if (owner is null) return;
        // 先显示以获得尺寸
        Show(owner);
        var x = owner.Position.X + owner.Width - Width - 16;
        var y = owner.Position.Y + owner.Height - Height - 16;
        Position = new PixelPoint((int)Math.Max(0, x), (int)Math.Max(0, y));
    }
}
