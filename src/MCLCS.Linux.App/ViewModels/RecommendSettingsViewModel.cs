using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Recommend;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 推荐（对齐 WPF RecommendationEngine）：智能推荐开关 + 触发推荐计算，
/// 展示推荐项。
/// </summary>
public class RecommendSettingsViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;
    private readonly LauncherProfile _profile;

    private IntelliRecommendMode _mode;
    public IntelliRecommendMode Mode
    {
        get => _mode;
        set => SetField(ref _mode, value);
    }

    public ObservableCollection<IntelliRecommendMode> Modes { get; } = new(
        new[] { IntelliRecommendMode.Enabled, IntelliRecommendMode.LocalOnly, IntelliRecommendMode.Disabled });

    private ObservableCollection<string> _items = new();
    public ObservableCollection<string> Items
    {
        get => _items;
        set => SetField(ref _items, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    public RecommendSettingsViewModel()
    {
        _profile = ProfileStore.Load(_gameRoot);
        _mode = _profile.IntelliRecommend;
    }

    public ICommand SaveCommand => new RelayCommand(_ => Save());
    public ICommand BuildCommand => new AsyncRelayCommand(_ => BuildAsync());

    private void Save()
    {
        try
        {
            _profile.IntelliRecommend = Mode;
            ProfileStore.Save(_profile);
            Status = "推荐设置已保存";
        }
        catch (Exception ex)
        {
            Status = $"保存失败：{ex.Message}";
        }
    }

    private async Task BuildAsync()
    {
        Busy = true;
        Status = "正在计算推荐…";
        try
        {
            var list = await RecommendationEngine.BuildAsync(_gameRoot, _profile, new HttpClient());
            Items = new ObservableCollection<string>(list.Select(i => i.Title));
            Status = $"共 {Items.Count} 条推荐";
        }
        catch (Exception ex)
        {
            Status = $"推荐计算失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
