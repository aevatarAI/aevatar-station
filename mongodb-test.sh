#!/bin/bash

# MongoDB测试入口脚本
# 这个脚本是tools/mongodb/mongo-helper.sh的简单包装器

# 设置基础目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
HELPER_SCRIPT="$SCRIPT_DIR/tools/mongodb/mongo-helper.sh"

# 显示使用帮助
show_help() {
  echo "Aevatar MongoDB测试辅助工具"
  echo ""
  echo "配置了MongoDB测试环境并运行MongoDB相关测试。"
  echo "详细说明请查看 README.TEST.md 或 tools/mongodb/README.md"
  echo ""
  echo "使用方法: $0 <命令>"
  echo ""
  echo "可用命令:"
  echo "  start        - 启动MongoDB容器"
  echo "  stop         - 停止并移除MongoDB容器"
  echo "  status       - 检查MongoDB容器状态"
  echo "  test-conn    - 测试MongoDB连接是否正常"
  echo "  run-test     - 运行特定的MongoDB测试"
  echo "  run-all      - 运行所有MongoDB测试"
  echo "  help         - 显示此帮助信息"
  echo ""
  exit 0
}

# 如果没有参数或参数是help，显示帮助信息
if [ $# -eq 0 ] || [ "$1" = "help" ]; then
  show_help
fi

# 确保辅助脚本可执行
chmod +x "$HELPER_SCRIPT"

# 执行辅助脚本，传递所有参数
"$HELPER_SCRIPT" "$@" 