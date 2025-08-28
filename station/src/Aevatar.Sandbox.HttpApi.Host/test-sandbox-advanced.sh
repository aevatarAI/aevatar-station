#!/bin/bash

# 定义API端点
API_ENDPOINT="http://localhost:7004/api/execute"

# 定义要执行的Python代码 - 数据分析示例
PYTHON_CODE=$(cat << 'EOF'
import json
import math
import random
from datetime import datetime, timedelta

# 生成模拟数据 - 一周的温度数据
def generate_temperature_data():
    start_date = datetime.now() - timedelta(days=7)
    data = []
    
    for i in range(7):
        current_date = start_date + timedelta(days=i)
        # 生成一天内24小时的温度数据
        daily_temps = []
        base_temp = random.uniform(15, 25)  # 基础温度
        
        for hour in range(24):
            # 温度在一天内有波动，早晨低，下午高
            hour_factor = math.sin((hour - 6) * math.pi / 12) * 5
            temp = round(base_temp + hour_factor + random.uniform(-1, 1), 1)
            daily_temps.append({
                "hour": hour,
                "temperature": temp
            })
        
        data.append({
            "date": current_date.strftime("%Y-%m-%d"),
            "day_of_week": current_date.strftime("%A"),
            "hourly_temperatures": daily_temps
        })
    
    return data

# 分析温度数据
def analyze_temperature_data(data):
    results = {
        "daily_averages": [],
        "overall_stats": {
            "min_temp": float("inf"),
            "max_temp": float("-inf"),
            "avg_temp": 0
        },
        "day_night_diff": []  # 日夜温差
    }
    
    all_temps = []
    
    for day_data in data:
        temps = [hour_data["temperature"] for hour_data in day_data["hourly_temperatures"]]
        day_temps = [hour_data["temperature"] for hour_data in day_data["hourly_temperatures"] if 6 <= hour_data["hour"] < 18]
        night_temps = [hour_data["temperature"] for hour_data in day_data["hourly_temperatures"] if hour_data["hour"] < 6 or hour_data["hour"] >= 18]
        
        avg_temp = sum(temps) / len(temps)
        min_temp = min(temps)
        max_temp = max(temps)
        day_avg = sum(day_temps) / len(day_temps)
        night_avg = sum(night_temps) / len(night_temps)
        
        results["daily_averages"].append({
            "date": day_data["date"],
            "day_of_week": day_data["day_of_week"],
            "average": round(avg_temp, 1),
            "min": round(min_temp, 1),
            "max": round(max_temp, 1)
        })
        
        results["day_night_diff"].append({
            "date": day_data["date"],
            "day_avg": round(day_avg, 1),
            "night_avg": round(night_avg, 1),
            "difference": round(day_avg - night_avg, 1)
        })
        
        all_temps.extend(temps)
        
        # 更新整体统计
        results["overall_stats"]["min_temp"] = min(results["overall_stats"]["min_temp"], min_temp)
        results["overall_stats"]["max_temp"] = max(results["overall_stats"]["max_temp"], max_temp)
    
    results["overall_stats"]["avg_temp"] = round(sum(all_temps) / len(all_temps), 1)
    results["overall_stats"]["temp_range"] = round(results["overall_stats"]["max_temp"] - results["overall_stats"]["min_temp"], 1)
    
    # 检测趋势
    daily_avgs = [day["average"] for day in results["daily_averages"]]
    if len(daily_avgs) >= 3:
        if all(daily_avgs[i] < daily_avgs[i+1] for i in range(len(daily_avgs)-1)):
            results["trend"] = "Rising temperatures throughout the week"
        elif all(daily_avgs[i] > daily_avgs[i+1] for i in range(len(daily_avgs)-1)):
            results["trend"] = "Falling temperatures throughout the week"
        else:
            results["trend"] = "Mixed temperature pattern"
    
    return results

# 生成数据
temp_data = generate_temperature_data()

# 分析数据
analysis_results = analyze_temperature_data(temp_data)

# 输出结果
print("===== TEMPERATURE DATA ANALYSIS =====")
print(f"Period: {temp_data[0]['date']} to {temp_data[-1]['date']}")
print(f"Overall average temperature: {analysis_results['overall_stats']['avg_temp']}°C")
print(f"Temperature range: {analysis_results['overall_stats']['min_temp']}°C to {analysis_results['overall_stats']['max_temp']}°C ({analysis_results['overall_stats']['temp_range']}°C difference)")

print("\nDaily Averages:")
for day in analysis_results["daily_averages"]:
    print(f"{day['day_of_week']} ({day['date']}): Avg: {day['average']}°C, Min: {day['min']}°C, Max: {day['max']}°C")

print("\nDay/Night Temperature Differences:")
for day in analysis_results["day_night_diff"]:
    print(f"{day['date']}: Day: {day['day_avg']}°C, Night: {day['night_avg']}°C, Diff: {day['difference']}°C")

if "trend" in analysis_results:
    print(f"\nTrend Analysis: {analysis_results['trend']}")

print("\n===== RAW DATA SAMPLE =====")
print(json.dumps(temp_data[0], indent=2))
EOF
)

# 使用jq正确转义Python代码
ESCAPED_CODE=$(echo "$PYTHON_CODE" | jq -Rs .)

# 构建请求体
REQUEST_BODY=$(cat << EOF
{
  "code": $ESCAPED_CODE,
  "language": "python",
  "resourceLimits": {
    "cpuLimitCores": 0.5,
    "memoryLimitBytes": 134217728,
    "timeoutSeconds": 15
  }
}
EOF
)

echo "Sending request to execute advanced Python data analysis code..."
echo "API Endpoint: $API_ENDPOINT"
echo "-----------------------------------"

# 发送请求
RESPONSE=$(curl -s -X POST "$API_ENDPOINT" \
     -H "Content-Type: application/json" \
     -d "$REQUEST_BODY")

echo "Response Status:"
echo "$RESPONSE" | jq -r '.message // "No status returned"'
echo "-----------------------------------"
echo "Execution Results:"
echo "$RESPONSE" | jq -r '.output // "No output returned"'

echo ""
echo "-----------------------------------"
echo "Test completed."