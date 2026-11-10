using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventCategory> EventCategories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map User entity to [User] table explicitly to avoid SQL reserved keyword conflicts
        modelBuilder.Entity<User>().ToTable("User");

        // Legacy single category on Event is optional now (kept so old data still loads)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // An event owns many categories
        modelBuilder.Entity<EventCategory>(b =>
        {
            b.Property(c => c.Name).IsRequired().HasMaxLength(100);
            b.Property(c => c.Distance).IsRequired().HasMaxLength(50);
            b.Property(c => c.EntryFee).HasColumnType("decimal(18,2)");

            b.HasOne(c => c.Event)
                .WithMany(e => e.EventCategories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // An enrolment remembers the chosen category.
        // Restrict (no cascade) avoids SQL Server "multiple cascade paths" errors.
        modelBuilder.Entity<Enrolment>(b =>
        {
            b.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Confirmed");

            b.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
