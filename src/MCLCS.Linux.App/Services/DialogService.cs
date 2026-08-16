using System.ComponentModel;
using System.Threading.Tasks;

namespace MCLCS.Linux.App;

/// <summary>
/// 模态对话框服务（单例）。管理当前活动对话框并异步等待用户选择。
/// 视图层 <see cref="Views.ModalHost"/> 绑定 <see cref="Instance"/>.<see cref="CurrentDialog"/>；
/// 按钮点击 / 关闭调用 <see cref="Complete"/> 交付结果。
/// </summary>
public sealed class DialogService : INotifyPropertyChanged
{
    public static readonly DialogService Instance = new();

    private DialogOptions? _current;
    private TaskCompletionSource<object?>? _tcs;

    /// <summary>当前活动对话框；为 null 时 ModalHost 隐藏。</summary>
    public DialogOptions? CurrentDialog
    {
        get => _current;
        private set
        {
            _current = value;
            OnPropertyChanged(nameof(CurrentDialog));
            OnPropertyChanged(nameof(HasDialog));
        }
    }

    /// <summary>是否有活动对话框（供覆盖层绑定可见性）。</summary>
    public bool HasDialog => _current is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>打开对话框并异步等待用户选择。返回点击按钮的 <see cref="DialogButton.Result"/>；
    /// ESC / 点击遮罩 / 关闭按钮返回 null（取消）。</summary>
    public Task<object?> ShowAsync(DialogOptions options)
    {
        _tcs = new TaskCompletionSource<object?>();
        CurrentDialog = options;
        return _tcs.Task;
    }

    /// <summary>由 ModalHost 在按钮点击 / 关闭时调用，结束对话框并交付结果（null = 取消）。</summary>
    public void Complete(object? result)
    {
        if (_tcs is null) return;
        var tcs = _tcs;
        _tcs = null;
        CurrentDialog = null;
        tcs.TrySetResult(result);
    }
}
