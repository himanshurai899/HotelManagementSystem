# Style Guide: Entity Models

## Unique Conventions in This Project

### 1. `int Id` Primary Key on All Entities
Every entity model uses `int Id` (not `Guid`, not `long`, not `{EntityName}Id`):

```csharp
public int Id { get; set; }
```

### 2. Navigation Properties + Foreign Key Int Side-by-Side
Both the FK integer and the navigation property are declared together:

```csharp
public int RoomTypeId { get; set; }
public RoomType RoomType { get; set; }
```

### 3. Collections Use `ICollection<T>`
One-to-many relationships use `ICollection<T>` (not `List<T>`, `IEnumerable<T>`, or `IQueryable<T>`):

```csharp
public ICollection<Booking> Bookings { get; set; }
public ICollection<RoomAmenity> RoomAmenities { get; set; }
```

### 4. Status Fields Use Project Enums
Booking and payment status use the enums from `HotelManagementSystem.Shared.Enums`, not raw strings or magic numbers:

```csharp
public BookingStatus Status { get; set; }
public PaymentStatus Status { get; set; }
```

### 5. User Overrides IdentityUser with Auto-Normalization
`User` extends `IdentityUser<int>` and overrides `UserName` to automatically keep `NormalizedUserName` in sync:

```csharp
public override string UserName
{
    get => _userName;
    set { _userName = value; NormalizedUserName = value?.ToUpperInvariant(); }
}
```

### 6. Comment in Model File
Each model file has a comment at the top of the class body identifying the class:
```csharp
// Room.cs
public class Room { ... }
```
