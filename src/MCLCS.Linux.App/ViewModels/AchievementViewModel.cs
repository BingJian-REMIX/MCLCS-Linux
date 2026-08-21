using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Utils;
using MCLCS.Linux.App.Services;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>单个存档的成就统计行。</summary>
public class AchievementStats : ObservableObject
{
    public string SaveName { get; init; } = "";
    public int Completed { get; set; }
    public int Total { get; set; }
    public int Purple { get; set; } // 紫色（挑战）成就
    public string Summary => $"达成 {Completed}/{Total}" + (Purple > 0 ? $"（{Purple} 紫色）" : "");
}

/// <summary>
/// 成就展示（对齐 WPF AchievementView）：读取各存档 <c>advancements/*.json</c>，
/// 统计达成 / 总数 / 紫色挑战成就数量。
/// </summary>
public class AchievementViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<AchievementStats> _saves = new();
    public ObservableCollection<AchievementStats> Saves
    {
        get => _saves;
        set => SetField(ref _saves, value);
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

    public ICommand RefreshCommand => new AsyncRelayCommand(_ => RefreshAsync());

    public AchievementViewModel()
    {
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        Busy = true;
        try
        {
            var savesDir = Path.Combine(_gameRoot, "saves");
            var stats = new List<AchievementStats>();

            if (!Directory.Exists(savesDir))
            {
                Status = "未找到 saves 目录";
                Saves = new ObservableCollection<AchievementStats>();
                return;
            }

            foreach (var saveDir in Directory.GetDirectories(savesDir))
            {
                var name = Path.GetFileName(saveDir);
                var advDir = Path.Combine(saveDir, "advancements");
                if (!Directory.Exists(advDir))
                {
                    stats.Add(new AchievementStats { SaveName = name, Completed = 0, Total = 0 });
                    continue;
                }

                var completed = 0;
                var total = 0;
                var purple = 0;
                var allJson = Directory.GetFiles(advDir, "*.json", SearchOption.AllDirectories);

                foreach (var jsonFile in allJson)
                {
                    try
                    {
                        await using var fs = File.OpenRead(jsonFile);
                        using var doc = await JsonDocument.ParseAsync(fs);
                        var root = doc.RootElement;
                        total++;
                        if (root.TryGetProperty("done", out var done) && done.GetBoolean())
                        {
                            completed++;
                            // 紫色（挑战）成就：display.frame 为 "challenge"
                            if (root.TryGetProperty("display", out var display) &&
                                display.TryGetProperty("frame", out var frame) &&
                                frame.GetString() == "challenge")
                                purple++;
                        }
                    }
                    catch { /* 跳过损坏文件 */ }
                }

                stats.Add(new AchievementStats
                {
                    SaveName = name,
                    Completed = completed,
                    Total = total,
                    Purple = purple
                });
            }

            Saves = new ObservableCollection<AchievementStats>(stats);
            Status = $"{stats.Count} 个存档扫描完成";
        }
        catch (Exception ex)
        {
            Status = $"扫描失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
