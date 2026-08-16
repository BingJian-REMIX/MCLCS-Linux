using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Download;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Resources;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 下载 → 资源包（对齐 WPF ExtraResourceInstaller + ResourcePackRepair）：
/// 安装本地资源包 zip、诊断/修复 pack 格式、重置到原版。
/// </summary>
public class ResourcePackViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private string _zipPath = "";
    public string ZipPath
    {
        get => _zipPath;
        set => SetField(ref _zipPath, value);
    }

    private string _diagnosis = "";
    public string Diagnosis
    {
        get => _diagnosis;
        set => SetField(ref _diagnosis, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand InstallCommand => new RelayCommand(_ => Install());
    public ICommand DiagnoseCommand => new RelayCommand(_ => Diagnose());
    public ICommand ResetCommand => new RelayCommand(_ => Reset());

    private void Install()
    {
        if (string.IsNullOrWhiteSpace(ZipPath) || !File.Exists(ZipPath))
        {
            Status = "请填写有效的资源包 zip 路径";
            return;
        }
        try
        {
            var r = ExtraResourceInstaller.Install(ZipPath, _gameRoot);
            Status = r.Ok ? $"安装成功：{r.Summary}" : $"安装失败：{r.Error}";
        }
        catch (Exception ex)
        {
            Status = $"安装异常：{ex.Message}";
        }
    }

    private void Diagnose()
    {
        try
        {
            var all = ResourcePackRepair.DiagnoseAll(_gameRoot);
            if (all.Count == 0)
            {
                Diagnosis = "未发现资源包。";
                Status = "诊断完成：0 个资源包";
                return;
            }
            var sb = new System.Text.StringBuilder();
            foreach (var d in all)
            {
                sb.AppendLine($"[{System.IO.Path.GetFileName(d.Path)}] format={d.PackFormat}, issues={d.Issues.Count}");
                foreach (var i in d.Issues)
                    sb.AppendLine($"  - {(i.Repairable ? "可修复" : "不可修复")} {i.Kind}: {i.Message}");
            }
            Diagnosis = sb.ToString();
            Status = $"诊断完成：{all.Count} 个资源包";
        }
        catch (Exception ex)
        {
            Status = $"诊断异常：{ex.Message}";
        }
    }

    private void Reset()
    {
        try
        {
            var r = ResourcePackRepairer.ResetToVanilla(_gameRoot);
            Status = r.Success ? $"已重置到原版（{r.Actions.Count} 项操作）" : $"重置失败：{r.Error}";
        }
        catch (Exception ex)
        {
            Status = $"重置异常：{ex.Message}";
        }
    }
}
