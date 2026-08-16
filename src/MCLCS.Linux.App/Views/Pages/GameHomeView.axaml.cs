using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>游戏主页（快速启动）。数据上下文为 GameHomeViewModel。</summary>
public partial class GameHomeView : UserControl
{
    public GameHomeView()
    {
        InitializeComponent();
        DataContext = new GameHomeViewModel();
    }
}
