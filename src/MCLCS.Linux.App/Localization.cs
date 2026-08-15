using System.Collections.Generic;
using MCLCS.Core.Localization;

namespace MCLCS.Linux.App;

/// <summary>
/// 极简本地化层：把 Core.UI 视图模型里的 l10n key（如 <c>tab.game</c>）翻译为显示名。
/// <para>
/// 上游 MCLCS.App（WPF）的标题来自一套资源字典；MCLCS-Linux 不依赖 WPF，
/// 因此在此用一个静态字典桥接。未知 key 原样返回（不丢信息、不报错）。
/// </para>
/// </summary>
public static class Localization
{
    /// <summary>把 key 翻译为显示名；委托 Core 的 LocaleManager（zh_CN + en_US 完整框架）。
    /// 未知 key 原样返回，空 key 返回空串（保持与既有单测约定一致）。</summary>
    public static string Get(string? key) =>
        key is null ? "" : LocaleManager.T(key);

    /// <summary>按副标签 Id 取一句话功能描述（用于右面板说明，标注「待接入」的属未来工作）。</summary>
    private static readonly Dictionary<string, string> DescMap = new()
    {
        ["tool.log"] = "查看与管理游戏日志文件",
        ["tool.crash"] = "分析与定位崩溃日志，给出修复建议",
        ["tool.perf"] = "实时性能监控与 FPS / TPS 分析",
        ["tool.network"] = "网络连接与登录服务器诊断",
        ["tool.filewatch"] = "监控游戏目录文件变更",
        ["tool.datapack"] = "数据包的启用 / 禁用 / 排序",
        ["tool.saves"] = "存档导入 / 导出 / 备份",
        ["tool.backup"] = "整盘备份与一键恢复",
        ["tool.screenshot"] = "截图管理与导出",
        ["tool.clean"] = "清理缓存与冗余文件",
        ["tool.modpackio"] = "整合包的导入与导出",
        ["tool.music"] = "背景音乐与音轨管理",
        ["tool.moddev"] = "模组开发辅助（模板 / 调试）",
        ["tool.packmaker"] = "整合包制作与打包",
        ["tool.nbt"] = "NBT 数据结构编辑器",
        ["tool.command"] = "生成复杂命令与函数",
        ["tool.skin"] = "皮肤预览与管理",
        ["tool.shortcut"] = "生成桌面 / 开始菜单快捷方式",
        ["tool.afk"] = "挂机与轻量自动化",
        ["tool.aichat"] = "对接 Core.Ai 的聊天助手",
    };

    /// <summary>副标签 Id → 一句话描述；未登记显示「待接入 Core 能力」。</summary>
    public static string ToolDescription(string? id) =>
        id is null ? "" : (DescMap.TryGetValue(id, out var v) ? v : "（待接入 Core 能力）");
}
