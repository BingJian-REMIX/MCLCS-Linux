using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using MCLCS.Core.Hud;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 性能监控（对齐 WPF 性能面板）：定时采样本进程 CPU / 内存，
/// 游戏进程接入后还可显示 FPS / 延迟 / 坐标（由 HudMetricsProvider 统一采集）。
/// </summary>
public class PerfViewModel : ObservableObject, IDisposable
{
    private readonly HudMetricsProvider _provider = new();
    private readonly DispatcherTimer _timer;
    private readonly Process _self = Process.GetCurrentProcess();

    private double _cpu;
    public double Cpu { get => _cpu; set => SetField(ref _cpu, value); }

    private double _memUsed;
    public double MemUsed { get => _memUsed; set => SetField(ref _memUsed, value); }

    private double _memMax;
    public double MemMax { get => _memMax; set => SetField(ref _memMax, value); }

    private double _fps;
    public double Fps { get => _fps; set => SetField(ref _fps, value); }

    private int _ping = -1;
    public int Ping { get => _ping; set => SetField(ref _ping, value); }

    private bool _running;
    public bool Running { get => _running; set => SetField(ref _running, value); }

    private string _toggleLabel = "开始监控";
    public string ToggleLabel { get => _toggleLabel; set => SetField(ref _toggleLabel, value); }

    private string _status = "已停止";
    public string Status { get => _status; set => SetField(ref _status, value); }

    public ICommand ToggleCommand => new RelayCommand(_ => Toggle());

    public PerfViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Sample();
    }

    private void Toggle()
    {
        if (_running)
        {
            _timer.Stop();
            Running = false;
            ToggleLabel = "开始监控";
            Status = "已停止";
        }
        else
        {
            _provider.SessionStart = DateTime.Now;
            _timer.Start();
            Running = true;
            ToggleLabel = "停止监控";
            Status = "监控中…";
            Sample();
        }
    }

    private void Sample()
    {
        var m = _provider.Sample(_self, 0);
        Cpu = m.CpuPercent;
        MemUsed = m.MemoryUsedMb;
        MemMax = m.MemoryMaxMb;
        Fps = m.Fps;
        Ping = m.PingMs;
    }

    public void Dispose()
    {
        _timer.Stop();
        try { _self.Dispose(); } catch { }
    }
}
