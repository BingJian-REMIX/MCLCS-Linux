using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MCLCS.Linux.App.Services;
using MCLCS.Linux.App.ViewModels;

namespace MCLCS.Linux.App.Views.Pages;

public partial class AnnualReportView : UserControl
{
    public AnnualReportView()
    {
        InitializeComponent();
        DataContext = new AnnualReportViewModel();
    }

    private async void CopyToken_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AnnualReportViewModel vm) return;
        var token = vm.TokenText;
        if (string.IsNullOrWhiteSpace(token))
        {
            vm.Status = "没有可导出的 Token（该年无数据）";
            return;
        }
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard is not null)
                await top.Clipboard.SetTextAsync(token);
            vm.Status = "分享 Token 已复制到剪贴板";
            Services.ToastService.Show("年度报告", "分享 Token 已复制", ToastKind.Success);
        }
        catch (Exception ex)
        {
            vm.Status = $"复制失败：{ex.Message}";
        }
    }
}
