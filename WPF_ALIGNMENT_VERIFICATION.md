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
| Game | 0 | 游戏页**无侧栏**，默认展示主页 `HomeView` 全宽内容 |
| Download | 7 | 含 `versionlist`(版本列表) |
| Toolbox | 21 | 20 原工具 + `devtools` 开发工具聚合入口 |
| Settings | 8 | 通用/启动/下载/推荐/账号/AI/外观/关于 |

> 已移除工具箱页的 `toolbox`(聚合总览) / `achievement`(成就) / `annual`(年度报告) 常驻入口；其中 `AchievementView`、`ToolboxView` 已删除，`AnnualReportView` 保留供主页周年日入口跳转。

导航路由在 `src/MCLCS.Linux.App/MainWindow.axaml.cs` 的 `ShowPage()`（`(_vm.SelectedTab.Kind, _vm.SelectedSidebarId) switch`）。

---

## 四、本地化补充

- `src/MCLCS.Core/Localization/LocaleManager.cs`（zh + en）：早期新增 `tool.versionlist/versionlist.desc`、`tool.devtools/devtools.desc`；`tool.home`/`tool.toolbox`/`tool.achievement`/`tool.annual`/`game.launch` 等已随对应入口移除。
- `src/MCLCS.Linux.App/Localization.cs`：`DescKeyMap` 现仅映射仍存于侧栏的 id（`devtools`/`versionlist` 等）。

---

## 五、测试

- 测试项目：`tests/MCLCS.Linux.Tests`
- 后续更新的断言：`Sidebar_Toolbox_HasItems_WithGroups`(21 项，且不含 toolbox/achievement/annual)、`Sidebar_Game_HasNoSidebar`(0 项)、`SidebarState_SwitchOwner_SelectsFirstItem`(Game 选 null)、`MainViewModel_SelectedTab_联动_SidebarItems`(Game 为空)。
- **结果：35/35 全部通过，无回归。**

---

## 六、本轮补齐（2026-08-23）：CrashView「尝试修复 / 降级恢复」缺口

此前 `CrashView` 仅能做崩溃分析（分类 / 原因 / 建议），**缺少「尝试修复」与「降级联动恢复」动作**。本轮补齐：

- `CrashRepairModels.ModConflictInfo`：新增 `IsKeepSelected`（继承 `ObservableObject`），支持"保留哪个 Mod"单选。
- `LauncherService.ApplyRepairAsync`（此前 Linux 完全未实现，仅注释占位）：移植 WPF 实现，接好 `JavaInstaller` / `LibraryRepair` / `ResourcePackRepairer` / `SaveDowngrader` / `ModrinthClient` 等全部底层能力；含冲突 Mod 禁用、缺失前置安装、内存 / Java 切换、库重下、资源包重置、§四.2 降级联动恢复（回滚备份 / 改用他法 / 安装原版本）。
- `CrashViewModel`：选中文档即 `CrashRepairEngine.BuildPlan` 生成方案，暴露 `CanRepair` / 修复面板属性 / `TryRepairCommand` / `DowngradeRecoveryCommand`。
- `CrashView.axaml`：新增「自动修复方案」面板（标题 / 说明 / 步骤 / 冲突 Mod 单选 / 缺失前置列表 / 非破坏性提示 + "尝试自动修复"）与「降级联动恢复」面板（3 个恢复按钮）。

**验证**：新增条件编译截图工具 `src/MCLCS.Linux.App/Screenshot/ScreenshotCapture.cs`（`-p:DefineConstants=SCREENSHOT`），Xvfb 下遍历四个主标签全部侧栏页自动渲染 PNG，共 **41 页全截图**（见 `/workspace/screenshots/`），每页颜色数 1535–4235，确认无空白页。正常（无 `SCREENSHOT`）构建与测试不受影响，仍 **35/35 通过**。

---

## 六之二、本轮 UI 修正（2026-08-23 第二轮）

按用户反馈修正 6 项导航 / UI 表现问题：

| 问题 | 修正 |
|------|------|
| 四色索引贴「选中页置顶」 | `TabItemViewModel.ZIndex` 不再因选中而置顶，改为严格按 `Order` 决定层叠（对齐 WPF `MainTabDefinition.ZIndex`）；`IsExpanded` 改为 `IsSelected \|\| Def.AlwaysExpanded`。游戏页 `AlwaysExpanded` 设为 false，未选中时折叠为色条且不显示「游戏」标题，与选中页一致；`TabMarginConverter` 改为在 VM 内计算 `Margin`，左邻展开时 -10 / 折叠时 -20，实现「重叠 + 展开动画」而非置顶。 |
| 主页无侧边栏 | `Sidebar.Game` 置空（0 项），`HasSidebar=false`；`MainWindow.ShowPage` 的 `Game` 路由默认展示 `HomeView` 全宽内容。`HasSidebar` 为 false 时页头标题/分组/描述自动隐藏。 |
| 工具箱页入口精简 | 从 `Sidebar.Toolbox` 移除 `toolbox`(聚合总览) / `achievement`(成就) / `annual`(年度报告) 三项；删除 `ToolboxView` + `AchievementView`（含 VM），保留 `AnnualReportView` 供周年入口跳转。Toolbox 现 21 项。 |
| 年度报告仅在周年日展示 | `PlayStats` 新增 `FirstLaunchUtc`（首次启动游戏时记录）；`HomeViewModel.IsAnnualReportVisible` 仅当「今日 == 首次启动月/日」为真时在主页显示「年度报告」入口卡片，点击跳转 `AnnualReportView`。其余日不展示。 |
| 皮肤编辑器按钮显示不完整 | `SkinEditorView` 中 2D 编辑 / 3D 预览 `ToggleButton` 尺寸由 64×24 / 字号 11 调整为 78×28 / 字号 12，避免文字截断。 |
| 皮肤编辑器字体方框（tofu） | 将页面内 emoji（🧍 / 🖱️ / 🔲）替换为纯文本提示（如「左键：填充 / 清除」「右键：取色」），消除字体不支持导致的方框。 |

**验证**：构建 Build succeeded（0 error）；测试 35/35 通过；Xvfb 全截图更新为 **37 页**（`/workspace/screenshots/`，含 `shot_Game_default.png` 主页无侧栏页）。

---

## 七、已知剩余缺口（后续可补）

| 位置 | 缺口 | 备注 |
|------|------|------|
| 对齐报告其余项 | 待全面复核 | 建议在功能冻结前跑一次全量对齐 diff |

---

## 八、提交与推送

- 所有改动**双推** `cnb` + `github`（同名仓库，`main` 分支）。
- 构建验证：`dotnet build .../MCLCS.Linux.App.csproj -c Debug` → Build succeeded（4 warning，0 error）。
