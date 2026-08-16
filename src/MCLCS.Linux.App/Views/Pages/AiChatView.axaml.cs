using Avalonia.Controls;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AiChatView : UserControl
{
    public AiChatView()
    {
        InitializeComponent();
        DataContext = new AiChatViewModel();
    }
}
