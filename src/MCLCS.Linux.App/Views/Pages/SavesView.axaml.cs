using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class SavesView : UserControl
{
    public SavesView()
    {
        InitializeComponent();
        DataContext = new SavesViewModel();
        if (DataContext is SavesViewModel vm)
            vm.Refresh();
    }
}
