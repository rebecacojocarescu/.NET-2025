using Microsoft.Extensions.Logging;

namespace OrderManagement.Common.Logging;

public static class LoggingExtensions
{
    public static void LogOrderCreationMetrics(this ILogger logger, OrderCreationMetrics metrics)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));

        logger.LogInformation(
            LogEvents.OrderCreationCompleted,
            "Order operation {OperationId} | Title: {Title} | ISBN: {ISBN} | Category: {Category} | " +
            "ValidationDurationMs: {ValidationDuration} | DatabaseSaveDurationMs: {DatabaseDuration} | " +
            "TotalDurationMs: {TotalDuration} | Success: {Success} | Error: {Error}",
            metrics.OperationId,
            metrics.OrderTitle,
            metrics.ISBN,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "None");
    }
}

