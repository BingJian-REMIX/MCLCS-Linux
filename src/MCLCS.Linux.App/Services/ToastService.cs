using System.Collections.ObjectModel;

namespace MCLCS.Linux.App;

/// <summary>Toast 通知服务（单例）。管理右下角堆叠的通知列表，供 <see cref="Views.ToastHost"/> 绑定。</summary>
public sealed class ToastService
{
    public static readonly ToastService Instance = new();

    public ObservableCollection<ToastModel> Toasts { get; } = new();

    public void Show(ToastOptions options) => Toasts.Add(new ToastModel(options));

    public void Remove(ToastModel model) => Toasts.Remove(model);
}
