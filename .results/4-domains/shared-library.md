# Domain: Shared Library

## Overview
`HotelManagementSystem.Shared` is the single cross-cutting class library referenced by all other projects. It contains entity models, DTOs, enums, the EF Core `DbContext`, migrations, the generic repository, interfaces, and utilities.

---

## Entity Models

All entity models are plain C# classes with:
- `int Id` primary key
- Navigation properties (full entity references) for relationships
- Foreign key `int` properties (e.g., `RoomTypeId`, `UserId`)

```csharp
public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; }
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; }
    public bool IsAvailable { get; set; }
    public ICollection<Booking> Bookings { get; set; }
    public ICollection<RoomAmenity> RoomAmenities { get; set; }
}
```

```csharp
public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
    public ICollection<Payment> Payments { get; set; }
}
```

`User` extends `IdentityUser<int>` and overrides `UserName` to auto-set `NormalizedUserName`:
```csharp
public class User : IdentityUser<int>
{
    private string _userName;
    public override string UserName
    {
        get => _userName;
        set { _userName = value; NormalizedUserName = value?.ToUpperInvariant(); }
    }
    public ICollection<Booking> Bookings { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; }
}
```

---

## DTOs

DTOs are flat data transfer objects — no navigation properties, only primitive fields. Denormalized string fields represent foreign entity names:

```csharp
public class BookingDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }  // denormalized
    public int RoomId { get; set; }
    public string RoomNumber { get; set; }  // denormalized
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
}
```

---

## Enums

Status enums located in `Enums/`:

```csharp
public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }
public enum PaymentStatus { /* Pending, Paid, Refunded etc. */ }
```

---

## Utilities

Static utility classes in `Utilities/`:

```csharp
public static class PasswordHasher
{
    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public static bool VerifyPassword(string password, string hashedPassword) => BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}
```
