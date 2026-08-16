using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 常规（对齐 WPF Settings→General）：仅含语言切换（即时生效）。
/// 启动相关项（Java 路径 / JVM 参数 / 内存 / 默认用户名）已归到设置→启动（LaunchSettingsView）。
/// </summary>
public class GeneralSettingsViewModel : ObservableObject
{
    /// <summary>当前语言（zh_CN / en_US）。切换时即时写入 LocaleManager。</summary>
    public string SelectedLanguage
    {
        get => LocaleManager.CurrentLocale;
        set
        {
            var norm = LocaleManager.NormalizeLocaleCode(value);
            if (!string.Equals(LocaleManager.CurrentLocale, norm, StringComparison.OrdinalIgnoreCase))
            {
                LocaleManager.CurrentLocale = norm;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>语言下拉项：简体中文。</summary>
    public string LangChinese => LocaleManager.T("lbl.chinese");
    /// <summary>语言下拉项：English。</summary>
    public string LangEnglish => LocaleManager.T("lbl.english");
}
