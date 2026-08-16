using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 音乐播放器（对齐 WPF 音乐面板）：管理播放列表与四种播放模式。
/// 实际音频解码交由 UI 层 MediaPlayer（Core 仅负责"下一首逻辑"等纯逻辑，见 MusicPlaylist）。
/// </summary>
public class MusicViewModel : ObservableObject
{
    private readonly MusicPlaylist _playlist = new();

    private readonly ObservableCollection<Track> _tracks = new();
    public ObservableCollection<Track> Tracks => _tracks;

    private string _currentTitle = "未选择曲目";
    public string CurrentTitle { get => _currentTitle; set => SetField(ref _currentTitle, value); }

    private string _modeText = MusicPlaylist.ModeText(PlayMode.LoopAll);
    public string ModeText { get => _modeText; set => SetField(ref _modeText, value); }

    private int _volume = 60;
    public int Volume { get => _volume; set { if (SetField(ref _volume, value)) _playlist.Volume = value; } }

    private bool _isPlaying;
    public bool IsPlaying { get => _isPlaying; set => SetField(ref _isPlaying, value); }

    private string _status = "请添加音乐文件夹";
    public string Status { get => _status; set => SetField(ref _status, value); }

    public ICommand AddFolderCommand => new RelayCommand(p => AddFolder(p as string));
    public ICommand PlayCommand => new RelayCommand(_ => Play());
    public ICommand PauseCommand => new RelayCommand(_ => Pause());
    public ICommand NextCommand => new RelayCommand(_ => Step(true));
    public ICommand PrevCommand => new RelayCommand(_ => Step(false));
    public ICommand CycleModeCommand => new RelayCommand(_ => CycleMode());

    private void SyncTracks()
    {
        _tracks.Clear();
        foreach (var t in _playlist.Tracks) _tracks.Add(t);
        if (_playlist.Current is { } c) CurrentTitle = c.Display;
    }

    public void AddFolder(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return;
        var n = _playlist.AddFolder(dir, recursive: true);
        SyncTracks();
        Status = n > 0 ? $"已导入 {n} 首曲目" : "未找到音频文件";
        if (_playlist.Current is null && _playlist.Count > 0) { _playlist.Select(0); SyncTracks(); }
    }

    private void Play()
    {
        if (_playlist.Count == 0) { Status = "播放列表为空"; return; }
        if (_playlist.Current is null) _playlist.Select(0);
        IsPlaying = true;
        SyncTracks();
    }

    private void Pause() => IsPlaying = false;

    private void Step(bool forward)
    {
        var t = forward ? _playlist.Next(userTriggered: true) : _playlist.Previous();
        if (t is null) { IsPlaying = false; Status = "播放结束"; }
        else { IsPlaying = true; SyncTracks(); }
    }

    private void CycleMode()
    {
        _playlist.CycleMode();
        ModeText = MusicPlaylist.ModeText(_playlist.Mode);
    }
}
