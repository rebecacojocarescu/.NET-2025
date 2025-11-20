using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OrderManagement.Common.Logging;
using OrderManagement.Exceptions;
using OrderManagement.Features.Orders;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;

namespace OrderManagement.Features.Orders.Handlers;
public class CreateOrderHandler
{
    private const string AllOrdersCacheKey = "all_orders";
    private readonly OrderManagementContext context;
    private readonly IValidator<CreateOrderProfileRequest> validator;
    private readonly IMapper mapper;
    private readonly IMemoryCache cache;
    private readonly ILogger<CreateOrderHandler> logger;

    public CreateOrderHandler(
        OrderManagementContext context,
        IValidator<CreateOrderProfileRequest> validator,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<CreateOrderHandler> logger)
    {
        this.context = context;
        this.validator = validator;
        this.mapper = mapper;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IResult> Handle(CreateOrderProfileRequest request, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var operationStopwatch = Stopwatch.StartNew();

        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["ISBN"] = request.ISBN,
            ["Category"] = request.Category.ToString()
        });

        logger.LogInformation(
            LogEvents.OrderCreationStarted,
            "Order creation started | OperationId: {OperationId} | Title: {Title} | Author: {Author} | ISBN: {ISBN} | Category: {Category}",
            operationId,
            request.Title,
            request.Author,
            request.ISBN,
            request.Category);

        var validationStopwatch = Stopwatch.StartNew();

        try
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationStopwatch.Stop();
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                logger.LogWarning(
                    LogEvents.OrderValidationFailed,
                    "Order validation failed | OperationId: {OperationId} | Errors: {Errors}",
                    operationId,
                    string.Join("; ", errors));

                throw new OrderManagement.Exceptions.ValidationException(errors);
            }

            logger.LogInformation(
                LogEvents.ISBNValidationPerformed,
                "ISBN uniqueness validation started | OperationId: {OperationId} | ISBN: {ISBN}",
                operationId,
                request.ISBN);

            var isbnExists = await context.Orders.AnyAsync(o => o.ISBN == request.ISBN, cancellationToken);
            if (isbnExists)
            {
                validationStopwatch.Stop();
                logger.LogWarning(
                    LogEvents.OrderValidationFailed,
                    "ISBN already exists | OperationId: {OperationId} | ISBN: {ISBN}",
                    operationId,
                    request.ISBN);

                throw new OrderManagement.Exceptions.ValidationException($"An order with ISBN '{request.ISBN}' already exists.");
            }

            logger.LogInformation(
                LogEvents.StockValidationPerformed,
                "Stock validation performed | OperationId: {OperationId} | StockQuantity: {Stock}",
                operationId,
                request.StockQuantity);

            validationStopwatch.Stop();

            var dbStopwatch = Stopwatch.StartNew();

            var order = mapper.Map<Order>(request);
            context.Orders.Add(order);

            logger.LogInformation(
                LogEvents.DatabaseOperationStarted,
                "Database save started | OperationId: {OperationId} | OrderId: {OrderId}",
                operationId,
                order.Id);

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                LogEvents.DatabaseOperationCompleted,
                "Database save completed | OperationId: {OperationId} | OrderId: {OrderId}",
                operationId,
                order.Id);

            dbStopwatch.Stop();

            cache.Remove(AllOrdersCacheKey);
            logger.LogInformation(
                LogEvents.CacheOperationPerformed,
                "Cache invalidated for key {CacheKey} | OperationId: {OperationId}",
                AllOrdersCacheKey,
                operationId);

            var dto = mapper.Map<OrderProfileDto>(order);

            operationStopwatch.Stop();

            logger.LogOrderCreationMetrics(new OrderCreationMetrics(
                operationId,
                order.Title,
                order.ISBN,
                order.Category,
                validationStopwatch.Elapsed,
                dbStopwatch.Elapsed,
                operationStopwatch.Elapsed,
                true,
                null));

            logger.LogInformation(
                LogEvents.OrderCreationCompleted,
                "Order creation completed | OperationId: {OperationId} | OrderId: {OrderId}",
                operationId,
                order.Id);

            return Results.Created($"/orders/{order.Id}", dto);
        }
        catch (Exception ex)
        {
            validationStopwatch.Stop();
            operationStopwatch.Stop();

            logger.LogError(
                ex,
                "Order creation failed | OperationId: {OperationId} | Title: {Title} | ISBN: {ISBN}",
                operationId,
                request.Title,
                request.ISBN);

            logger.LogOrderCreationMetrics(new OrderCreationMetrics(
                operationId,
                request.Title,
                request.ISBN,
                request.Category,
                validationStopwatch.Elapsed,
                TimeSpan.Zero,
                operationStopwatch.Elapsed,
                false,
                ex.Message));

            throw;
        }
    }
}
