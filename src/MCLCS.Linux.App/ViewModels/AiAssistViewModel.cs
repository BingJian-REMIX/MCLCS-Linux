using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MCLCS.Core.Ai;
using MCLCS.Core.Launcher;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Statistics;
using MCLCS.Linux.App;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>聊天消息：role 为 user / assistant。</summary>
public class ChatMessage : ObservableObject
{
    public string Role { get; }
    public string Content { get; }
    public bool IsUser => Role == "user";

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

/// <summary>AI 助手面板（工具箱 aichat）：单页聊天界面。
/// 自由输入走 Assistant.ChatAsync；另保留崩溃解读 / Mod 翻译 / 配装推荐 / 年度总结 快捷操作，避免功能回退。</summary>
public class AiAssistViewModel : ObservableObject
{
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private string _inputText = "";
    public string InputText { get => _inputText; set => SetField(ref _inputText, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public bool AiEnabled => Assistant.Config.Enabled;

    /// <summary>① 品牌官方商标（拉取成功时显示，覆盖首字徽章）。</summary>
    private Bitmap? _assistantLogo;
    public Bitmap? AssistantLogo
    {
        get => _assistantLogo;
        private set
        {
            if (SetField(ref _assistantLogo, value)) UpdateAvatarStates();
        }
    }

    private bool _hasLogo;
    public bool HasLogo
    {
        get => _hasLogo;
        private set
        {
            if (SetField(ref _hasLogo, value)) UpdateAvatarStates();
        }
    }

    /// <summary>② 品牌首字徽章（商标拉取失败时兜底）：首字 + 品牌色。</summary>
    private string _assistantInitial = "AI";
    public string AssistantInitial
    {
        get => _assistantInitial;
        private set => SetField(ref _assistantInitial, value);
    }

    private IBrush _assistantBrandBrush = Brushes.Gray;
    public IBrush AssistantBrandBrush
    {
        get => _assistantBrandBrush;
        private set => SetField(ref _assistantBrandBrush, value);
    }

    /// <summary>是否已推断出品牌（决定徽章是否可用）。</summary>
    private bool _hasBrand;
    public bool HasBrand
    {
        get => _hasBrand;
        private set
        {
            if (SetField(ref _hasBrand, value)) UpdateAvatarStates();
        }
    }

    /// <summary>② 首字徽章可见：仅在「识别出品牌但商标没拉到」时显示。
    /// 注：WPF 端把徽章直接绑 HasBrand，导致商标加载成功后仍被不透明徽章盖住；此处按文档语义修正。</summary>
    private bool _showBrandBadge;
    public bool ShowBrandBadge
    {
        get => _showBrandBadge;
        private set => SetField(ref _showBrandBadge, value);
    }

    /// <summary>③ 机器人兜底：仅当 AI 未启用/未配置（品牌与商标都拿不到）时显示。</summary>
    private bool _showRobotFallback = true;
    public bool ShowRobotFallback
    {
        get => _showRobotFallback;
        private set => SetField(ref _showRobotFallback, value);
    }

    private void UpdateAvatarStates()
    {
        ShowBrandBadge = HasBrand && !HasLogo;
        ShowRobotFallback = !HasLogo && !HasBrand;
    }

    public ICommand SendCommand => new AsyncRelayCommand(_ => SendAsync(), _ => !IsBusy);
    public ICommand CrashCommand => new AsyncRelayCommand(_ => CrashAnalyzeAsync(), _ => !IsBusy);
    public ICommand TranslateCommand => new AsyncRelayCommand(_ => TranslateAsync(), _ => !IsBusy);
    public ICommand RecommendCommand => new AsyncRelayCommand(_ => RecommendAsync(), _ => !IsBusy);
    public ICommand SummaryCommand => new AsyncRelayCommand(_ => SummaryAsync(), _ => !IsBusy);

    public AiAssistViewModel()
    {
        // 设计稿问候语（首条助手气泡）
        Messages.Add(new ChatMessage("assistant",
            "你好！我是 MCLCS AI 助手。可直接输入问题，支持崩溃分析、Mod 推荐、翻译等。"));
        ResolveBrand();                  // 同步推断品牌：设置首字 / 品牌色徽章
        _ = LoadAssistantLogoAsync();    // 异步拉官方商标，成功则覆盖徽章，失败保留徽章
    }

    // ---- 助手头像：① 品牌官方商标 → ② 国内 iowen 聚合 → ③ 首字徽章 → ④ 机器人 ----
    private async Task LoadAssistantLogoAsync()
    {
        // 拉取候选：优先品牌官方图标（国内可直连），失败回退国内 iowen 聚合服务；
        // 仍失败则保留同步算出的品牌首字徽章（HasBrand 兜底）。
        var candidates = new List<string>(2);
        if (!string.IsNullOrEmpty(_brandLogoUrl)) candidates.Add(_brandLogoUrl!);
        if (!string.IsNullOrEmpty(_brandDomain)) candidates.Add($"https://api.iowen.cn/favicon/{_brandDomain}.png");
        if (candidates.Count == 0) return;

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) MCLCS");

        foreach (var url in candidates)
        {
            try
            {
                var data = await ReadCacheOrDownloadAsync(client, url);
                if (data is null || data.Length == 0) continue;

                // Avalonia 在构造时即完整解码，using 流安全。
                // 解码失败（如地区限制返回 HTML 页面）会抛异常 → 不写缓存，继续尝试下一个候选。
                Bitmap bmp;
                using (var ms = new MemoryStream(data)) bmp = new Bitmap(ms);
                await WriteCacheAsync(url, data);   // 仅解码成功才落盘，避免 HTML 污染缓存
                AssistantLogo = bmp;
                HasLogo = true;
                return; // 任一来源成功即用
            }
            catch
            {
                // 该来源失败，尝试下一个候选
            }
        }
        // 全部失败：保持品牌首字徽章兜底
    }

    private static string CacheDir => Path.Combine(Path.GetTempPath(), "MCLCS");

    /// <summary>缓存文件名：URL 的 SHA256 前 16 位，避免每进一次页面重复联网。</summary>
    private static string CacheFileFor(string url) =>
        Path.Combine(CacheDir,
            "logo_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16] + ".png");

    private static async Task<byte[]?> ReadCacheOrDownloadAsync(HttpClient client, string url)
    {
        var cacheFile = CacheFileFor(url);
        if (File.Exists(cacheFile))
        {
            try { return await File.ReadAllBytesAsync(cacheFile); }
            catch { /* 缓存损坏则重新下载 */ }
        }
        return await client.GetByteArrayAsync(url);
    }

    private static async Task WriteCacheAsync(string url, byte[] data)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            await File.WriteAllBytesAsync(CacheFileFor(url), data);
        }
        catch { /* 缓存写入失败忽略 */ }
    }

