using System.ComponentModel.DataAnnotations;
using OrderManagement.Features.Orders;

namespace OrderManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OrderCategoryAttribute : ValidationAttribute
{
    private readonly HashSet<OrderCategory> allowedCategories;

    public OrderCategoryAttribute(params OrderCategory[] categories)
    {
        allowedCategories = categories.Any()
            ? categories.ToHashSet()
            : Enum.GetValues<OrderCategory>().ToHashSet();
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is OrderCategory category)
        {
            return allowedCategories.Contains(category);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        var allowedList = string.Join(", ", allowedCategories);
        return ErrorMessage ?? $"{name} must be one of: {allowedList}.";
    }
}

