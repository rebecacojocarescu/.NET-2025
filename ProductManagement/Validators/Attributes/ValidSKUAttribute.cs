using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProductManagement.Validators.Attributes;

public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        if (value is not string sku) return false;
        
        var cleanSku = sku.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(cleanSku, @"^[a-zA-Z0-9\-]{5,20}$");
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.Add("data-val-sku", "SKU must be alphanumeric with hyphens, 5-20 characters");
        context.Attributes.Add("data-val-sku-pattern", @"^[a-zA-Z0-9\-]{5,20}$");
    }
}