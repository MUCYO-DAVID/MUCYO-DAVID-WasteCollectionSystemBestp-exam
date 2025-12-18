using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Models;

namespace WasteCollectionSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WasteRequest> WasteRequests { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Truck> Trucks { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<GuestCart> GuestCarts { get; set; } = null!;
        public DbSet<GuestCartItem> GuestCartItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WasteRequest → User (string key)
            modelBuilder.Entity<WasteRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.WasteRequests)
                .HasForeignKey(r => r.UserId);

            // Other relationships stay the same
            modelBuilder.Entity<WasteRequest>()
                .HasMany(r => r.Payments)
                .WithOne(p => p.WasteRequest)
                .HasForeignKey(p => p.RequestID);

            // Payments – enforce precision to avoid truncation
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WasteRequest>()
                .HasMany(r => r.Assignments)
                .WithOne(a => a.WasteRequest)
                .HasForeignKey(a => a.RequestID);

            modelBuilder.Entity<Truck>()
                .HasMany(t => t.Assignments)
                .WithOne(a => a.Truck)
                .HasForeignKey(a => a.TruckID);

            // Truck → Driver (ApplicationUser) relationship (many-to-one: many trucks can have one driver)
            modelBuilder.Entity<Truck>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.SetNull); // If driver is deleted, set DriverId to null

            // ApplicationUser → Truck relationship (one-to-one: one driver has one truck)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.AssignedTruck)
                .WithOne(t => t.Driver)
                .HasForeignKey<Truck>(t => t.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            // Truck → CurrentAssignment relationship (optional one-to-one)
            // Using NoAction to avoid cascade path conflicts with Truck -> Assignments relationship
            modelBuilder.Entity<Truck>()
                .HasOne(t => t.CurrentAssignment)
                .WithMany()
                .HasForeignKey(t => t.CurrentAssignmentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Notification → User relationship
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // GuestCart → GuestCartItems relationship
            modelBuilder.Entity<GuestCart>()
                .HasMany(gc => gc.Items)
                .WithOne(gci => gci.GuestCart)
                .HasForeignKey(gci => gci.GuestCartId)
                .OnDelete(DeleteBehavior.Cascade);

            // GuestCartItem → WasteRequest relationship
            modelBuilder.Entity<GuestCartItem>()
                .HasOne(gci => gci.WasteRequest)
                .WithMany()
                .HasForeignKey(gci => gci.WasteRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on SessionId for fast guest cart lookups
            modelBuilder.Entity<GuestCart>()
                .HasIndex(gc => gc.SessionId);
        }
    }
}