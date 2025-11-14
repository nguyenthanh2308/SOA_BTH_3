using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options) { }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("products");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");

                e.Property(x => x.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255)
                    .IsRequired();

                e.Property(x => x.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1000);

                e.Property(x => x.Price)
                    .HasColumnName("price")
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

                e.Property(x => x.Quantity)
                    .HasColumnName("quantity")
                    .IsRequired();

                // ⬇️ Map đúng 2 cột thời gian, KHÔNG dùng shadow property
                e.Property(x => x.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                e.Property(x => x.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                    .IsRequired(false); // nullable
                  
            });
        }
    }
}
