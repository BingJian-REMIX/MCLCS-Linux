using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 文件变更检测面板（详情窗口内）：展示自上次「标记为已知」以来，手动丢入
/// mods / resourcepacks / shaderpacks 的新增 / 删除 / 修改；支持重新扫描（只看不更新基线）、
/// 标记为已知（重建基线）与本面板开关总开关（同步到 profile）。
/// 自动检测由 <see cref="Services.FileWatchService"/> 在启动 / 焦点回归时触发。
/// </summary>
public class FileWatchViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<FileChange> _changes = new();
    private bool _fileWatchEnabled = true;
    private string _status = "";
    private string _summary = "";
    private bool _isBusy;

    public ObservableCollection<FileChange> Changes
    {
        get => _changes;
        set => SetField(ref _changes, value);
    }

    /// <summary>总开关（设置 → 常规 → 启用文件监控）。</summary>
    public bool FileWatchEnabled
    {
        get => _fileWatchEnabled;
        set
        {
            if (!SetField(ref _fileWatchEnabled, value)) return;
            SaveEnabled();
        }
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string Summary
    {
        get => _summary;
        set => SetField(ref _summary, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public ICommand RefreshCommand => new AsyncRelayCommand(_ => RefreshAsync(), _ => !IsBusy);
    public ICommand ResetBaselineCommand => new AsyncRelayCommand(_ => ResetAsync(), _ => !IsBusy);

    public FileWatchViewModel()
    {
        _fileWatchEnabled = ProfileStore.Load(_gameRoot).FileWatchEnabled;
        _ = RefreshAsync();
    }

    /// <summary>只看不更新基线：面板可反复刷新而不清掉「待确认」的列表。</summary>
    private async Task RefreshAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsBusy = true);
        var diff = new SnapshotDiff();
        string? error = null;
        try
        {
            diff = await Task.Run(() => FileChangeDetector.PreviewChanges(_gameRoot));
        }
        catch (System.Exception ex)
        {
            error = ex.Message;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsBusy = false;
            if (error is not null)
            {
                Status = $"扫描失败：{error}";
                return;
            }
            Changes = new ObservableCollection<FileChange>(diff.Changes);
            Summary = diff.HasChanges ? diff.Summary : "与基线一致，没有未确认的变更";
            Status = diff.HasChanges
                ? "下列变更尚未「标记为已知」，下次启动 / 回到启动器会再次提醒"
                : "没有未确认的变更";
        });
    }

    /// <summary>把当前列表全部标记为已知（重建基线）。</summary>
    private async Task ResetAsync()
    {
        if (Changes.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Status = "当前没有待确认的变更，无需标记");
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => IsBusy = true);
        var ok = false;
        try
        {
            ok = await Task.Run(() => FileChangeDetector.ResetBaseline(_gameRoot));
        }
        catch (System.Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Status = $"重建基线失败：{ex.Message}");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsBusy = false);
        }

        if (ok)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Status = $"已把 {Changes.Count} 个文件标记为已知（重建基线）";
                ToastService.Instance.Show(new ToastOptions
                {
                    Title = "文件变更检测",
                    Message = Status,
                    DurationMs = 4000
                });
            });
            await RefreshAsync();
        }
    }

    private void SaveEnabled()
    {
        try
        {
            var p = ProfileStore.Load(_gameRoot);
            p.FileWatchEnabled = _fileWatchEnabled;
            ProfileStore.Save(p);
            Status = _fileWatchEnabled ? "已开启文件变更检测" : "已关闭文件变更检测";
        }
        catch (System.Exception ex)
        {
            Status = $"保存设置失败：{ex.Message}";
        }
    }
}
