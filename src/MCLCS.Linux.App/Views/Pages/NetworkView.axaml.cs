using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class NetworkView : UserControl
{
    public NetworkView()
    {
        InitializeComponent();
        DataContext = new NetworkViewModel();
    }
}
