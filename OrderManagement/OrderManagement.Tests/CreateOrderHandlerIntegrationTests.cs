using System.Globalization;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderManagement.Common.Logging;
using OrderManagement.Common.Mapping;
using OrderManagement.Exceptions;
using OrderManagement.Features.Orders;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Handlers;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;
using OrderManagement.Validators;

namespace OrderManagement.Tests;

public class CreateOrderHandlerIntegrationTests : IDisposable
{
    private readonly OrderManagementContext context;
    private readonly IMapper mapper;
    private readonly IMemoryCache memoryCache;
    private readonly CreateOrderProfileValidator validator;
    private readonly Mock<ILogger<CreateOrderHandler>> handlerLoggerMock = new();
    private readonly ILogger<CreateOrderProfileValidator> validatorLogger;

    public CreateOrderHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<OrderManagementContext>()
            .UseInMemoryDatabase($"orders-db-{Guid.NewGuid()}")
            .Options;

        context = new OrderManagementContext(options);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new OrderMappingProfile());
            cfg.AddProfile(new AdvancedOrderMappingProfile());
        });
        mapper = mapperConfig.CreateMapper();

        memoryCache = new MemoryCache(new MemoryCacheOptions());
        validatorLogger = NullLogger<CreateOrderProfileValidator>.Instance;
        validator = new CreateOrderProfileValidator(context, validatorLogger);
    }

    public void Dispose()
    {
        context.Dispose();
        memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }

    private CreateOrderHandler CreateHandler()
    {
        return new CreateOrderHandler(context, validator, mapper, memoryCache, handlerLoggerMock.Object);
    }

    private static CreateOrderProfileRequest BuildBaseRequest(OrderCategory category, decimal price, int stockQuantity, DateTime? publishedDate = null)
    {
        return new CreateOrderProfileRequest(
            Title: category switch
            {
                OrderCategory.Technical => "Cloud Architecture Essentials",
                OrderCategory.Children => "Friendly Forest Adventures",
                _ => "The Timeless Classic"
            },
            Author: "Ada Lovelace",
            ISBN: GenerateNumericIsbn(),
            Category: category,
            Price: price,
            PublishedDate: publishedDate ?? DateTime.UtcNow.AddDays(-180),
            CoverImageUrl: "https://example.com/cover.jpg",
            StockQuantity: stockQuantity);
    }

    private static string GenerateNumericIsbn()
    {
        Span<char> buffer = stackalloc char[13];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (char)('0' + Random.Shared.Next(0, 10));
        }
        return new string(buffer);
    }

    [Fact]
    public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMappings()
    {
        var handler = CreateHandler();
        var request = BuildBaseRequest(OrderCategory.Technical, 45.00m, 8, DateTime.UtcNow.AddMonths(-6));

        var result = await handler.Handle(request);

        var created = Assert.IsType<Created<OrderProfileDto>>(result);
        var dto = created.Value;

        Assert.NotNull(dto);
        Assert.Equal("Technical & Professional", dto.CategoryDisplayName);
        Assert.Equal("AL", dto.AuthorInitials);
        Assert.Equal("In Stock", dto.AvailabilityStatus);
        Assert.Equal("6 months old", dto.PublishedAge);
        Assert.Contains(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, dto.FormattedPrice);
        Assert.True(dto.IsAvailable);

        handlerLoggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                LogEvents.OrderCreationStarted,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
    {
        var handler = CreateHandler();
        var sharedIsbn = "1234567890123";

        context.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            Title = "Existing Order",
            Author = "Existing Author",
            ISBN = sharedIsbn,
            Category = OrderCategory.NonFiction,
            Price = 25m,
            PublishedDate = DateTime.UtcNow.AddYears(-1),
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            IsAvailable = true,
            StockQuantity = 5
        });
        await context.SaveChangesAsync();

        var request = new CreateOrderProfileRequest(
            Title: "New Order",
            Author: "Another Author",
            ISBN: sharedIsbn,
            Category: OrderCategory.Fiction,
            Price: 30m,
            PublishedDate: DateTime.UtcNow.AddMonths(-1),
            CoverImageUrl: "https://example.com/cover.jpg",
            StockQuantity: 2);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(request));

        Assert.Contains(exception.Errors, e => e.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        handlerLoggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                LogEvents.OrderValidationFailed,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
    {
        var handler = CreateHandler();
        var request = BuildBaseRequest(
            OrderCategory.Children,
            30m,
            3,
            DateTime.UtcNow.AddDays(-10));

        var result = await handler.Handle(request);
        var created = Assert.IsType<Created<OrderProfileDto>>(result);
        var dto = created.Value;

        Assert.NotNull(dto);
        Assert.Equal("Children's Orders", dto.CategoryDisplayName);
        Assert.Equal(3, dto.StockQuantity);
        Assert.Equal(27m, dto.Price);
        Assert.Equal("Limited Stock", dto.AvailabilityStatus);
        Assert.True(dto.IsAvailable);
        Assert.Null(dto.CoverImageUrl);
        Assert.Contains(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, dto.FormattedPrice);
    }
}

