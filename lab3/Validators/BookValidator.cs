using FluentValidation;
using BookApi.Models;

namespace BookApi.Validators
{
    public class BookValidator : AbstractValidator<Book>
    {
        public BookValidator()
        {
            RuleFor(book => book.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(book => book.Author)
                .NotEmpty().WithMessage("Author is required.")
                .MaximumLength(100).WithMessage("Author name cannot exceed 100 characters.");

            RuleFor(book => book.Year)
                .GreaterThan(0).WithMessage("Year must be greater than 0.")
                .LessThanOrEqualTo(DateTime.Now.Year).WithMessage("Year cannot be in the future.");
        }
    }
}