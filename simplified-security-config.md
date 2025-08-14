# 🔐 Aevatar Station 安全验证配置指南

## 📋 运维配置清单

### 🎯 **最小必要配置（当前需要）**

```json
{
  "Security": {
    "Switch": {
      "EnableReCAPTCHA": true,
      "EnableRateLimit": true
    },
    "ReCAPTCHA": {
      "SiteKey": "6Lc****************************",
      "SecretKey": "6Lc****************************"
    },
    "RateLimit": {
      "FreeRequestsPerDay": 5
    }
  }
}
```

### 🚀 **完整配置（包含未来iOS/Android）**

```json
{
  "Security": {
    "Switch": {
      "EnableReCAPTCHA": true,
      "EnableRateLimit": true
    },
    "ReCAPTCHA": {
      "SiteKey": "6Lc****************************",
      "SecretKey": "6Lc****************************"
    },
    "AppleDeviceCheck": {
      "EnableValidation": false,
      "TeamId": "TEAM123456",
      "KeyId": "KEY123456",
      "PrivateKey": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----"
    },
    "PlayIntegrity": {
      "EnableValidation": false,
      "ProjectId": "your-android-project-id",
      "ServiceAccountKey": "{\"type\":\"service_account\",\"project_id\":\"...\"}"
    },
    "RateLimit": {
      "FreeRequestsPerDay": 5
    }
  }
}
```

## ⚠️ **配置容错机制**

### ✅ **安全保障**
- 如果 `Security` 节点不存在 → 系统正常运行，reCAPTCHA 默认禁用
- 如果 `Switch` 不存在 → 系统正常运行，reCAPTCHA 默认禁用  
- 如果 `PlayIntegrity` 不配置 → 系统正常运行，Android 降级到 reCAPTCHA
- 如果 `AppleDeviceCheck` 不配置 → 系统正常运行，iOS 降级到 reCAPTCHA

### 🛡️ **错误处理逻辑**
```csharp
// 代码中的容错机制
if (options.Switch?.EnableReCAPTCHA != true) {
    // reCAPTCHA 被禁用，跳过验证
}

if (_options.PlayIntegrity?.EnableValidation != true) {
    // Play Integrity 被禁用，接受任何格式正确的 token
}
```

## 🔧 **Google reCAPTCHA 配置获取**

### 1️⃣ **访问 Google reCAPTCHA 控制台**
```
https://www.google.com/recaptcha/admin/create
```

### 2️⃣ **创建新站点**
- **标签**: `Aevatar Station`
- **reCAPTCHA 类型**: 选择 `reCAPTCHA v2` → `"我不是机器人"复选框`
- **域名**: 添加你的域名 (例如: `yourdomain.com`, `localhost`)

### 3️⃣ **获取密钥**
创建完成后会得到：
- **站点密钥** (Site Key): 前端使用，可以公开
- **密钥** (Secret Key): 后端使用，必须保密

### 4️⃣ **前端集成示例**
```html
<!-- 在 HTML 中添加 -->
<script src="https://www.google.com/recaptcha/api.js" async defer></script>

<!-- reCAPTCHA 容器 -->
<div class="g-recaptcha" data-sitekey="YOUR_SITE_KEY"></div>

<!-- 获取 token -->
<script>
function onSubmit() {
    const response = grecaptcha.getResponse();
    // response 就是需要发送给后端的 reCAPTCHA token
}
</script>
```

## 📱 **移动端配置（未来规划）**

### 🍎 **iOS - Apple DeviceCheck** 
配置获取路径：
1. Apple Developer Account → Certificates, Identifiers & Profiles
2. Keys → Create new key → Enable DeviceCheck
3. 下载 .p8 私钥文件
4. 记录 Team ID 和 Key ID

### 🤖 **Android - Google Play Integrity**
配置获取路径：
1. Google Cloud Console → 创建项目
2. 启用 Play Integrity API
3. 创建服务账号并下载 JSON 密钥
4. Google Play Console → 配置应用

## 🚦 **部署检查清单**

### ✅ **必须配置（立即）**
- [ ] `Security.Switch.EnableReCAPTCHA`: 设为 `true`
- [ ] `Security.Switch.EnableRateLimit`: 设为 `true`  
- [ ] `Security.ReCAPTCHA.SiteKey`: 从 Google 获取
- [ ] `Security.ReCAPTCHA.SecretKey`: 从 Google 获取
- [ ] `Security.RateLimit.FreeRequestsPerDay`: 建议 `5`

### 🔮 **可选配置（未来）**
- [ ] `Security.AppleDeviceCheck.*`: iOS 应用上线时配置
- [ ] `Security.PlayIntegrity.*`: Android 应用上线时配置

## 🎯 **当前影响接口**

仅影响：`POST /api/account/send-register-code`

**验证策略：**
- 前 5 次请求/24小时：无需验证
- 超过 5 次：需要 reCAPTCHA 验证

## ⚡ **性能说明**

- **缓存存储**: 使用现有 Redis，无额外存储开销
- **API 调用**: 仅在需要验证时调用 Google API
- **降级机制**: 所有验证失败都有 reCAPTCHA 降级保障

---

**总结**: 当前只需配置 reCAPTCHA 相关参数，其他配置不设置不会导致系统报错，系统有完善的容错机制。
