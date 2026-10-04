using InventoryManagementApi.Data.Entities;
using InventoryManagementApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // Categories
        // =========================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(c => c.Name)
                .IsUnique();

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.CreatedAt)
                .IsRequired();
        });

        // =========================
        // Products
        // =========================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(p => p.Sku)
                .IsUnique();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Description)
                .HasMaxLength(500);

            entity.Property(p => p.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.QuantityInStock)
                .IsRequired();

            entity.Property(p => p.ReorderLevel)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Products_UnitPrice",
                    "\"UnitPrice\" >= 0");

                t.HasCheckConstraint(
                    "CK_Products_QuantityInStock",
                    "\"QuantityInStock\" >= 0");

                t.HasCheckConstraint(
                    "CK_Products_ReorderLevel",
                    "\"ReorderLevel\" >= 0");
            });
        });

        // =========================
        // StockTransactions
        // =========================
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(st => st.Id);

            entity.Property(st => st.Type)
                .IsRequired();

            entity.Property(st => st.Quantity)
                .IsRequired();

            entity.Property(st => st.Note)
                .HasMaxLength(250);

            entity.Property(st => st.TransactionDate)
                .IsRequired();

            entity.Property(st => st.Source)
                .IsRequired()
                .HasDefaultValue(StockTransactionSource.Api);

            entity.HasOne(st => st.Product)
                .WithMany(p => p.StockTransactions)
                .HasForeignKey(st => st.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_StockTransactions_Quantity",
                    "\"Quantity\" <> 0");
            });
        });

        // =========================
        // Seed Data
        // =========================

        var seedDate = new DateTime(
            2026, 1, 1, 0, 0, 0,
            DateTimeKind.Utc);

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1001,
                Name = "Electronics",
                Description = "Electronic devices and accessories",
                CreatedAt = seedDate
            },
            new Category
            {
                Id = 1002,
                Name = "Office Supplies",
                Description = "Office and stationery products",
                CreatedAt = seedDate
            },
            new Category
            {
                Id = 1003,
                Name = "Networking",
                Description = "Networking devices and equipment",
                CreatedAt = seedDate
            }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1001,
                CategoryId = 1001,
                Sku = "ELEC-001",
                Name = "Wireless Mouse",
                Description = "2.4 GHz wireless mouse",
                UnitPrice = 150000m,
                QuantityInStock = 20,
                ReorderLevel = 5,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1002,
                CategoryId = 1001,
                Sku = "ELEC-002",
                Name = "Mechanical Keyboard",
                Description = "USB mechanical keyboard",
                UnitPrice = 650000m,
                QuantityInStock = 15,
                ReorderLevel = 5,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1003,
                CategoryId = 1001,
                Sku = "ELEC-003",
                Name = "USB Headset",
                Description = "USB headset with microphone",
                UnitPrice = 325000m,
                QuantityInStock = 12,
                ReorderLevel = 4,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1004,
                CategoryId = 1002,
                Sku = "OFF-001",
                Name = "A4 Paper",
                Description = "A4 paper 80 gsm",
                UnitPrice = 65000m,
                QuantityInStock = 50,
                ReorderLevel = 10,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1005,
                CategoryId = 1002,
                Sku = "OFF-002",
                Name = "Ballpoint Pen",
                Description = "Blue ballpoint pen",
                UnitPrice = 5000m,
                QuantityInStock = 100,
                ReorderLevel = 20,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1006,
                CategoryId = 1002,
                Sku = "OFF-003",
                Name = "Notebook",
                Description = "A5 ruled notebook",
                UnitPrice = 25000m,
                QuantityInStock = 40,
                ReorderLevel = 10,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1007,
                CategoryId = 1003,
                Sku = "NET-001",
                Name = "WiFi Router",
                Description = "Dual band wireless router",
                UnitPrice = 550000m,
                QuantityInStock = 8,
                ReorderLevel = 3,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1008,
                CategoryId = 1003,
                Sku = "NET-002",
                Name = "Network Switch",
                Description = "8 port gigabit switch",
                UnitPrice = 475000m,
                QuantityInStock = 6,
                ReorderLevel = 2,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1009,
                CategoryId = 1003,
                Sku = "NET-003",
                Name = "Ethernet Cable",
                Description = "CAT6 ethernet cable",
                UnitPrice = 45000m,
                QuantityInStock = 30,
                ReorderLevel = 10,
                CreatedAt = seedDate
            },
            new Product
            {
                Id = 1010,
                CategoryId = 1001,
                Sku = "ELEC-004",
                Name = "USB Hub",
                Description = "4 port USB hub",
                UnitPrice = 125000m,
                QuantityInStock = 18,
                ReorderLevel = 5,
                CreatedAt = seedDate
            }
        );

        modelBuilder.Entity<StockTransaction>().HasData(
            new StockTransaction
            {
                Id = 1001,
                ProductId = 1001,
                Type = StockTransactionType.In,
                Quantity = 20,
                Note = "Initial stock",
                TransactionDate = seedDate,
                Source = StockTransactionSource.Api
            },
            new StockTransaction
            {
                Id = 1002,
                ProductId = 1002,
                Type = StockTransactionType.In,
                Quantity = 15,
                Note = "Initial stock",
                TransactionDate = seedDate,
                Source = StockTransactionSource.Api
            },
            new StockTransaction
            {
                Id = 1003,
                ProductId = 1004,
                Type = StockTransactionType.In,
                Quantity = 50,
                Note = "Initial stock",
                TransactionDate = seedDate,
                Source = StockTransactionSource.Api
            },
            new StockTransaction
            {
                Id = 1004,
                ProductId = 1007,
                Type = StockTransactionType.In,
                Quantity = 10,
                Note = "Initial stock",
                TransactionDate = seedDate,
                Source = StockTransactionSource.Api
            },
            new StockTransaction
            {
                Id = 1005,
                ProductId = 1007,
                Type = StockTransactionType.Out,
                Quantity = 2,
                Note = "Initial sale",
                TransactionDate = seedDate.AddHours(1),
                Source = StockTransactionSource.Api
            }
        );
    }
}