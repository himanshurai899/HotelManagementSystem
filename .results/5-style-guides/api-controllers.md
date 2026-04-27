# Style Guide: API Controllers

## Unique Conventions in This Project

### 1. Primary Constructor Injection
Controllers use C# primary constructor syntax — **not** field declarations followed by a constructor body:

```csharp
// ✅ This project's pattern
public class RoomsController(IRepository<Room> roomRepository) : ControllerBase
{
    private readonly IRepository<Room> _roomRepository = roomRepository;
}

// ❌ Not used in this project
public class RoomsController : ControllerBase
{
    private readonly IRepository<Room> _roomRepository;
    public RoomsController(IRepository<Room> roomRepository)
    {
        _roomRepository = roomRepository;
    }
}
```

### 2. IRepository<T> as the Only Dependency
Controllers always inject `IRepository<EntityType>` directly — no service layer, no unit of work, no custom repository interfaces beyond the generic one.

### 3. Dual [Authorize] Attributes for Role + Policy
When a controller requires both role and policy authorization, two separate `[Authorize]` attributes are stacked:

```csharp
[Authorize(Roles = "Administrator")]
[Authorize(Policy = "ManageRooms")]
public class RoomsController : ControllerBase { ... }
```

### 4. POST Returns CreatedAtAction with Nameof
```csharp
return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
```

### 5. ProducesResponseType on Read-Only Endpoints
Read-only controllers (like `RolesController`) annotate action results with `[ProducesResponseType]`:
```csharp
[ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
```
