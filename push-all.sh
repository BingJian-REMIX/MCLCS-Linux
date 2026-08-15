#!/usr/bin/env bash
# MCLCS-Linux 双远端推送：cnb.cool 主库 + GitHub 镜像
# 用法:
#   ./push-all.sh                          # 推送当前分支到两个远端
#   ./push-all.sh --setup <cnb-url> <github-url>   # 首次配置远端地址
set -euo pipefail

CNB_URL="${CNB_URL:-https://cnb.cool/RLRS-Studio/MCLCS-Linux.git}"
GH_URL="${GH_URL:-git@github.com:BingJian-REMIX/MCLCS-Linux.git}"

if [[ "${1:-}" == "--setup" ]]; then
  shift
  [[ $# -ge 1 ]] && CNB_URL="$1"
  [[ $# -ge 2 ]] && GH_URL="$2"
  git remote remove cnb 2>/dev/null || true
  git remote remove github 2>/dev/null || true
  git remote add cnb "$CNB_URL"
  git remote add github "$GH_URL"
  echo "remotes configured:"
  git remote -v
  exit 0
fi

BRANCH="$(git branch --show-current)"
echo "==> push ${BRANCH} -> cnb (${CNB_URL})"
git push cnb "${BRANCH}"
echo "==> push ${BRANCH} -> github (${GH_URL})"
git push github "${BRANCH}"
echo "done."
