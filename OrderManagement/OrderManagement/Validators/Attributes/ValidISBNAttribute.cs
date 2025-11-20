using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OrderManagement.Validators.Attributes;

public class ValidISBNAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var normalized = Normalize(value.ToString()!);
        return (normalized.Length == 10 || normalized.Length == 13) && normalized.All(char.IsDigit);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-validisbn", ErrorMessage ?? "ISBN must be 10 or 13 digits.");
    }

    private static string Normalize(string value) => value.Replace("-", string.Empty).Replace(" ", string.Empty);

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}

