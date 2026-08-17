using SkiaSharp;
using MCLCS.Linux.App.Controls;

namespace MCLCS.Linux.EngineProbe;

/// <summary>
/// 皮肤引擎诊断（z-buffer 版）：四格式 × 8 角度渲染 Skin3DRenderer。
/// 探针皮肤用对比色布局（身前蓝/身后绿、头前肤色+眼睛/头后深蓝、右臂橙/左臂青、
/// 右腿紫/左腿黄绿、叠加层只涂正面），验证：
/// 1. z-buffer 深度测试（正背面不串色、叠加层不遮挡背面）
/// 2. 任意分辨率归一化采样（128×128 / 256×256 高清不错位）
/// 3. 旧格式 2:1 镜像（64×32 / 128×64 左臂/左腿 = 右侧颜色）
/// 4. 左右对称（yaw90 vs yaw270）
/// 用法：MCLCS.Linux.EngineProbe &lt;输出目录&gt;
/// </summary>
internal static class Program
{
    private const int W = 320, H = 380;

    private static int Main(string[] args)
    {
        var outDir = Path.GetFullPath(args.Length > 0 ? args[0] : "/workspace/shots/engine");
        Directory.CreateDirectory(outDir);

        var probe = BuildProbe64(); // 64×64 BGRA 探针（第一层 y0-32 + 第二层 y32-64）
        var formats = new[] { (Name: "64x64", W: 64, H: 64), (Name: "128x128", W: 128, H: 128), (Name: "64x32", W: 64, H: 32), (Name: "128x64", W: 128, H: 64) };
        var yaws = new[] { 0, 45, 90, 135, 180, 225, 270, 315 };

        Console.WriteLine("==== 皮肤引擎诊断（z-buffer 光栅化）====");

        var summary = new List<(string Fmt, int F0, int F180, int F90, int F270)>();
        foreach (var fmt in formats)
        {
            using var skin = BuildSkin(probe, fmt.W, fmt.H);
            Console.WriteLine($"\n--- 格式 {fmt.Name}（{(fmt.H * 2 <= fmt.W ? "旧格式 2:1，左镜像" : "新格式，含叠加层")}）---");

            var stats = new List<(int Yaw, int Opaque)>();
            foreach (var yaw in yaws)
            {
                using var frame = Skin3DRenderer.Render(skin, slim: false, yawDeg: yaw, pitchDeg: -8, camZ: 58, W, H);
                var path = Path.Combine(outDir, $"engine-{fmt.Name}-yaw{yaw:D3}.png");
                using (var fs = File.Create(path))
                    frame.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fs);

                var (opaque, centerRatio) = Analyze(frame);
                stats.Add((yaw, opaque));
                Console.WriteLine($"yaw={yaw,3}°  非透明像素={opaque,5}  中心非透明比={centerRatio,6:P1}  -> {Path.GetFileName(path)}");
            }

            var f0 = stats.First(s => s.Yaw == 0).Opaque;
            var f180 = stats.First(s => s.Yaw == 180).Opaque;
            var f90 = stats.First(s => s.Yaw == 90).Opaque;
            var f270 = stats.First(s => s.Yaw == 270).Opaque;
            summary.Add((fmt.Name, f0, f180, f90, f270));

            Console.WriteLine($"  正背面差 |yaw0-yaw180|={Math.Abs(f0 - f180),5}（身蓝 vs 身绿，应明显）");
            Console.WriteLine($"  左右对称 |yaw90-yaw270|={Math.Abs(f90 - f270),4}（应小）");
        }

        Console.WriteLine("\n==== 汇总 ====");
        Console.WriteLine($"{"格式",-8}{"yaw0",6}{"yaw180",8}{"正背差",8}{"yaw90",7}{"yaw270",8}{"左右差",7}");
        foreach (var s in summary)
            Console.WriteLine($"{s.Fmt,-8}{s.F0,6}{s.F180,8}{Math.Abs(s.F0 - s.F180),8}{s.F90,7}{s.F270,8}{Math.Abs(s.F90 - s.F270),7}");

