#!/bin/bash

# MongoDB测试辅助脚本

# 设置基础目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." &>/dev/null && pwd)"

# 命令用法
usage() {
  echo "MongoDB测试辅助脚本"
  echo ""
  echo "用法: $0 <命令>"
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
  exit 1
}

# 检查Docker是否运行
check_docker() {
  if ! docker info > /dev/null 2>&1; then
    echo "错误: Docker未运行，请先启动Docker Desktop"
    exit 1
  fi
}

# 启动MongoDB容器
start_mongodb() {
  check_docker
  
  if docker ps | grep aevatar-mongodb > /dev/null; then
    echo "MongoDB容器已经在运行..."
    return 0
  fi
  
  echo "启动MongoDB容器..."
  cd "$PROJECT_ROOT" && docker-compose up -d
  
  echo "等待MongoDB初始化完成..."
  for i in {1..20}; do
    echo -n "."
    sleep 1
    if docker ps | grep "aevatar-mongodb" | grep "(healthy)" > /dev/null; then
      echo -e "\nMongoDB容器已就绪！"
      return 0
    fi
  done
  
  echo -e "\n警告: MongoDB容器启动可能需要更长时间，请使用 '$0 status' 检查状态"
}

# 停止MongoDB容器
stop_mongodb() {
  check_docker
  
  echo "停止并移除MongoDB容器..."
  cd "$PROJECT_ROOT" && docker-compose down -v
  
  echo "清理完成！"
}

# 检查MongoDB状态
check_status() {
  check_docker
  
  echo "MongoDB容器状态:"
  docker ps --filter "name=aevatar-mongodb" --format "表格 {{.ID}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}"
  
  if docker ps | grep "aevatar-mongodb" > /dev/null; then
    echo -e "\n容器日志（最近10行）:"
    docker logs aevatar-mongodb --tail 10
  else
    echo -e "\nMongoDB容器未运行"
  fi
}

# 测试MongoDB连接
test_connection() {
  check_docker
  
  if ! docker ps | grep aevatar-mongodb > /dev/null; then
    echo "MongoDB容器未运行，正在启动..."
    start_mongodb
  fi
  
  echo "创建测试数据库和集合..."
  docker exec -it aevatar-mongodb mongosh -u admin -p admin --authenticationDatabase admin --eval '
    db = db.getSiblingDB("Aevatar");
    db.createCollection("TestCollection");
    db.TestCollection.insertOne({
      name: "测试文档",
      description: "这是一个测试文档，用于验证MongoDB连接",
      createdAt: new Date()
    });
    db.TestCollection.find().pretty();
  '
  
  echo -e "\n验证MongoDB连接字符串有效性..."
  docker exec -it aevatar-mongodb mongosh "mongodb://admin:admin@localhost:27017/Aevatar?authSource=admin" --eval '
    db.stats();
    db.TestCollection.find().pretty();
  '
  
  echo -e "\nMongoDB连接测试完成！"
}

# 设置环境变量，启用MongoDB测试
set_mongodb_env() {
  export DOTNET_ENVIRONMENT=Testing
  export USE_MONGODB=true
}

# 运行特定的MongoDB测试
run_specific_test() {
  check_docker
  
  if ! docker ps | grep aevatar-mongodb > /dev/null; then
    echo "MongoDB容器未运行，正在启动..."
    start_mongodb
  fi
  
  echo "运行指定的MongoDB测试..."
  cd "$PROJECT_ROOT" && set_mongodb_env && dotnet test test/Aevatar.MongoDB.Tests/bin/Debug/net9.0/Aevatar.MongoDB.Tests.dll --filter "FullyQualifiedName~Aevatar.MongoDb.Applications.Account.MongoDBAccountServiceTests"
  
  echo "测试完成！"
}

# 运行所有MongoDB测试
run_all_tests() {
  check_docker
  
  if ! docker ps | grep aevatar-mongodb > /dev/null; then
    echo "MongoDB容器未运行，正在启动..."
    start_mongodb
  fi
  
  echo "运行所有MongoDB测试..."
  cd "$PROJECT_ROOT" && set_mongodb_env && dotnet test test/Aevatar.MongoDB.Tests/bin/Debug/net9.0/Aevatar.MongoDB.Tests.dll
  
  echo "测试完成！"
}

# 主函数
main() {
  if [ $# -eq 0 ]; then
    usage
  fi
  
  case "$1" in
    start)
      start_mongodb
      ;;
    stop)
      stop_mongodb
      ;;
    status)
      check_status
      ;;
    test-conn)
      test_connection
      ;;
    run-test)
      run_specific_test
      ;;
    run-all)
      run_all_tests
      ;;
    help)
      usage
      ;;
    *)
      echo "未知命令: $1"
      usage
      ;;
  esac
}

# 执行主函数
main "$@" 