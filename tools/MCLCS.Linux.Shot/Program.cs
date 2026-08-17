using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MCLCS.Linux.App;
using MCLCS.Linux.App.ViewModels;
using MCLCS.Linux.App.Views.Pages;

namespace MCLCS.Linux.Shot;

/// <summary>
/// 无头截图工具：加载真实 App（含 App.axaml 资源 / 主题 / 转换器），
/// 对下载中心 6 个副标签与 AI 助手 / AI 设置页逐一渲染为 PNG，
/// 用于离线验证 XAML 布局与绑定不崩溃。
/// 用法：MCLCS.Linux.Shot &lt;输出目录&gt;（默认 /workspace/shots）
/// </summary>
internal static class Program
{
    private static readonly string[] SubTabs = { "minecraft", "mod", "shader", "resourcepack", "modpack", "map" };
    private static readonly string[] AiPages = { "ai-assist", "ai-settings" };
    private const int W = 1280, H = 820;

    [STAThread]
    private static int Main(string[] args)
    {
        var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "/workspace/shots");
        Directory.CreateDirectory(outDir);

        var app = AppBuilder.Configure<MCLCS.Linux.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        MainViewModel.Instance = new MainViewModel();
        var failures = 0;

        // ---- 下载中心 6 子页 ----
        var vm = DownloadPageViewModel.Instance;
        foreach (var id in SubTabs)
        {
            try
            {
                var view = new DownloadPageView(); // 内部已绑定 DownloadPageViewModel.Instance
                vm.SetSubTab(id);
                var path = Path.Combine(outDir, $"dl-{id}.png");
                Render(view, path);
                Console.WriteLine($"[ok]   {id,-12} -> {path}  cards={vm.Cards.Count} queue={vm.Queue.Count}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"[fail] {id}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ---- AI 助手 / AI 设置 ----
        foreach (var id in AiPages)
        {
            try
            {
                Control view = id switch
                {
                    "ai-assist" => new AiAssistView(),
                    _ => new AiSettingsView(),
                };
                var path = Path.Combine(outDir, $"{id}.png");
                Render(view, path);
                Console.WriteLine($"[ok]   {id,-12} -> {path}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"[fail] {id}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Console.WriteLine(failures == 0 ? "ALL OK" : $"{failures} FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Render(Control view, string path)
    {
        var window = new Window
        {
            Width = W,
            Height = H,
            Content = view,
            Background = new SolidColorBrush(Color.Parse("#0F1115")),
        };
        window.Show();
        window.InvalidateVisual();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        using var bmp = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("CaptureRenderedFrame 返回 null");
        using (var fs = File.Create(path))
            bmp.Save(fs);
        window.Close();
    }
}
