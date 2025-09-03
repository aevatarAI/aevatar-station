# PsiOmni Event Tracing Troubleshooting

## Issue: No logs appearing

### Step 1: Check Console Output
Look for these debug messages when your agent starts:
```
[DEBUG] PsiOmni EventTracing - Enabled: true, Logger: PsiOmni.EventTracing.PsiOmniGAgent
[DEBUG] Test log sent to event logger
```

### Step 2: Configuration Adjustments

#### If "Enabled: false"
Your configuration isn't loading. Try these solutions:

1. **Use main appsettings.json** instead of appsettings.PsiOmni.json
2. **Set environment variable**: `ASPNETCORE_ENVIRONMENT=Development`
3. **Check your app startup** - ensure it's loading configuration correctly

#### If "Enabled: true" but no event logs
The logger category doesn't match. Update your appsettings.json:

```json
{
  "PsiOmni": {
    "EnableEventTracing": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PsiOmni.EventTracing.PsiOmniGAgent": "Debug",
      "PsiOmni.EventTracing": "Debug"
    }
  }
}
```

#### Quick Test Configuration
Use this minimal configuration in your main appsettings.json:

```json
{
  "PsiOmni": {
    "EnableEventTracing": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Step 3: Environment Variables
You can also enable tracing via environment variables:
```bash
export PsiOmni__EnableEventTracing=true
export Logging__LogLevel__Default=Debug
```

### Step 4: Verify Test Log
You should see this test message when an agent is created:
```
TEST: PsiOmni event tracing is enabled and working!
```

If you see this test message, the tracing is working and you should see event logs during operation.