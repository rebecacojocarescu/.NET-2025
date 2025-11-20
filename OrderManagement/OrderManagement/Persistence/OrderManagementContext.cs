using Microsoft.EntityFrameworkCore;
using OrderManagement.Features.Orders;

namespace OrderManagement.Persistence;

public class OrderManagementContext(DbContextOptions<OrderManagementContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