        // 一致性检查
        Console.WriteLine("\n==== 检查 ====");
        var ok = true;
        foreach (var s in summary)
        {
            if (Math.Abs(s.F0 - s.F180) < 100) { ok = false; Console.WriteLine($"  ⚠ {s.Fmt}: 正背面差过小（{Math.Abs(s.F0 - s.F180)}）——可能 UV 镜像/深度测试错误"); }
            else Console.WriteLine($"  ✓ {s.Fmt}: 正背面差 {Math.Abs(s.F0 - s.F180)}（对比色正确区分）");
            if (Math.Abs(s.F90 - s.F270) > 120) { ok = false; Console.WriteLine($"  ⚠ {s.Fmt}: 左右不对称（差 {Math.Abs(s.F90 - s.F270)}）——可能镜像方向错误"); }
            else Console.WriteLine($"  ✓ {s.Fmt}: 左右对称（差 {Math.Abs(s.F90 - s.F270)}）");
        }
        Console.WriteLine(ok ? "\n全部通过" : "\n存在异常，需人工查看 PNG");
        Console.WriteLine($"\n全部 PNG: {outDir}");
        return ok ? 0 : 1;
    }

    /// <summary>64 空间探针（BGRA，第一层 y0-32 + 第二层 y32-64）。第一遍用 fillEmpty 模式整图涂肤色作为 fallback，第二遍默认覆盖局部细节。第二层只涂正面（其余肤色），验证 alpha 跳过。</summary>
    private static byte[] BuildProbe64()
    {
        var px = new byte[64 * 64 * 4];
        // 整图肤色兜底：fillEmpty=true 只涂未设色像素（初始全 0 → 全部涂肤色）
        Rect(0, 0, 64, 64, 0xC8, 0xA8, 0x90, fillEmpty: true);
        void Rect(int x0, int y0, int w, int h, byte r, byte g, byte b, bool fillEmpty = false)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                {
                    if (x < 0 || x >= 64 || y < 0 || y >= 64) continue;
                    int i = (y * 64 + x) * 4;
                    // fillEmpty 模式：只涂未设色 (RGB==0)；默认强制覆盖
                    if (fillEmpty && (px[i] != 0 || px[i + 1] != 0 || px[i + 2] != 0)) continue;
                    px[i] = b; px[i + 1] = g; px[i + 2] = r; px[i + 3] = 255;
                }
        }

        // ---- 第一层 ----
        // 头 x0-32 y0-16：肤色 + 棕发顶 + 眼睛 + 嘴；头后(24,8) 深蓝以区分正背面
        Rect(0, 0, 32, 16, 0xE8, 0xB6, 0x9B);           // 头整体肤色
        Rect(0, 0, 32, 4, 0x4A, 0x2C, 0x18);           // 头发顶
        Rect(0, 4, 2, 4, 0x4A, 0x2C, 0x18);            // 左发
        Rect(6, 4, 2, 4, 0x4A, 0x2C, 0x18);            // 左发中
        Rect(8, 4, 4, 4, 0x00, 0x00, 0x00);            // 左眼
        Rect(20, 4, 4, 4, 0x00, 0x00, 0x00);           // 右眼
        Rect(12, 10, 8, 2, 0x80, 0x40, 0x40);          // 嘴
        Rect(24, 8, 8, 8, 0x1E, 0x3A, 0x6E);           // 头后（back 面 x24-32 y8-16）= 深蓝
        // 头 top/bottom 过渡色（UV 在 y0-8，避免 y90/y180 旋转时这些面呈黑色）
        Rect(8, 0, 8, 8, 0x4A, 0x2C, 0x18);            // 头顶 top = 棕发
        Rect(16, 0, 8, 8, 0xC8, 0xA8, 0x90);           // 头底 bottom = 颈部肤色
        // 身体 x16-40 y16-32：front(20,20,8,12) 蓝 / back(32,20,8,12) 绿 / 侧面(16,20)(28,20) 蓝
        Rect(20, 20, 8, 12, 0x22, 0x66, 0xCC);         // 身前 = 亮蓝
        Rect(16, 20, 4, 12, 0x22, 0x66, 0xCC);         // 身左
        Rect(28, 20, 4, 12, 0x22, 0x66, 0xCC);         // 身右
        Rect(32, 20, 8, 12, 0x1E, 0x8A, 0x3A);         // 身后 = 亮绿
        // 身 top/bottom 过渡色（UV y16-20，避免侧视黑块）
        Rect(20, 16, 8, 4, 0x22, 0x66, 0xCC);          // 身顶（与身前同色：肩膀）
        Rect(28, 16, 8, 4, 0x1E, 0x8A, 0x3A);          // 身底（与身后同色：腰后）
        // 右臂 x40-56 y16-32：橙（front 44,20）
        Rect(44, 20, 4, 12, 0xF0, 0x7A, 0x20);         // 前
        Rect(40, 20, 4, 12, 0xD0, 0x60, 0x10);         // 内
        Rect(48, 20, 4, 12, 0xD0, 0x60, 0x10);         // 外
        Rect(52, 20, 4, 12, 0xA0, 0x40, 0x08);         // 后（深橙）
        // 右腿 top/bottom 过渡色
        Rect(4, 16, 4, 4, 0x88, 0x33, 0xCC);           // 顶（臀前紫）
        Rect(8, 16, 4, 4, 0x55, 0x11, 0x88);           // 底（臀后深紫）
        // 右腿 x0-16 y16-32：紫（front 4,20）
        Rect(4, 20, 4, 12, 0x88, 0x33, 0xCC);          // 前
        Rect(0, 20, 4, 12, 0x70, 0x22, 0xAA);          // 内
        Rect(8, 20, 4, 12, 0x70, 0x22, 0xAA);          // 外
        Rect(12, 20, 4, 12, 0x55, 0x11, 0x88);         // 后（深紫）
        // 左臂 x32-48 y48-64：青（front 36,52）
        Rect(36, 52, 4, 12, 0x20, 0xC0, 0xC0);         // 前
        Rect(32, 52, 4, 12, 0x18, 0xA0, 0xA0);         // 内
        Rect(40, 52, 4, 12, 0x18, 0xA0, 0xA0);         // 外
        Rect(44, 52, 4, 12, 0x0E, 0x70, 0x70);         // 后（深青）
        // 左臂 top/bottom 过渡色
        Rect(36, 48, 4, 4, 0x20, 0xC0, 0xC0);          // 顶（前青）
        Rect(40, 48, 4, 4, 0x0E, 0x70, 0x70);          // 底（后深青）
        // 左腿 x16-32 y48-64：黄绿（front 20,52）
        Rect(20, 52, 4, 12, 0x9A, 0xC8, 0x2A);         // 前
        Rect(16, 52, 4, 12, 0x7E, 0xA8, 0x1E);         // 内
        Rect(24, 52, 4, 12, 0x7E, 0xA8, 0x1E);         // 外
        Rect(28, 52, 4, 12, 0x5C, 0x80, 0x14);         // 后（深黄绿）
        // 左腿 top/bottom 过渡色
        Rect(20, 48, 4, 4, 0x9A, 0xC8, 0x2A);          // 顶（臀前黄绿）
        Rect(24, 48, 4, 4, 0x5C, 0x80, 0x14);          // 底（臀后深黄绿）

        // ---- 第二层（y32-64，只涂正面，验证透明 alpha 跳过露出实体层）----
        Rect(40, 8, 8, 8, 0xC0, 0x39, 0x2B);           // 帽子 front(40,8,8,8) = 红
        Rect(40, 0, 8, 8, 0xC0, 0x39, 0x2B);           // 帽子 top(40,0,8,8) = 红
        Rect(20, 36, 8, 12, 0x1A, 0x4A, 0x8A);         // 外套 front(20,36,8,12) = 深蓝
        Rect(44, 36, 4, 12, 0xC0, 0x50, 0x10);         // 右袖 front(44,36) = 深橙
        Rect(52, 52, 4, 12, 0x16, 0x8A, 0x8A);         // 左袖 front(52,52) = 深青
        Rect(4, 36, 4, 12, 0x66, 0x18, 0x99);          // 右裤 front(4,36) = 深紫
        Rect(4, 52, 4, 12, 0x6E, 0x94, 0x1A);          // 左裤 front(4,52) = 深黄绿
        return px;
    }

    /// <summary>从 64 空间探针生成指定格式皮肤：正方形=整体缩放；2:1=取第一层(y0-32 空间)缩放。</summary>
    private static SKBitmap BuildSkin(byte[] probe64, int w, int h)
    {
        var sk = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
        bool legacy = h * 2 <= w;
        double sx = w / 64.0;
        double sy = h / (legacy ? 32.0 : 64.0);
        var buf = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            int py = (int)(y / sy); // 64 空间 y（第一层 0-32，第二层 32-64）
            for (int x = 0; x < w; x++)
            {
                int px = (int)(x / sx);
                if (px < 0 || px >= 64 || py < 0 || py >= 64) continue;
                int si = (py * 64 + px) * 4;
                int di = (y * w + x) * 4;
                buf[di] = probe64[si];
                buf[di + 1] = probe64[si + 1];
                buf[di + 2] = probe64[si + 2];
                buf[di + 3] = 255;
            }
        }
        System.Runtime.InteropServices.Marshal.Copy(buf, 0, sk.GetPixels(), buf.Length);
        return sk;
    }

    /// <summary>统计非透明像素 + 中心列非透明占比（背景为透明）。</summary>
    private static (int Opaque, double CenterRatio) Analyze(SKBitmap frame)
    {
        int opaque = 0, opaqueCenter = 0, totalCenter = 0;
        int cx = frame.Width / 2;
        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
        {
            var p = frame.GetPixel(x, y);
            if (p.Alpha > 10) opaque++;
            if (Math.Abs(x - cx) <= 10)
            {
                totalCenter++;
                if (p.Alpha > 10) opaqueCenter++;
            }
        }
        return (opaque, totalCenter == 0 ? 0 : (double)opaqueCenter / totalCenter);
    }
}
