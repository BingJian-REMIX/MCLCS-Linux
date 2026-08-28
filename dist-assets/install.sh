#!/usr/bin/env bash
# 将 MCLCS-Linux 安装到用户目录，并创建「应用菜单」入口。
# 用法：./install.sh [目标目录]
#   默认目标目录：~/.local/share/MCLCS-Linux
set -euo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEST="${1:-$HOME/.local/share/MCLCS-Linux}"

echo "== MCLCS-Linux 安装程序 =="
echo "源目录 : $SRC"
echo "目标目录: $DEST"

mkdir -p "$DEST"

# 复制全部内容（二进制、图标、desktop、本脚本等）
cp -a "$SRC/." "$DEST/"

# 用真实绝对路径填充 .desktop 模板里的占位符
DESKTOP_TPL="$SRC/MCLCS-Linux.desktop"
DESKTOP_DST="$DEST/MCLCS-Linux.desktop"
if [ -f "$DESKTOP_TPL" ]; then
    sed "s|__INSTALL_DIR__|$DEST|g" "$DESKTOP_TPL" > "$DESKTOP_DST"
fi

# 确保可执行
chmod +x "$DEST/MCLCS.Linux.App" "$DEST/mclcs" 2>/dev/null || true

# 注册到应用菜单
APPS_DIR="$HOME/.local/share/applications"
mkdir -p "$APPS_DIR"
cp "$DESKTOP_DST" "$APPS_DIR/com.mclcs.MCLCSLinux.desktop"
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$APPS_DIR" 2>/dev/null || true
fi

echo "完成。现在可以从应用菜单启动「MCLCS-Linux」。"
echo "（如需卸载：删除 $DEST 与 $APPS_DIR/com.mclcs.MCLCSLinux.desktop 即可）"
