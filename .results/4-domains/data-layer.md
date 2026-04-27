# Domain: Data Layer

## Overview
All persistence uses **Entity Framework Core** with a **SQL Server** backend, accessed exclusively through a **generic repository pattern** (`IRepository<T>` / `Repository<T>`).

---

## DbContext

`HotelDbContext` extends `IdentityDbContext<User, Role, int>` and declares DbSets for all domain entities:

```csharp
public class HotelDbContext : IdentityDbContext<User, Role, int>
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<Staff> Staff { get; set; }
}
```

### OnModelCreating Conventions
- **Many-to-many**: `RoomAmenity` join entity with composite key `{ RoomId, AmenityId }`
- **Unique indexes**: `User.Email`, `User.PhoneNumber`, `Role.Name`, `Room.RoomNumber`, `RoomType.Name`, `Amenity.Name`, `Staff.Email`
- **Seed data**: Three roles seeded — `Administrator`, `Guest`, `Customer`

---

## Repository Pattern

### Interface
```csharp
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
```

### Implementation
```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly HotelDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(HotelDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<T> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public async Task AddAsync(T entity) { await _dbSet.AddAsync(entity); await _context.SaveChangesAsync(); }
    public async Task UpdateAsync(T entity) { _dbSet.Update(entity); await _context.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var entity = await GetByIdAsync(id); _dbSet.Remove(entity); await _context.SaveChangesAsync(); }
}
```

### Registration
```csharp
// Program.cs (API)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

---

## DTO Mapping (AutoMapper)

All entity ↔ DTO conversions are registered in `MappingProfile.cs`:

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDTO>().ReverseMap();
        CreateMap<Role, RoleDTO>().ReverseMap();
        CreateMap<Room, RoomDTO>().ReverseMap();
        CreateMap<RoomType, RoomTypeDTO>().ReverseMap();
        CreateMap<Booking, BookingDTO>().ReverseMap();
        CreateMap<Payment, PaymentDTO>().ReverseMap();
        CreateMap<Amenity, AmenityDTO>().ReverseMap();
        CreateMap<Staff, StaffDTO>().ReverseMap();
    }
}
```

DTOs are **flat** — navigation properties are replaced by denormalized string fields:

```csharp
// RoomDTO.cs
public class RoomDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } // flattened, not RoomType object
    public bool IsAvailable { get; set; }
}
```

---

## Migrations
EF Core migrations live in `HotelManagementSystem.Shared/Migrations/`. Commands are documented in `MigrationCommands.txt`.
