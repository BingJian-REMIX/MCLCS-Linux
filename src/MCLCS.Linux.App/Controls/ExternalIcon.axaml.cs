using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 外联图标控件：先显示 PNG/文字占位（<see cref="FallbackToken"/>），再异步从 <see cref="Url"/> 加载真实封面图，
/// 加载完成后替换占位；任意失败保留占位。Linux 端对应 WPF 的 theme:ExternalIcon。
/// <para>用法：<c>&lt;ctl:ExternalIcon Url="{Binding IconUrl}" FallbackToken="pack" /&gt;</c></para>
/// </summary>
public partial class ExternalIcon : UserControl
{
    /// <summary>外部图像 URL（空则不加载，仅显示占位）。</summary>
    public static readonly StyledProperty<string?> UrlProperty =
        AvaloniaProperty.Register<ExternalIcon, string?>(nameof(Url));

    /// <summary>占位图标 token（对应内嵌 PNG 文件名 / 文字占位）。</summary>
    public static readonly StyledProperty<string> FallbackTokenProperty =
        AvaloniaProperty.Register<ExternalIcon, string>(nameof(FallbackToken), "image");

    public string? Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public string FallbackToken
    {
        get => GetValue(FallbackTokenProperty);
        set => SetValue(FallbackTokenProperty, value);
    }

    private CancellationTokenSource? _cts;
    private static readonly HttpClient _http = new();

    public ExternalIcon()
    {
        InitializeComponent();
        UrlProperty.Changed.AddClassHandler<ExternalIcon>((x, _) => x.Refresh());
        FallbackTokenProperty.Changed.AddClassHandler<ExternalIcon>((x, _) => x.RefreshFallback());
        Loaded += (_, _) => { RefreshFallback(); Refresh(); };
        Unloaded += (_, _) => _cts?.Cancel();
    }

    private void RefreshFallback()
    {
        if (FallbackText is null) return;
        FallbackText.Text = FallbackToken switch
        {
            "pack" => "整合包",
            "map" => "地图",
            "shader" => "光影",
            "tex" => "材质",
            "mod" => "Mod",
            "download" => "下载",
            _ => "封面"
        };
    }

    private void Refresh()
    {
        _cts?.Cancel();
        if (Cover is not null) Cover.IsVisible = false;
        if (FallbackBorder is not null) FallbackBorder.IsVisible = true;

        var url = Url;
        if (string.IsNullOrWhiteSpace(url)) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var captured = url;

        _ = Task.Run(async () =>
        {
            try
            {
                var bytes = await _http.GetByteArrayAsync(captured, token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;
                await using var ms = new MemoryStream(bytes);
                var bmp = new Bitmap(ms);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Cover is null) return;
                    Cover.Source = bmp;
                    Cover.IsVisible = true;
                    if (FallbackBorder is not null) FallbackBorder.IsVisible = false;
                });
            }
            catch
            {
                // 保留占位
            }
        }, token);
    }
}
