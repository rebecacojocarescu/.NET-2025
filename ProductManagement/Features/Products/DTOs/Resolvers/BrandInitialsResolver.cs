using AutoMapper;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;

namespace ProductManagement.Features.Products.Resolvers;

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source.Brand))
            return "?";
        
        var words = source.Brand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 1)
            return words[0][0].ToString().ToUpper();
        
        if (words.Length >= 2)
            return (words[0][0].ToString() + words[^1][0].ToString()).ToUpper();
        
        return "?";
    }
}