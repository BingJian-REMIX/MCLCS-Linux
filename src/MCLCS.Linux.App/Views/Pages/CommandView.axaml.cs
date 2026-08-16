using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class CommandView : UserControl
{
    public CommandView()
    {
        InitializeComponent();
        DataContext = new CommandViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // 复制拼接结果到系统剪贴板（需在 UI 线程执行）。
    private async void CopyBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CommandViewModel vm) return;
        if (string.IsNullOrWhiteSpace(vm.Composed)) { vm.Status = "没有可复制的内容"; return; }
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard is not null)
            {
                await top.Clipboard.SetTextAsync(vm.Composed);
                vm.Status = "已复制到剪贴板";
            }
        }
        catch (Exception ex)
        {
            vm.Status = $"复制失败：{ex.Message}";
        }
    }
}
