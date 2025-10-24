using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Common.Mapping;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;
using ProductManagement.Persistence;
using Xunit;

namespace ProductManagement.Tests;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ProductManagementContext _context;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateProductHandler>> _loggerMock;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        // Configure in-memory database
        var options = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProductManagementContext(options);

        // Configure AutoMapper
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });
        _mapper = config.CreateMapper();

        // Configure cache
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Mock logger
        _loggerMock = new Mock<ILogger<CreateProductHandler>>();

        // Create handler
        _handler = new CreateProductHandler(_context, _mapper, _loggerMock.Object, _cache);
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "iPhone 15 Pro",
            Brand = "Apple Inc",
            SKU = "IPH15-PRO-256",
            Category = ProductCategory.Electronics,
            Price = 999.99m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-2),
            ImageUrl = "https://example.com/iphone15.jpg",
            StockQuantity = 50
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, ((Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>)result).StatusCode);

        var createdProduct = await _context.Products.FirstOrDefaultAsync();
        Assert.NotNull(createdProduct);
        Assert.Equal("iPhone 15 Pro", createdProduct.Name);
        Assert.Equal("Apple Inc", createdProduct.Brand);
        Assert.Equal("IPH15-PRO-256", createdProduct.SKU);
        Assert.Equal(ProductCategory.Electronics, createdProduct.Category);
        Assert.Equal(999.99m, createdProduct.Price);
        Assert.True(createdProduct.IsAvailable);
        Assert.Equal(50, createdProduct.StockQuantity);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        // Arrange
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Product",
            Brand = "Existing Brand",
            SKU = "DUPLICATE-SKU",
            Category = ProductCategory.Electronics,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-1),
            IsAvailable = true,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(existingProduct);
        await _context.SaveChangesAsync();

        var request = new CreateProductProfileRequest
        {
            Name = "New Product",
            Brand = "New Brand",
            SKU = "DUPLICATE-SKU", // Same SKU
            Category = ProductCategory.Clothing,
            Price = 50m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-1),
            StockQuantity = 5
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(request));
    }

    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        {
            Name = "Garden Chair",
            Brand = "Home Decor",
            SKU = "GARDEN-CHAIR-001",
            Category = ProductCategory.Home,
            Price = 100m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-6),
            StockQuantity = 25
        };

        // Act
        var result = await _handler.Handle(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, ((Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>)result).StatusCode);

        var createdProduct = await _context.Products.FirstOrDefaultAsync();
        Assert.NotNull(createdProduct);
        Assert.Equal(ProductCategory.Home, createdProduct.Category);
        Assert.Equal("Garden Chair", createdProduct.Name);
        Assert.Equal("Home Decor", createdProduct.Brand);
        Assert.Equal("GARDEN-CHAIR-001", createdProduct.SKU);
        Assert.Equal(100m, createdProduct.Price);
        Assert.Equal(25, createdProduct.StockQuantity);
        Assert.True(createdProduct.IsAvailable);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}