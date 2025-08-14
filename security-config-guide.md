# 🔐 安全验证配置指南

## 📋 运维配置清单

### ✅ **当前最小必要配置**
```json
{
  "Security": {
    "Switch": {
      "EnableReCAPTCHA": true,
      "EnableRateLimit": true
    },
    "ReCAPTCHA": {
      "SecretKey": "6Lc****************************"
    },
    "RateLimit": {
      "FreeRequestsPerDay": 5
    }
  }
}
```

### 🔮 **完整配置（包含移动端）**
```json
{
  "Security": {
    "Switch": {
      "EnableReCAPTCHA": true, 
      "EnableRateLimit": true
    },
    "ReCAPTCHA": {
      "SecretKey": "your-secret-key",
      "VerifyUrl": "https://www.google.com/recaptcha/api/siteverify"
    },
    "AppleDeviceCheck": {
      "EnableValidation": false,
      "TeamId": "TEAM123456", 
      "KeyId": "KEY123456",
      "PrivateKey": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----",
      "ApiUrl": "https://api.devicecheck.apple.com/v1/validate_device_token"
    },
    "PlayIntegrity": {
      "EnableValidation": false,
      "ProjectId": "your-android-project-id",
      "ServiceAccountKey": "{\"type\":\"service_account\",...}",
      "ApiUrl": "https://playintegrity.googleapis.com/v1"
    },
    "RateLimit": {
      "FreeRequestsPerDay": 5
    }
  }
}
```

## ⚠️ **安全保障**

- ✅ **PlayIntegrity 未配置不会报错** - `EnableValidation: false` 时系统正常运行
- ✅ **容错机制完善** - 任何配置缺失都有默认值
- ✅ **降级策略** - 移动端验证失败会降级到 reCAPTCHA

## 🔗 **URL 配置说明**

### reCAPTCHA 
- **国际**: `https://www.google.com/recaptcha/api/siteverify`
- **国内**: `https://recaptcha.net/recaptcha/api/siteverify` (可选)

### Apple DeviceCheck
- **默认**: `https://api.devicecheck.apple.com/v1/validate_device_token`
- **需要**: TeamId + KeyId + PrivateKey 生成 JWT 认证

### Google Play Integrity  
- **默认**: `https://playintegrity.googleapis.com/v1`

## 🎯 **当前影响范围**

仅影响：`POST /api/account/send-register-code`
- 前5次/24小时：无需验证
- 超过5次：需要验证
