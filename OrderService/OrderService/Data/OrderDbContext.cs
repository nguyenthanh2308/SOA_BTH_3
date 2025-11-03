using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ===== orders =====
        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);

            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(255);
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(255);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);

            // Nếu total_amount do DB/trigger tính:
            e.Property(x => x.TotalAmount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(12,2)")
                .ValueGeneratedOnAddOrUpdate();

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime")
                .IsRequired(false); // ✅ phù hợp với DateTime?
        });

        // ===== order_items =====
        b.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);

            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(255);
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(12,2)");

            // total_price là cột computed trong MySQL
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

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime")
                .IsRequired(false); // ✅ phù hợp với DateTime?

            e.HasOne(x => x.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
