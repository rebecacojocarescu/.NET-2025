using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal min;
    private readonly decimal max;

    public PriceRangeAttribute(double minimum, double maximum)
    {
        min = (decimal)minimum;
        max = (decimal)maximum;
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not IConvertible convertible)
        {
            return false;
        }

        var price = Convert.ToDecimal(convertible);
        return price >= min && price <= max;
    }

    public override string FormatErrorMessage(string name)
    {
        return ErrorMessage ?? $"{name} must be between {min:C2} and {max:C2}.";
    }
}

