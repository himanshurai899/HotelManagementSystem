# Style Guide: DTOs

## Unique Conventions in This Project

### 1. Flat DTOs — No Nested Navigation Objects
DTOs never contain navigation property objects. Instead, string fields are added with the related entity's key display value:

```csharp
// ✅ Flat DTO pattern
public class RoomDTO
{
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } // To avoid complex object graph
}

// ❌ Not used
public class RoomDTO
{
    public RoomTypeDTO RoomType { get; set; }
}
```

### 2. Comment on Denormalized Fields
Denormalized string fields are annotated with a comment: `// To avoid complex object graph`

### 3. DTOs Mirror Entity Structure Otherwise
All scalar fields from the entity are replicated in the DTO with identical names and types (no renaming, no omitting scalar fields unless intentional like password).

### 4. Enum Types Preserved in DTOs
Status enums (`BookingStatus`, `PaymentStatus`) are used directly in DTOs — not converted to strings or ints:

```csharp
public BookingStatus Status { get; set; }
```

### 5. Auth DTOs are Simple Flat Classes
`LoginDTO` and `RegisterDTO` contain only the minimal fields needed for the operation — no base classes, no validation attributes in the codebase:

```csharp
public class LoginDTO
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
```
