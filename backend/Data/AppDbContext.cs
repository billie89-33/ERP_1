using Microsoft.EntityFrameworkCore;
using JamineERP.Backend.Models;

namespace JamineERP.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Quotation> Quotations { get; set; } = null!;
    public DbSet<QuotationItem> QuotationItems { get; set; } = null!;
    public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure JSONB for Product.Specifications
        modelBuilder.Entity<Product>()
            .Property(p => p.Specifications)
            .HasColumnType("jsonb");

        // Unique constraints (From ERD)
        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        modelBuilder.Entity<Quotation>().HasIndex(q => q.QuoteNumber).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasIndex(s => s.OrderNumber).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.PoNumber).IsUnique();

        // Global Query Filter for Soft Delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Quotation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QuotationItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SalesOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SalesOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => !e.IsDeleted);
    }
}
