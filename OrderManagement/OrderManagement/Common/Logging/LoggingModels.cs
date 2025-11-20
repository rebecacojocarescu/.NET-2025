using OrderManagement.Features.Orders;

namespace OrderManagement.Common.Logging;

public static class LogEvents
{
    public static readonly EventId OrderCreationStarted = new(2001, nameof(OrderCreationStarted));
    public static readonly EventId OrderValidationFailed = new(2002, nameof(OrderValidationFailed));
    public static readonly EventId OrderCreationCompleted = new(2003, nameof(OrderCreationCompleted));
    public static readonly EventId DatabaseOperationStarted = new(2004, nameof(DatabaseOperationStarted));
    public static readonly EventId DatabaseOperationCompleted = new(2005, nameof(DatabaseOperationCompleted));
    public static readonly EventId CacheOperationPerformed = new(2006, nameof(CacheOperationPerformed));
    public static readonly EventId ISBNValidationPerformed = new(2007, nameof(ISBNValidationPerformed));
    public static readonly EventId StockValidationPerformed = new(2008, nameof(StockValidationPerformed));
}

public record OrderCreationMetrics(
    string OperationId,
    string OrderTitle,
    string ISBN,
    OrderCategory Category,
    TimeSpan ValidationDuration,
    TimeSpan DatabaseSaveDuration,
    TimeSpan TotalDuration,
    bool Success,
    string? ErrorReason);

