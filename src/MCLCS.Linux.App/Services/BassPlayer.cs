using System;
using Avalonia.Threading;
using ManagedBass;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Services;

/// <summary>
/// 用 BASS（libbass.so）实现 <see cref="IMediaPlayer"/>（实际音频解码宿主），
/// 对应 WPF 的 MediaElementPlayer。仅负责加载 / 播放 / 暂停 / 停止 / 音量，
/// 所有播放列表导航交给 <see cref="MusicPlayerViewModel"/>。
/// 必须挂在可视树上（Window 内），故 MainWindow 中以隐藏元素承载。
/// 无音频设备的环境（如 headless 沙箱）下 Bass.Init 会失败，播放器自动降级为「仅状态展示」。
/// </summary>
public sealed class BassPlayer : IMediaPlayer, IDisposable
{
    private int _channel;
    private float _lastVolume = 60f;
    private bool _available;
    private bool _disposed;

    public event Action? Ended;

    /// <summary>音频后端是否可用（Bass.Init 成功）。</summary>
    public bool Available => _available;

    public BassPlayer()
    {
        // 委托固定引用，避免被 GC 回收导致回调失效
        _endSync = OnEndSync;
        try
        {
            // device = -1 使用默认设备；无音频设备时返回 false → 降级
            _available = Bass.Init(-1, 44100, DeviceInitFlags.Default, IntPtr.Zero);
            if (!_available)
                MusicPlayerViewModel.Instance.StatusText = "音频后端不可用：未检测到音频设备";
        }
        catch (Exception ex)
        {
            _available = false;
            MusicPlayerViewModel.Instance.StatusText = "音频后端初始化失败：" + ex.Message;
        }
    }

    public void LoadAndPlay(string path)
    {
        if (!_available) { MusicPlayerViewModel.Instance.StatusText = "音频后端不可用，无法播放"; return; }
        try
        {
            FreeChannel();
            var isUrl = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            _channel = Bass.CreateStream(path, 0, 0, BassFlags.Default);
            if (_channel == 0)
            {
                MusicPlayerViewModel.Instance.StatusText = "无法打开音源：" + path + "（" + Bass.LastError + "）";
                return;
            }
            Bass.ChannelSetAttribute(_channel, ChannelAttribute.Volume, _lastVolume / 100f);
            Bass.ChannelSetSync(_channel, SyncFlags.End, 0, _endSync, IntPtr.Zero);
            Bass.ChannelPlay(_channel);
        }
        catch (Exception ex)
        {
            MusicPlayerViewModel.Instance.StatusText = "播放失败：" + ex.Message;
        }
    }

    public void Pause()
    {
        if (_channel != 0) Bass.ChannelPause(_channel);
    }

    public void Resume()
    {
        if (_channel != 0) Bass.ChannelPlay(_channel);
    }

    public void Stop() => FreeChannel();

    public void SetVolume(int volume)
    {
        _lastVolume = Math.Clamp(volume, 0, 100);
        if (_channel != 0)
            Bass.ChannelSetAttribute(_channel, ChannelAttribute.Volume, _lastVolume / 100f);
    }

    private readonly SyncProcedure _endSync;

    private void OnEndSync(int handle, int channel, int data, IntPtr user)
    {
        // BASS 回调线程 → 切回 UI 线程通知 VM 切下一首
        Dispatcher.UIThread.Post(() => Ended?.Invoke());
    }

    private void FreeChannel()
    {
        if (_channel != 0)
        {
            Bass.ChannelStop(_channel);
            Bass.StreamFree(_channel);
            _channel = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        FreeChannel();
        try { Bass.Free(); } catch { }
    }
}
