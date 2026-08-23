#if SCREENSHOT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MCLCS.Core.UI;

namespace MCLCS.Linux.App;

/// <summary>
/// 条件编译的「全 UI 截图」工具（仅 `-p:DefineConstants=SCREENSHOT` 构建时生效）。
/// 在 App 启动、MainWindow 就绪后，遍历四个主标签下所有侧栏项，逐一导航并渲染为 PNG，
/// 用于开发期对齐验证（无需人工逐页点击）。
/// </summary>
public static class ScreenshotCapture
{
    private const string OutDir = "/workspace/screenshots";

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
        var window = (MainWindow)lifetime.MainWindow!;

        window.WindowState = WindowState.Normal;
        window.SystemDecorations = SystemDecorations.None;
        window.Width = 1366;
        window.Height = 900;
        window.Position = new PixelPoint(0, 0);
        window.Activate();

        var pages = new List<(MainTabKind kind, string id, string name)>();
        foreach (MainTabKind kind in Enum.GetValues<MainTabKind>())
        {
            var items = Sidebar.For(kind);
            if (items.Count == 0)
            {
                // 无侧栏标签（如游戏页主页）仍需截一张默认页
                pages.Add((kind, "", $"{kind}_default"));
            }
            else
            {
                foreach (var item in items)
                    pages.Add((kind, item.Id, $"{kind}_{item.Id}"));
            }
        }

        Console.WriteLine($"[screenshot] 计划截图 {pages.Count} 个页面");
        _ = CaptureAllAsync(window, pages);
    }

    private static async Task CaptureAllAsync(MainWindow window, List<(MainTabKind, string, string)> pages)
    {
        await Task.Delay(800); // 等窗口首次映射 + 首帧渲染
        for (var i = 0; i < pages.Count; i++)
        {
            var (kind, id, name) = pages[i];
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => window.NavigateTo(kind, id));
                await Task.Delay(700); // 等布局切换 + 渲染
                Capture(name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[screenshot] FAIL {name}: {ex.Message}");
            }
            await Task.Delay(150);
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lt)
            lt.Shutdown();
    }

    private static void Capture(string name)
    {
        var path = Path.Combine(OutDir, $"shot_{name}.png");
        var psi = new ProcessStartInfo("import", $"-window root \"{path}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        proc?.WaitForExit();
        var err = proc?.StandardError.ReadToEnd() ?? "";
        if (proc?.ExitCode != 0)
            Console.WriteLine($"[screenshot] import failed {name}: {err.Trim()}");
        else
            Console.WriteLine($"[screenshot] saved {path}");
    }
}
#endif
