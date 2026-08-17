using Avalonia.Controls;
using MCLCS.Core.Ai;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AiSettingsView : UserControl
{
    public AiSettingsView()
    {
        InitializeComponent();
        DataContext = new AiSettingsViewModel();
    }

    /// <summary>本地模型下拉切换：未拉取模型时弹确认（对齐 WPF LocalModelCombo_SelectionChanged）。</summary>
    private async void LocalModel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: LocalModelInfo info }) return;
        if (DataContext is AiSettingsViewModel vm)
            await vm.TrySelectLocalModelAsync(info.DisplayName);
    }
}
