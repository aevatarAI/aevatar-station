# 🏠 智能家居AI助手 - Demo快速启动指南

I'm HyperEcho, 我在共振demo展示的快速启动 🚀✨

## 🎯 一键启动Demo

### 最简单的方式

```csharp
// 注入IGAgentFactory到你的服务中
var gAgentFactory = serviceProvider.GetRequiredService<IGAgentFactory>();
var logger = serviceProvider.GetRequiredService<ILogger<SmartHomeDemoHelper>>();

// 🚀 一键启动完整Demo
var demoHelper = new SmartHomeDemoHelper(gAgentFactory, logger);
await demoHelper.RunCompleteDemo("OpenAI");
```

### 分步骤Demo

```csharp
// 1. 初始化智能家居AI助手
var smartHomeAI = await gAgentFactory.CreateSmartHomeDemoAsync("OpenAI");

// 2. 执行单个命令
var result1 = await smartHomeAI.ExecuteSmartHomeCommandAsync("打开客厅主灯");
Console.WriteLine(result1); // ✅ 客厅主灯已打开

var result2 = await smartHomeAI.ExecuteSmartHomeCommandAsync("把卧室台灯调到50%亮度");
Console.WriteLine(result2); // ✅ 卧室台灯亮度已调至50%

// 3. 查看设备状态
var demoHelper = new SmartHomeDemoHelper(gAgentFactory, logger);
await demoHelper.ShowHomeStatusAsync();
```

## 📱 Hardcoded设备列表 (Demo专用)

| 设备ID | 设备名称 | 设备类型 | Demo命令示例 |
|--------|----------|----------|--------------|
| light001 | 客厅主灯 | smart-light | "打开客厅主灯" |
| light002 | 卧室台灯 | smart-light | "把卧室台灯调到50%亮度" |
| temp001 | 客厅温度传感器 | temperature-sensor | "查看客厅温度" |
| temp002 | 卧室温度传感器 | temperature-sensor | "卧室湿度是多少" |
| switch001 | 客厅总开关 | smart-switch | "打开客厅总开关" |
| switch002 | 厨房插座 | smart-switch | "关闭厨房插座" |

## 🎬 预设Demo场景

运行`RunDemoScenariosAsync()`会自动执行以下命令序列：

1. "打开客厅主灯"
2. "把卧室台灯调到50%亮度"  
3. "将客厅灯设为红色"
4. "查看客厅温度"
5. "打开客厅总开关"
6. "检查所有传感器状态"
7. "把所有灯调成暖白色"
8. "关闭所有灯"

## 🗣️ 支持的Demo命令

### 灯光控制
- "打开客厅主灯"
- "关闭卧室台灯"
- "把客厅灯调到70%亮度"
- "将厨房灯设为蓝色"
- "把所有灯调成暖白色"
- "关闭所有灯"

### 开关控制
- "打开客厅总开关"
- "关闭厨房插座"
- "切换客厅开关"

### 传感器查询
- "客厅温度是多少"
- "查看卧室湿度"
- "检查所有传感器电池"
- "显示所有传感器状态"

### 批量操作
- "打开所有灯"
- "关闭所有设备"
- "检查所有设备状态"

## ⚙️ Demo环境要求

1. **Virtual Device Hub API**: 确保在`localhost:9001`运行
2. **LLM配置**: 确保OpenAI配置正确
3. **模块依赖**: 确保`AevatarGAgentsSmartHomeModule`已加载

## 🔧 Demo配置

设备已hardcoded，只需要确保以下配置：

```json
// appsettings.json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "Azure",
      "ModelIdEnum": "OpenAI", 
      "ModelName": "gpt-4o",
      "Endpoint": "*",
      "ApiKey": "*"
    }
  }
}
```

## 🎉 Demo展示效果

运行Demo后你会看到：

```
🏠 初始化智能家居AI助手 (Demo模式)
✅ 智能家居AI助手初始化完成
📱 已注册设备 (6个):
   🟢 客厅主灯 (light001) - smart-light
   🟢 卧室台灯 (light002) - smart-light
   🟢 客厅温度传感器 (temp001) - temperature-sensor
   🟢 卧室温度传感器 (temp002) - temperature-sensor
   🟢 客厅总开关 (switch001) - smart-switch
   🟢 厨房插座 (switch002) - smart-switch

🎬 开始运行智能家居Demo场景
🗣️ 执行命令: 打开客厅主灯
✅ 命令结果: 客厅主灯已成功打开
🗣️ 执行命令: 把卧室台灯调到50%亮度
✅ 命令结果: 卧室台灯亮度已调整至50%
...
🎉 Demo场景运行完成
```

这样就可以完美地向老板展示智能家居AI助手的功能了！🏠🤖✨
