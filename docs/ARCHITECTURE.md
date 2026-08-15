# MCLCS-Linux 架构说明（以实际代码验证为准）

> 结论基于 2026-08 对 `cnb.cool/RLRS-Studio/MCLCS-WPF` main 分支的实际编译/运行验证，
> 而非仅依据仓库内文档（内部 md 存在过时漂移）。

## 1. 上游代码验证结论

| 工程 | 目标框架 | Linux 实测 |
|---|---|---|
| `src/MCLCS.Core` | net6.0（本仓库升至 net10.0） | ✅ 编译零警告；47 项测试全过 |
| `src/MCLCS.App` | net8.0-windows + WPF | ❌ 纯 Windows（预期内，UI 层不复用） |
| `tools/MCLCS.Cli` | net8.0-windows，**引用 MCLCS.App** | ❌ 焊死 WPF，Linux 不可用 → 本仓库另建 `MCLCS.Linux.Cli` |
| `tools/MCLCS.SelfCheck` | net6.0 | ❌ 编译失败：`LauncherUpdater.CheckAsync` 调用 3 参 vs Core 实际 2 参（url 参数已移除） |
| `tests/MCLCS.Core.Tests` | net6.0 | ✅ Linux 上 47/47 通过 |

### Core 内 Windows 专属点（共 2 处，均已有运行时守卫）

1. `Launcher/JavaDetector.cs` — 注册表扫描 `AddRegistryJava`，调用点被
   `if (OperatingSystem.IsWindows())` 包裹；Linux 走 `JAVA_HOME`、`/usr/lib/jvm`、`/opt/java`、PATH。
   本仓库补充 `[SupportedOSPlatform("windows")]` 注解消除 CA1416 编译警告。
2. `Toolbox/ShortcutGenerator.cs` — `.lnk` 生成 `TryCreateLnk` 使用 WSH COM，
   调用点被 `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` 包裹；
   Linux 分支生成 `.desktop`。同样补充注解。

### Core 可复用的 UI 无关资产（Linux UI 直接绑定）

- `UI/MainTabDefinition.cs`：`MainTabs.All`（四色主标签：游戏绿/下载蓝/工具箱橙/设置灰）、
  `TabThemeConfig`（配色自定义、提亮/暗化计算）
- `UI/SidebarModel.cs`：`Sidebar.For(kind)`（下载 6 项 / 工具箱 20 项 / 设置 8 项）、
  `SidebarState`（悬停展开状态机）
- `Mvvm/ObservableObject.cs`：零依赖 MVVM 基类（替代 CommunityToolkit.Mvvm）

## 2. 本仓库架构

```
┌────────────────────────────────────────────┐
│  MCLCS.Linux.App（Avalonia 11, net10.0）   │  GUI：绑定 Core.UI 视图模型
│  MCLCS.Linux.Cli（net10.0）                │  CLI：仅引用 Core
├────────────────────────────────────────────┤
│  MCLCS.Core（vendor，源自 MCLCS-WPF）      │  引擎：启动/下载/安装/鉴权/
│                                            │  崩溃修复/模组/存档/皮肤/统计…
└────────────────────────────────────────────┘
```

原则：

1. **引擎复用**：Core 源码 vendor 进本仓库（用户决策），不引 WPF 任何代码。
2. **UI 同构**：不照抄 WPF XAML，而是绑定 Core 已有的平台无关视图模型，
   四色标签 / 侧边栏 / 状态机天然与上游规格一致（`MainTabs` / `Sidebar` 即规格）。
3. **平台分支收敛在 Core**：Linux 特有逻辑（.desktop、jvm 目录）Core 已内置，
   新增 Linux 专属能力优先下沉 Core 并以 `OperatingSystem.IsLinux()` 守卫。

## 3. 与上游同步流程（vendor 方式）

```bash
# 上游 Core 有更新时：
cd /workspace/MCLCS-WPF && git pull
rsync -av --delete --exclude bin --exclude obj src/MCLCS.Core/ /workspace/MCLCS-Linux/src/MCLCS.Core/
# 重新应用本仓库的两处 SupportedOSPlatform 注解（如冲突需手工合并）
# 构建 + 测试全绿后提交
```

## 4. CI

- `.cnb.yml`：cnb.cool 主库流水线（dotnet 10，build + test）
- `.github/workflows/ci.yml`：GitHub 镜像同套检查

## 5. 当前状态（v0.x 开发阶段）

- Core：net10.0 编译零警告，10/10 桥接测试绿（含 Linux Java 探测冒烟）
- CLI：`detect-java` 实测扫出本机 Java 20（sdkman 路径）
- GUI：骨架可编译，四色标签 + 侧边栏 + Java 面板已通
- 待办：逐页接入 Core 能力（版本下载 / 整合包 / 启动 / 工具箱 20 项 / 崩溃分析…）
