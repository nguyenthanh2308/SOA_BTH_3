// Data/ReportDbContext.cs
using Microsoft.EntityFrameworkCore;
using ReportService.Models;

namespace ReportService.Data
{
    public class ReportDbContext : DbContext
    {
        public ReportDbContext(DbContextOptions<ReportDbContext> options) 
            : base(options) { }

        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportDetail> ReportDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Period).IsRequired();
                entity.Property(e => e.GeneratedAt).IsRequired();
                entity.HasMany(e => e.Details)
                      .WithOne(d => d.Report)
                      .HasForeignKey(d => d.ReportId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReportDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).HasMaxLength(100);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            });
        }
    }
}