using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MCLCS.Linux.App;
using MCLCS.Linux.App.Controls;
using MCLCS.Linux.App.ViewModels;
using MCLCS.Linux.App.Views.Pages;
using Avalonia.VisualTree;
using MCLCS.Core.Skin;
using SkiaSharp;

namespace MCLCS.Linux.Shot;

/// <summary>
/// 无头截图工具：加载真实 App（含 App.axaml 资源 / 主题 / 转换器），
/// 对下载中心 6 副标签、AI 助手/设置、皮肤页逐一渲染为 PNG，
/// 并直接输出皮肤 3D 软件渲染结果（skin3d.png），用于离线验证布局与绑定不崩溃。
/// 用法：MCLCS.Linux.Shot &lt;输出目录&gt;（默认 /workspace/shots）
/// </summary>
internal static class Program
{
    private static readonly string[] SubTabs = { "minecraft", "mod", "shader", "resourcepack", "modpack", "map" };
    private static readonly string[] AiPages = { "ai-assist", "ai-settings" };
    private static readonly string[] P4Pages = { "backup", "nbt", "afk" };
    private const int W = 1280, H = 820;

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

        // ---- 皮肤页（注入测试皮肤，验证 3D 预览控件集成）----
        try
        {
            var skinView = new SkinView();
            // headless 下 DispatcherTimer 会让 RunJobs 卡死：关闭自动旋转，改为显式注入触发渲染
            foreach (var c in skinView.GetVisualDescendants().OfType<SkinPreview3D>())
                c.AutoRotate = false;

            var window = new Window
            {
                Width = W,
                Height = H,
                Content = skinView,
                Background = new SolidColorBrush(Color.Parse("#0F1115")),
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // 布局完成后再注入皮肤：触发 3D 控件属性回调 → RebuildTexture + Render
            if (skinView.DataContext is SkinViewModel svm)
            {
                svm.SkinImage = CreateTestSkin();
                svm.HasSkin = true;
                svm.SkinInfo = new SkinInfo { SkinUrl = "test://skin", Model = "classic" };
            }
            Dispatcher.UIThread.RunJobs();

            var path = Path.Combine(outDir, "skin.png");
            using var bmp = window.CaptureRenderedFrame()
                ?? throw new InvalidOperationException("CaptureRenderedFrame 返回 null");
            using (var fs = File.Create(path))
                bmp.Save(fs);
            window.Close();
            Console.WriteLine($"[ok]   skin         -> {path}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"[fail] skin: {ex.GetType().Name}: {ex.Message}");
        }

        // ---- 皮肤 3D 渲染管线直出（不依赖 UI，确定性验证）----
        try
        {
            using var skin = CreateTestSkinSkia();
            using var frame = Skin3DRenderer.Render(skin, slim: false, yawDeg: 35, pitchDeg: -8, camZ: 58, 480, 640);
            var p3d = Path.Combine(outDir, "skin3d.png");
            using (var fs = File.Create(p3d))
                frame.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);
            Console.WriteLine($"[ok]   skin3d       -> {p3d}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine($"[fail] skin3d: {ex.GetType().Name}: {ex.Message}");
        }

        // ---- P4：备份 / NBT / AFK ----
        foreach (var id in P4Pages)
        {
            try
            {
                Control view = id switch
                {
                    "backup" => new BackupView(),
                    "nbt" => new NbtView(),
                    _ => new AfkView(),
                };
                // AFK 注入两个示例动作并选中第一个，验证动作列表/编辑区/Token 预览
                if (id == "afk" && view.DataContext is AfkViewModel avm)
                {
                    avm.AddActionCommand.Execute(null);
                    avm.AddActionCommand.Execute(null);
                    if (avm.Actions.Count >= 2)
                    {
                        avm.Actions[0].ActionType = "F";
                        avm.Actions[0].Param = "60";
                        avm.Actions[1].ActionType = "C";
                        avm.Actions[1].Param = "1-500";
                        avm.SelectedAction = avm.Actions[0];
                    }
                }
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
