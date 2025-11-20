using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Common.Mapping;
using OrderManagement.Common.Middleware;
using OrderManagement.Features.Orders.Handlers;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;
using OrderManagement.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<OrderManagementContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

builder.Services.AddAutoMapper(typeof(OrderMappingProfile), typeof(AdvancedOrderMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderProfileValidator>();
builder.Services.AddScoped<IValidator<CreateOrderProfileRequest>, CreateOrderProfileValidator>();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetAllOrdersHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderManagementContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapPost("/orders", async (
        CreateOrderProfileRequest request,
        CreateOrderHandler handler,
        CancellationToken cancellationToken) =>
        await handler.Handle(request, cancellationToken))
    .WithName("CreateOrder")
    .WithSummary("Creates a new order with advanced validation and mapping")
    .WithDescription("Creates an order, applying advanced AutoMapper profiles, structured logging, and validation.")
    .Produces(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest);

app.MapGet("/orders", async (
        GetAllOrdersHandler handler,
        CancellationToken cancellationToken) =>
        await handler.Handle(new GetAllOrdersRequest(), cancellationToken))
    .WithName("GetAllOrders")
    .WithSummary("Retrieves all orders")
    .WithDescription("Returns all orders with caching support. Results are cached for 5 minutes.")
    .Produces<List<OrderManagement.Features.Orders.DTOs.OrderProfileDto>>(StatusCodes.Status200OK);

app.MapGet("/orders/{id:guid}", async (
        Guid id,
        GetOrderByIdHandler handler,
        CancellationToken cancellationToken) =>
        await handler.Handle(new GetOrderByIdRequest(id), cancellationToken))
    .WithName("GetOrderById")
    .WithSummary("Retrieves an order by ID")
    .WithDescription("Returns a specific order by its unique identifier.")
    .Produces<OrderManagement.Features.Orders.DTOs.OrderProfileDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.Run();
