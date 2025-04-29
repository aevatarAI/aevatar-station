import requests
import time

url = "https://station-developer-staging.aevatar.ai/pressuretest-client/api/agent/publishEvent"
headers = {
    "Authorization": "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkI2QjRENTkyQzM2MUFFQ0ZCQzk3NUZBNkRDMUM5RDAzMDE0QzYzMkQiLCJ4NXQiOiJ0clRWa3NOaHJzLThsMS1tM0J5ZEF3Rk1ZeTAiLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODIvIiwiZXhwIjoxNzQ2MDY4Nzg1LCJpYXQiOjE3NDU4OTU5ODYsImF1ZCI6IkFldmF0YXIiLCJzY29wZSI6IkFldmF0YXIiLCJqdGkiOiIyMmFhYTk1My01NjMzLTRjZTItODYyMi1mYTU1N2Q0ZThmNzEiLCJzdWIiOiI4OGUwZjZkNS1kODg2LWNjNjAtZGMyMS0zYTE3NjA3NDBhMmYiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AYWJwLmlvIiwicm9sZSI6ImFkbWluIiwiZ2l2ZW5fbmFtZSI6ImFkbWluIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjoiRmFsc2UiLCJlbWFpbF92ZXJpZmllZCI6IkZhbHNlIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsInNlY3VyaXR5X3N0YW1wIjoiTE1OREhJM0NKQkJXU1JJU0JFQTZSTFlKTzVFV1BVUVoiLCJvaV9wcnN0IjoiQWV2YXRhckF1dGhTZXJ2ZXIiLCJjbGllbnRfaWQiOiJBZXZhdGFyQXV0aFNlcnZlciIsIm9pX3Rrbl9pZCI6IjBmNjc5NzEzLTlhOTQtNTQyYi05ZTJkLTNhMTk5MWFmNmMzNSJ9.UN-Fz3ZRSlXGrC6I_mDgxucDbNt5LX8gKJabyZe0YKIAkV7c3iPhYPpksoPjvrGkPgRtD4VkIuPRR9UHKVGcg8u5JGGrom4NGX4T5HwgAaU_-3pNO0T7JGPS4_OuasRNxJi5IjARJ-TcTgQ6wZSQG9OBlSPBPJKiQ93ZnT0LjhWJbo3pnAUPe3EG7kD9HFdQuDyds11Ul8783qWnUgrgN8nkS1svU113-F0UsVqousmRCyCQV_gBrb13hZ5VN5hfGqDstEb-NCmDOg8HpZiCqotWmVGqqGAFGc2KKMg6wXKmNTh4vYVM1RbB_W9OiRuyBcOeXhzw9j5kGHR0nfOUZg",
    "Content-Type": "application/json"
}

# 循环发送递增请求
for i in range(1, 3000):  # 示例发送100次
    payload = {
        "agentId": "d870f136-d6b9-440c-a30e-a8902a2b265d",
        "EventType": "MineAiFun.GAgents.GAgents.Common.GEvents.CreateUserGAgentGEvent",
        "eventProperties": {
            "PkAddress": f"pk{i:04d}",  # 生成4位补零序号，如pk0001
            "Name": f"pk{i:03d}",
            "ThirdAddress": "third",
            "Color": "Red",
            "TwitterName": "pk001",
            "AgentType": 0,
            "AgentModel": 0,
            "LogoUrl": "/assets/static/adopt-agent-image-demo.gKnwFA1U.png"
        }
    }

    try:
        response = requests.post(url, json=payload, headers=headers)
        response.raise_for_status()  # 自动处理错误状态码
        print(f"成功发送: PK{i} 状态码: {response.content}")
    except requests.exceptions.RequestException as e:
        print(f"请求失败: {e}")

    time.sleep(0.1)  # 添加间隔防止服务器过载