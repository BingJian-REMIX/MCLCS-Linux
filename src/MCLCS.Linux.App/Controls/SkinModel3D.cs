using SkiaSharp;

namespace MCLCS.Linux.App.Controls;

/// <summary>一个盒子的 6 面贴图矩形。</summary>
using Uv = (MCLCS.Linux.App.Controls.UvRect F, MCLCS.Linux.App.Controls.UvRect B, MCLCS.Linux.App.Controls.UvRect L, MCLCS.Linux.App.Controls.UvRect R, MCLCS.Linux.App.Controls.UvRect T, MCLCS.Linux.App.Controls.UvRect Bo);

/// <summary>3D 点（模型坐标系，Y 向上，单位 = 皮肤像素）。</summary>
public readonly record struct Vec3(double X, double Y, double Z);

/// <summary>皮肤贴图矩形（像素坐标，相对皮肤实际宽高）。</summary>
public readonly record struct UvRect(double X, double Y, double W, double H);

/// <summary>一个四边形面：4 顶点（未旋转的模型坐标）+ 贴图矩形。</summary>
public readonly record struct SkinFace(Vec3 TL, Vec3 TR, Vec3 BR, Vec3 BL, UvRect Uv);

/// <summary>
/// 根据 Minecraft 皮肤位图尺寸构建方块角色 3D 面列表（对齐 WPF SkinModel3D）。
/// <para>
/// UV 布局（标准 64×64，Java 版 1.8+）：头 x0-32/y0-16、帽子 x32-64/y0-16；右腿 x0-16、
/// 躯干 x16-40、右臂 x40-56 均在 y16-32；右裤 x0-16、外套 x16-40、右袖 x40-56 均在 y32-48；
/// 左裤 x0-16、左腿 x16-32、左臂 x32-48、左袖 x48-64 均在 y48-64。
/// 64×32 旧皮肤无第二层，左臂 / 左腿镜像复用右侧。slim 双臂宽 3px。
/// 模型已平移使垂直中心落于原点，便于绕身体旋转。
/// </para>
/// </summary>
public static class SkinModel3D
{
    private const double CenterY = 16.0; // 身高 32，平移使垂直中心落在原点

    /// <summary>第一层（实体）定义。顺序：头/躯干/右臂/左臂/右腿/左腿。Cx/Cy 为盒中心（Cy 含 +16 偏移）。</summary>
    private static readonly (Uv Uv, double W, double H, double D, double Cx, double Cy)[] FirstLayer =
    {
        (HeadUv(),          8, 8,  8,  0, 28),
        (BodyUv(),          8, 12, 4,  0, 18),
        (LimbUv(44, 20),    4, 12, 4,  6, 18),  // 右臂 Right Arm  x40-56 / y16-32
        (LimbUv(36, 52),    4, 12, 4, -6, 18),  // 左臂 Left Arm   x32-48 / y48-64
        (LimbUv(4, 20),     4, 12, 4,  2, 6),   // 右腿 Right Leg  x0-16  / y16-32
        (LimbUv(20, 52),    4, 12, 4, -2, 6),   // 左腿 Left Leg   x16-32 / y48-64
    };

    /// <summary>64×32 旧皮肤：左臂 / 左腿由右侧镜像复用（与原版渲染一致）。</summary>
    private static readonly Uv LegacyLeftArm = LimbUv(44, 20);
    private static readonly Uv LegacyLeftLeg = LimbUv(4, 20);

    /// <summary>第二层（叠加：帽子/外套/袖/裤）UV 表，与第一层一一对应。仅 64×64 使用。</summary>
    private static readonly Uv[] Overlay =
    {
        HatUv(),                 // 0 帽子   Hat      x32-64 / y0-16
        JacketUv(),              // 1 外套   Jacket   x16-40 / y32-48
        LimbUv(44, 36),          // 2 右袖   R Sleeve x40-56 / y32-48
        LimbUv(52, 52),          // 3 左袖   L Sleeve x48-64 / y48-64
        LimbUv(4, 36),           // 4 右裤   R Pants  x0-16  / y32-48
        LimbUv(4, 52),           // 5 左裤   L Pants  x0-16  / y48-64
    };

