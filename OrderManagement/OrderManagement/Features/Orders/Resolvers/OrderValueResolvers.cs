using System.Globalization;
using AutoMapper;
using OrderManagement.Common.Mapping;
using OrderManagement.Features.Orders.DTOs;

namespace OrderManagement.Features.Orders.Resolvers;

public class CategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            OrderCategory.Fiction => "Fiction & Literature",
            OrderCategory.NonFiction => "Non-Fiction",
            OrderCategory.Technical => "Technical & Professional",
            OrderCategory.Children => "Children's Orders",
            _ => "Uncategorized"
        };
    }
}

public class PriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        var price = OrderMappingHelpers.GetEffectivePrice(source);
        return price.ToString("C2", CultureInfo.CurrentCulture);
    }
}

public class PublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        var daysSincePublished = (DateTime.UtcNow - source.PublishedDate).TotalDays;

        if (daysSincePublished < 0)
        {
            return "Releases Soon";
        }

        if (daysSincePublished < 30)
        {
            return "New Release";
        }

        if (daysSincePublished < 365)
        {
            var months = Math.Max(1, (int)Math.Floor(daysSincePublished / 30));
            return $"{months} months old";
        }

        if (daysSincePublished < 1825)
        {
            var years = Math.Max(1, (int)Math.Floor(daysSincePublished / 365));
            return $"{years} years old";
        }

        return "Classic";
    }
}

public class AuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Author))
        {
            return "?";
        }

        var parts = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0][0].ToString().ToUpperInvariant();
        }

        var first = parts.First()[0];
        var last = parts.Last()[0];
        return $"{char.ToUpperInvariant(first)}{char.ToUpperInvariant(last)}";
    }
}


public class AvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock";
        }

        if (source.StockQuantity <= 0)
        {
            return "Unavailable";
        }

        return source.StockQuantity switch
        {
            1 => "Last Copy",
            <= 5 => "Limited Stock",
            > 5 => "In Stock"
        };
    }
}

