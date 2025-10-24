using AutoMapper;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;

namespace ProductManagement.Features.Products.Resolvers;

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var age = DateTime.UtcNow - source.ReleaseDate;
        
        if (age.TotalDays < 30)
            return "New Release";
        if (age.TotalDays < 365)
            return $"{(int)(age.TotalDays / 30)} months old";
        if (age.TotalDays < 1825)
            return $"{(int)(age.TotalDays / 365)} years old";
        if (age.TotalDays == 1825)
            return "Classic";
        
        return $"{(int)(age.TotalDays / 365)} years old";
    }
}