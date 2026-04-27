# Style Guide: DB Context

## Unique Conventions in This Project

### 1. Extends IdentityDbContext with Typed User and Role
`HotelDbContext` uses the generic `IdentityDbContext<User, Role, int>` — the int type parameter means all Identity tables use integer primary keys:

```csharp
public class HotelDbContext : IdentityDbContext<User, Role, int>
```

### 2. DbSets Initialized in Constructor via Set<T>()
Every DbSet is explicitly initialized in the constructor body:

```csharp
public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
{
    Rooms = Set<Room>();
    RoomTypes = Set<RoomType>();
    // ...
}
```

### 3. Unique Index for Every Meaningful String Property
`OnModelCreating` adds `.HasIndex().IsUnique(true)` for every business-key string: `User.Email`, `User.PhoneNumber`, `Role.Name`, `Room.RoomNumber`, `RoomType.Name`, `Amenity.Name`, `Staff.Email`.

### 4. Seed Data for Roles in OnModelCreating
Roles are seeded directly in `OnModelCreating` using `HasData()` with explicit integer IDs:

```csharp
modelBuilder.Entity<Role>().HasData(
    new Role { Id = 1, Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
    new Role { Id = 2, Name = "Guest", NormalizedName = "GUEST" },
    new Role { Id = 3, Name = "Customer", NormalizedName = "CUSTOMER" }
);
```

### 5. Many-to-Many via Explicit Join Entity
The `Room` ↔ `Amenity` relationship is modelled via an explicit `RoomAmenity` join entity with a composite PK, not EF Core's automatic skip navigation:

```csharp
modelBuilder.Entity<RoomAmenity>()
    .HasKey(ra => new { ra.RoomId, ra.AmenityId });
```
