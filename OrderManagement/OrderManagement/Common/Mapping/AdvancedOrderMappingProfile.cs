using AutoMapper;
using OrderManagement.Features.Orders;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Requests;
using OrderManagement.Features.Orders.Resolvers;

namespace OrderManagement.Common.Mapping;

public class AdvancedOrderMappingProfile : Profile
{
    public AdvancedOrderMappingProfile()
    {
        CreateMap<CreateOrderProfileRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<Order, OrderProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => OrderMappingHelpers.GetEffectivePrice(src)))
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
            .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>())
            .ForMember(
                dest => dest.CoverImageUrl,
                opt => opt.MapFrom(src => src.Category == OrderCategory.Children ? null : src.CoverImageUrl));
    }
}

internal static class OrderMappingHelpers
{
    private const decimal ChildrenDiscountFactor = 0.9m;

    public static decimal GetEffectivePrice(Order order)
    {
        return order.Category == OrderCategory.Children
            ? decimal.Multiply(order.Price, ChildrenDiscountFactor)
            : order.Price;
    }
}

