using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Models;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 版本设置（对话框主体 ViewModel）：展示当前版本的工作目录、版本隔离开关，
/// 并按该版本的工作目录扫描成就。版本隔离直连已有的 <see cref="VersionIsolation"/>（Core）。
/// </summary>
public class VersionSettingsViewModel : ObservableObject
{
    private readonly string _gameRoot;
    private readonly string _versionId;

    public string VersionId => _versionId;
    public string VersionType { get; }

    /// <summary>该版本运行时的游戏工作目录（隔离 → versions/&lt;id&gt;，否则 → 共享根）。</summary>
    public string GameDir => VersionIsolation.GameDirFor(_gameRoot, _versionId);

    private bool _isIsolated;
    /// <summary>是否启用版本隔离。切换即调用 VersionIsolation.Enable/Disable 并重扫成就。</summary>
    public bool IsIsolated
    {
        get => _isIsolated;
        set
        {
            if (SetField(ref _isIsolated, value))
            {
                if (_initialized)
                {
                    if (value) VersionIsolation.Enable(_gameRoot, _versionId, "版本设置手动开启");
                    else VersionIsolation.Disable(_gameRoot, _versionId);
                    RefreshAchievements();
                    OnPropertyChanged(nameof(GameDir));
                }
            }
        }
    }

    private IReadOnlyList<AchievementStats> _achievements = new List<AchievementStats>();
    public IReadOnlyList<AchievementStats> Achievements
    {
        get => _achievements;
        private set => SetField(ref _achievements, value);
    }

    public int TotalCompleted => Achievements.Sum(a => a.Completed);
    public int TotalAchievements => Achievements.Sum(a => a.Total);
    public int TotalPurple => Achievements.Sum(a => a.Purple);
    public bool HasAchievements => Achievements.Count > 0;
    public string AchievementSummary =>
        HasAchievements ? $"共 {Achievements.Count} 个存档 · 达成 {TotalCompleted}/{TotalAchievements}" +
                          (TotalPurple > 0 ? $"（{TotalPurple} 紫色挑战）" : "")
                        : "该版本工作目录下暂无成就数据（saves/&lt;存档&gt;/advancements）";

    private string _status = "";
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _initialized;

    public VersionSettingsViewModel(string versionId, string versionType, string gameRoot)
    {
        _versionId = versionId;
        _gameRoot = gameRoot;
        VersionType = versionType;

        // 初始状态直接赋值，不触发开关逻辑
        _isIsolated = VersionIsolation.IsIsolated(_gameRoot, _versionId);
        RefreshAchievements();
        _initialized = true;
    }

    private void RefreshAchievements()
    {
        Achievements = AchievementScanner.Scan(GameDir);
        OnPropertyChanged(nameof(TotalCompleted));
        OnPropertyChanged(nameof(TotalAchievements));
        OnPropertyChanged(nameof(TotalPurple));
        OnPropertyChanged(nameof(HasAchievements));
        OnPropertyChanged(nameof(AchievementSummary));
        Status = IsIsolated
            ? $"已隔离：工作目录 {GameDir}"
            : $"未隔离：工作目录 {GameDir}（与默认 .minecraft 共享）";
    }
}