    /// <summary>构建角色面列表（头/躯干/双臂/双腿；64×64 额外含第二层叠加）。</summary>
    public static List<SkinFace> BuildFaces(double skinWidth, double skinHeight, bool slim)
    {
        var faces = new List<SkinFace>(72);
        bool legacy = skinHeight <= 32; // 64×32 旧皮肤：无第二层
        double tw = Math.Max(1, skinWidth);
        double th = Math.Max(1, skinHeight);

        // 第一层
        for (int i = 0; i < FirstLayer.Length; i++)
        {
            var p = FirstLayer[i];
            bool isArm = i is 2 or 3;
            double w = (isArm && slim) ? 3 : p.W;   // slim：双臂均收窄为 3px
            // slim 手臂贴住躯干侧面，宽度减少的 1px 全部从外侧收回，故中心向内移 0.5。
            double cx = (isArm && slim) ? (i == 2 ? p.Cx - 0.5 : p.Cx + 0.5) : p.Cx;

            Uv uv = p.Uv;
            if (legacy)
            {
                // 64×32：无左臂 / 左腿独立区域，镜像复用右侧
                if (i == 3) uv = LegacyLeftArm;
                else if (i == 5) uv = LegacyLeftLeg;
            }
            if (isArm && slim) uv = SlimUv(uv);

            AddBox(faces, cx, p.Cy - CenterY, 0, w, p.H, p.D, uv, tw, th);
        }

        // 第二层叠加（仅 64×64）：帽子层外扩 0.5px/边（盒 ±1，8→9）；衣裤层外扩 0.25px/边（盒 ±0.5）。
        if (!legacy)
        {
            for (int i = 0; i < Overlay.Length; i++)
            {
                var p = FirstLayer[i];
                bool isHead = i == 0;
                bool isArm = i is 2 or 3;
                double expand = isHead ? 1.0 : 0.5;              // 帽子 0.5/边；衣裤 0.25/边
                double w = (isArm && slim) ? 3 + expand : p.W + expand;
                double cx = (isArm && slim) ? (i == 2 ? p.Cx - 0.5 : p.Cx + 0.5) : p.Cx;
                Uv uv = (isArm && slim) ? SlimUv(Overlay[i]) : Overlay[i];
                AddBox(faces, cx, p.Cy - CenterY, 0, w, p.H + expand, p.D + expand, uv, tw, th);
            }
        }

        return faces;
    }

    // —— UV 构造助手（坐标均来自标准 64×64 布局，第一层与 64×32 共用） ——

    private static Uv HeadUv() => (
        new UvRect(8, 8, 8, 8), new UvRect(24, 8, 8, 8), new UvRect(16, 8, 8, 8), new UvRect(0, 8, 8, 8),
        new UvRect(8, 0, 8, 8), new UvRect(16, 0, 8, 8));

    private static Uv BodyUv() => (
        new UvRect(20, 20, 8, 12), new UvRect(32, 20, 8, 12), new UvRect(16, 20, 4, 12), new UvRect(28, 20, 4, 12),
        new UvRect(20, 16, 8, 4), new UvRect(28, 16, 8, 4));

    // 四肢：front=fx,fy；inner(Left)=fx-4；outer(Right)=fx+4；back=fx+8；top/bottom 在 front 正上方 4px。
    private static Uv LimbUv(int fx, int fy) => (
        new UvRect(fx, fy, 4, 12), new UvRect(fx + 8, fy, 4, 12), new UvRect(fx - 4, fy, 4, 12), new UvRect(fx + 4, fy, 4, 12),
        new UvRect(fx, fy - 4, 4, 4), new UvRect(fx + 4, fy - 4, 4, 4));

    // 帽子层位于头部右侧（x32-64 / y0-16），即头部区域整体右移 32。
    private static Uv HatUv() => (
        new UvRect(40, 8, 8, 8), new UvRect(56, 8, 8, 8), new UvRect(48, 8, 8, 8), new UvRect(32, 8, 8, 8),
        new UvRect(40, 0, 8, 8), new UvRect(48, 0, 8, 8));

    // 外套层位于躯干正下方（x16-40 / y32-48），即躯干区域下移 16。
    private static Uv JacketUv() => (
        new UvRect(20, 36, 8, 12), new UvRect(32, 36, 8, 12), new UvRect(16, 36, 4, 12), new UvRect(28, 36, 4, 12),
        new UvRect(20, 32, 8, 4), new UvRect(28, 32, 8, 4));

    // slim：左臂仅 front/back 宽度由 4 收窄为 3（取插槽最左 3px），其余面维持不变。
    private static Uv SlimUv(Uv uv) => uv with
    {
        F = uv.F with { W = 3 },
        B = uv.B with { W = 3 },
    };

    private static void AddBox(List<SkinFace> faces, double cx, double cy, double cz,
        double w, double h, double d, Uv uv, double tw, double th)
    {
        double hx = w / 2, hy = h / 2, hz = d / 2;

        Vec3 P(double x, double y, double z) => new(cx + x, cy + y, cz + z);

        // 各面四角（外视 CCW：左上、右上、右下、左下）
        faces.Add(new SkinFace(
            P(-hx, +hy, +hz), P(+hx, +hy, +hz), P(+hx, -hy, +hz), P(-hx, -hy, +hz), uv.F));   // 正面 +Z
        faces.Add(new SkinFace(
            P(+hx, +hy, -hz), P(-hx, +hy, -hz), P(-hx, -hy, -hz), P(+hx, -hy, -hz), uv.B));    // 背面 -Z
        faces.Add(new SkinFace(
            P(-hx, +hy, -hz), P(-hx, +hy, +hz), P(-hx, -hy, +hz), P(-hx, -hy, -hz), uv.L));    // 左面 -X
        faces.Add(new SkinFace(
            P(+hx, +hy, +hz), P(+hx, +hy, -hz), P(+hx, -hy, -hz), P(+hx, -hy, +hz), uv.R));   // 右面 +X
        faces.Add(new SkinFace(
            P(-hx, +hy, -hz), P(+hx, +hy, -hz), P(+hx, +hy, +hz), P(-hx, +hy, +hz), uv.T));    // 顶面 +Y
        faces.Add(new SkinFace(
            P(-hx, -hy, +hz), P(+hx, -hy, +hz), P(+hx, -hy, -hz), P(-hx, -hy, -hz), uv.Bo));   // 底面 -Y
    }
}
