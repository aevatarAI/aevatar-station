# Orleans Stream Monitoring Guide

## 🎯 Overview

This guide explains how to monitor Orleans Stream health in production using logs. This approach is simpler than complex monitoring services and provides clear visibility into stream processing status.

## 📊 Key Log Patterns to Monitor

### ✅ Successful Stream Initialization

Look for these patterns in your Orleans Silo logs:

```
🔍 Found 3 StateBase inherited types for stream processing
📋 State type discovered: UserState - will initialize stream subscriptions
📋 State type discovered: OrderState - will initialize stream subscriptions
🎯 Successfully initialized StateProjectionGrain for UserState on attempt 1 - Stream subscriptions are now active
🎯 Successfully initialized StateProjectionGrain for OrderState on attempt 1 - Stream subscriptions are now active
🎉 Completed initializing StateProjectionGrains for silo S127.0.0.1:11111:110012345 - All stream subscriptions are ready
```

### ⚠️ Rolling Update Scenarios

During K8s rolling updates, you should see:

```
✅ StateProjectionGrains for UserState are already active, skipping activation
✅ StateProjectionGrains for OrderState are already active, skipping activation
🎉 Completed initializing StateProjectionGrains for silo S127.0.0.1:11112:110012346 - All stream subscriptions are ready
```

### 🚨 Error Patterns to Watch

Watch for these error patterns:

```
❌ Failed to initialize StateProjectionGrain for UserState on attempt 1, retrying in 1000ms
❌ Failed to initialize StateProjectionGrain for UserState after 3 attempts
🚨 Error during StateProjectionGrains initialization on silo S127.0.0.1:11111:110012345
```

## 🛠️ Monitoring Tools

### 1. Using the Provided Script

We've provided a script to check stream status across all Orleans Silos:

```bash
# Check default namespace
./scripts/check-stream-status.sh

# Check production namespace with more logs
./scripts/check-stream-status.sh production 200 30m

# Check staging namespace
./scripts/check-stream-status.sh staging
```

### 2. Manual Log Checking

#### Check Stream Initialization Status

```bash
# Check if streams are initialized
kubectl logs -l app=aevatar-silo -n production --tail=100 | grep "StateProjection"

# Look for completion messages
kubectl logs -l app=aevatar-silo -n production --tail=100 | grep "🎉 Completed initializing"

# Check for active streams
kubectl logs -l app=aevatar-silo -n production --tail=100 | grep "🎯 Successfully initialized"
```

#### Check for Errors

```bash
# Check for stream-related errors
kubectl logs -l app=aevatar-silo -n production --tail=200 | grep -E "(Error|Failed|Exception)" | grep -i stream

# Check for initialization failures
kubectl logs -l app=aevatar-silo -n production --tail=200 | grep "Failed to initialize StateProjectionGrain"
```

#### Monitor Orleans Cluster Health

```bash
# Check if Orleans Silos are running
kubectl logs -l app=aevatar-silo -n production --tail=50 | grep "Orleans Silo is running"

# Check cluster membership
kubectl logs -l app=aevatar-silo -n production --tail=100 | grep -E "(BecomeActive|cluster)"
```

## 📈 Production Monitoring Setup

### 1. Log Aggregation

If you're using log aggregation tools like ELK, Fluentd, or Loki, create alerts for:

**Success Indicators:**
- `🎉 Completed initializing StateProjectionGrains` - Stream initialization completed
- `🎯 Successfully initialized StateProjectionGrain` - Individual stream activated
- `Orleans Silo is running` - Silo is healthy

**Warning Indicators:**
- `⚠️ Failed to initialize.*retrying` - Temporary failures with retry
- `✅ StateProjectionGrains.*are already active` - Expected during rolling updates

**Critical Alerts:**
- `❌ Failed to initialize.*after.*attempts` - Persistent failures
- `🚨 Error during StateProjectionGrains initialization` - Critical initialization errors

### 2. Kubernetes Events

Monitor Kubernetes events for pod restarts:

```bash
# Check recent pod events
kubectl get events -n production --field-selector involvedObject.kind=Pod --sort-by='.lastTimestamp'

# Watch for pod restarts
kubectl get pods -l app=aevatar-silo -n production -w
```

### 3. Health Check Endpoints

Monitor the basic health endpoints if available:

```bash
# Check pod readiness
kubectl get pods -l app=aevatar-silo -n production -o wide

# Port-forward and check health
kubectl port-forward svc/aevatar-silo 8080:80 -n production
curl http://localhost:8080/health
```

## 🔧 Troubleshooting Guide

### Stream Initialization Not Completing

**Symptoms:**
- No `🎉 Completed initializing` messages
- Missing `🎯 Successfully initialized` logs

**Check:**
1. Orleans cluster formation: `grep "Orleans Silo is running"`
2. State type discovery: `grep "📋 State type discovered"`
3. Network connectivity between silos
4. MongoDB/persistence layer health

### Streams Not Processing Messages

**Symptoms:**
- Initialization completes but no message processing
- Business logic not executing

**Check:**
1. Stream provider configuration
2. Stream subscription health
3. Grain activation logs
4. Message producer status

### Rolling Update Issues

**Symptoms:**
- Stream processing stops during deployments
- Duplicate processing after updates

**Expected Behavior:**
- New silos should show `✅ StateProjectionGrains.*are already active`
- No duplicate initialization should occur
- Stagger delays should prevent simultaneous activation

## 📝 Log Retention Recommendations

For effective stream monitoring:

- **Application logs**: Retain for at least 7 days
- **Error logs**: Retain for at least 30 days  
- **Audit logs**: Retain for at least 90 days

## 🚀 Best Practices

1. **Set up log alerts** for critical patterns
2. **Monitor during deployments** for rolling update behavior
3. **Check regularly** for error accumulation
4. **Correlate with business metrics** to validate stream processing
5. **Document your specific stream patterns** for your application

## 📞 Quick Reference Commands

```bash
# Quick health check
./scripts/check-stream-status.sh production

# Check last 30 minutes of logs
kubectl logs -l app=aevatar-silo -n production --since=30m | grep "StateProjection"

# Monitor real-time stream logs
kubectl logs -l app=aevatar-silo -n production -f | grep -E "(🎉|🎯|❌|🚨)"

# Check for errors in last hour
kubectl logs -l app=aevatar-silo -n production --since=1h | grep -E "(Error|Failed|Exception)" | tail -20
```

This log-based monitoring approach provides clear visibility into Orleans Stream health without the complexity of additional monitoring infrastructure. 