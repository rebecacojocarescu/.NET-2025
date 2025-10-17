using Microsoft.EntityFrameworkCore;
using BookApi.Data;
using BookApi.Validators;
using FluentValidation;
using BookApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure DbContext with InMemory database
builder.Services.AddDbContext<BookDbContext>(options =>
    options.UseInMemoryDatabase("BooksDb"));

// Register FluentValidation
builder.Services.AddScoped<IValidator<Book>, BookValidator>();

// Add OpenAPI (for .NET 9)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();