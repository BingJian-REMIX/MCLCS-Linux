using System.Collections.ObjectModel;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Tokens;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 挂机脚本解析（对齐 WPF AfkWorkflowToken）：把挂机 Token 解析为可执行指令序列。
/// </summary>
public class AfkViewModel : ObservableObject
{
    private string _token = "F10;D4;C1-500;*3";
    public string Token
    {
        get => _token;
        set => SetField(ref _token, value);
    }

    private ObservableCollection<AfkInstruction> _instructions = new();
    public ObservableCollection<AfkInstruction> Instructions
    {
        get => _instructions;
        set => SetField(ref _instructions, value);
    }

    private int _repeatCount;
    public int RepeatCount
    {
        get => _repeatCount;
        set => SetField(ref _repeatCount, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand ParseCommand => new RelayCommand(_ => Parse());

    private void Parse()
    {
        var r = AfkWorkflowToken.Parse(Token);
        if (!r.Ok)
        {
            Instructions = new ObservableCollection<AfkInstruction>();
            Status = $"解析失败：{r.Error}";
            return;
        }
        Instructions = new ObservableCollection<AfkInstruction>(r.Instructions);
        RepeatCount = r.RepeatCount;
        Status = $"解析成功：{Instructions.Count} 条指令，整体重复 {RepeatCount} 次";
    }
}
