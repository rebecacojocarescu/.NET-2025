using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OrderManagement.Common.Logging;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;

namespace OrderManagement.Features.Orders.Handlers;

public class GetAllOrdersHandler
{
    private const string AllOrdersCacheKey = "all_orders";
    private readonly OrderManagementContext context;
    private readonly IMapper mapper;
    private readonly IMemoryCache cache;
    private readonly ILogger<GetAllOrdersHandler> logger;

    public GetAllOrdersHandler(
        OrderManagementContext context,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<GetAllOrdersHandler> logger)
    {
        this.context = context;
        this.mapper = mapper;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<IResult> Handle(GetAllOrdersRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving all orders");

        if (cache.TryGetValue(AllOrdersCacheKey, out List<OrderProfileDto>? cachedOrders) && cachedOrders != null)
        {
            logger.LogInformation("Orders retrieved from cache");
            return Results.Ok(cachedOrders);
        }

        var orders = await context.Orders.ToListAsync(cancellationToken);
        var orderDtos = mapper.Map<List<OrderProfileDto>>(orders);

        cache.Set(AllOrdersCacheKey, orderDtos, TimeSpan.FromMinutes(5));
        logger.LogInformation("Orders retrieved from database and cached. Count: {Count}", orderDtos.Count);

        return Results.Ok(orderDtos);
    }
}

