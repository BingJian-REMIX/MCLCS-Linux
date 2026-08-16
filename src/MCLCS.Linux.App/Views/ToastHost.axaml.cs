using Avalonia.Controls;

namespace MCLCS.Linux.App.Views;

public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
        DataContext = ToastService.Instance;
    }
}
