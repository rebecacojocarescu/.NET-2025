using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Exceptions;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;

namespace OrderManagement.Features.Orders.Handlers;

public class GetOrderByIdHandler
{
    private readonly OrderManagementContext context;
    private readonly IMapper mapper;
    private readonly ILogger<GetOrderByIdHandler> logger;

    public GetOrderByIdHandler(
        OrderManagementContext context,
        IMapper mapper,
        ILogger<GetOrderByIdHandler> logger)
    {
        this.context = context;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<IResult> Handle(GetOrderByIdRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving order by ID: {OrderId}", request.Id);

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        
        if (order == null)
        {
            logger.LogWarning("Order not found: {OrderId}", request.Id);
            return Results.NotFound();
        }

        var orderDto = mapper.Map<OrderProfileDto>(order);
        logger.LogInformation("Order retrieved successfully: {OrderId}", request.Id);

        return Results.Ok(orderDto);
    }
}

