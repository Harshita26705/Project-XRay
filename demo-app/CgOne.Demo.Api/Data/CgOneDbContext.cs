using CgOne.Demo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CgOne.Demo.Api.Data;

public class CgOneDbContext : DbContext
{
    public CgOneDbContext(DbContextOptions<CgOneDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    public DbSet<Order> Orders { get; set; } = null!;

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
}
