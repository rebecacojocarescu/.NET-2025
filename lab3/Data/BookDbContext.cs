using Microsoft.EntityFrameworkCore;
using BookApi.Models;

namespace BookApi.Data
{
    public class BookDbContext : DbContext
    {
        public BookDbContext(DbContextOptions<BookDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed initial data
            modelBuilder.Entity<Book>().HasData(
                new Book { Id = 1, Title = "1984", Author = "George Orwell", Year = 1949 },
                new Book { Id = 2, Title = "To Kill a Mockingbird", Author = "Harper Lee", Year = 1960 },
                new Book { Id = 3, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Year = 1925 },
                new Book { Id = 4, Title = "Pride and Prejudice", Author = "Jane Austen", Year = 1813 },
                new Book { Id = 5, Title = "The Catcher in the Rye", Author = "J.D. Salinger", Year = 1951 },
                new Book { Id = 6, Title = "Brave New World", Author = "Aldous Huxley", Year = 1932 },
                new Book { Id = 7, Title = "Animal Farm", Author = "George Orwell", Year = 1945 },
                new Book { Id = 8, Title = "Lord of the Flies", Author = "William Golding", Year = 1954 },
                new Book { Id = 9, Title = "The Hobbit", Author = "J.R.R. Tolkien", Year = 1937 },
                new Book { Id = 10, Title = "Fahrenheit 451", Author = "Ray Bradbury", Year = 1953 },
                new Book { Id = 11, Title = "Jane Eyre", Author = "Charlotte Bronte", Year = 1847 },
                new Book { Id = 12, Title = "Wuthering Heights", Author = "Emily Bronte", Year = 1847 }
            );
        }
    }
}