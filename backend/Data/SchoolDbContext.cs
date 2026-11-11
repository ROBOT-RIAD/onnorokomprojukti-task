using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class SchoolDbContext : DbContext
{
    public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
    {
    }

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<SeatHold> Holds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.TotalSeats).IsRequired();
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.HasKey(h => h.HoldId);
            entity.Property(h => h.ClassId).IsRequired().HasMaxLength(50);
            entity.Property(h => h.StudentId).IsRequired().HasMaxLength(20);
            entity.Property(h => h.Status).IsRequired().HasMaxLength(20);

            entity.HasIndex(h => new { h.ClassId, h.Status });
            entity.HasIndex(h => h.StudentId);
            entity.HasIndex(h => h.ExpiresAt);

            entity.HasOne(h => h.Class)
                  .WithMany(c => c.Holds)
                  .HasForeignKey(h => h.ClassId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
