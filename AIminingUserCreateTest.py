import requests
import time

url = "https://station-developer-staging.aevatar.ai/pressuretest-client/api/agent/publishEvent"
headers = {
    "Authorization": "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkFGNDJDN0IxNTNEMzU2QkRDMEU0ODYyQjAzNkY5RjgwQjRGMDVBQUMiLCJ4NXQiOiJyMExIc1ZQVFZyM0E1SVlyQTItZmdMVHdXcXciLCJ0eXAiOiJhdCtqd3QifQ.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjgwODIvIiwiZXhwIjoxNzQ0OTcwMjQ3LCJpYXQiOjE3NDQ3OTc0NDgsImF1ZCI6IkFldmF0YXIiLCJzY29wZSI6IkFldmF0YXIiLCJqdGkiOiIzOGQyMzU1OS05Y2I3LTQ3YTgtODkyMC04Y2ZlNzVhYzAxMzEiLCJzdWIiOiI4OGUwZjZkNS1kODg2LWNjNjAtZGMyMS0zYTE3NjA3NDBhMmYiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AYWJwLmlvIiwicm9sZSI6ImFkbWluIiwiZ2l2ZW5fbmFtZSI6ImFkbWluIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjoiRmFsc2UiLCJlbWFpbF92ZXJpZmllZCI6IkZhbHNlIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsInNlY3VyaXR5X3N0YW1wIjoiN1ZWV05OQUNOR0dBQkxWTE1UQlVGQ1JLSFBRM0pMVjciLCJvaV9wcnN0IjoiQWV2YXRhckF1dGhTZXJ2ZXIiLCJjbGllbnRfaWQiOiJBZXZhdGFyQXV0aFNlcnZlciIsIm9pX3Rrbl9pZCI6IjZkMmJiYzIxLTdjMjctMzI3OS0wY2JhLTNhMTk1MDM1MTIwYSJ9.GnaDjlabzfu_CrlINt1xSXAsRGCbbR2Gw0whScVhqD5Zd6iLPyyNkKIW4Xu0Ss2F7XcPLq7UCGUeuAokuXesEfNogHiMztn5V6Cukszbchry6LXN-zCehvETBvZahOnFHk6TrzJzxGPC7v52zIV7_zgjDvE62_HZEvv9AUTDKFZ-arhCTfFYfoo7zk2EpckevwoyO-M469b3iit-4n0yvMln29DxePX9i8ArqPJf0Kxw5mEQ6B1sN94ZS75NZNzQhrMGIkavx4edjs5MIlViuRlJ4v6aoo0k6R-PIh63gv3yAcMVd1T76aoBBD5SGFbd8_4L2XeGh1b8OSrqYG_o8A",
    "Content-Type": "application/json"
}

# 循环发送递增请求
for i in range(600, 1200):  # 示例发送100次
    payload = {
        "agentId": "5f30c48b-2c98-4316-afc4-9deb00050020",
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