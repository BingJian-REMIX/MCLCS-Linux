using System.Collections.Generic;
using Avalonia.Controls;

namespace MCLCS.Linux.App;

/// <summary>对话框在屏幕上的对齐方式（对齐设计稿 scrim 的三种变体：居中 / 右上 / 右下）。</summary>
public enum DialogAlignment
{
    Center,      // 居中（默认，modalRoot 居中）
    TopRight,    // 右上角（下载队列弹窗，宽度 420）
    BottomRight  // 右下角（播放列表弹窗，宽度 320）
}

/// <summary>对话框按钮的视觉风格（对齐设计稿 .btn / .btn.primary / .btn.ghost）。</summary>
public enum DialogButtonKind
{
    Normal,
    Primary, // 主题色填充
    Ghost    // 透明描边
}

/// <summary>单个对话框按钮定义。</summary>
public sealed class DialogButton
{
    public string Text { get; }
    public object? Result { get; }
    public DialogButtonKind Kind { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }

    public DialogButton(string text, object? result = null,
                        DialogButtonKind kind = DialogButtonKind.Normal,
                        bool isDefault = false, bool isCancel = false)
    {
        Text = text;
        Result = result ?? text;
        Kind = kind;
        IsDefault = isDefault;
        IsCancel = isCancel;
    }
}

/// <summary>打开一个对话框的配置（对齐设计稿「不同 askuserquestion」的多种变体）。</summary>
public sealed class DialogOptions
{
    /// <summary>标题；为 null 时不显示标题行。</summary>
    public string? Title { get; init; }

    /// <summary>主体内容：字符串自动呈现为文本；传入 Control 则直接呈现（用于富内容变体）。</summary>
    public object? Content { get; init; }

    /// <summary>按钮集合，默认仅「确定」。</summary>
    public IReadOnlyList<DialogButton> Buttons { get; init; } =
        new[] { new DialogButton("确定", isDefault: true) };

    /// <summary>对齐方式，默认居中。</summary>
    public DialogAlignment Alignment { get; init; } = DialogAlignment.Center;

    /// <summary>模态宽度（设计稿：560 居中 / 420 队列 / 320 播放列表）。null 用默认 560。</summary>
    public double? Width { get; init; }

    /// <summary>锚定控件：非空时对话框以该控件为锚点弹出（水平中线对齐控件中心、向下展开），
    /// 忽略 Alignment。用于「下载队列」从标题栏下载按钮弹出的场景。</summary>
    public Control? Anchor { get; init; }

    /// <summary>点击遮罩是否关闭（交付取消结果）。默认 true。</summary>
    public bool DismissOnScrim { get; init; } = true;
}

/// <summary>通用对话框结果常量（方便调用方比对）。</summary>
public static class DialogResults
{
    public const string Ok = "ok";
    public const string Cancel = "cancel";
    public const string Yes = "yes";
    public const string No = "no";
}
