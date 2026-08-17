using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Save;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>树形界面里的一个 NBT 节点。</summary>
public class NbtNodeViewModel : ObservableObject
{
    private bool _isExpanded;
    private bool _isSelected;
    private string _valueText = "";

    public NbtNodeViewModel(NbtTag tag, string path)
    {
        Tag = tag;
        Path = path;
        _valueText = NbtEditor.ValueText(tag);

        Children = new ObservableCollection<NbtNodeViewModel>();
        if (tag.Children is not null)
        {
            for (var i = 0; i < tag.Children.Count; i++)
            {
                var child = tag.Children[i];
                // Compound 的子节点用名字寻址，List 的子节点用下标寻址
                var childPath = child.Name is null
                    ? $"{path}[{i}]"
                    : string.IsNullOrEmpty(path) ? child.Name : $"{path}.{child.Name}";
                Children.Add(new NbtNodeViewModel(child, childPath));
            }
        }
    }

    public NbtTag Tag { get; }

    /// <summary>寻址路径（<see cref="NbtEditor.Resolve"/> 用）。</summary>
    public string Path { get; }

    public string DisplayName => Tag.Name ?? $"[{Path[(Path.LastIndexOf('[') + 1)..].TrimEnd(']')}]";

    public string TypeText => Tag.Type.ToString();

    public bool IsScalar => NbtEditor.IsScalar(Tag.Type);

    public string ValueText
    {
        get => _valueText;
        set => SetField(ref _valueText, value);
    }

    public ObservableCollection<NbtNodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>值变更后刷新右列显示。</summary>
    public void RefreshValue() => ValueText = NbtEditor.ValueText(Tag);

    /// <summary>递归展开 / 折叠。</summary>
    public void SetExpandedDeep(bool expanded)
    {
        IsExpanded = expanded;
        foreach (var c in Children) c.SetExpandedDeep(expanded);
    }
}

/// <summary>
/// NBT 编辑器（对齐 WPF NbtViewModel）：树状编辑，保存自动备份原文件。
/// Linux 降级：无文件选择器 / 确认框 / Toast —— 打开用路径输入框与 level.dat 快捷下拉，
/// 另存为 / 导出文本落到当前文件同目录；未保存确认直接放弃。
/// </summary>
public class NbtViewModel : ObservableObject
{
    private static readonly NbtTagType[] AddableTypes =
    {
        NbtTagType.Byte, NbtTagType.Short, NbtTagType.Int, NbtTagType.Long,
        NbtTagType.Float, NbtTagType.Double, NbtTagType.String,
        NbtTagType.List, NbtTagType.Compound
    };

    private ObservableCollection<NbtNodeViewModel> _roots = new();
    private ObservableCollection<string> _quickFiles = new();
    private NbtNodeViewModel? _selectedNode;
    private NbtTag? _root;

    private string _filePath = "";
    private string _editValue = "";
    private string _newChildName = "";
    private NbtTagType _newChildType = NbtTagType.String;
    private string _statusMessage = "打开一个 .dat / .nbt 文件开始编辑（存档的 level.dat 已列在下拉里）";
    private string _summary = "";
    private bool _isDirty;
    private bool _autoBackup = true;

    public ObservableCollection<NbtNodeViewModel> Roots
    {
        get => _roots;
        set => SetField(ref _roots, value);
    }

    /// <summary>快捷入口：当前游戏目录里各存档的 level.dat。</summary>
    public ObservableCollection<string> QuickFiles
    {
        get => _quickFiles;
        set => SetField(ref _quickFiles, value);
    }

