using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

/// <summary>设置 → 账户页。数据上下文为 AccountsViewModel。</summary>
public partial class AccountsView : UserControl
{
    public AccountsView()
    {
        InitializeComponent();
        DataContext = new AccountsViewModel();
    }
}
