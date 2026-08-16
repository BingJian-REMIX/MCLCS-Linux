using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class DataPackView : UserControl
{
    public DataPackView()
    {
        InitializeComponent();
        DataContext = new DataPackViewModel();
        if (DataContext is DataPackViewModel vm) vm.RefreshSaves();
    }
}
