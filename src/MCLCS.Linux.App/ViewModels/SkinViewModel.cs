using System.IO;
using System.Net.Http;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Skin;
using MCLCS.Core.Toolbox;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 工具箱 → 皮肤预览（对齐 WPF SkinFetcher/SkinEditor）：
/// 按用户名获取正版皮肤，加载为位图并校验尺寸 / 模型。
/// </summary>
public class SkinViewModel : ObservableObject
{
    private static readonly HttpClient Http = new();

    private string _username = "";
    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    private SkinInfo? _skinInfo;
    public SkinInfo? SkinInfo
    {
        get => _skinInfo;
        set => SetField(ref _skinInfo, value);
    }

    private Bitmap? _skinImage;
    public Bitmap? SkinImage
    {
        get => _skinImage;
        set => SetField(ref _skinImage, value);
    }

    private SkinValidation? _validation;
    public SkinValidation? Validation
    {
        get => _validation;
        set => SetField(ref _validation, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetField(ref _busy, value);
    }

    public ICommand FetchCommand => new AsyncRelayCommand(_ => FetchAsync());

    private async Task FetchAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            Status = "请输入玩家用户名";
            return;
        }
        Busy = true;
        Status = "正在获取皮肤…";
        try
        {
            var info = await SkinFetcher.FetchByUsernameAsync(Http, Username.Trim());
            if (info?.SkinUrl is null)
            {
                Status = LocaleManager.T("msg.skin_fetch_failed");
                SkinInfo = null;
                SkinImage = null;
                Validation = null;
                return;
            }
            SkinInfo = info;
            var bytes = await SkinFetcher.DownloadImageBytesAsync(Http, info.SkinUrl);
            if (bytes is null)
            {
                Status = "皮肤图片下载失败";
                return;
            }
            Validation = SkinEditor.Validate(bytes);
            using var ms = new MemoryStream(bytes);
            SkinImage = new Bitmap(ms);
            Status = "皮肤加载完成";
        }
        catch (Exception ex)
        {
            Status = $"获取失败：{ex.Message}";
        }
        finally
        {
            Busy = false;
        }
    }
}
