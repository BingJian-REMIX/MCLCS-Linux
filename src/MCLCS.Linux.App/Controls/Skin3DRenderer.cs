using System.Runtime.InteropServices;
using SkiaSharp;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 皮肤 3D 软件渲染器（z-buffer 光栅化，对齐 WPF 硬件 z-buffer / WebGL 语义）。
/// <para>
/// 与之前「画家算法（深度排序）」版本的关键差异：逐三角形逐像素光栅化 +
/// 深度缓冲测试——每个面按 WPF 三角剖分（TriangleIndices 0,1,2 / 0,2,3）拆成
/// 2 个三角形，三角形内每像素经边缘函数求重心坐标，插值深度与 UV，与深度缓冲
/// 比较后写入。彻底消除画家算法的深度排序缺陷（排序方向错误 / 三角形交叉 /
/// 循环遮挡），与 WPF/WebGL 渲染行为一致。
/// </para>
/// <para>
/// 双面渲染（对齐 WPF BackMaterial=material）：不剔除背面——屏幕空间面积为负时
/// 翻转重心符号，背面纹理呈镜像（与 WPF BackMaterial 从背面观察行为一致）。
/// 皮肤像素 alpha=0 跳过写入（叠加层外扩的透明区域露出实体层，对齐原版渲染）；
/// alpha&gt;0 直接覆盖（皮肤叠加层以二值透明为主，近似原版）。
/// </para>
/// <para>
/// 纹理采样归一化：面列表 UvRect 使用 64×64 标准布局坐标（旧格式第一层与
/// 64×32 共用同一坐标空间），按皮肤实际尺寸缩放采样——x 恒 /64·sw；
/// y 新格式 /64·sh、旧格式 /32·sh。天然支持任意分辨率皮肤
/// （64×64 / 128×128 / 256×256 / 64×32 / 128×64 / 256×128 …）。
/// </para>
/// </summary>
public static class Skin3DRenderer
{
    private const double FovDeg = 35.0;

