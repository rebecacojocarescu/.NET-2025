using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using ProductManagement.Common.Logging;
using ProductManagement.Common.Mapping;
using ProductManagement.Common.Middleware;
using ProductManagement.Features.Products;
using ProductManagement.Features.Products.DTOs;
using ProductManagement.Middleware;
using ProductManagement.Persistence;
using ProductManagement.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Management API",
        Version = "v1",
        Description = "API for managing products with advanced AutoMapper patterns, structured logging, and complex validation.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
});

// Database
builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));

// AutoMapper
builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile), typeof(Program));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductProfileValidator>();

// Memory Cache
builder.Services.AddMemoryCache();

// Handlers
builder.Services.AddScoped<CreateProductHandler>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management API V1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

// Middleware
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Product endpoints
app.MapPost("/products", async (CreateProductProfileRequest req, CreateProductHandler handler) =>
    await handler.Handle(req));

app.Run();