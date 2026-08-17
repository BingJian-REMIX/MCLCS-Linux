using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AiAssistView : UserControl
{
    public AiAssistView()
    {
        InitializeComponent();
        DataContext = new AiAssistViewModel();
    }
}
