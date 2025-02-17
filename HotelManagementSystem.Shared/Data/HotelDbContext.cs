using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Shared.Data
{
    public class HotelDbContext : IdentityDbContext<User, Role, int>
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
            Rooms = Set<Room>();
            RoomTypes = Set<RoomType>();
            Bookings = Set<Booking>();
            Payments = Set<Payment>();
            Amenities = Set<Amenity>();
            Staff = Set<Staff>();
        }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<Staff> Staff { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between Room and Amenity
            modelBuilder.Entity<RoomAmenity>()
                .HasKey(ra => new { ra.RoomId, ra.AmenityId });

            modelBuilder.Entity<RoomAmenity>()
                .HasOne(ra => ra.Room)
                .WithMany(r => r.RoomAmenities)
                .HasForeignKey(ra => ra.RoomId);

            modelBuilder.Entity<RoomAmenity>()
                .HasOne(ra => ra.Amenity)
                .WithMany(a => a.RoomAmenities)
                .HasForeignKey(ra => ra.AmenityId);

            // Add any additional configurations here
            modelBuilder.Entity<User>()
                .HasIndex(e => e.Email)
                .IsUnique(true);

            modelBuilder.Entity<User>()
               .HasIndex(e => e.PhoneNumber)
               .IsUnique(true);

            modelBuilder.Entity<Role>()
                .HasIndex(e => e.Name)
                .IsUnique(true);

            modelBuilder.Entity<Room>()
                .HasIndex(e => e.RoomNumber)
                .IsUnique(true);

            modelBuilder.Entity<RoomType>()
                .HasIndex(e => e.Name)
                .IsUnique(true);

            modelBuilder.Entity<Amenity>()
                .HasIndex(e => e.Name)
                .IsUnique(true);

            modelBuilder.Entity<Staff>()
                .HasIndex(e => e.Email)
                .IsUnique(true);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Administrator" , NormalizedName = "ADMINISTRATOR" },
                new Role { Id = 2, Name = "Guest", NormalizedName = "GUEST" },
                new Role { Id = 3, Name = "Customer", NormalizedName = "CUSTOMER" }
            );

            // Seed Roles with claims
            modelBuilder.Entity<IdentityRoleClaim<int>>().HasData(
                new IdentityRoleClaim<int> { Id = 1, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageUsers" },
                new IdentityRoleClaim<int> { Id = 2, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageRoles" },
                new IdentityRoleClaim<int> { Id = 3, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageRooms" },
                new IdentityRoleClaim<int> { Id = 4, RoleId = 2, ClaimType = "Permission", ClaimValue = "ViewDashboard" },
                new IdentityRoleClaim<int> { Id = 5, RoleId = 3, ClaimType = "Permission", ClaimValue = "MakeBooking" }
            );

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "admin_password"),
                    RoleId = 1, // Administrator role
                    PhoneNumber = "1234567890"
                },
                new User
                {
                    Id = 2,
                    UserName = "guest",
                    NormalizedUserName = "GUEST",
                    Email = "guest@example.com",
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "guest_password"),
                    RoleId = 2, // Guest role
                    PhoneNumber = "0987654321"
                }
            );

            // Seed Users with claims
            modelBuilder.Entity<IdentityUserClaim<int>>().HasData(
                new IdentityUserClaim<int> { Id = 1, UserId = 1, ClaimType = "Permission", ClaimValue = "FullAccess" },
                new IdentityUserClaim<int> { Id = 2, UserId = 1, ClaimType = "Department", ClaimValue = "IT" },
                new IdentityUserClaim<int> { Id = 3, UserId = 2, ClaimType = "Permission", ClaimValue = "LimitedAccess" },
                new IdentityUserClaim<int> { Id = 4, UserId = 2, ClaimType = "Department", ClaimValue = "Sales" }
            );

            // Seed UserRoles
            modelBuilder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int> { UserId = 1, RoleId = 1 }, // Assign admin role to admin user
                new IdentityUserRole<int> { UserId = 2, RoleId = 2 }  // Assign guest role to guest user
            );

            // Modify foreign key constraint to use ON DELETE NO ACTION
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal properties
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RoomType>()
                .Property(rt => rt.BasePrice)
                .HasColumnType("decimal(18,2)");

            // Configure foreign key with no action on delete
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

        }
    }
}
