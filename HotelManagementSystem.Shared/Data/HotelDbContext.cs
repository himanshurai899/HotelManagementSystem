using HotelManagementSystem.Shared.Enums;
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
            Invoices = Set<Invoice>();
            InvoiceItems = Set<InvoiceItem>();
            CompanyProfiles = Set<CompanyProfile>();
            Tenants = Set<Tenant>();
            UserTenants = Set<UserTenant>();
        }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UserTenant> UserTenants { get; set; }

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
                new Role { Id = 3, Name = "Customer", NormalizedName = "CUSTOMER" },
                new Role { Id = 4, Name = "SuperAdmin", NormalizedName = "SUPERADMIN" }
            );

            // Seed Roles with claims
            modelBuilder.Entity<IdentityRoleClaim<int>>().HasData(
                new IdentityRoleClaim<int> { Id = 1, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageUsers" },
                new IdentityRoleClaim<int> { Id = 2, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageRoles" },
                new IdentityRoleClaim<int> { Id = 3, RoleId = 1, ClaimType = "Permission", ClaimValue = "ManageRooms" },
                new IdentityRoleClaim<int> { Id = 4, RoleId = 2, ClaimType = "Permission", ClaimValue = "ViewDashboard" },
                new IdentityRoleClaim<int> { Id = 5, RoleId = 3, ClaimType = "Permission", ClaimValue = "MakeBooking" },
                // SuperAdmin — every permission
                new IdentityRoleClaim<int> { Id = 6,  RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageUsers" },
                new IdentityRoleClaim<int> { Id = 7,  RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageRoles" },
                new IdentityRoleClaim<int> { Id = 8,  RoleId = 4, ClaimType = "Permission", ClaimValue = "ManagePermissions" },
                new IdentityRoleClaim<int> { Id = 9,  RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageRooms" },
                new IdentityRoleClaim<int> { Id = 10, RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageRoomTypes" },
                new IdentityRoleClaim<int> { Id = 11, RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageAmenities" },
                new IdentityRoleClaim<int> { Id = 12, RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageStaff" },
                new IdentityRoleClaim<int> { Id = 13, RoleId = 4, ClaimType = "Permission", ClaimValue = "ManageBookings" },
                new IdentityRoleClaim<int> { Id = 14, RoleId = 4, ClaimType = "Permission", ClaimValue = "ViewReports" },
                new IdentityRoleClaim<int> { Id = 15, RoleId = 4, ClaimType = "Permission", ClaimValue = "ViewDashboard" },
                new IdentityRoleClaim<int> { Id = 16, RoleId = 4, ClaimType = "Permission", ClaimValue = "MakeBooking" }
            );

            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "admin_password"),
                    RoleId = 1, // Administrator role
                    PhoneNumber = "1234567890",
                    SecurityStamp = "ADMIN-STATIC-STAMP-0001"
                },
                new User
                {
                    Id = 2,
                    UserName = "guest",
                    NormalizedUserName = "GUEST",
                    Email = "guest@example.com",
                    NormalizedEmail = "GUEST@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "guest_password"),
                    RoleId = 2, // Guest role
                    PhoneNumber = "0987654321",
                    SecurityStamp = "GUEST-STATIC-STAMP-0001"
                },
                new User
                {
                    Id = 3,
                    UserName = "superadmin",
                    NormalizedUserName = "SUPERADMIN",
                    Email = "superadmin@example.com",
                    NormalizedEmail = "SUPERADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = new PasswordHasher<User>().HashPassword(null, "SuperAdmin@123"),
                    RoleId = 4, // SuperAdmin role
                    PhoneNumber = "1112223333",
                    SecurityStamp = "SUPERADMIN-STATIC-STAMP-0001"
                }
            );

            // Seed Users with claims
            modelBuilder.Entity<IdentityUserClaim<int>>().HasData(
                new IdentityUserClaim<int> { Id = 1, UserId = 1, ClaimType = "Permission", ClaimValue = "FullAccess" },
                new IdentityUserClaim<int> { Id = 2, UserId = 1, ClaimType = "Department", ClaimValue = "IT" },
                new IdentityUserClaim<int> { Id = 3, UserId = 2, ClaimType = "Permission", ClaimValue = "LimitedAccess" },
                new IdentityUserClaim<int> { Id = 4, UserId = 2, ClaimType = "Department", ClaimValue = "Sales" },
                new IdentityUserClaim<int> { Id = 5, UserId = 3, ClaimType = "Permission", ClaimValue = "FullAccess" },
                new IdentityUserClaim<int> { Id = 6, UserId = 3, ClaimType = "Department", ClaimValue = "Management" }
            );

            // Seed UserRoles
            modelBuilder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int> { UserId = 1, RoleId = 1 }, // admin → Administrator
                new IdentityUserRole<int> { UserId = 2, RoleId = 2 }, // guest → Guest
                new IdentityUserRole<int> { UserId = 3, RoleId = 4 }  // superadmin → SuperAdmin
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

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
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

            // ── Invoice / InvoiceItem ──────────────────────────────────────────
            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Items)
                .WithOne(ii => ii.Invoice)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.Amount)
                .HasColumnType("decimal(18,2)");

            // ── Phase 9: Multi-Tenant SaaS ────────────────────────────────────

            // Tenant — unique subdomain index
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Subdomain)
                .IsUnique();

            // UserTenant — composite PK
            modelBuilder.Entity<UserTenant>()
                .HasKey(ut => new { ut.UserId, ut.TenantId });

            modelBuilder.Entity<UserTenant>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTenants)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserTenant>()
                .HasOne(ut => ut.Tenant)
                .WithMany(t => t.UserTenants)
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // TenantId FK on tenanted entities — NoAction to avoid cascade conflicts
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tenant)
                .WithMany()
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RoomType>()
                .HasOne(rt => rt.Tenant)
                .WithMany()
                .HasForeignKey(rt => rt.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Amenity>()
                .HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Tenant)
                .WithMany()
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            // Seed DefaultTenant — satisfies FK for all existing seeded data
            modelBuilder.Entity<Tenant>().HasData(new Tenant
            {
                Id = 1,
                Name = "Default",
                Subdomain = "default",
                Plan = TenantPlan.Enterprise,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CurrencyCode = "INR",
                Locale = "en-IN"
            });

            // Assign all existing seeded users to DefaultTenant
            modelBuilder.Entity<UserTenant>().HasData(
                new UserTenant { UserId = 1, TenantId = 1, TenantRole = "Administrator", JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserTenant { UserId = 2, TenantId = 1, TenantRole = null,            JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserTenant { UserId = 3, TenantId = 1, TenantRole = "Administrator", JoinedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
