using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 可旋转的 Minecraft 皮肤 3D 预览控件（对齐 WPF SkinPreview3D）：
/// Skia 软件渲染 + 鼠标拖拽旋转 + 滚轮缩放 + 空闲自动旋转。
/// 通过 <see cref="SkinImage"/> 与 <see cref="Slim"/> 属性驱动模型重建。
/// </summary>
public partial class SkinPreview3D : UserControl
{
    public static readonly StyledProperty<Bitmap?> SkinImageProperty =
        AvaloniaProperty.Register<SkinPreview3D, Bitmap?>(nameof(SkinImage));

    public static readonly StyledProperty<bool> SlimProperty =
        AvaloniaProperty.Register<SkinPreview3D, bool>(nameof(Slim));

    public static readonly StyledProperty<bool> AutoRotateProperty =
        AvaloniaProperty.Register<SkinPreview3D, bool>(nameof(AutoRotate), true);

    public Bitmap? SkinImage
    {
        get => GetValue(SkinImageProperty);
        set => SetValue(SkinImageProperty, value);
    }

    public bool Slim
    {
        get => GetValue(SlimProperty);
        set => SetValue(SlimProperty, value);
    }

    public bool AutoRotate
    {
        get => GetValue(AutoRotateProperty);
        set => SetValue(AutoRotateProperty, value);
    }

    private double _yaw = 35;     // 绕 Y 轴（偏航）
    private double _pitch = -8;   // 绕 X 轴（俯仰）
    private double _camZ = 58;    // 相机距离（32~90）
    private Point _lastPoint;
    private bool _dragging;
    private SKBitmap? _texture;   // 皮肤纹理（Avalonia 位图转 Skia）
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };

    public SkinPreview3D()
    {
        InitializeComponent();
        _timer.Tick += (_, _) =>
        {
            if (AutoRotate && !_dragging)
            {
                _yaw += 0.4;
                Render();
            }
        };

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnWheel;
        PointerCaptureLost += (_, _) => _dragging = false;

        Loaded += (_, _) => { if (AutoRotate) _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();

        SkinImageProperty.Changed.AddClassHandler<SkinPreview3D>((o, _) => { o.RebuildTexture(); o.Render(); });
        SlimProperty.Changed.AddClassHandler<SkinPreview3D>((o, _) => o.Render());
        AutoRotateProperty.Changed.AddClassHandler<SkinPreview3D>((o, e) =>
        {
            if (o.AutoRotate) o._timer.Start();
            else o._timer.Stop();
        });
    }

    private void RebuildTexture()
    {
        _texture?.Dispose();
        _texture = null;
        var bmp = SkinImage;
        if (bmp is null) return;
        _texture = ToSkia(bmp);
    }

    /// <summary>把 Avalonia 位图转为 Skia 纹理（BGRA 预乘，最近邻采样由渲染端控制）。</summary>
    private static SKBitmap ToSkia(Bitmap bmp)
    {
        int w = bmp.PixelSize.Width, h = bmp.PixelSize.Height;
        if (w <= 0 || h <= 0) return new SKBitmap(1, 1);
        var sk = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        var ptr = sk.GetPixels();
        bmp.CopyPixels(new PixelRect(0, 0, w, h), ptr, w * h * 4, w * 4);
        return sk;
    }

    private void Render()
    {
        if (_texture is null) return;
        double w = Bounds.Width, h = Bounds.Height;
        if (w < 16 || h < 16) return;
        int iw = (int)w, ih = (int)h;

        using var frame = Skin3DRenderer.Render(_texture, Slim, _yaw, _pitch, _camZ, iw, ih);
        var wb = new WriteableBitmap(new PixelSize(iw, ih), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = wb.Lock())
        {
            unsafe
            {
                byte* src = (byte*)frame.GetPixels();
                byte* dst = (byte*)fb.Address;
                int sw = frame.RowBytes, dw = fb.RowBytes;
                int copy = Math.Min(sw, dw);
                for (int y = 0; y < ih; y++)
                    Buffer.MemoryCopy(src + (long)y * sw, dst + (long)y * dw, dw, copy);
            }
        }
        View.Source = wb;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragging = true;
            _lastPoint = e.GetPosition(this);
            e.Pointer.Capture(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(this);
        var dx = p.X - _lastPoint.X;
        var dy = p.Y - _lastPoint.Y;
        _lastPoint = p;
        _yaw += dx * 0.5;
        _pitch = Math.Clamp(_pitch + dy * 0.5, -80, 80);
        Render();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        e.Pointer.Capture(null);
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        var z = _camZ - e.Delta.Y * 1.5;
        _camZ = Math.Clamp(z, 32, 90);
        Render();
    }
}
