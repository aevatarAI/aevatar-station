import sys

try:
    # 检查是否传递了正确数量的参数
    if len(sys.argv) < 2:
        print("错误：缺少参数！请提供 'succ' 或 'fail' 作为参数。")
        sys.exit(1)

    # 提取第一个参数
    command = sys.argv[1].lower()  # 转为小写以避免大小写问题

    # 根据参数值执行相应逻辑
    if command == "succ":
        print("执行成功！")
        sys.exit(0)  # 成功返回状态码 0
    elif command == "fail":
        print("执行失败！")
        sys.exit(1)  # 失败返回状态码 1
    else:
        print("错误：无效参数！请使用 'succ' 或 'fail'。")
        sys.exit(1)

except Exception as e:
    print(f"发生错误: {e}")
    sys.exit(1)