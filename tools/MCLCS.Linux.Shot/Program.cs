using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MCLCS.Linux.App;
using MCLCS.Linux.App.Controls;
using MCLCS.Linux.App.Services;
using MCLCS.Linux.App.ViewModels;
using MCLCS.Core.Skin;
using MCLCS.Core.UI;
using SkiaSharp;

namespace MCLCS.Linux.Shot;

/// <summary>
/// 全量截屏工程：加载真实 App + 真实 MainWindow（含侧栏 / 标签 / 状态栏壳层），
/// 经 NavigateTo 路由到全部主标签与副页逐一截图。
/// 覆盖：Game×1 + Download×6 + Toolbox×20 + Settings×8 + skin3d 直出 + UI 组件×2。
/// 用法：MCLCS.Linux.Shot &lt;输出目录&gt;（默认 /workspace/shots）
/// </summary>
internal static class Program
{
    private const int W = 1600, H = 1000;

    // (主标签, 侧栏 id, 文件名)
    private static readonly (MainTabKind Kind, string Sid, string Name)[] Nav =
    {
        (MainTabKind.Game, "", "home"),

        (MainTabKind.Download, "minecraft", "dl-minecraft"),
        (MainTabKind.Download, "mod", "dl-mod"),
        (MainTabKind.Download, "shader", "dl-shader"),
        (MainTabKind.Download, "resourcepack", "dl-resourcepack"),
        (MainTabKind.Download, "modpack", "dl-modpack"),
        (MainTabKind.Download, "map", "dl-map"),

        (MainTabKind.Toolbox, "log", "tb-log"),
        (MainTabKind.Toolbox, "clean", "tb-clean"),
        (MainTabKind.Toolbox, "backup", "tb-backup"),
        (MainTabKind.Toolbox, "screenshot", "tb-screenshot"),
        (MainTabKind.Toolbox, "crash", "tb-crash"),
        (MainTabKind.Toolbox, "datapack", "tb-datapack"),
        (MainTabKind.Toolbox, "saves", "tb-saves"),
        (MainTabKind.Toolbox, "skin", "tb-skin"),
        (MainTabKind.Toolbox, "network", "tb-network"),
        (MainTabKind.Toolbox, "filewatch", "tb-filewatch"),
        (MainTabKind.Toolbox, "nbt", "tb-nbt"),
        (MainTabKind.Toolbox, "shortcut", "tb-shortcut"),
        (MainTabKind.Toolbox, "afk", "tb-afk"),
        (MainTabKind.Toolbox, "aichat", "tb-aichat"),
        (MainTabKind.Toolbox, "perf", "tb-perf"),
        (MainTabKind.Toolbox, "modpackio", "tb-modpackio"),
        (MainTabKind.Toolbox, "music", "tb-music"),
        (MainTabKind.Toolbox, "moddev", "tb-moddev"),
        (MainTabKind.Toolbox, "packmaker", "tb-packmaker"),
        (MainTabKind.Toolbox, "command", "tb-command"),

        (MainTabKind.Settings, "appearance", "st-appearance"),
        (MainTabKind.Settings, "account", "st-account"),
        (MainTabKind.Settings, "general", "st-general"),
        (MainTabKind.Settings, "launch", "st-launch"),
        (MainTabKind.Settings, "download", "st-download"),
        (MainTabKind.Settings, "recommend", "st-recommend"),
        (MainTabKind.Settings, "ai", "st-ai"),
        (MainTabKind.Settings, "about", "st-about"),
    };

    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "/workspace/shots");
        Directory.CreateDirectory(outDir);

        var app = AppBuilder.Configure<MCLCS.Linux.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        MainViewModel.Instance = new MainViewModel();
        var failures = 0;

        // ---- 真实 MainWindow：完整壳层（侧栏 + 主标签 + 状态栏）----
        var mw = new MainWindow { Width = W, Height = H };
        mw.Show();
        Dispatcher.UIThread.RunJobs();

        foreach (var (kind, sid, name) in Nav)
        {
            try
            {
                mw.NavigateTo(kind, sid);
                Dispatcher.UIThread.RunJobs();

                // 皮肤页：注入测试皮肤以渲染 3D（并关闭自动旋转，headless 下 DispatcherTimer 会卡 RunJobs）
                if (sid == "skin")
                {
                    foreach (var c in mw.GetVisualDescendants().OfType<SkinPreview3D>())
                        c.AutoRotate = false;
                    if (FindSkinVm(mw) is { } svm)
                    {
                        svm.SkinImage = CreateTestSkin();
                        svm.HasSkin = true;
                        svm.SkinInfo = new SkinInfo { SkinUrl = "test://skin", Model = "classic" };
                    }
                    Dispatcher.UIThread.RunJobs();
                }

                var path = Path.Combine(outDir, $"{name}.png");
                Capture(mw, path);
                Console.WriteLine($"[ok]   {name,-16} -> {path}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"[fail] {name}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        mw.Close();

        // ---- 皮肤 3D 渲染管线直出（不依赖 UI，确定性验证）----
        try
        {
            using var skin = CreateTestSkinSkia();
            using var frame = Skin3DRenderer.Render(skin, slim: false, yawDeg: 35, pitchDeg: -8, camZ: 58, 480, 640);
            var p3d = Path.Combine(outDir, "skin3d.png");
            using (var fs = File.Create(p3d))
                frame.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);
            Console.WriteLine($"[ok]   skin3d         -> {p3d}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"[fail] skin3d: {ex.GetType().Name}: {ex.Message}");
        }

        // ---- UI 组件：确认对话框 / Toast ----
        try
        {
            var dlg = new ConfirmDialog("确认恢复",
                "将备份「Test World（2026-08-17 15:00）」恢复到：\n/root/.minecraft/saves/Test World\n\n恢复前会自动备份当前状态。\n\n确定继续？",
                "确定", danger: true);
            var p1 = Path.Combine(outDir, "ui-confirm.png");
            RenderWindow(dlg, p1);
            Console.WriteLine($"[ok]   ui-confirm     -> {p1}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"[fail] ui-confirm: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            var toast = new ToastWindow("备份完成", "存档 · Test World → 3.4 MB", ToastKind.Success);
            var p2 = Path.Combine(outDir, "ui-toast.png");
            RenderWindow(toast, p2);
            Console.WriteLine($"[ok]   ui-toast       -> {p2}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"[fail] ui-toast: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine(failures == 0 ? "ALL OK" : $"{failures} FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static SkinViewModel? FindSkinVm(Window mw)
    {
        foreach (var c in mw.GetVisualDescendants())
        {
            if (c is Control { DataContext: SkinViewModel svm }) return svm;
        }
        return null;
    }

    private static void Capture(Window window, string path)
    {
        using var bmp = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("CaptureRenderedFrame 返回 null");
        using (var fs = File.Create(path))
            bmp.Save(fs);
    }

    /// <summary>渲染独立窗口（对话框 / Toast 等），先强制布局收敛再截图。</summary>
    private static void RenderWindow(Window window, string path)
    {
        window.Show();
        window.Measure(new Size(window.Width, double.PositiveInfinity));
        window.Arrange(new Rect(0, 0, window.DesiredSize.Width, window.DesiredSize.Height));
        Dispatcher.UIThread.RunJobs();
        window.InvalidateVisual();
        Dispatcher.UIThread.RunJobs();

        using var bmp = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("CaptureRenderedFrame 返回 null");
        using (var fs = File.Create(path))
            bmp.Save(fs);
        window.Close();
    }

    /// <summary>生成 64×64 测试皮肤（各部位不同颜色，便于目视验证 UV 映射）。</summary>
    private static SKBitmap CreateTestSkinSkia()
    {
        var sk = new SKBitmap(64, 64, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var canvas = new SKCanvas(sk);
        canvas.Clear(new SKColor(0x2E, 0x34, 0x40)); // 底色（默认像素）

        // 头 x0-32 y0-16：肤色 + 眼睛
        using var head = new SKPaint { Color = new SKColor(0xF5, 0xC9, 0xA8) };
        canvas.DrawRect(0, 0, 32, 16, head);
        using var eye = new SKPaint { Color = new SKColor(0x1F, 0x29, 0x33) };
        canvas.DrawRect(8, 8, 4, 4, eye);
        canvas.DrawRect(20, 8, 4, 4, eye);

        // 帽子 x32-64 y0-16
        using var hat = new SKPaint { Color = new SKColor(0xC0, 0x39, 0x2B) };
        canvas.DrawRect(32, 0, 32, 16, hat);

        // 躯干 x16-40 y16-32
        using var body = new SKPaint { Color = new SKColor(0x2E, 0x86, 0xC1) };
        canvas.DrawRect(16, 16, 24, 16, body);

        // 右臂 x40-56 y16-32 / 右腿 x0-16 y16-32
        using var limb = new SKPaint { Color = new SKColor(0xE6, 0x7E, 0x22) };
        canvas.DrawRect(40, 16, 16, 16, limb);
        canvas.DrawRect(0, 16, 16, 16, limb);

        // 外套 x16-40 y32-48
        using var jacket = new SKPaint { Color = new SKColor(0x1A, 0x5F, 0x8C) };
        canvas.DrawRect(16, 32, 24, 16, jacket);

        // 右裤 x0-16 y32-48 / 右袖 x40-56 y32-48
        using var pants = new SKPaint { Color = new SKColor(0x8E, 0x44, 0xAD) };
        canvas.DrawRect(0, 32, 16, 16, pants);
        canvas.DrawRect(40, 32, 16, 16, pants);

        // 左腿 x16-32 y48-64 / 左臂 x32-48 y48-64 / 左裤 x0-16 y48-64 / 左袖 x48-64 y48-64
        using var left = new SKPaint { Color = new SKColor(0x16, 0xA0, 0x85) };
        canvas.DrawRect(16, 48, 16, 16, left);
        canvas.DrawRect(32, 48, 16, 16, left);
        using var left2 = new SKPaint { Color = new SKColor(0x0E, 0x6B, 0x58) };
        canvas.DrawRect(0, 48, 16, 16, left2);
        canvas.DrawRect(48, 48, 16, 16, left2);

        return sk;
    }

    private static Bitmap CreateTestSkin()
    {
        using var sk = CreateTestSkinSkia();
        using var png = sk.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        png.SaveTo(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }
}
