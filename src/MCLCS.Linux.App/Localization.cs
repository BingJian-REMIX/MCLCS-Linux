using System.Collections.Generic;

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
    private static readonly Dictionary<string, string> TitleMap = new()
    {
        // 四色主标签
        ["tab.game"] = "游戏",
        ["tab.download"] = "下载",
        ["tab.toolbox"] = "工具箱",
        ["tab.settings"] = "设置",

        // 下载页副标签
        ["tab.minecraft"] = "Minecraft 版本",
        ["tab.mods"] = "模组",
        ["tab.shader"] = "光影",
        ["tab.resourcepack"] = "资源包",
        ["lbl.modpack"] = "整合包",
        ["tab.map"] = "地图",

        // 工具箱分组名
        ["tool.group.diag"] = "诊断与排障",
        ["tool.group.resource"] = "资源与内容",
        ["tool.group.dev"] = "开发工具",
        ["tool.group.other"] = "其他",

        // 设置页副标签
        ["settings.general"] = "通用",
        ["settings.launch"] = "启动",
        ["settings.download"] = "下载设置",
        ["settings.recommend"] = "推荐配置",
        ["settings.account"] = "账户",
        ["settings.ai"] = "AI 助手",
        ["settings.appearance"] = "外观",
        ["settings.about"] = "关于",

        // 工具箱 20 项
        ["tool.log"] = "日志",
        ["tool.crash"] = "崩溃分析",
        ["tool.perf"] = "性能监控",
        ["tool.network"] = "网络诊断",
        ["tool.filewatch"] = "文件监控",
        ["tool.datapack"] = "数据包",
        ["tool.saves"] = "存档管理",
        ["tool.backup"] = "备份与恢复",
        ["tool.screenshot"] = "截图库",
        ["tool.clean"] = "清理",
        ["tool.modpackio"] = "整合包导入",
        ["tool.music"] = "音乐管理",
        ["tool.moddev"] = "模组开发",
        ["tool.packmaker"] = "整合包制作",
        ["tool.nbt"] = "NBT 编辑",
        ["tool.command"] = "命令生成",
        ["tool.skin"] = "皮肤",
        ["tool.shortcut"] = "桌面快捷方式",
        ["tool.afk"] = "挂机助手",
        ["tool.aichat"] = "AI 聊天",
    };

    /// <summary>把 key 翻译为显示名；未知 key 原样返回。</summary>
    public static string Get(string? key) =>
        key is null ? "" : (TitleMap.TryGetValue(key, out var v) ? v : key);

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
