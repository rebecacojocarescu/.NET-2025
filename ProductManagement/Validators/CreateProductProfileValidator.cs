using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;
using ProductManagement.Persistence;

namespace ProductManagement.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    public CreateProductProfileValidator(ProductManagementContext context, ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        // Name Validation Rules
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MinimumLength(1).MaximumLength(200).WithMessage("Product name must be between 1 and 200 characters")
            .MustAsync(BeValidName).WithMessage("Product name contains inappropriate content")
            .MustAsync(BeUniqueName).WithMessage("Product name must be unique for the same brand");

        // Brand Validation Rules
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required")
            .MinimumLength(2).MaximumLength(100).WithMessage("Brand must be between 2 and 100 characters")
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters");

        // SKU Validation Rules
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required")
            .Must(BeValidSKU).WithMessage("SKU must be alphanumeric with hyphens, 5-20 characters")
            .MustAsync(BeUniqueSKU).WithMessage("SKU already exists in system");

        // Category Validation Rules
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid enum value");

        // Price Validation Rules
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(10000).WithMessage("Price must be less than $10,000");

        // ReleaseDate Validation Rules
        RuleFor(x => x.ReleaseDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Release date cannot be in the future")
            .GreaterThanOrEqualTo(new DateTime(1900, 1, 1)).WithMessage("Release date cannot be before year 1900");

        // StockQuantity Validation Rules
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative")
            .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000");

        // ImageUrl Validation Rules
        RuleFor(x => x.ImageUrl)
            .Must(BeValidImageUrl).WithMessage("Image URL must be valid and end with .jpg, .jpeg, .png, .gif, or .webp")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        // Business Rules Validation
        RuleFor(x => x)
            .MustAsync(PassBusinessRules).WithMessage("Product does not meet business rules");
    }

    private async Task<bool> BeValidName(string name, CancellationToken cancellationToken)
    {
        var inappropriateWords = new[] { "spam", "scam", "fake", "illegal" };
        return !inappropriateWords.Any(word => name.ToLower().Contains(word));
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken cancellationToken)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Name == name && p.Brand == request.Brand, cancellationToken);
        return !exists;
    }

    private bool BeValidBrandName(string brand)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(brand, @"^[a-zA-Z0-9\s\-'\.]+$");
    }

    private bool BeValidSKU(string sku)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(sku, @"^[a-zA-Z0-9\-]{5,20}$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken cancellationToken)
    {
        var exists = await _context.Products.AnyAsync(p => p.SKU == sku, cancellationToken);
        return !exists;
    }

    private bool BeValidImageUrl(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return true;
        
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == "http" || uri.Scheme == "https") &&
               validExtensions.Any(ext => imageUrl.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken cancellationToken)
    {
        // Rule 1: Daily product limit (500 per day)
        var today = DateTime.UtcNow.Date;
        var todayCount = await _context.Products.CountAsync(p => p.CreatedAt.Date == today, cancellationToken);
        if (todayCount >= 500)
        {
            _logger.LogWarning("Daily product limit exceeded: {Count}", todayCount);
            return false;
        }

        // Rule 2: Electronics minimum price
        if (request.Category == ProductCategory.Electronics && request.Price < 50.00m)
        {
            _logger.LogWarning("Electronics product price too low: {Price}", request.Price);
            return false;
        }

        // Rule 3: Home products content restrictions
        if (request.Category == ProductCategory.Home)
        {
            var restrictedWords = new[] { "weapon", "dangerous", "hazardous" };
            if (restrictedWords.Any(word => request.Name.ToLower().Contains(word)))
            {
                _logger.LogWarning("Home product contains restricted content: {Name}", request.Name);
                return false;
            }
        }

        // Rule 4: High-value products stock limit
        if (request.Price > 500m && request.StockQuantity > 10)
        {
            _logger.LogWarning("High-value product stock too high: {Price}, {Stock}", request.Price, request.StockQuantity);
            return false;
        }

        return true;
    }
}