    /// <summary>回退用的可注册域名（供 iowen 聚合服务拉取）。</summary>
    private string? _brandDomain;

    /// <summary>品牌官方图标 URL（尽量 .ico/.png，避开 .svg；为 null 时仅回退 iowen）。</summary>
    private string? _brandLogoUrl;

    /// <summary>清空品牌状态（未启用 / 未配置 / 解析异常时），避免残留上一次的域名与首字。</summary>
    private void ClearBrand()
    {
        _brandDomain = null;
        _brandLogoUrl = null;
        AssistantInitial = "AI";
        HasBrand = false;
    }

    /// <summary>同步推断当前部署品牌：设置首字、品牌色、官方图标 URL 与回退域名；未配置则保持机器人兜底。</summary>
    private void ResolveBrand()
    {
        try
        {
            if (Assistant.Config is null || !Assistant.Config.Enabled)
            {
                ClearBrand();
                return;
            }

            if (Assistant.Config.Mode == AiMode.Local)
            {
                _brandDomain = "ollama.com";
                // 实测 ollama.com/favicon.ico 返回 404，真实图标为 /public/icon-*.png
                _brandLogoUrl = "https://ollama.com/public/icon-64x64.png";
                AssistantInitial = "O";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                HasBrand = true;
                return;
            }

            var ep = Assistant.Config.Endpoint ?? "";
            if (string.IsNullOrWhiteSpace(ep))
            {
                ClearBrand();
                return;
            }

            string host;
            try { host = new Uri(ep).Host; }
            catch { ClearBrand(); return; }
            if (string.IsNullOrWhiteSpace(host))
            {
                ClearBrand();
                return;
            }

            var h = host.ToLowerInvariant();

            if (h.Contains("openai.com") || h.Contains("api.openai.com"))
            {
                _brandDomain = "openai.com";
                _brandLogoUrl = "https://openai.com/favicon.ico";
                AssistantInitial = "O";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(16, 163, 127));
                HasBrand = true;
                return;
            }
            if (h.Contains("deepseek.com"))
            {
                _brandDomain = "deepseek.com";
                _brandLogoUrl = "https://www.deepseek.com/favicon.ico";
                AssistantInitial = "D";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(76, 154, 255));
                HasBrand = true;
                return;
            }
            if (h.Contains("anthropic.com"))
            {
                _brandDomain = "anthropic.com";
                // claude.ai 对部分地区返回 302 → /app-unavailable-in-region（HTML，非图片），
                // 解码会失败并自动回退 iowen；此处保留官方地址，国内用户将走回退分支。
                _brandLogoUrl = "https://claude.ai/images/claude_app_icon.png";
                AssistantInitial = "A";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(207, 90, 85));
                HasBrand = true;
                return;
            }
            if (h.Contains("moonshot.cn"))
            {
                _brandDomain = "moonshot.cn";
                _brandLogoUrl = "https://statics.moonshot.cn/kimi-web-seo/favicon.ico";
                AssistantInitial = "K";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(255, 102, 0));
                HasBrand = true;
                return;
            }
            if (h.Contains("aliyun.com") || h.Contains("dashscope"))
            {
                _brandDomain = "aliyun.com";
                _brandLogoUrl = "https://g.alicdn.com/qwenweb/qwen-ai-fe/0.0.4/favicon.ico";
                AssistantInitial = "通";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(109, 40, 217));
                HasBrand = true;
                return;
            }
            if (h.Contains("mistral.ai"))
            {
                _brandDomain = "mistral.ai";
                _brandLogoUrl = "https://mistral.ai/favicon.ico";
                AssistantInitial = "M";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(255, 0, 106));
                HasBrand = true;
                return;
            }
            if (h.Contains("groq.com"))
            {
                _brandDomain = "groq.com";
                _brandLogoUrl = "https://groq.com/favicon.ico";
                AssistantInitial = "G";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(250, 0, 80));
                HasBrand = true;
                return;
            }
            if (h.Contains("googleapis.com"))
            {
                _brandDomain = "google.com";
                _brandLogoUrl = null; // Gemini 官方图标在海外 gstatic，国内不稳，仅走 iowen 回退
                AssistantInitial = "G";
                AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244));
                HasBrand = true;
                return;
            }

            _brandDomain = GetRegistrableDomain(host);
            _brandLogoUrl = null; // 未知品牌：仅走 iowen 回退
            AssistantInitial = char.ToUpperInvariant(host[0]).ToString();
            AssistantBrandBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            HasBrand = true;
        }
        catch
        {
            ClearBrand();
        }
    }

    /// <summary>简化版注册域名提取（无额外依赖；未知品牌取二级域名，常见二级公共后缀单独处理）。</summary>
    private static string GetRegistrableDomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length <= 2) return host;
        var lastTwo = parts[^2] + "." + parts[^1];
        var twoLevelTlds = new[] { "co.uk", "com.cn", "org.cn", "net.cn", "com.au", "co.jp" };
        return Array.Exists(twoLevelTlds, t => t == lastTwo)
            ? parts[^3] + "." + lastTwo
            : lastTwo;
    }

    // ---- 自由对话 ----
    private async Task SendAsync()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputText = "";
        Messages.Add(new ChatMessage("user", text));
        IsBusy = true;
        try
        {
            var reply = await Assistant.ChatAsync(text);
            Messages.Add(new ChatMessage("assistant", reply));
        }
        finally { IsBusy = false; }
    }

    // ---- 快捷操作：崩溃分析 ----
    private async Task CrashAnalyzeAsync()
    {
        IsBusy = true;
        try
        {
            var root = Services.LauncherService.Instance.GameRoot;
            var latest = CrashDetector.FindLatestCrashReport(root);
            if (latest is null)
            {
                Messages.Add(new ChatMessage("user", "帮我分析上次崩溃"));
                Messages.Add(new ChatMessage("assistant",
                    "未找到崩溃报告文件（crash-reports 目录为空）。如有日志，可直接粘贴到下方输入框，我会帮你分析。"));
                return;
            }
            Messages.Add(new ChatMessage("user", $"帮我分析上次崩溃（{Path.GetFileName(latest)}）"));
            var result = await Assistant.InterpretCrashAsync(File.ReadAllText(latest));
            Messages.Add(new ChatMessage("assistant", result));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("assistant", $"分析失败：{ex.Message}"));
        }
        finally { IsBusy = false; }
    }

    // ---- 快捷操作：Mod 描述翻译 ----
    private async Task TranslateAsync()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusMessage = "请在输入框粘贴 Mod 描述后点击「Mod 翻译」";
            return;
        }
        Messages.Add(new ChatMessage("user", $"请翻译这段 Mod 描述：\n{text}"));
        InputText = "";
        IsBusy = true;
        try
        {
            var r = await Assistant.TranslateModDescriptionAsync(text);
            Messages.Add(new ChatMessage("assistant", r));
        }
        finally { IsBusy = false; }
    }

    // ---- 快捷操作：配装推荐 ----
    private async Task RecommendAsync()
    {
        var pref = InputText?.Trim();
        if (string.IsNullOrEmpty(pref))
        {
            StatusMessage = "请在输入框描述你的玩法偏好后点击「配装推荐」";
            return;
        }
        Messages.Add(new ChatMessage("user", $"帮我推荐适合的 Mod：{pref}"));
        InputText = "";
        IsBusy = true;
        try
        {
            var r = Assistant.Config.Enabled
                ? await Assistant.InterpretCrashAsync($"请根据以下偏好推荐5个Minecraft Mod（仅列名称和简要理由）：{pref}")
                : "AI 未启用，请在「设置 → AI 助手」中开启后使用此功能。";
            Messages.Add(new ChatMessage("assistant", r));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("assistant", $"推荐失败：{ex.Message}"));
        }
        finally { IsBusy = false; }
    }

    // ---- 快捷操作：年度总结 ----
    private async Task SummaryAsync()
    {
        Messages.Add(new ChatMessage("user", "生成我的年度总结"));
        IsBusy = true;
        try
        {
            if (!AiEnabled)
            {
                Messages.Add(new ChatMessage("assistant", "AI 未启用，请在「设置 → AI 助手」中开启后使用此功能。"));
                return;
            }
            var data = AnnualReport.GenerateFrom(Services.LauncherService.Instance.GameRoot, DateTime.Now.Year);
            var md = data.HasData ? AnnualReport.RenderMarkdown(data) : "今年还没有游玩记录。";
            var r = await Assistant.InterpretCrashAsync($"请将以下年度游戏报告总结成一段100字以内的话：\n{md}");
            Messages.Add(new ChatMessage("assistant", r));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("assistant", $"生成失败：{ex.Message}"));
        }
        finally { IsBusy = false; }
    }
}
