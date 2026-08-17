using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using SkiaSharp;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 皮肤编辑器（对齐 WPF SkinEditorViewModel + 采纳 HTML 迭代设计）：
/// 36 面子画布编辑 + 3D 实时预览 + 取色板 + 对称绘制 + 撤销/重做 +
/// PNG 导入导出 + 应用到离线账号 + 从 0 创建。
/// 像素缓冲为 BGRA 预乘，64×64。
/// </summary>
public class SkinEditorViewModel : ObservableObject
{
    private readonly byte[] _pixels = new byte[64 * 64 * 4];
    private WriteableBitmap _bitmap;
    private WriteableBitmap _faceBitmap;

    // 撤销栈（最多 50 步）
    private readonly Stack<byte[]> _undoStack = new();
    private readonly Stack<byte[]> _redoStack = new();
    private const int MaxUndo = 50;

    // 当前编辑状态
    private SkinPart? _selectedPart;
    private SkinFace? _selectedFace;
    private Color _primaryColor = Color.FromRgb(255, 255, 255);
    private Color _secondaryColor = Color.FromRgb(0, 0, 0);
    private int _brushSize = 1;
    private bool _symmetryEnabled = true;
    private bool _isEraser;
    private int _faceZoom = 10;
    private string _statusMessage = "从 0 创建皮肤，或导入 PNG 开始编辑";
    private bool _hasSkin;
    private bool _isEditing2D = true;

