using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Features.Orders;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Persistence;

namespace OrderManagement.Validators;

public class CreateOrderProfileValidator : AbstractValidator<CreateOrderProfileRequest>
{
    private static readonly string[] InappropriateWords =
    [
        "banned", "explicit", "forbidden", "violent"
    ];

    private static readonly string[] TechnicalKeywords =
    [
        "cloud", "data", "ai", "machine", "network", "programming", "security", "database", "architecture",
        "algorithm", "software", "hardware"
    ];

    private static readonly string[] ChildrenRestrictedWords =
    [
        "violence", "horror", "adult", "war", "blood"
    ];

    private static readonly Regex AuthorRegex = new(@"^[A-Za-z\s\-\.'’]+$", RegexOptions.Compiled);

    private readonly OrderManagementContext context;
    private readonly ILogger<CreateOrderProfileValidator> logger;

    public CreateOrderProfileValidator(OrderManagementContext context, ILogger<CreateOrderProfileValidator> logger)
    {
        this.context = context;
        this.logger = logger;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(1, 200).WithMessage("Title must be between 1 and 200 characters.")
            .Must(BeValidTitle).WithMessage("Title contains inappropriate content.")
            .MustAsync(BeUniqueTitle).WithMessage("An order with the same title and author already exists.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .Length(2, 100).WithMessage("Author must be between 2 and 100 characters.")
            .Must(BeValidAuthorName).WithMessage("Author contains invalid characters.");

        RuleFor(x => x.ISBN)
            .NotEmpty().WithMessage("ISBN is required.")
            .Must(BeValidIsbn).WithMessage("ISBN must be 10 or 13 digits (hyphens optional).")
            .MustAsync(BeUniqueIsbn).WithMessage("An order with this ISBN already exists.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Category must be a valid value.");

        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Price must be greater than 0.")
            .LessThan(10_000m).WithMessage("Price must be less than $10,000.");

        RuleFor(x => x.PublishedDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future.")
            .GreaterThanOrEqualTo(new DateTime(1400, 1, 1)).WithMessage("Published date cannot be before year 1400.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100_000).WithMessage("Stock quantity cannot exceed 100,000.");

        When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl), () =>
        {
            RuleFor(x => x.CoverImageUrl!)
                .Must(BeValidImageUrl)
                .WithMessage("Cover image URL must be a valid HTTP/HTTPS image URL (.jpg, .jpeg, .png, .gif, .webp).");
        });

        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Order violates business rules. Check logs for details.");

        When(order => order.Category == OrderCategory.Technical, () =>
        {
            RuleFor(order => order.Price)
                .GreaterThanOrEqualTo(20m)
                .WithMessage("Technical orders must cost at least $20.00.");

            RuleFor(order => order.Title)
                .Must(ContainTechnicalKeywords)
                .WithMessage("Technical orders must contain technical keywords in the title.");

            RuleFor(order => order.PublishedDate)
                .Must(date => date >= DateTime.UtcNow.AddYears(-5))
                .WithMessage("Technical orders must be published within the last 5 years.");
        });

        When(order => order.Category == OrderCategory.Children, () =>
        {
            RuleFor(order => order.Price)
                .LessThanOrEqualTo(50m)
                .WithMessage("Children's orders cannot exceed $50.00.");

            RuleFor(order => order.Title)
                .Must(BeAppropriateForChildren)
                .WithMessage("Children's orders must have child-appropriate titles.");
        });

        When(order => order.Category == OrderCategory.Fiction, () =>
        {
            RuleFor(order => order.Author)
                .MinimumLength(5)
                .WithMessage("Fiction orders require author names of at least 5 characters.");
        });

        RuleFor(order => order)
            .Must(order => order.Price <= 100m || order.StockQuantity <= 20)
            .WithMessage("Orders over $100 must have stock quantity of 20 or less.");
    }

    private bool BeValidTitle(string title)
    {
        var containsInappropriateWord = InappropriateWords.Any(word =>
            title.Contains(word, StringComparison.OrdinalIgnoreCase));
        if (containsInappropriateWord)
        {
            logger.LogWarning("Title validation failed due to inappropriate content: {Title}", title);
        }

        return !containsInappropriateWord;
    }

    private async Task<bool> BeUniqueTitle(CreateOrderProfileRequest request, string title, CancellationToken cancellationToken)
    {
        var exists = await context.Orders
            .AnyAsync(
                order => order.Title == title && order.Author == request.Author,
                cancellationToken);

        if (exists)
        {
            logger.LogWarning("Duplicate title detected for author {Author}", request.Author);
        }

        return !exists;
    }

    private bool BeValidAuthorName(string author)
    {
        var isValid = AuthorRegex.IsMatch(author);
        if (!isValid)
        {
            logger.LogWarning("Author validation failed: {Author}", author);
        }

        return isValid;
    }

    private bool BeValidIsbn(string isbn)
    {
        var normalized = NormalizeIsbn(isbn);
        var isValid = (normalized.Length == 10 || normalized.Length == 13) && normalized.All(char.IsDigit);

        if (!isValid)
        {
            logger.LogWarning("ISBN format invalid: {Isbn}", isbn);
        }

        return isValid;
    }

    private async Task<bool> BeUniqueIsbn(string isbn, CancellationToken cancellationToken)
    {
        var exists = await context.Orders.AnyAsync(o => o.ISBN == isbn, cancellationToken);
        if (exists)
        {
            logger.LogWarning("Duplicate ISBN detected: {Isbn}", isbn);
        }

        return !exists;
    }

    private bool BeValidImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            logger.LogWarning("Invalid image URL format: {Url}", url);
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            logger.LogWarning("Image URL must use HTTP/HTTPS: {Url}", url);
            return false;
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var hasValidExtension = allowedExtensions.Any(ext =>
            uri.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        if (!hasValidExtension)
        {
            logger.LogWarning("Image URL missing valid extension: {Url}", url);
        }

        return hasValidExtension;
    }

    private bool ContainTechnicalKeywords(CreateOrderProfileRequest request, string title)
    {
        var containsKeyword = TechnicalKeywords.Any(keyword =>
            title.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (!containsKeyword)
        {
            logger.LogWarning("Technical order missing keywords: {Title}", title);
        }

        return containsKeyword;
    }

    private bool BeAppropriateForChildren(CreateOrderProfileRequest request, string title)
    {
        var containsRestrictedWord = ChildrenRestrictedWords.Any(keyword =>
            title.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (containsRestrictedWord)
        {
            logger.LogWarning("Children's order title contains restricted word: {Title}", title);
        }

        return !containsRestrictedWord;
    }

    private async Task<bool> PassBusinessRules(CreateOrderProfileRequest request, CancellationToken cancellationToken)
    {
        var dailyLimitPassed = await PassDailyOrderLimitAsync(cancellationToken);
        var technicalPricePassed = PassTechnicalMinimumPrice(request);
        var childrenContentPassed = PassChildrenContentRestriction(request);
        var highValueStockPassed = PassHighValueStockLimit(request);

        return dailyLimitPassed && technicalPricePassed && childrenContentPassed && highValueStockPassed;
    }

    private async Task<bool> PassDailyOrderLimitAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var countToday = await context.Orders.CountAsync(order => order.CreatedAt.Date == today, cancellationToken);
        var passed = countToday < 500;

        if (!passed)
        {
            logger.LogWarning("Daily order limit exceeded. Count: {Count}", countToday);
        }
        else
        {
            logger.LogInformation("Daily order limit check passed. Count: {Count}", countToday);
        }

        return passed;
    }

    private bool PassTechnicalMinimumPrice(CreateOrderProfileRequest request)
    {
        if (request.Category != OrderCategory.Technical)
        {
            return true;
        }

        var passed = request.Price >= 20m;
        if (!passed)
        {
            logger.LogWarning("Technical order price below minimum: {Price}", request.Price);
        }

        return passed;
    }

    private bool PassChildrenContentRestriction(CreateOrderProfileRequest request)
    {
        if (request.Category != OrderCategory.Children)
        {
            return true;
        }

        var isAppropriate = BeAppropriateForChildren(request, request.Title);
        if (!isAppropriate)
        {
            logger.LogWarning("Children order content restriction violated: {Title}", request.Title);
        }

        return isAppropriate;
    }

    private bool PassHighValueStockLimit(CreateOrderProfileRequest request)
    {
        if (request.Price <= 500m)
        {
            return true;
        }

        var passed = request.StockQuantity <= 10;
        if (!passed)
        {
            logger.LogWarning("High value order stock limit exceeded. Price: {Price} Stock: {Stock}",
                request.Price,
                request.StockQuantity);
        }

        return passed;
    }

    private static string NormalizeIsbn(string isbn) => isbn.Replace("-", string.Empty).Replace(" ", string.Empty);
}

