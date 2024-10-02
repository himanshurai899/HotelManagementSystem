using HotelManagementSystem.Shared.Models;
using HotelManagementSystem.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Shared.Data
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
            Users = Set<User>();
            Roles = Set<Role>();
            Rooms = Set<Room>();
            RoomTypes = Set<RoomType>();
            Bookings = Set<Booking>();
            Payments = Set<Payment>();
            Amenities = Set<Amenity>();
            Staff = Set<Staff>();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
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
                new Role { Id = 1, Name = "Administrator" },
                new Role { Id = 2, Name = "Guest" },
                new Role { Id = 3, Name = "Customer" }
            );

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    UserName = "admin",
                    Email = "admin@example.com",
                    Password = PasswordHasher.HashPassword("admin_password"), // Hash the password
                    RoleId = 1 // Administrator role
                },
                new User
                {
                    Id = 2,
                    UserName = "guest",
                    Email = "guest@example.com",
                    Password = PasswordHasher.HashPassword("guest_password"), // Hash the password
                    RoleId = 2 // Guest role
                }
            );
        }
    }
}
