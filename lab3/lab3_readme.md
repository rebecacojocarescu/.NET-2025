# Lab3 - Book API

## What I implemented

- Book Model (Id, Title, Author, Year)
- CRUD operations:
  - GET /api/books - all books
  - GET /api/books/{id} - book by ID
  - GET /api/books/paginated?page=1&pageSize=10 - with pagination
  - POST /api/books - create book
  - PUT /api/books/{id} - update book
  - DELETE /api/books/{id} - delete book
- FluentValidation for validation
- Entity Framework Core InMemory database

## Tricky Question

**What happens if you apply pagination after materializing the query with .ToList()? Why is this problematic for large datasets?**

If you use .ToList() BEFORE pagination:

```csharp
var books = context.Books.ToList()
    .Skip((page - 1) * pageSize)
    .Take(pageSize);
```

**Problem:**
- All books from the database are loaded into memory
- If you have 1 million books, all are loaded into RAM (~500MB)
- Very poor performance
- Risk of OutOfMemoryException

**Correct solution:**

```csharp
var books = await context.Books
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

- Pagination happens at SQL level
- Database returns only 10 books
- Minimal memory consumption
- Fast and scalable

**Difference:** For 1 million books, wrong approach = 500MB RAM, correct approach = 5KB RAM