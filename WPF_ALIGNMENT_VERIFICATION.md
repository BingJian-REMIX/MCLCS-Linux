# MCLCS-Linux ↔ MCLCS-WPF 功能对齐验证

> 目标：Linux 启动器（Avalonia / .NET 10）功能同步 cnb 标准 **MCLCS-WPF**。
> 实现方式由开发方确定，当前为开发阶段。本文档跟踪"WPF 有、Linux 缺失/补齐"的页面级缺口。
> 构建规则：必须使用 `/opt/dotnet10` SDK（`export PATH="/opt/dotnet10:$PATH" && export DOTNET_ROOT=/opt/dotnet10`），
> 系统默认 `/usr/share/dotnet` 为 6.0，会触发 `NETSDK1045`。

---

## 一、本轮补齐（2026-08-21 提交）：6 个完全缺失功能页

此前因 429 中断，以下页面在 Linux 侧**完全缺失**（无 VM / 无 View / 无导航注册）。本轮一次性补齐：

| 页面 | VM | View | 侧栏入口 | 复用 Core/App 能力 | 状态 |
|------|----|------|----------|--------------------|------|
| 主页 Home | `HomeViewModel` | `HomeView` | `Game` → `home` | `GameLauncher` / `PlaytimeTracker` / `RecommendationEngine` | ✅ 已补齐 |
| 版本列表 VersionList | `VersionListViewModel` | `VersionListView` | `Download` → `versionlist` | `PathEx.VersionsDir` / `VersionJson` / `GameLauncher` | ✅ 已补齐 |
| 工具箱总览 Toolbox | `ToolboxViewModel` | `ToolboxView` | `Toolbox` → `toolbox` | 遍历 `Sidebar.Toolbox` 生成卡片网格 | ✅ 已补齐 |
| 成就 Achievement | `AchievementViewModel` | `AchievementView` | `Toolbox` → `achievement` | 读各存档 `advancements/*.json` 统计 | ✅ 已补齐 |
| 年度报告 AnnualReport | `AnnualReportViewModel` | `AnnualReportView` | `Toolbox` → `annual` | `Core.Statistics.AnnualReport`（`GenerateFrom`/`ExportToken`/`ImportToken`/`RenderMarkdown`） | ✅ 已补齐 |
| 开发工具 DevTools | `DevToolsViewModel` | `DevToolsView` | `Toolbox` → `devtools` | 33 条命令速查表 + `UIService.PickFolderAsync` 生成 mod/资源包骨架 | ✅ 已补齐 |

**设计决策**
- `ToolboxView` 做成"工具总览卡片网格"（点击跳转到已有的独立工具页），而非 WPF 的内嵌 21 子视图聚合 —— 更契合 Linux 已有的独立工具页架构。
- `Game` 主标签从"无侧栏"改为含**启动(`play`) / 主页(`home`)**两项；原 `launch` 因与 `Settings` 的 `launch` 在 `DescKeyMap` 键冲突，改名为 `play`。
- 本 Avalonia 版本**不支持** `Grid.ColumnSpacing/RowSpacing`，间距统一改用子元素 `Margin` 实现。

---

## 二、上轮补齐（提交 `52bd8a3`）：SavesView 4 个缺失命令

| 命令 | 实现 |
|------|------|
| 备份 Backup | `BackupManager.Create` + `SaveCompatibilityDetector.FindBackups` |
| 删除 Delete | 删除存档目录（带模态确认） |
| 恢复 Restore | `SaveDowngrader.RestoreBackupAsync` |
| 提取种子 ExtractSeed | 解析 `level.dat`（`NbtFile.ReadGzip`）提取 Seed，结果复制到剪贴板 |

---

## 三、侧栏结构（注册于 `src/MCLCS.Core/UI/SidebarModel.cs`）

| 主标签 | 项数 | 说明 |
|--------|------|------|
| Game | 2 | `play`(启动) / `home`(主页) |
| Download | 7 | 含 `versionlist`(版本列表) |
| Toolbox | 24 | 20 原工具 + `toolbox`/`devtools`/`achievement`/`annual` 4 入口 |
| Settings | 8 | 通用/启动/下载/推荐/账号/AI/外观/关于 |

导航路由在 `src/MCLCS.Linux.App/MainWindow.axaml.cs` 的 `ShowPage()`（`(_vm.SelectedTab.Kind, _vm.SelectedSidebarId) switch`）。

---

## 四、本地化补充

- `src/MCLCS.Core/Localization/LocaleManager.cs`（zh + en）：新增 `tool.home/home.desc`、`tool.versionlist/versionlist.desc`、`tool.toolbox/toolbox.desc`、`tool.devtools/devtools.desc`、`tool.achievement/achievement.desc`、`tool.annual/annual.desc`、`game.launch/launch.desc`。
- `src/MCLCS.Linux.App/Localization.cs`：`DescKeyMap` 新增上述 7 个 id 的 desc 键映射，避免未登记导致测试失败（`Localization.ToolDescription` 未登记会降级为"待接入 Core 能力"）。

---

## 五、测试

- 测试项目：`tests/MCLCS.Linux.Tests`
- 本轮更新断言：`Sidebar_Toolbox_HasItems_WithGroups`(24 项)、`Sidebar_Game_HasLaunchAndHome`(2 项含 play/home)、`SidebarState_SwitchOwner_SelectsFirstItem`(Game 选 play)、`MainViewModel_SelectedTab_联动_SidebarItems`(Game 不再 Empty)。
- **结果：35/35 全部通过，无回归。**

---

## 六、已知剩余缺口（后续可补）

| 位置 | 缺口 | 备注 |
|------|------|------|
| `CrashView` | "尝试修复 / 降级恢复" 真缺口 | 上一轮对齐报告标记的保留项，建议后续按 WPF 实现补全 |
| 对齐报告其余项 | 待全面复核 | 建议在功能冻结前跑一次全量对齐 diff |

---

## 七、提交与推送

- 所有改动**双推** `cnb` + `github`（同名仓库，`main` 分支）。
- 构建验证：`dotnet build .../MCLCS.Linux.App.csproj -c Debug` → Build succeeded（4 warning，0 error）。
