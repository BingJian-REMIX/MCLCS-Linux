using System.Linq;
using Avalonia;
using Avalonia.Headless;
using MCLCS.Linux.App;
using MCLCS.Linux.App.ViewModels;
using Xunit;

namespace MCLCS.Linux.Tests;

/// <summary>headless App 初始化（WriteableBitmap 等平台对象需要）。</summary>
public class HeadlessAppFixture
{
    public HeadlessAppFixture()
    {
        AppBuilder.Configure<MCLCS.Linux.App.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
            .SetupWithoutStarting();
    }
}

[CollectionDefinition("Headless")]
public class HeadlessCollection : ICollectionFixture<HeadlessAppFixture>
{
}

/// <summary>皮肤编辑器：36 面 UV 布局 / 画笔与对称绘制 / 撤销重做 / 导出。</summary>
[Collection("Headless")]
public class SkinEditorTests
{
    [Fact]
    public void SkinLayout_六部位_36面_坐标全部有效()
    {
        Assert.Equal(6, SkinLayout.Parts.Count);

        var faces = SkinLayout.Parts.SelectMany(p => p.Faces).ToList();
        Assert.Equal(36, faces.Count); // 6 部位 × 6 面

        // 每个面都在 64×64 内且尺寸为正
        foreach (var f in faces)
        {
            Assert.InRange(f.SrcX, 0, 63);
            Assert.InRange(f.SrcY, 0, 63);
            Assert.True(f.W > 0 && f.H > 0, $"{f.Display} 尺寸非法");
            Assert.True(f.SrcX + f.W <= 64, $"{f.Display} 越界 X");
            Assert.True(f.SrcY + f.H <= 64, $"{f.Display} 越界 Y");
        }

        // 头 / 身 / 四肢每组都有 6 面（正背左右顶底）
        Assert.All(SkinLayout.Parts, p => Assert.Equal(6, p.Faces.Count));
    }

    [Fact]
    public void 画笔_写入像素并镜像到对称面()
    {
        var vm = new SkinEditorViewModel();
        var raFront = SkinLayout.Find("右臂", "正面")!;
        vm.SelectedFace = raFront; // 右臂正面（镜像=左臂正面）
        vm.PrimaryColor = Avalonia.Media.Color.FromRgb(255, 0, 0);
        vm.BrushSize = 1;
        vm.SymmetryEnabled = true;

        // 在右臂正面左上角像素（面内坐标 0,0）落笔
        vm.Paint(new Point(0, 0));

        // 右臂正面 (44,20) 处应为红色
        var leftArm = SkinLayout.Find("左臂", "正面")!;
        // 镜像面左臂正面 (36,48)：水平翻转 → 面内 x = 3（宽 4）
        Assert.True(HasColorAt(vm, raFront.SrcX, raFront.SrcY, 255, 0, 0), "右臂正面左上角应已着色");
        Assert.True(HasColorAt(vm, leftArm.SrcX + 3, leftArm.SrcY, 255, 0, 0), "左臂正面镜像位置应已着色（对称）");

        // 导出 PNG 字节非空且以 PNG 签名开头
        var bytes = vm.ExportBytes();
        Assert.True(bytes.Length > 8);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void 撤销重做_恢复与重放像素()
    {
        var vm = new SkinEditorViewModel();
        vm.SelectedFace = SkinLayout.Find("头部", "正面")!;
        vm.PrimaryColor = Avalonia.Media.Color.FromRgb(0, 0, 255);
        vm.BrushSize = 1;
        vm.SymmetryEnabled = false;

        // 初始透明
        Assert.False(vm.HasSkin);

        // 画一笔 → 有内容
        vm.Paint(new Point(0, 0));
        Assert.True(vm.HasSkin);
        Assert.True(HasColorAt(vm, 8, 8, 0, 0, 255)); // 头正面左上 (8,8)

        // 撤销 → 恢复透明
        vm.UndoCommand.Execute(null);
        Assert.False(vm.HasSkin);

        // 重做 → 内容回来
        vm.RedoCommand.Execute(null);
        Assert.True(vm.HasSkin);
        Assert.True(HasColorAt(vm, 8, 8, 0, 0, 255));
    }

    private static bool HasColorAt(SkinEditorViewModel vm, int x, int y, byte r, byte g, byte b)
    {
        var bytes = vm.ExportBytes();
        using var sk = SkiaSharp.SKBitmap.Decode(bytes);
        var px = sk.GetPixel(x, y);
        return px.Red == r && px.Green == g && px.Blue == b;
    }
}
