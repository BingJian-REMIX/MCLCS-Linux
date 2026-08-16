using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class CleanerView : UserControl
{
    public CleanerView()
    {
        InitializeComponent();
        DataContext = new CleanerViewModel();
    }
}
