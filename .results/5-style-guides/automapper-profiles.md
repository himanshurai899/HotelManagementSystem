# Style Guide: AutoMapper Profiles

## Unique Conventions in This Project

### 1. All Maps Registered in a Single Profile Class
`MappingProfile` (in `HotelManagementSystem.API/Profiles/`) contains every entity ↔ DTO map in one class — no split profiles:

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDTO>().ReverseMap();
        CreateMap<Role, RoleDTO>().ReverseMap();
        // ... all other maps
    }
}
```

### 2. .ReverseMap() on Every Registration
All mappings use `.ReverseMap()` so both directions (entity → DTO and DTO → entity) are registered with a single line.

### 3. Registered via Assembly Scanning
AutoMapper is registered in `Program.cs` pointing at `MappingProfile`'s assembly:

```csharp
builder.Services.AddAutoMapper(typeof(MappingProfile));
```
