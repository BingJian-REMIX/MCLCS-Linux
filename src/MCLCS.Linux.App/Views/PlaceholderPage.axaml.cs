using Avalonia.Controls;
using Avalonia.Media;
using MCLCS.Core.Localization;
using MCLCS.Core.UI;
using MCLCS.Linux.App.Converters;

namespace MCLCS.Linux.App.Views;

/// <summary>未移植 sidebar 项的开发中占位页：展示图标 / 标题 / 描述 + 开发提示。</summary>
public partial class PlaceholderPage : UserControl
{
    public PlaceholderPage()
    {
        InitializeComponent();
    }

    /// <summary>由页面路由在挂接前调用，填充该 sidebar 项的本地化信息。</summary>
    public void Configure(string? sidebarId, MainTabKind kind)
    {
        var item = Sidebar.ById(kind, sidebarId);
        TitleText.Text = item is not null
            ? Localization.Get(item.Title)
            : Localization.Get(MainTabs.Get(kind).Title);
        DescText.Text = item is not null ? Localization.ToolDescription(item.Id) : "";
        if (item is not null && !string.IsNullOrEmpty(item.Icon))
            Icon.Source = TokenToBitmapConverter.ToBitmap(item.Icon);
    }
}
