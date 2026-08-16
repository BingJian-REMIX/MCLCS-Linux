using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class MusicView : UserControl
{
    public MusicView()
    {
        InitializeComponent();
        DataContext = new MusicViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 添加音乐文件夹：打开文件夹选择对话框，把路径交给 VM 扫描导入。
    private async void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MusicViewModel vm) return;
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is null) return;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "选择音乐文件夹", AllowMultiple = false });
        if (folders.Count > 0)
            vm.AddFolder(folders[0].Path.LocalPath);
    }
}
