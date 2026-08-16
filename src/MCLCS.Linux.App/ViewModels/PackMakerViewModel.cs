using System.Windows.Input;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 资源包创建器（对齐 WPF 资源包面板）：生成最小可加载的资源包结构
/// （pack.mcmeta + assets 占位），便于在此基础上填充贴图 / 语言文件。
/// </summary>
public class PackMakerViewModel : ObservableObject
{
    private string _packName = "my-resource-pack";
    public string PackName { get => _packName; set => SetField(ref _packName, value); }

    private string _description = "由 MCLCS 创建";
    public string Description { get => _description; set => SetField(ref _description, value); }

    private int _packFormat = 34; // 1.21.x 对应格式号
    public int PackFormat { get => _packFormat; set => SetField(ref _packFormat, value); }

    private string _namespace = "minecraft";
    public string Namespace { get => _namespace; set => SetField(ref _namespace, value); }

    private string _targetDir = "";
    public string TargetDir { get => _targetDir; set => SetField(ref _targetDir, value); }

    private bool _busy;
    public bool Busy { get => _busy; set => SetField(ref _busy, value); }

    private string _status = "填写信息后生成资源包";
    public string Status { get => _status; set => SetField(ref _status, value); }

    public ICommand GenerateCommand => new AsyncRelayCommand(_ => GenerateAsync());

    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(PackName) || string.IsNullOrWhiteSpace(TargetDir))
        {
            Status = "请填写资源包名称与目标目录";
            return;
        }

        Busy = true;
        Status = "正在生成资源包…";
        try
        {
            var root = Path.Combine(TargetDir, PackName);
            Directory.CreateDirectory(Path.Combine(root, "assets", Namespace, "textures"));

            var mcmeta = $$"""
            {
              "pack": {
                "pack_format": {{PackFormat}},
                "description": "{{Description}}"
              }
            }
            """;
            File.WriteAllText(Path.Combine(root, "pack.mcmeta"), mcmeta);
            // 占位贴图（1x1 透明 png 可避免加载警告）
            File.WriteAllBytes(Path.Combine(root, "assets", Namespace, "textures", "pack.png"),
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M8AAAMBAQDJ/pLvAAAAAElFTkSuQmCC"));

            Status = $"已生成到 {root}";
        }
        catch (Exception ex)
        {
            Status = $"生成失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