    /// <summary>把皮肤位图渲染为指定尺寸的 SKBitmap（BGRA8888 Premul，透明背景）。</summary>
    public static SKBitmap Render(SKBitmap skin, bool slim, double yawDeg, double pitchDeg, double camZ, int width, int height)
    {
        var bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        if (skin is null || skin.Width < 1 || skin.Height < 1 || width < 1 || height < 1) return bmp;

        int sw = skin.Width, sh = skin.Height;
        var tex = new byte[sw * sh * 4];
        CopyPixels(skin, tex);

        var faces = SkinModel3D.BuildFaces(sw, sh, slim);
        if (faces.Count == 0) return bmp;

        // 纹理采样归一化因子（UvRect 为 64×64 标准布局坐标）
        bool legacy = sh * 2 <= sw;                 // 2:1 旧格式：y 空间 32
        double uScale = sw / 64.0;
        double vScale = sh / (legacy ? 32.0 : 64.0);

        // 旋转矩阵（Rx(pitch) 后 Ry(yaw)，与 WPF 相机一致）+ 透视投影
        double yaw = yawDeg * Math.PI / 180.0;
        double pitch = pitchDeg * Math.PI / 180.0;
        double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
        double cx = Math.Cos(pitch), sx = Math.Sin(pitch);
        double tanHalf = Math.Tan(FovDeg * Math.PI / 360.0);

        // 面 → 三角形（对齐 WPF TriangleIndices 0,1,2 / 0,2,3；UV 角 TL=(0,0) TR=(1,0) BR=(1,1) BL=(0,1)）
        var tris = new List<PTri>(faces.Count * 2);
        foreach (var f in faces)
        {
            PushTri(tris, f.TL, f.TR, f.BR, 0, 0, 1, 0, 1, 1, f.Uv,
                cx, sx, cy, sy, tanHalf, camZ, width, height);
            PushTri(tris, f.TL, f.BR, f.BL, 0, 0, 1, 1, 0, 1, f.Uv,
                cx, sx, cy, sy, tanHalf, camZ, width, height);
        }

        // z-buffer：存「最近」深度（旋转后 z 越大 = 距相机越近），初始负无穷
        var zbuf = new float[width * height];
        Array.Fill(zbuf, float.NegativeInfinity);
        var outPx = new byte[width * height * 4];

        foreach (var t in tris)
        {
            int ix0 = Math.Max(0, (int)Math.Floor(Math.Min(t.X0, Math.Min(t.X1, t.X2))));
            int ix1 = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(t.X0, Math.Max(t.X1, t.X2))));
            int iy0 = Math.Max(0, (int)Math.Floor(Math.Min(t.Y0, Math.Min(t.Y1, t.Y2))));
            int iy1 = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(t.Y0, Math.Max(t.Y1, t.Y2))));
            if (ix0 > ix1 || iy0 > iy1) continue;

            for (int py = iy0; py <= iy1; py++)
            {
                double cyy = py + 0.5; // 像素中心
                int rowIdx = py * width;
                for (int px = ix0; px <= ix1; px++)
                {
                    double cxx = px + 0.5;

                    // 边缘函数 → 重心（顶点 0 的对边 P1P2，顶点 1 的对边 P2P0，顶点 2 的对边 P0P1）
                    double e0 = (cxx - t.X1) * (t.Y2 - t.Y1) - (cyy - t.Y1) * (t.X2 - t.X1);
                    double e1 = (cxx - t.X2) * (t.Y0 - t.Y2) - (cyy - t.Y2) * (t.X0 - t.X2);
                    double e2 = (cxx - t.X0) * (t.Y1 - t.Y0) - (cyy - t.Y0) * (t.X1 - t.X0);
                    double sum = e0 + e1 + e2;
                    if (Math.Abs(sum) < 1e-9) continue;

                    if (sum < 0) { e0 = -e0; e1 = -e1; e2 = -e2; sum = -sum; } // 背面：不剔除，翻转符号
                    if (e0 < 0 || e1 < 0 || e2 < 0) continue;                  // 三角形外

                    double a0 = e0 / sum, a1 = e1 / sum, a2 = e2 / sum;

                    // 深度测试（z 越大越近）
                    double z = a0 * t.Z0 + a1 * t.Z1 + a2 * t.Z2;
                    int idx = rowIdx + px;
                    if (z <= zbuf[idx]) continue;
                    zbuf[idx] = (float)z;

                    // UV 插值 + 归一化采样
                    double u = a0 * t.U0 + a1 * t.U1 + a2 * t.U2;
                    double v = a0 * t.V0 + a1 * t.V1 + a2 * t.V2;
                    int tx = Clamp((int)((t.Uv.X + u * t.Uv.W) * uScale), 0, sw - 1);
                    int ty = Clamp((int)((t.Uv.Y + v * t.Uv.H) * vScale), 0, sh - 1);

                    int oi = idx * 4;
                    int si = (ty * sw + tx) * 4;
                    if (tex[si + 3] == 0) continue; // 透明跳过：叠加层外扩露出实体层
                    outPx[oi] = tex[si];
                    outPx[oi + 1] = tex[si + 1];
                    outPx[oi + 2] = tex[si + 2];
                    outPx[oi + 3] = tex[si + 3];
                }
            }
        }

        Marshal.Copy(outPx, 0, bmp.GetPixels(), outPx.Length);
        return bmp;
    }

    /// <summary>把皮肤位图像素（含 RowBytes 对齐）拷贝到紧凑 BGRA 数组。</summary>
    private static void CopyPixels(SKBitmap skin, byte[] dst)
    {
        var ptr = skin.GetPixels();
        int rb = skin.RowBytes;
        for (int y = 0; y < skin.Height; y++)
            Marshal.Copy(IntPtr.Add(ptr, y * rb), dst, y * skin.Width * 4, skin.Width * 4);
    }

    /// <summary>旋转 + 透视投影单个顶点，输出屏幕坐标与旋转后深度（越大越近）。</summary>
    private static void Project(Vec3 p, double cx, double sx, double cy, double sy,
        double tanHalf, double camZ, int width, int height, out double sx2, out double sy2, out double sz)
    {
        double y1 = p.Y * cx - p.Z * sx;
        double z1 = p.Y * sx + p.Z * cx;
        double x2 = p.X * cy + z1 * sy;
        double z2 = -p.X * sy + z1 * cy;
        double camDist = camZ - z2;
        if (camDist < 1) camDist = 1;
        double scale = (height / 2.0) / (tanHalf * camDist);
        sx2 = width / 2.0 + x2 * scale;
        sy2 = height / 2.0 - y1 * scale;
        sz = z2;
    }

    /// <summary>压入一个三角形（三个顶点 + 各自归一化 UV 角 + 所属面 UV 矩形）。</summary>
    private static void PushTri(List<PTri> tris, Vec3 a, Vec3 b, Vec3 c,
        double ua, double va, double ub, double vb, double uc, double vc, UvRect uv,
        double cx, double sx, double cy, double sy, double tanHalf, double camZ, int width, int height)
    {
        Project(a, cx, sx, cy, sy, tanHalf, camZ, width, height, out var x0, out var y0, out var z0);
        Project(b, cx, sx, cy, sy, tanHalf, camZ, width, height, out var x1, out var y1, out var z1);
        Project(c, cx, sx, cy, sy, tanHalf, camZ, width, height, out var x2, out var y2, out var z2);
        tris.Add(new PTri(x0, y0, z0, ua, va, x1, y1, z1, ub, vb, x2, y2, z2, uc, vc, uv));
    }

    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

    /// <summary>投影后三角形：3 顶点屏幕坐标 + 深度（越大越近）+ 归一化 UV 角 + 面 UV 矩形。</summary>
    private readonly struct PTri
    {
        public readonly double X0, Y0, Z0, U0, V0;
        public readonly double X1, Y1, Z1, U1, V1;
        public readonly double X2, Y2, Z2, U2, V2;
        public readonly UvRect Uv;

        public PTri(double x0, double y0, double z0, double u0, double v0,
                    double x1, double y1, double z1, double u1, double v1,
                    double x2, double y2, double z2, double u2, double v2,
                    UvRect uv)
        {
            X0 = x0; Y0 = y0; Z0 = z0; U0 = u0; V0 = v0;
            X1 = x1; Y1 = y1; Z1 = z1; U1 = u1; V1 = v1;
            X2 = x2; Y2 = y2; Z2 = z2; U2 = u2; V2 = v2;
            Uv = uv;
        }
    }
}
