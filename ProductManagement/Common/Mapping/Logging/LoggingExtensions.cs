using Microsoft.Extensions.Logging;
using ProductManagement.Features.Products;

namespace ProductManagement.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            "Product Creation Metrics - OperationId: {OperationId}, ProductName: {ProductName}, SKU: {SKU}, Category: {Category}, " +
            "ValidationDuration: {ValidationDuration}ms, DatabaseSaveDuration: {DatabaseSaveDuration}ms, " +
            "TotalDuration: {TotalDuration}ms, Success: {Success}, ErrorReason: {ErrorReason}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason);
    }
}