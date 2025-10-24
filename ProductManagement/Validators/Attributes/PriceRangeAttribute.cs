using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Validators.Attributes;

public class PriceRangeAttribute : ValidationAttribute
{
    private readonly double _minPrice;
    private readonly double _maxPrice;

    public PriceRangeAttribute(double minPrice, double maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override bool IsValid(object? value)
    {
        if (value is not decimal price) return false;
        return price >= (decimal)_minPrice && price <= (decimal)_maxPrice;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field must be between {_minPrice:C} and {_maxPrice:C}";
    }
}