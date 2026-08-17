using System.IO;
using System.Net.Http;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Skin;
using MCLCS.Core.Toolbox;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>皮肤预览页 ViewModel（对齐 WPF SkinViewModel）：
/// 按用户名获取正版皮肤，下载位图供 3D 角色预览，暴露 Slim 标志区分 classic / slim，
/// 并额外校验皮肤尺寸（Linux 增强，保留旧版 64×32 提示）。</summary>
public class SkinViewModel : ObservableObject
{
    private static readonly HttpClient Http = new();

    private string _playerName = "";
    private string _skinUrl = "";
    private string _modelType = "classic";
    private string _statusMessage = LocaleManager.T("status.ready");
    private bool _isBusy;
    private bool _hasSkin;
    private Bitmap? _skinImage;
    private SkinInfo? _skinInfo;
    private SkinValidation? _validation;

    public string PlayerName
    {
        get => _playerName;
        set => SetField(ref _playerName, value);
    }

    public string SkinUrl
    {
        get => _skinUrl;
        set => SetField(ref _skinUrl, value);
    }

    /// <summary>是否为 slim（Alex）模型：左臂 3px 宽。</summary>
    public bool Slim => _modelType == "slim";

    public string ModelType
    {
        get => _modelType;
        set
        {
            if (SetField(ref _modelType, value))
                OnPropertyChanged(nameof(Slim));
        }
    }

    /// <summary>皮肤位图（3D 预览纹理 + 2D 缩略图共用）。</summary>
    public Bitmap? SkinImage
    {
        get => _skinImage;
        set => SetField(ref _skinImage, value);
    }

    public SkinInfo? SkinInfo
    {
        get => _skinInfo;
        set => SetField(ref _skinInfo, value);
    }

    public SkinValidation? Validation
    {
        get => _validation;
        set => SetField(ref _validation, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public bool HasSkin
    {
        get => _hasSkin;
        set => SetField(ref _hasSkin, value);
    }

    public ICommand FetchSkinCommand { get; }

    public SkinViewModel()
    {
        FetchSkinCommand = new AsyncRelayCommand(_ => FetchSkinAsync());
    }

    private async Task FetchSkinAsync()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            StatusMessage = "请输入玩家用户名";
            return;
        }
        IsBusy = true;
        HasSkin = false;
        SkinImage = null;
        SkinInfo = null;
        Validation = null;
        try
        {
            var info = await SkinFetcher.FetchByUsernameAsync(Http, PlayerName.Trim());
            if (info?.SkinUrl is null)
            {
                StatusMessage = $"未找到玩家 {PlayerName.Trim()} 的皮肤";
                return;
            }
            SkinInfo = info;
            SkinUrl = info.SkinUrl;
            ModelType = info.Model;
            var bytes = await SkinFetcher.DownloadImageBytesAsync(Http, info.SkinUrl);
            if (bytes is { Length: > 0 })
            {
                Validation = SkinEditor.Validate(bytes);
                using var ms = new MemoryStream(bytes);
                SkinImage = new Bitmap(ms);
            }
            HasSkin = true;
            StatusMessage = $"已获取 {PlayerName.Trim()} 的皮肤（{ModelType}）";
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取皮肤失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
