using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class CrashView : UserControl
{
    public CrashView()
    {
        InitializeComponent();
        DataContext = new CrashViewModel();
        if (DataContext is CrashViewModel vm) vm.Refresh();
    }
}
