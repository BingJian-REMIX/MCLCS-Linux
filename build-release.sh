#!/usr/bin/env bash
# 构建 MCLCS-Linux 发行包（自包含单文件 GUI + CLI，打包 tar.gz / zip）。
# 用法：./build-release.sh
set -euo pipefail

cd "$(dirname "$0")"

# 优先使用本地 .NET 10
if [ -x /opt/dotnet10/dotnet ]; then
    export PATH="/opt/dotnet10:$PATH"
    export DOTNET_ROOT="/opt/dotnet10"
fi

RID="linux-x64"
PUB="publish"
APP_OUT="$PUB/app"
CLI_OUT="$PUB/cli"
DIST="dist"
PKG="$DIST/MCLCS-Linux"

VER="$(cat VERSION 2>/dev/null | tr -d '[:space:]')"
if [ -z "$VER" ]; then VER="dev"; fi

echo "==> 清理旧产物"
rm -rf "$PUB" "$DIST"
mkdir -p "$APP_OUT" "$CLI_OUT" "$PKG"

echo "==> 发布 GUI（App，自包含单文件）"
dotnet publish src/MCLCS.Linux.App/MCLCS.Linux.App.csproj \
    -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$APP_OUT"

echo "==> 发布 CLI（mclcs，自包含单文件）"
dotnet publish tools/MCLCS.Linux.Cli/MCLCS.Linux.Cli.csproj \
    -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=true \
    -o "$CLI_OUT"

echo "==> 组装发行目录"
cp "$APP_OUT/MCLCS.Linux.App" "$PKG/"
cp "$CLI_OUT/mclcs"           "$PKG/"
cp dist-assets/mclcs.png          "$PKG/"
cp dist-assets/MCLCS-Linux.desktop "$PKG/"
cp dist-assets/install.sh         "$PKG/"
cp RELEASE_NOTES.md               "$PKG/README.md"
chmod +x "$PKG/MCLCS.Linux.App" "$PKG/mclcs" "$PKG/install.sh"

echo "==> 打包"
cd "$DIST"
NAME="MCLCS-Linux-$VER-$RID"
rm -f "$NAME.tar.gz" "$NAME.zip"
tar -czf "$NAME.tar.gz" MCLCS-Linux
if command -v zip >/dev/null 2>&1; then
    zip -r -q "$NAME.zip" MCLCS-Linux
fi
cd ..

echo "==> 产出"
ls -lh "$DIST/$NAME.tar.gz" "$DIST/$NAME.zip" 2>/dev/null
echo "完成。发行包位于 $DIST/"
