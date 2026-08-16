using System;

namespace MCLCS.Linux.App;

/// <summary>Toast 运行时模型（含唯一 Id，供服务移除）。</summary>
public sealed class ToastModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastOptions Options { get; }

    public ToastModel(ToastOptions options) => Options = options;
}