    public NbtNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (!SetField(ref _selectedNode, value)) return;
            EditValue = value?.IsScalar == true ? value.ValueText : "";
            OnPropertyChanged(nameof(SelectedPath));
            OnPropertyChanged(nameof(SelectedTypeText));
            OnPropertyChanged(nameof(CanEditValue));
        }
    }

    public string SelectedPath => SelectedNode?.Path ?? "（未选择）";

    public string SelectedTypeText => SelectedNode?.TypeText ?? "-";

    public bool CanEditValue => SelectedNode?.IsScalar == true;

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public string EditValue
    {
        get => _editValue;
        set => SetField(ref _editValue, value);
    }

    public string NewChildName
    {
        get => _newChildName;
        set => SetField(ref _newChildName, value);
    }

    public NbtTagType[] NewChildTypes => AddableTypes;

    public NbtTagType NewChildType
    {
        get => _newChildType;
        set => SetField(ref _newChildType, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string Summary
    {
        get => _summary;
        set => SetField(ref _summary, value);
    }

    /// <summary>是否有未保存的改动（标题上带个 * 提示）。</summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (SetField(ref _isDirty, value)) OnPropertyChanged(nameof(TitleText));
        }
    }

    public string TitleText => string.IsNullOrEmpty(FilePath)
        ? "未打开文件"
        : Path.GetFileName(FilePath) + (IsDirty ? " *" : "");

    /// <summary>保存时自动备份原文件（规格要求，默认开）。</summary>
    public bool AutoBackup
    {
        get => _autoBackup;
        set => SetField(ref _autoBackup, value);
    }

    public ICommand OpenCommand { get; }
    public ICommand OpenQuickCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand ApplyValueCommand { get; }
    public ICommand AddChildCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public ICommand ExportTextCommand { get; }

    public NbtViewModel()
    {
        OpenCommand = new AsyncRelayCommand(_ => OpenAsync());
        OpenQuickCommand = new RelayCommand(p => LoadFile(p as string ?? ""));
        SaveCommand = new RelayCommand(_ => Save(FilePath), _ => _root is not null);
        SaveAsCommand = new AsyncRelayCommand(_ => SaveAsAsync(), _ => _root is not null);
        ApplyValueCommand = new RelayCommand(_ => ApplyValue(), _ => CanEditValue);
        AddChildCommand = new RelayCommand(_ => AddChild(), _ => _root is not null);
        RemoveCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedNode is not null);
        RenameCommand = new RelayCommand(_ => RenameSelected(), _ => SelectedNode is not null);
        ExpandAllCommand = new RelayCommand(_ => SetExpanded(true));
        CollapseAllCommand = new RelayCommand(_ => SetExpanded(false));
        ExportTextCommand = new AsyncRelayCommand(_ => ExportTextAsync(), _ => _root is not null);

        ScanQuickFiles();
    }

    private static string GameRoot => Services.LauncherService.Instance.GameRoot;

    /// <summary>扫描 saves/*/level.dat，方便一键打开（最常改的就是它）。</summary>
    private void ScanQuickFiles()
    {
        var list = new List<string>();
        try
        {
            var savesDir = Path.Combine(GameRoot, "saves");
            if (Directory.Exists(savesDir))
            {
                foreach (var dir in Directory.GetDirectories(savesDir).OrderBy(Path.GetFileName))
                {
                    var dat = Path.Combine(dir, "level.dat");
                    if (File.Exists(dat)) list.Add(dat);
                }
            }
        }
        catch { /* 目录不可读时留空即可 */ }

        QuickFiles = new ObservableCollection<string>(list);
    }

    private async Task OpenAsync()
    {
        var picked = await Services.UIService.PickFileAsync("打开 NBT 文件", "*.dat;*.nbt;*.dat_old;*.schematic");
        if (!string.IsNullOrWhiteSpace(picked)) LoadFile(picked);
    }

    private void LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var root = NbtEditor.Load(path);
        if (root is null)
        {
            StatusMessage = $"打开失败：{path}（不是有效的 gzip NBT 文件？）";
            Services.ToastService.Show("NBT 编辑器", "文件打开失败", Services.ToastKind.Error);
            return;
        }

        _root = root;
        FilePath = path;
        IsDirty = false;

        var rootVm = new NbtNodeViewModel(root, "") { IsExpanded = true };
        Roots = new ObservableCollection<NbtNodeViewModel> { rootVm };
        SelectedNode = rootVm;

        Summary = $"{NbtEditor.CountNodes(root)} 个标签｜{new FileInfo(path).Length / 1024.0:F1} KB";
        StatusMessage = $"已打开 {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(TitleText));
    }

    private void ApplyValue()
    {
        if (_root is null || SelectedNode is null) return;

        var result = NbtEditor.SetValue(_root, SelectedNode.Path, EditValue);
        if (!result.Ok)
        {
            StatusMessage = $"赋值失败：{result.Error}";
            return;
        }

        SelectedNode.RefreshValue();
        IsDirty = true;
        StatusMessage = $"已修改 {SelectedNode.Path}（还未写入磁盘，记得保存）";
    }

    private void AddChild()
    {
        if (_root is null || SelectedNode is null) { StatusMessage = "请先选中一个 Compound 节点"; return; }
        if (string.IsNullOrWhiteSpace(NewChildName)) { StatusMessage = "请先填写新标签的名称"; return; }

        var result = NbtEditor.AddChild(_root, SelectedNode.Path, NewChildName.Trim(), NewChildType);
        if (!result.Ok)
        {
            StatusMessage = $"新增失败：{result.Error}";
            return;
        }

        IsDirty = true;
        StatusMessage = $"已新增 {NewChildName}（{NewChildType}）";
        NewChildName = "";
        RebuildTree(keepPath: SelectedNode.Path);
    }

    private void RemoveSelected()
    {
        if (_root is null || SelectedNode is null) return;
        if (string.IsNullOrEmpty(SelectedNode.Path)) { StatusMessage = "不能删除根标签"; return; }

        var result = NbtEditor.Remove(_root, SelectedNode.Path);
        if (!result.Ok)
        {
            StatusMessage = $"删除失败：{result.Error}";
            return;
        }

        IsDirty = true;
        StatusMessage = "已删除（未写入磁盘）";
        RebuildTree(keepPath: "");
    }

    private void RenameSelected()
    {
        if (_root is null || SelectedNode is null) return;
        if (string.IsNullOrWhiteSpace(NewChildName))
        {
            StatusMessage = "请在「名称」框里填写新名字，然后再点重命名";
            return;
        }

        var result = NbtEditor.Rename(_root, SelectedNode.Path, NewChildName.Trim());
        if (!result.Ok)
        {
            StatusMessage = $"重命名失败：{result.Error}";
            return;
        }

        IsDirty = true;
        StatusMessage = $"已重命名为 {NewChildName}";
        NewChildName = "";
        RebuildTree(keepPath: "");
    }

    /// <summary>结构变化后重建整棵树（NBT 文件不大，重建比增量同步更不容易出错）。</summary>
    private void RebuildTree(string keepPath)
    {
        if (_root is null) return;

        var rootVm = new NbtNodeViewModel(_root, "") { IsExpanded = true };
        Roots = new ObservableCollection<NbtNodeViewModel> { rootVm };
        Summary = $"{NbtEditor.CountNodes(_root)} 个标签";

        SelectedNode = FindByPath(rootVm, keepPath) ?? rootVm;
        ExpandTo(rootVm, SelectedNode.Path);
    }

    private static NbtNodeViewModel? FindByPath(NbtNodeViewModel node, string path)
    {
        if (node.Path == path) return node;
        foreach (var c in node.Children)
        {
            var hit = FindByPath(c, path);
            if (hit is not null) return hit;
        }
        return null;
    }

    private static bool ExpandTo(NbtNodeViewModel node, string path)
    {
        if (node.Path == path) return true;
        foreach (var c in node.Children)
        {
            if (!ExpandTo(c, path)) continue;
            node.IsExpanded = true;
            return true;
        }
        return false;
    }

    private void SetExpanded(bool expanded)
    {
        foreach (var r in Roots) r.SetExpandedDeep(expanded);
    }

    private async Task SaveAsAsync()
    {
        var picked = await Services.UIService.PickFolderAsync("选择保存目录");
        if (string.IsNullOrWhiteSpace(picked)) return;

        var name = string.IsNullOrEmpty(FilePath) ? "output.dat" : Path.GetFileName(FilePath);
        Save(Path.Combine(picked, name));
    }

    private void Save(string path)
    {
        if (_root is null || string.IsNullOrWhiteSpace(path)) return;

        var result = NbtEditor.Save(_root, path, AutoBackup);
        if (!result.Ok)
        {
            StatusMessage = $"保存失败：{result.Error}";
            Services.ToastService.Show("NBT 编辑器", $"保存失败：{result.Error}", Services.ToastKind.Error);
            return;
        }

        FilePath = path;
        IsDirty = false;

        StatusMessage = result.BackupPath is null
            ? $"已保存（{result.SizeBytes / 1024.0:F1} KB）"
            : $"已保存（{result.SizeBytes / 1024.0:F1} KB），原文件备份为 {Path.GetFileName(result.BackupPath)}";
        Services.ToastService.Show("NBT 已保存", Path.GetFileName(path), Services.ToastKind.Success);
        OnPropertyChanged(nameof(TitleText));
    }

    private async Task ExportTextAsync()
    {
        if (_root is null) return;

        try
        {
            var dir = await Services.UIService.PickFolderAsync("选择导出目录");
            if (string.IsNullOrWhiteSpace(dir)) return;
            var name = (string.IsNullOrEmpty(FilePath) ? "nbt" : Path.GetFileNameWithoutExtension(FilePath))
                       + ".txt";
            var dest = Path.Combine(dir, name);
            File.WriteAllText(dest, NbtEditor.RenderTree(_root, maxDepth: 32));
            StatusMessage = $"已导出文本树：{dest}";
            Services.ToastService.Show("NBT 编辑器", $"已导出 {name}", Services.ToastKind.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败：{ex.Message}";
        }
    }
}
