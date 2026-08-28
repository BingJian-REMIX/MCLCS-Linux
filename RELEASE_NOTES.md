# MCLCS-Linux 发行说明

> MCLCS-WPF 的 Linux 原生版本，复用同一套 `MCLCS.Core` 引擎（Avalonia 11 + .NET 10）。
> 当前为**开发阶段（dev）**，功能持续向 MCLCS-WPF 对齐。

## 本版本包含

- **图形界面（Avalonia）**：四色主标签（游戏 / 资源 / 工具箱 / 设置）、侧边栏、暗色/亮色主题、高分屏图标。
- **启动器核心**：Vanilla / Forge / Fabric / NeoForge / Quilt 安装；微软 / 离线 / authlib-injector 登录；崩溃分析与自动修复。
- **版本隔离（本阶段重点）**：
  - 每版本独立 `gameDir` / `mods` / `jar`，互不串扰；
  - 每实例独立的 `options.txt`、账户绑定、Java 参数；
  - **按游戏版本自动匹配 Java**（低版本不喂高 Java），并注入 `-Dfile.encoding=UTF-8`；
  - **版本锁定**：锁定后阻止改写版本文件（安装加载器 / 增删 Mod 等），不影响启动游戏；
  - **每版本账户绑定**：可为每个版本指定默认登录账号，切换版本自动选中。
- **文件变更检测（自动）**：启动器启动或焦点回到窗口时自动两段式扫描——
  先比元数据（大小/占用空间/mtime），无变化跳过哈希；有变化时仅对疑似文件算 SHA-256，
  剔除 mtime 抖动误报。发现新增文件右下角弹 Toast，可「查看详情」。
- **AI 助手**：单页聊天界面，头像取自所接 AI 后端的标志（网络获取）。
- **其他**：环形下载进度按钮、启动预热（light）、备份/皮肤预览等工具箱能力。

## 快速开始

### 方式一：直接运行（绿色版）
解压后直接执行：
```bash
./MCLCS.Linux.App
# 命令行版本：
./mclcs --help
```

### 方式二：安装到应用菜单
```bash
./install.sh            # 默认装到 ~/.local/share/MCLCS-Linux
# 或指定目录：
./install.sh /opt/mclcs
```

## 运行依赖

- **Linux x64**，glibc 2.35+（Ubuntu 22.04 / Fedora 36 及以上）。
- 本包为**自包含（self-contained）**，无需另行安装 .NET 运行时。
- 音频播放依赖 BASS 原生库（已随包提供 `libbass.so`）；无音频设备的环境会自动降级为「仅状态展示」。
- 首次启动会在用户目录创建配置与游戏根目录（默认 `~/.mclcs` 或按设置）。

## 已知限制（dev）

- 处于开发阶段，部分高级功能仍在对齐 WPF 中；
- 文件变更检测目前为「自动扫描 + 通知」，面板入口通过 Toast「查看详情」进入；
- 镜像下载源默认走上游配置，弱网环境建议手动指定镜像。

## 构建产物

- `MCLCS.Linux.App` —— 图形界面主程序（自包含单文件）
- `mclcs` —— 命令行工具（自包含单文件）
- `mclcs.png` / `MCLCS-Linux.desktop` / `install.sh` —— 桌面集成资源
