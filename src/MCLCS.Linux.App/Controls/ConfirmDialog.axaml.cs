using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace MCLCS.Linux.App.Controls;

/// <summary>
/// 模态确认对话框（Avalonia 无内置 MessageBox，对齐 WPF UIService.Confirm 职责）。
/// danger=true 时确定按钮红色（删除/覆盖等破坏性操作）。
/// </summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message, string okText = "确定", bool danger = false) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        OkButton.Content = okText;
        if (danger)
        {
            OkButton.Background = new SolidColorBrush(Color.Parse("#E74C3C"));
            OkButton.BorderBrush = new SolidColorBrush(Color.Parse("#E74C3C"));
        }
        else
        {
            OkButton.Background = new SolidColorBrush(Color.Parse("#3B82F6"));
            OkButton.BorderBrush = new SolidColorBrush(Color.Parse("#3B82F6"));
        }

        CancelButton.Click += (_, _) => Close(false);
        OkButton.Click += (_, _) => Close(true);
    }

    /// <summary>以模态方式展示确认框；无 owner 时返回 null（调用方自行兜底）。</summary>
    public static Task<bool?> ShowAsync(Window? owner, string title, string message,
        string okText = "确定", bool danger = false)
    {
        var dlg = new ConfirmDialog(title, message, okText, danger);
        if (owner is null) return Task.FromResult<bool?>(null);
        return dlg.ShowDialog<bool>(owner).ContinueWith(t => (bool?)t.Result);
    }
}
