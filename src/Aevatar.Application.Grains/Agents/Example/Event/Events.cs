using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.Example.Event;

[GenerateSerializer]
public class OrderEventBase: EventBase
{
    [Id(0)] public string OrderId { get; set; }
}


[GenerateSerializer]
public class OrderCreatedEvent : OrderEventBase
{
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public List<string> Items { get; set; }
}

[GenerateSerializer]
public class InventoryCheckedEvent : OrderEventBase
{
    [Id(1)] public bool Success { get; set; }
}

[GenerateSerializer]
public class InventoryOutOfStockEvent : OrderEventBase
{
    [Id(1)] public List<string> OutOfStockItems { get; set; }
}

[GenerateSerializer]
public class UserValidatedEvent : OrderEventBase
{
    [Id(1)] public string UserId { get; set; }
}

[GenerateSerializer]
public class PaymentProcessedEvent : OrderEventBase
{
    [Id(1)] public decimal Amount { get; set; }
}

[GenerateSerializer]
public class PaymentFailedEvent : OrderEventBase
{
    [Id(1)] public string Reason { get; set; }
}

[GenerateSerializer]
public class OrderFulfilledEvent : OrderEventBase
{
    [Id(1)] public List<string> FulfilledItems { get; set; }
}

[GenerateSerializer]
public class NotificationSentEvent : OrderEventBase
{
    [Id(1)] public string NotificationMessage { get; set; }
}

[GenerateSerializer]
public class WorkflowCompletedEvent : OrderEventBase
{
    [Id(1)] public string ReportContent { get; set; }
}