using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class NbtView : UserControl
{
    public NbtView()
    {
        InitializeComponent();
        DataContext = new NbtViewModel();
    }
}
