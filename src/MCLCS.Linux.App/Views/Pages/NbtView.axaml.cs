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

    /// <summary>level.dat 快捷下拉：选中即打开。</summary>
    private void QuickFile_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: string path } || string.IsNullOrEmpty(path)) return;
        if (DataContext is NbtViewModel vm)
            vm.OpenQuickCommand.Execute(path);
    }
}
