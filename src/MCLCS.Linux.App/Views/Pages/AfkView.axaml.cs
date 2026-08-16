using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AfkView : UserControl
{
    public AfkView()
    {
        InitializeComponent();
        DataContext = new AfkViewModel();
    }
}
