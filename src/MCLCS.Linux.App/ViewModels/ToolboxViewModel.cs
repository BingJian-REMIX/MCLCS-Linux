using System.Collections.ObjectModel;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.UI;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>工具箱总览卡片（对齐 WPF ToolboxView 的面板聚合入口）。</summary>
public class ToolboxCard : ObservableObject
{
    public string Id { get; init; } = "";
    public string Icon { get; init; } = "";
    /// <summary>侧栏标题（已本地化）。</summary>
    public string Title { get; init; } = "";
    /// <summary>分组本地化名。</summary>
    public string Group { get; init; } = "";
}

/// <summary>
/// 工具箱聚合入口：把 Toolbox 主标签下的工具以卡片网格呈现，点按卡片跳转到对应工具。
/// 沿用 WPF ToolboxView「聚合入口」的定位，但复用 Linux 已有的分工具页（而非内嵌子视图）。
/// </summary>
public class ToolboxViewModel : ObservableObject
{
    public ObservableCollection<ToolboxCard> Cards { get; } = new();

    public ToolboxViewModel()
    {
        foreach (var item in Sidebar.Toolbox)
        {
            if (item.Id == "toolbox") continue; // 不把聚合页自身算作一张卡片
            Cards.Add(new ToolboxCard
            {
                Id = item.Id,
                Icon = item.Icon,
                Title = LocaleManager.T(item.Title),
                Group = item.Group is { } g ? LocaleManager.T(g) : ""
            });
        }
    }
}
