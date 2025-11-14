using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OrderService.Models;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ========== customers ==========
        b.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.PasswordMd5).HasColumnName("password_md5").HasMaxLength(32).IsRequired();

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => x.Email).IsUnique();
        });

        // ========== orders ==========
        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");

            // ✅ map đúng cột FK (trùng kiểu trong model: int? → IsRequired(false))
            e.Property(x => x.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired(false);

            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(255);
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);

            // Nếu total_amount do trigger/tính toán DB -> để generated
            e.Property(x => x.TotalAmount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(12,2)")
                .ValueGeneratedOnAddOrUpdate();

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Nếu Order.UpdatedAt là DateTime? trong model:
            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime")
                .IsRequired(false);

            // Quan hệ: many Orders → one Customer
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId)
                .HasConstraintName("fk_orders_customer")
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ========== order_items ==========
        b.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(255);
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(12,2)");

            // total_price là computed column trong MySQL
            e.Property(x => x.TotalPrice)
                .HasColumnName("total_price")
                .HasColumnType("decimal(12,2)")
                .HasComputedColumnSql("`quantity` * `unit_price`", stored: true)
                .ValueGeneratedOnAddOrUpdate();
            e.Property(x => x.TotalPrice).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            e.Property(x => x.TotalPrice).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Nếu OrderItem.UpdatedAt là DateTime? trong model:
            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime")
                .IsRequired(false);

            e.HasOne(x => x.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId)
                .HasConstraintName("fk_order_items_order")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

}