    public SkinEditorViewModel()
    {
        _bitmap = new WriteableBitmap(new PixelSize(64, 64), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        _faceBitmap = new WriteableBitmap(new PixelSize(8, 8), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        ClearToColor(Colors.Transparent);
        FlushFull();
        SelectedPart = SkinLayout.Parts.FirstOrDefault();

        BrushCommand = new RelayCommand(p => Paint(p as Point?));
        FillCommand = new RelayCommand(p => FloodFill(p as Point?));
        PickColorCommand = new RelayCommand(p => PickColor(p as Point?));
        SelectFaceCommand = new RelayCommand(p => { if (p is SkinFace f) SelectedFace = f; });
        SetColorCommand = new RelayCommand(p => { if (p is Color c) PrimaryColor = c; });
        UndoCommand = new RelayCommand(_ => Undo());
        RedoCommand = new RelayCommand(_ => Redo());
        ClearCommand = new RelayCommand(_ => { SaveUndo(); ClearToColor(Colors.Transparent); FlushFull(); StatusMessage = "已清空，可开始绘制"; });
        ExportCommand = new AsyncRelayCommand(_ => ExportSkinAsync());
        ImportCommand = new AsyncRelayCommand(_ => ImportSkinAsync());
        ApplyToAccountCommand = new RelayCommand(_ => ApplyToAccount());
        PickColorCommand = new RelayCommand(p => PickColor(p as Point?));
    }

    // ---- 属性 ----

    public ObservableCollection<SkinPart> Parts => new(SkinLayout.Parts);

    public SkinPart? SelectedPart
    {
        get => _selectedPart;
        set { SetField(ref _selectedPart, value); if (value?.Faces.Count > 0) SelectedFace = value.Faces[0]; }
    }

    public SkinFace? SelectedFace
    {
        get => _selectedFace;
        set { SetField(ref _selectedFace, value); UpdateFacePreview(); }
    }

    /// <summary>当前面显示标题（HTML 设计：头部 · 正面）。</summary>
    public string FaceTitle => SelectedFace?.Display ?? "未选择面";

    public WriteableBitmap FullBitmap
    {
        get => _bitmap;
        set => SetField(ref _bitmap, value);
    }

    public WriteableBitmap FaceBitmap
    {
        get => _faceBitmap;
        set => SetField(ref _faceBitmap, value);
    }

    public Color PrimaryColor { get => _primaryColor; set { if (SetField(ref _primaryColor, value)) OnPropertyChanged(nameof(PrimaryBrush)); } }
    public Color SecondaryColor { get => _secondaryColor; set { if (SetField(ref _secondaryColor, value)) OnPropertyChanged(nameof(SecondaryBrush)); } }
    public IBrush PrimaryBrush => new SolidColorBrush(_primaryColor);
    public IBrush SecondaryBrush => new SolidColorBrush(_secondaryColor);
    public int BrushSize { get => _brushSize; set => SetField(ref _brushSize, value); }
    public bool SymmetryEnabled { get => _symmetryEnabled; set => SetField(ref _symmetryEnabled, value); }
    public bool IsEraser { get => _isEraser; set => SetField(ref _isEraser, value); }
    public int FaceZoom { get => _faceZoom; set { if (SetField(ref _faceZoom, value)) { OnPropertyChanged(nameof(FaceZoomedW)); OnPropertyChanged(nameof(FaceZoomedH)); } } }
    public int FaceZoomedW => (SelectedFace?.W ?? 8) * FaceZoom;
    public int FaceZoomedH => (SelectedFace?.H ?? 8) * FaceZoom;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    /// <summary>是否有皮肤内容（驱动空状态提示显隐，HTML 设计）。</summary>
    public bool HasSkin { get => _hasSkin; set => SetField(ref _hasSkin, value); }
    /// <summary>当前是否处于 2D 编辑模式（false = 3D 预览）。xaml 双向绑定 Editor2D/Preview3D 可见性。</summary>
    public bool IsEditing2D { get => _isEditing2D; set => SetField(ref _isEditing2D, value); }

    public Color[] Palette { get; } =
    {
        Color.FromRgb(255,255,255), Color.FromRgb(180,180,180), Color.FromRgb(112,112,112), Color.FromRgb(56,56,56),
        Color.FromRgb(0,0,0), Color.FromRgb(240,120,120), Color.FromRgb(216,60,60), Color.FromRgb(164,96,60),
        Color.FromRgb(180,108,24), Color.FromRgb(240,180,48), Color.FromRgb(252,236,60), Color.FromRgb(120,204,48),
        Color.FromRgb(60,160,60), Color.FromRgb(48,124,168), Color.FromRgb(72,88,216), Color.FromRgb(136,64,176),
        Color.FromRgb(196,112,208), Color.FromRgb(160,100,80), Color.FromRgb(220,160,120), Color.FromRgb(240,200,168),
        Color.FromRgb(236,176,88), Color.FromRgb(224,128,0), Color.FromRgb(72,56,40), Color.FromRgb(240,124,140),
    };

    // ---- 命令 ----

    public ICommand BrushCommand { get; }
    public ICommand FillCommand { get; }
    public ICommand PickColorCommand { get; }
    public ICommand SelectFaceCommand { get; }
    public ICommand SetColorCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ApplyToAccountCommand { get; }

    // ---- 像素操作 ----

    public void Paint(Point? p)
    {
        if (p is null || SelectedFace is null) return;
        SaveUndo();
        var px = (int)(p.Value.X / FaceZoom) + SelectedFace.SrcX;
        var py = (int)(p.Value.Y / FaceZoom) + SelectedFace.SrcY;
        var c = IsEraser ? Colors.Transparent : PrimaryColor;

        for (var dy = 0; dy < BrushSize; dy++)
        for (var dx = 0; dx < BrushSize; dx++)
        {
            SetPixel(px + dx, py + dy, c);
            if (SymmetryEnabled)
            {
                var (mx, my) = SkinLayout.MirrorPixel(px + dx, py + dy, SelectedFace);
                SetPixel(mx, my, c);
            }
        }
        FlushFull();
        UpdateFacePreview();
    }

    /// <summary>右键取色：把点击像素的颜色设为当前主色。</summary>
    public void PickColor(Point? p)
    {
        if (p is null || SelectedFace is null) return;
        var px = (int)(p.Value.X / FaceZoom) + SelectedFace.SrcX;
        var py = (int)(p.Value.Y / FaceZoom) + SelectedFace.SrcY;
        var c = GetPixel(px, py);
        if (c.A == 0) return; // 透明像素不取色
        PrimaryColor = c;
        StatusMessage = $"已取色 #{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    public void FloodFill(Point? p)
    {
        if (p is null || SelectedFace is null) return;
        SaveUndo();
        var px = (int)(p.Value.X / FaceZoom) + SelectedFace.SrcX;
        var py = (int)(p.Value.Y / FaceZoom) + SelectedFace.SrcY;
        var target = GetPixel(px, py);
        var c = IsEraser ? Colors.Transparent : PrimaryColor;
        if (ColorEquals(target, c)) return;

        FillRegion(SelectedFace.SrcX, SelectedFace.SrcY, SelectedFace.W, SelectedFace.H, px, py, target, c);
        if (SymmetryEnabled)
        {
            var mirror = SkinLayout.Mirror(SelectedFace);
            if (mirror is not null)
            {
                var (mx, my) = SkinLayout.MirrorPixel(px, py, SelectedFace);
                var mt = GetPixel(mx, my);
                FillRegion(mirror.SrcX, mirror.SrcY, mirror.W, mirror.H, mx, my, mt, c);
            }
        }
        FlushFull();
        UpdateFacePreview();
    }

    private void FillRegion(int rX, int rY, int rW, int rH, int sx, int sy, Color target, Color fill)
    {
        var stack = new Stack<(int, int)>();
        stack.Push((sx, sy));
        var visited = new HashSet<(int, int)>();
        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();
            if (cx < rX || cx >= rX + rW || cy < rY || cy >= rY + rH) continue;
            if (!visited.Add((cx, cy))) continue;
            if (!ColorEquals(GetPixel(cx, cy), target)) continue;
            SetPixel(cx, cy, fill);
            stack.Push((cx + 1, cy)); stack.Push((cx - 1, cy));
            stack.Push((cx, cy + 1)); stack.Push((cx, cy - 1));
        }
    }

    // ---- 撤销 / 重做 ----

    private void SaveUndo()
    {
        _undoStack.Push((byte[])_pixels.Clone());
        _redoStack.Clear();
        while (_undoStack.Count > MaxUndo) _undoStack.TryPop(out _);
    }

    private void Undo()
    {
        if (!_undoStack.TryPop(out var prev)) return;
        _redoStack.Push((byte[])_pixels.Clone());
        Array.Copy(prev, _pixels, _pixels.Length);
        FlushFull(); UpdateFacePreview();
    }

    private void Redo()
    {
        if (!_redoStack.TryPop(out var next)) return;
        _undoStack.Push((byte[])_pixels.Clone());
        Array.Copy(next, _pixels, _pixels.Length);
        FlushFull(); UpdateFacePreview();
    }

    // ---- 导入 / 导出 ----

    private async Task ExportSkinAsync()
    {
        var path = await Services.UIService.SaveFileAsync("保存皮肤 PNG", "skin.png", "*.png");
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            ExportTo(path);
            StatusMessage = $"已导出 {Path.GetFileName(path)}";
            Services.ToastService.Show("皮肤编辑器", "已导出", Services.ToastKind.Success);
        }
        catch (Exception ex) { StatusMessage = $"导出失败: {ex.Message}"; }
    }

    /// <summary>把当前像素写入 PNG 文件（供导出与应用共用）。</summary>
    public void ExportTo(string path)
    {
        using var sk = new SkiaSharp.SKBitmap(64, 64, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
        CopyPixelsTo(sk, _pixels);
        using var img = SkiaSharp.SKImage.FromBitmap(sk);
        using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(path);
        data.SaveTo(fs);
    }

    /// <summary>导出到内存 PNG 字节（供 3D 预览 / 测试复用）。</summary>
    public byte[] ExportBytes()
    {
        using var sk = new SkiaSharp.SKBitmap(64, 64, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
        CopyPixelsTo(sk, _pixels);
        using var img = SkiaSharp.SKImage.FromBitmap(sk);
        using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void CopyPixelsTo(SkiaSharp.SKBitmap sk, byte[] px)
    {
        var dst = sk.GetPixels();
        for (var y = 0; y < 64; y++)
            System.Runtime.InteropServices.Marshal.Copy(px, y * 64 * 4, dst + y * sk.RowBytes, 64 * 4);
    }

    private async Task ImportSkinAsync()
    {
        var path = await Services.UIService.PickFileAsync("导入皮肤 PNG", "*.png");
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            using var bmp = new Bitmap(path);
            if (bmp.PixelSize.Width != 64 || (bmp.PixelSize.Height != 64 && bmp.PixelSize.Height != 32))
            {
                StatusMessage = "皮肤必须为 64x64（新版）或 64x32（旧版）像素";
                return;
            }
            SaveUndo();
            var w = bmp.PixelSize.Width;
            var h = bmp.PixelSize.Height;
            var src = new byte[w * h * 4];
            unsafe
            {
                fixed (byte* p = src)
                    bmp.CopyPixels(new PixelRect(0, 0, w, h), (IntPtr)p, src.Length, w * 4);
            }
            Array.Clear(_pixels);
            var copyH = Math.Min(64, h);
            for (var y = 0; y < copyH; y++)
                Array.Copy(src, y * w * 4, _pixels, y * 64 * 4, w * 4);
            FlushFull(); UpdateFacePreview();
            StatusMessage = $"已导入 {Path.GetFileName(path)}";
        }
        catch (Exception ex) { StatusMessage = $"导入失败: {ex.Message}"; }
    }

    private void ApplyToAccount()
    {
        var account = AccountStore.GetLastUsed(Services.LauncherService.Instance.GameRoot);
        if (account is null || account.AuthType != "offline")
        { StatusMessage = "只能应用到离线账号"; return; }
        try
        {
            var skinDir = Path.Combine(Services.LauncherService.Instance.GameRoot, "skins");
            Directory.CreateDirectory(skinDir);
            var skinPath = Path.Combine(skinDir, $"{account.Username}_skin.png");
            ExportTo(skinPath);
            StatusMessage = $"皮肤已应用到 {account.DisplayName}";
            Services.ToastService.Show("皮肤编辑器", "已应用", Services.ToastKind.Success);
        }
        catch (Exception ex) { StatusMessage = $"应用失败: {ex.Message}"; }
    }

    // ---- 内部方法 ----

    public void UpdateFacePreview()
    {
        if (SelectedFace is null) return;
        var fw = SelectedFace.W;
        var fh = SelectedFace.H;
        _faceBitmap = new WriteableBitmap(new PixelSize(fw, fh), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        var facePx = new byte[fw * fh * 4];
        for (var y = 0; y < fh; y++)
        for (var x = 0; x < fw; x++)
        {
            var si = ((SelectedFace.SrcY + y) * 64 + SelectedFace.SrcX + x) * 4;
            var di = (y * fw + x) * 4;
            Array.Copy(_pixels, si, facePx, di, 4);
        }
        WritePixels(_faceBitmap, facePx, fw, fh);
        OnPropertyChanged(nameof(FaceBitmap));
        OnPropertyChanged(nameof(FaceZoomedW));
        OnPropertyChanged(nameof(FaceZoomedH));
        OnPropertyChanged(nameof(FaceTitle));
        OnPropertyChanged(nameof(HasSkin));
    }

    private static void WritePixels(WriteableBitmap wb, byte[] px, int w, int h)
    {
        using var fb = wb.Lock();
        unsafe
        {
            byte* dst = (byte*)fb.Address;
            int stride = Math.Min(fb.RowBytes, w * 4);
            for (var y = 0; y < h; y++)
                System.Runtime.InteropServices.Marshal.Copy(px, y * w * 4, (IntPtr)(dst + (long)y * fb.RowBytes), stride);
        }
    }

    private void SetPixel(int x, int y, Color c)
    {
        if (x < 0 || x >= 64 || y < 0 || y >= 64) return;
        var i = (y * 64 + x) * 4;
        _pixels[i] = c.B; _pixels[i + 1] = c.G;
        _pixels[i + 2] = c.R; _pixels[i + 3] = c.A;
    }

    private Color GetPixel(int x, int y)
    {
        if (x < 0 || x >= 64 || y < 0 || y >= 64) return Colors.Transparent;
        var i = (y * 64 + x) * 4;
        return Color.FromArgb(_pixels[i + 3], _pixels[i + 2], _pixels[i + 1], _pixels[i]);
    }

    private static bool ColorEquals(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

    private void ClearToColor(Color c)
    {
        for (var i = 0; i < _pixels.Length; i += 4)
        { _pixels[i] = c.B; _pixels[i + 1] = c.G; _pixels[i + 2] = c.R; _pixels[i + 3] = c.A; }
    }

    public void FlushFull()
    {
        WritePixels(_bitmap, _pixels, 64, 64);
        HasSkin = AnyOpaque(_pixels);
        OnPropertyChanged(nameof(FullBitmap));
        OnPropertyChanged(nameof(HasSkin));
    }

    /// <summary>从外部 SKBitmap 加载 64×64 皮肤（供截屏/测试注入等场景，绕过文件选择对话框）。</summary>
    public void LoadFromSkia(SKBitmap bmp)
    {
        if (bmp is null || bmp.Width != 64 || bmp.Height != 64)
        {
            StatusMessage = $"皮肤尺寸应为 64×64，当前 {(bmp?.Width ?? 0)}×{(bmp?.Height ?? 0)}";
            return;
        }
        var src = bmp.GetPixels();
        int rb = bmp.RowBytes;
        for (int y = 0; y < 64; y++)
            Marshal.Copy(IntPtr.Add(src, y * rb), _pixels, y * 64 * 4, 64 * 4);
        SaveUndo();
        FlushFull();
        UpdateFacePreview();
        StatusMessage = "已加载皮肤（注入）";
    }

    /// <summary>整张皮肤是否存在非透明像素（驱动空状态提示）。</summary>
    private static bool AnyOpaque(byte[] px)
    {
        for (var i = 3; i < px.Length; i += 4)
            if (px[i] != 0) return true;
        return false;
    }
}
