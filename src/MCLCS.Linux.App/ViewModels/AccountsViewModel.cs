using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MCLCS.Core.Localization;
using MCLCS.Core.Mvvm;
using MCLCS.Core.Profiles;
using MCLCS.Core.Utils;

namespace MCLCS.Linux.App.ViewModels;

/// <summary>
/// 设置 → 账户 视图模型：列出 / 新增离线账号 / 删除（Core AccountStore 持久化到 mclcs_accounts.json）。
/// 微软 / 第三方（authlib）登录属更大的认证流程，本页先提供离线账号管理；后续接入完整登录。
/// </summary>
public class AccountsViewModel : ObservableObject
{
    private readonly string _gameRoot = GameConstants.DefaultGameRoot;

    private ObservableCollection<AccountEntry> _accounts = new();
    public ObservableCollection<AccountEntry> Accounts
    {
        get => _accounts;
        set => SetField(ref _accounts, value);
    }

    private AccountEntry? _selectedAccount;
    public AccountEntry? SelectedAccount
    {
        get => _selectedAccount;
        set => SetField(ref _selectedAccount, value);
    }

    /// <summary>新增离线账号时输入的昵称。</summary>
    private string _newUsername = "";
    public string NewUsername
    {
        get => _newUsername;
        set => SetField(ref _newUsername, value);
    }

    private string _status = LocaleManager.T("status.ready");
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public ICommand AddOfflineCommand { get; }
    public ICommand RemoveCommand { get; }

    public AccountsViewModel()
    {
        AddOfflineCommand = new RelayCommand(_ => AddOffline());
        RemoveCommand = new RelayCommand(p => RemoveAccount(p as AccountEntry));
        Load();
    }

    private void Load()
    {
        Accounts = new ObservableCollection<AccountEntry>(AccountStore.Load(_gameRoot));
        SelectedAccount = AccountStore.GetLastUsed(_gameRoot);
    }

    private void AddOffline()
    {
        var name = NewUsername.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Status = "请输入账号昵称";
            return;
        }
        var entry = new AccountEntry
        {
            DisplayName = name,
            Username = name,
            AuthType = "offline"
        };
        AccountStore.Upsert(_gameRoot, entry);
        Load();
        NewUsername = "";
        Status = $"已添加离线账号：{name}";
    }

    private void RemoveAccount(AccountEntry? acc)
    {
        if (acc is null) return;
        AccountStore.Remove(_gameRoot, acc.Id);
        Load();
        Status = $"已删除账号：{acc.DisplayName}";
    }
}
