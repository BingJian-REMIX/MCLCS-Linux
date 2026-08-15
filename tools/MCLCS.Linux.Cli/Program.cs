using MCLCS.Core.Launcher;
using MCLCS.Core.UI;

namespace MCLCS.Linux.Cli;

/// <summary>
/// MCLCS-Linux 命令行入口（替代上游焊死 WPF 的 MCLCS.Cli）。
/// 仅调用平台无关的 MCLCS.Core，可在任意 .NET 6+ 环境运行。
/// </summary>
internal static class Program
{
    private static readonly string Version =
        typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    private static async Task<int> Main(string[] args)
    {
        var cmd = (args.Length > 0 ? args[0] : "help").ToLowerInvariant();
        return cmd switch
        {
            "detect-java" => await DetectJavaAsync(),
            "tabs" => ShowTabs(),
            "sidebar" => ShowSidebar(args),
            "version" => VersionCmd(),
            "help" or "--help" or "-h" or "" => Help(),
            _ => Fail($"未知命令: {cmd}")
        };
    }

    private static async Task<int> DetectJavaAsync()
    {
        Console.WriteLine("正在扫描 Java 安装（JAVA_HOME / /usr/lib/jvm / /opt/java / PATH）...");
        var list = await JavaDetector.DetectAsync();
        if (list.Count == 0)
        {
            Console.WriteLine("未检测到 Java。请安装 JDK/JRE 或设置 JAVA_HOME。");
            return 1;
        }
        Console.WriteLine($"检测到 {list.Count} 个 Java：");
        foreach (var j in list.OrderByDescending(j => j.MajorVersion))
            Console.WriteLine($"  - {j}  raw={j.RawVersion}");
        return 0;
    }

    private static int ShowTabs()
    {
        Console.WriteLine("四色主标签（来自 Core.UI.MainTabs）：");
        foreach (var t in MainTabs.All.OrderBy(t => t.Order))
            Console.WriteLine($"  [{t.Order}] {t.Kind,-9} {t.Title,-14} {t.DefaultColor}  sidebar={t.HasSidebar}");
        return 0;
    }

    private static int ShowSidebar(string[] args)
    {
        var kindArg = args.Length > 1 ? args[1] : "download";
        var kind = MainTabs.ParseKind(kindArg);
        if (kind is null)
        {
            Console.WriteLine($"未知主标签: {kindArg}（可选 game/download/toolbox/settings）");
            return 1;
        }
        var items = Sidebar.For(kind!.Value);
        if (items.Count == 0)
        {
            Console.WriteLine($"{kind} 页无侧边栏（游戏页常驻，无副标签）");
            return 0;
        }
        Console.WriteLine($"{kind} 页副标签（{items.Count} 项）：");
        foreach (var i in items.OrderBy(i => i.Order))
            Console.WriteLine($"  [{i.Order}] {i.Id,-12} {i.Title,-18} group={i.Group ?? "-"}");
        return 0;
    }

    private static int VersionCmd()
    {
        Console.WriteLine($"mclcs {Version} (MCLCS-Linux CLI, net10.0 + MCLCS.Core)");
        return 0;
    }

    private static int Help()
    {
        Console.WriteLine($"mclcs {Version} — MCLCS-Linux 命令行工具");
        Console.WriteLine("用法: mclcs <command> [args]");
        Console.WriteLine("  detect-java        扫描并列出本机 Java 安装");
        Console.WriteLine("  tabs               列出四色主标签");
        Console.WriteLine("  sidebar <kind>     列出某主标签的副标签 (game/download/toolbox/settings)");
        Console.WriteLine("  version            查看版本");
        Console.WriteLine("  help               显示本帮助");
        return 0;
    }

    private static int Fail(string msg)
    {
        Console.Error.WriteLine(msg);
        Console.Error.WriteLine("运行 'mclcs help' 查看可用命令。");
        return 2;
    }
}
