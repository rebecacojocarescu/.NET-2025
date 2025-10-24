using System.ComponentModel.DataAnnotations;
using ProductManagement.Features.Products;

namespace ProductManagement.Validators.Attributes;

public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowedCategories;

    public ProductCategoryAttribute(params ProductCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories;
    }

    public override bool IsValid(object? value)
    {
        if (value is not ProductCategory category) return false;
        return _allowedCategories.Contains(category);
    }

    public override string FormatErrorMessage(string name)
    {
        var allowedNames = string.Join(", ", _allowedCategories.Select(c => c.ToString()));
        return $"The {name} field must be one of: {allowedNames}";
    }
}