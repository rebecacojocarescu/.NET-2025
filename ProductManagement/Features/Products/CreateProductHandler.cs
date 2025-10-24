using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Common.Logging;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;
using ProductManagement.Persistence;

namespace ProductManagement.Features.Products;

public class CreateProductHandler(
    ProductManagementContext context, 
    IMapper mapper, 
    ILogger<CreateProductHandler> logger,
    IMemoryCache cache)
{
    public async Task<IResult> Handle(CreateProductProfileRequest request)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var operationStartTime = DateTime.UtcNow;
        
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["ProductName"] = request.Name,
            ["Brand"] = request.Brand,
            ["SKU"] = request.SKU,
            ["Category"] = request.Category.ToString()
        });

        try
        {
            logger.LogInformation(LogEvents.ProductCreationStarted, 
                "Product creation started - Name: {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
                request.Name, request.Brand, request.SKU, request.Category);

            // SKU Validation
            var skuValidationStart = DateTime.UtcNow;
            logger.LogInformation(LogEvents.SKUValidationPerformed, 
                "SKU validation performed for: {SKU}", request.SKU);
            var skuValidationDuration = DateTime.UtcNow - skuValidationStart;

            // Stock Validation
            var stockValidationStart = DateTime.UtcNow;
            logger.LogInformation(LogEvents.StockValidationPerformed, 
                "Stock validation performed for: {StockQuantity}", request.StockQuantity);
            var stockValidationDuration = DateTime.UtcNow - stockValidationStart;

            var validationDuration = skuValidationDuration + stockValidationDuration;

            // Database Operations
            var dbOperationStart = DateTime.UtcNow;
            logger.LogInformation(LogEvents.DatabaseOperationStarted, 
                "Database operation started for product: {Name}", request.Name);

            // Map CreateProductProfileRequest to Product using AutoMapper
            var product = mapper.Map<Product>(request);
            
            // Add to context and save
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var dbOperationDuration = DateTime.UtcNow - dbOperationStart;
            logger.LogInformation(LogEvents.DatabaseOperationCompleted, 
                "Database operation completed for product: {ProductId}", product.Id);

            // Cache Operations
            logger.LogInformation(LogEvents.CacheOperationPerformed, 
                "Cache operation performed with key: all_products");
            cache.Remove("all_products");

            // Map Product to ProductProfileDto for response
            var productProfileDto = mapper.Map<ProductProfileDto>(product);

            var totalDuration = DateTime.UtcNow - operationStartTime;

            // Log comprehensive metrics
            var metrics = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationDuration,
                dbOperationDuration,
                totalDuration,
                true
            );

            logger.LogProductCreationMetrics(metrics);

            logger.LogInformation(LogEvents.ProductCreationCompleted, 
                "Product created successfully with ID: {ProductId}", product.Id);

            return Results.Created($"/products/{product.Id}", productProfileDto);
        }
        catch (Exception ex)
        {
            var totalDuration = DateTime.UtcNow - operationStartTime;
            
            var metrics = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                TimeSpan.Zero,
                TimeSpan.Zero,
                totalDuration,
                false,
                ex.Message
            );

            logger.LogProductCreationMetrics(metrics);
            logger.LogError(ex, "Product creation failed for: {Name}", request.Name);
            
            throw;
        }
    }
}