using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Save;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → NBT 编辑器（对齐 WPF NbtEditor）：加载 .dat（gzip）或原始 NBT，
/// 浏览顶层标签、按路径取值 / 改值，并可写回。常用于改 level.dat 的 DataVersion。
/// </summary>
public class NbtViewModel : ObservableObject
{
    private string _filePath = "";
    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    private NbtTag? _root;
    public NbtTag? Root
    {
        get => _root;
        set => SetField(ref _root, value);
    }

    private ObservableCollection<NbtTag> _topLevel = new();
    public ObservableCollection<NbtTag> TopLevel
    {
        get => _topLevel;
        set => SetField(ref _topLevel, value);
    }

    private string _resolvePath = "DataVersion";
    public string ResolvePath
    {
        get => _resolvePath;
        set => SetField(ref _resolvePath, value);
    }

    private string _resolveResult = "";
    public string ResolveResult
    {
        get => _resolveResult;
        set => SetField(ref _resolveResult, value);
    }

    private string _setValuePath = "DataVersion";
    public string SetValuePath
    {
        get => _setValuePath;
        set => SetField(ref _setValuePath, value);
    }

    private string _setValue = "";
    public string SetValue
    {
        get => _setValue;
        set => SetField(ref _setValue, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand LoadCommand => new RelayCommand(_ => Load());
    public ICommand ResolveCommand => new RelayCommand(_ => Resolve());
    public ICommand SetCommand => new RelayCommand(_ => Set());
    public ICommand SaveCommand => new RelayCommand(_ => Save());

    public void Load()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            Status = "请填写有效的 NBT 文件路径";
            return;
        }
        try
        {
            NbtTag? tag = null;
            try { tag = NbtFile.ReadGzip(FilePath); }
            catch { tag = NbtFile.Read(File.OpenRead(FilePath)); }
            Root = tag;
            TopLevel = new ObservableCollection<NbtTag>(tag.Children ?? new System.Collections.Generic.List<NbtTag>());
            Status = $"已加载：{TopLevel.Count} 个顶层标签";
        }
        catch (Exception ex)
        {
            Status = $"加载失败：{ex.Message}";
        }
    }

    public void Resolve()
    {
        if (Root is null) { Status = "请先加载文件"; return; }
        var node = NbtEditor.Resolve(Root, string.IsNullOrWhiteSpace(ResolvePath) ? null : ResolvePath);
        ResolveResult = node is null ? "（路径未找到）" : $"[{node.Type}] {node.Name} = {NbtValueText(node)}";
        Status = "已取值";
    }

    public void Set()
    {
        if (Root is null) { Status = "请先加载文件"; return; }
        var r = NbtEditor.SetValue(Root, SetValuePath, SetValue);
        ResolveResult = r.Ok ? $"已写入：{SetValuePath} = {SetValue}" : $"写入失败：{r.Error}";
        Status = r.Ok ? "已写入（未保存）" : "写入失败";
    }

    public void Save()
    {
        if (Root is null || string.IsNullOrWhiteSpace(FilePath)) { Status = "请先加载文件"; return; }
        var r = NbtEditor.Save(Root, FilePath, backup: true);
        Status = r.Ok ? $"已保存（备份：{r.BackupPath}）" : $"保存失败：{r.Error}";
    }

    public static string NbtValueText(NbtTag t) => t.Type switch
    {
        NbtTagType.Byte => t.ByteValue.ToString(),
        NbtTagType.Short => t.ShortValue.ToString(),
        NbtTagType.Int => t.IntValue.ToString(),
        NbtTagType.Long => t.LongValue.ToString(),
        NbtTagType.Float => t.FloatValue.ToString(),
        NbtTagType.Double => t.DoubleValue.ToString(),
        NbtTagType.String => t.StringValue ?? "",
        NbtTagType.Compound => $"Compound({t.Children?.Count ?? 0})",
        NbtTagType.List => $"List({t.Children?.Count ?? 0})",
        _ => t.StringValue ?? ""
    };
}
