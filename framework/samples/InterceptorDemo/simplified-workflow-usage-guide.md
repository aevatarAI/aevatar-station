# 🎯 简化的Workflow日志使用指南

## 新的简化使用方式

### 之前 (复杂方式)
```csharp
[Interceptor(IsWorkflowStep = true, 
             WorkflowId = "order-proc-123", 
             StepName = "validate-order", 
             WorkflowType = "OrderProcessing")]
private async Task<bool> ValidateOrder(string orderId, decimal amount)
```

### 现在 (简化方式) ✨
```csharp
[Interceptor(IsWorkflowStep = true)]
private async Task<bool> ValidateOrder(string orderId, decimal amount)
```

## 运行时设置WorkflowId

### 使用WorkflowContext.ExecuteInWorkflowAsync
```csharp
public async Task ProcessOrderWorkflow(string orderId, string customerEmail, decimal amount)
{
    // 为整个workflow设置上下文
    await WorkflowContext.ExecuteInWorkflowAsync($"order-proc-{orderId}", "OrderProcessing", async () =>
    {
        var isValid = await ValidateOrder(orderId, amount);
        var payment = await ProcessPayment(orderId, amount, customerEmail);
        await FulfillOrder(orderId, payment.TransactionId);
    });
}
```

### 使用WorkflowContext.SetWorkflow (手动管理)
```csharp
public async Task ProcessOrderWorkflow(string orderId, string customerEmail, decimal amount)
{
    WorkflowContext.SetWorkflow($"order-proc-{orderId}", "OrderProcessing");
    
    try
    {
        var isValid = await ValidateOrder(orderId, amount);
        var payment = await ProcessPayment(orderId, amount, customerEmail);
        await FulfillOrder(orderId, payment.TransactionId);
    }
    finally
    {
        WorkflowContext.Clear(); // 清理上下文
    }
}
```

## 自动推导功能

### StepName自动生成
方法名会自动转换为kebab-case格式：
- `ValidateOrder` → `validate-order`
- `ProcessPayment` → `process-payment` 
- `SendWelcomeEmail` → `send-welcome-email`

### WorkflowType自动推导
如果未在`WorkflowContext`中指定，会从类名自动推导：
- `OrderProcessingService` → `OrderProcessing`
- `UserOnboardingDemo` → `UserOnboarding`
- `DataValidationWorkflow` → `DataValidation`

## 完整使用示例

```csharp
public class OrderService
{
    // 主workflow入口方法
    public async Task ProcessOrder(string orderId, decimal amount)
    {
        // 设置整个workflow的上下文
        await WorkflowContext.ExecuteInWorkflowAsync($"order-{orderId}", "OrderProcessing", async () =>
        {
            // 所有被调用的方法都会自动获得workflow上下文
            var isValid = await ValidateOrder(orderId, amount);
            if (isValid)
            {
                var payment = await ProcessPayment(orderId, amount);
                await ShipOrder(orderId, payment.TransactionId);
            }
        });
    }
    
    [Interceptor(IsWorkflowStep = true)]
    private async Task<bool> ValidateOrder(string orderId, decimal amount)
    {
        // 自动记录为: 
        // WorkflowId: order-{orderId}
        // StepName: validate-order  
        // WorkflowType: OrderProcessing
        return amount > 0;
    }
    
    [Interceptor(IsWorkflowStep = true)]
    private async Task<PaymentResult> ProcessPayment(string orderId, decimal amount)
    {
        // 自动记录为:
        // WorkflowId: order-{orderId} 
        // StepName: process-payment
        // WorkflowType: OrderProcessing
        return new PaymentResult { TransactionId = "tx-123" };
    }
    
    [Interceptor(IsWorkflowStep = true)]
    private async Task ShipOrder(string orderId, string transactionId)
    {
        // 自动记录为:
        // WorkflowId: order-{orderId}
        // StepName: ship-order
        // WorkflowType: OrderProcessing
        Console.WriteLine($"Shipping order {orderId}");
    }
}
```

## 日志输出示例

运行上述代码会产生以下日志：

```
WORKFLOW: [WorkflowId=order-123] [Step=validate-order] [Type=OrderProcessing] ENTER: ValidateOrder
WORKFLOW: [WorkflowId=order-123] [Step=validate-order] [Type=OrderProcessing] INPUT: {"orderId":"123","amount":99.99}
WORKFLOW: [WorkflowId=order-123] [Step=validate-order] [Type=OrderProcessing] EXIT: ValidateOrder
WORKFLOW: [WorkflowId=order-123] [Step=validate-order] [Type=OrderProcessing] OUTPUT: true

WORKFLOW: [WorkflowId=order-123] [Step=process-payment] [Type=OrderProcessing] ENTER: ProcessPayment
WORKFLOW: [WorkflowId=order-123] [Step=process-payment] [Type=OrderProcessing] INPUT: {"orderId":"123","amount":99.99}
WORKFLOW: [WorkflowId=order-123] [Step=process-payment] [Type=OrderProcessing] EXIT: ProcessPayment
WORKFLOW: [WorkflowId=order-123] [Step=process-payment] [Type=OrderProcessing] OUTPUT: {"TransactionId":"tx-123"}

WORKFLOW: [WorkflowId=order-123] [Step=ship-order] [Type=OrderProcessing] ENTER: ShipOrder  
WORKFLOW: [WorkflowId=order-123] [Step=ship-order] [Type=OrderProcessing] INPUT: {"orderId":"123","transactionId":"tx-123"}
WORKFLOW: [WorkflowId=order-123] [Step=ship-order] [Type=OrderProcessing] EXIT: ShipOrder
```

## Elasticsearch结构化字段

每条日志仍然包含完整的结构化属性：

```json
{
  "IsWorkflow": true,
  "WorkflowId": "order-123",
  "WorkflowStep": "validate-order", 
  "WorkflowType": "OrderProcessing",
  "WorkflowAction": "ENTER",
  "MethodName": "ValidateOrder"
}
```

## 使用建议

### ✅ 推荐做法
1. **一个workflow一个ExecuteInWorkflowAsync调用**
2. **使用有意义的WorkflowId，包含业务标识符** (如订单号、用户ID)
3. **WorkflowType使用简洁的业务领域名称**
4. **方法名使用清晰的动词+名词组合**

### ❌ 避免的做法
1. 嵌套多个`ExecuteInWorkflowAsync`调用
2. WorkflowId使用纯GUID，不利于调试
3. 在长时间运行的方法中使用，可能影响上下文传递

## 对比总结

| 方面 | 旧方式 | 新方式 ✨ |
|------|--------|----------|
| **Attribute复杂度** | 5个属性 | 1个属性 |
| **WorkflowId设置** | 硬编码在Attribute | 运行时动态设置 |
| **StepName** | 手动指定 | 自动从方法名推导 |
| **WorkflowType** | 手动指定 | 自动从类名推导或上下文获取 |
| **维护成本** | 高 | 低 |
| **灵活性** | 低 | 高 |
| **易用性** | 复杂 | 简单 |

现在你只需要一个简单的`[Interceptor(IsWorkflowStep = true)]`标签，其他的信息都通过运行时上下文和自动推导来完成！🎉
