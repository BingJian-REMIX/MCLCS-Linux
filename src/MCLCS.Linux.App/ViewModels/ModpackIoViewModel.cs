using System.Windows.Input;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 整合包导入导出（对齐 WPF 整合包面板）：将当前版本环境
/// （mods / config / resourcepacks / shaderpacks / 可选 saves）导出为整合包 zip，
/// 由 Core.Toolbox.ModpackExporter 负责写入 mclcs 清单。
/// </summary>
public class ModpackIoViewModel : ObservableObject
{
    private string _gameRoot = "";
    public string GameRoot { get => _gameRoot; set => SetField(ref _gameRoot, value); }

    private string _versionId = "";
    public string VersionId { get => _versionId; set => SetField(ref _versionId, value); }

    private string _displayName = "";
    public string DisplayName { get => _displayName; set => SetField(ref _displayName, value); }

    private string _destZip = "";
    public string DestZip { get => _destZip; set => SetField(ref _destZip, value); }

    private bool _includeMods = true;
    public bool IncludeMods { get => _includeMods; set => SetField(ref _includeMods, value); }

    private bool _includeConfig = true;
    public bool IncludeConfig { get => _includeConfig; set => SetField(ref _includeConfig, value); }

    private bool _includeResourcePacks = true;
    public bool IncludeResourcePacks { get => _includeResourcePacks; set => SetField(ref _includeResourcePacks, value); }

    private bool _includeShaderPacks = true;
    public bool IncludeShaderPacks { get => _includeShaderPacks; set => SetField(ref _includeShaderPacks, value); }

    private bool _includeSaves;
    public bool IncludeSaves { get => _includeSaves; set => SetField(ref _includeSaves, value); }

    private bool _busy;
    public bool Busy { get => _busy; set => SetField(ref _busy, value); }

    private string _status = "就绪";
    public string Status { get => _status; set => SetField(ref _status, value); }

    public ICommand ExportCommand => new AsyncRelayCommand(_ => ExportAsync());

    private async Task ExportAsync()
    {
        if (string.IsNullOrWhiteSpace(GameRoot) || string.IsNullOrWhiteSpace(DestZip))
        {
            Status = "请先填写游戏目录与导出路径";
            return;
        }

        Busy = true;
        Status = "正在导出整合包…";
        try
        {
            var opts = new ModpackExportOptions
            {
                IncludeMods = IncludeMods,
                IncludeConfig = IncludeConfig,
                IncludeResourcePacks = IncludeResourcePacks,
                IncludeShaderPacks = IncludeShaderPacks,
                IncludeSaves = IncludeSaves,
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName
            };
            var outPath = ModpackExporter.Export(GameRoot, VersionId, DestZip, opts);
            Status = $"导出完成：{outPath}";
        }
        catch (Exception ex)
        {
            Status = $"导出失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
