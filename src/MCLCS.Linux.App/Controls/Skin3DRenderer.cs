using SkiaSharp;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 皮肤 3D 软件渲染器：yaw/pitch 旋转 → 透视投影 → 画家算法排序 → Skia 仿射贴图。
/// 对齐 WPF SkinPreview3D 的相机（FOV 35°、默认 yaw=35° pitch=-8°、相机沿 +Z）。
/// 输出 SKBitmap（BGRA 预乘），由控件转为 Avalonia WriteableBitmap 显示。
/// </summary>
public static class Skin3DRenderer
{
    private const double FovDeg = 35.0;

    /// <summary>把皮肤位图渲染为指定尺寸的 SKBitmap。</summary>
    public static SKBitmap Render(SKBitmap skin, bool slim, double yawDeg, double pitchDeg, double camZ, int width, int height)
    {
        var bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        var faces = SkinModel3D.BuildFaces(skin.Width, skin.Height, slim);
        if (faces.Count == 0) return bmp;

        double yaw = yawDeg * Math.PI / 180.0;
        double pitch = pitchDeg * Math.PI / 180.0;
        double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
        double cx = Math.Cos(pitch), sx = Math.Sin(pitch);

        double tanHalf = Math.Tan(FovDeg * Math.PI / 360.0);

        // 投影 + 深度排序（先画远的）
        var order = faces
            .Select(f =>
            {
                var rotated = new Vec3[4];
                var screen = new System.Numerics.Vector2[4];
                double depthSum = 0;
                Vec3[] src = { f.TL, f.TR, f.BR, f.BL };
                for (int i = 0; i < 4; i++)
                {
                    var p = src[i];
                    // Rx(pitch) 后 Ry(yaw)
                    double y1 = p.Y * cx - p.Z * sx;
                    double z1 = p.Y * sx + p.Z * cx;
                    double x2 = p.X * cy + z1 * sy;
                    double z2 = -p.X * sy + z1 * cy;
                    rotated[i] = new Vec3(x2, y1, z2);
                    depthSum += z2;
                }
                double camDist = camZ - depthSum / 4.0;
                if (camDist < 1) camDist = 1;
                double scale = (height / 2.0) / (tanHalf * camDist);
                for (int i = 0; i < 4; i++)
                {
                    screen[i] = new System.Numerics.Vector2(
                        (float)(width / 2.0 + rotated[i].X * scale),
                        (float)(height / 2.0 - rotated[i].Y * scale));
                }
                return (f, Screen: screen, Depth: depthSum);
            })
            .OrderByDescending(x => x.Depth)
            .ToArray();

        using var paint = new SKPaint { FilterQuality = SKFilterQuality.None, IsAntialias = false };
        using var image = SKImage.FromBitmap(skin);

        foreach (var item in order)
        {
            var f = item.f;
            var s = item.Screen;
            // source 是 image-local 像素坐标，dest 是当前画布坐标（单位正方形，经 Concat 后映射到屏幕四边形）
            var srcRect = new SKRect(
                (float)f.Uv.X,
                (float)f.Uv.Y,
                (float)(f.Uv.X + f.Uv.W),
                (float)(f.Uv.Y + f.Uv.H));

            // 仿射：把单位正方形映射到屏幕四边形（TL/TR/BL 三点）
            var m = new SKMatrix
            {
                ScaleX = s[1].X - s[0].X,
                SkewX = s[3].X - s[0].X,
                TransX = s[0].X,
                SkewY = s[1].Y - s[0].Y,
                ScaleY = s[3].Y - s[0].Y,
                TransY = s[0].Y,
                Persp0 = 0,
                Persp1 = 0,
                Persp2 = 1
            };
            canvas.Save();
            canvas.Concat(ref m);
            canvas.DrawImage(image, srcRect, new SKRect(0, 0, 1, 1), paint);
            canvas.Restore();
        }

        return bmp;
    }
}
