using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>下载 → 原版安装页。数据上下文为 InstallViewModel。</summary>
public partial class InstallView : UserControl
{
    public InstallView()
    {
        InitializeComponent();
        DataContext = new InstallViewModel();
    }
}
