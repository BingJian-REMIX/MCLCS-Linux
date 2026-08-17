using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>音乐播放器面板（工具箱）。播放状态与命令来自 <see cref="MusicPlayerViewModel"/> 单例；
/// 实际解码由主窗口注入的 BASS 宿主完成，本面板只负责展示与交互。
/// 因 VM 为单例，切到其它页再回来时播放状态保持。</summary>
public partial class MusicView : UserControl
{
    public MusicView()
    {
        InitializeComponent();
        DataContext = MusicPlayerViewModel.Instance;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
