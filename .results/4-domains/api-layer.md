# Domain: API Layer

## Overview
The ASP.NET Core Web API (`HotelManagementSystem.API`) is the central data hub. All clients (Blazor Server, Angular) communicate with it exclusively through HTTP via the `ApiClient` class.

---

## Controller Pattern

Controllers use **primary constructor injection**, inherit `ControllerBase`, and are decorated with `[Route("api/[controller]")]` and `[ApiController]`:

```csharp
[Authorize(Roles = "Administrator")]
[Authorize(Policy = "ManageRooms")]
[Route("api/[controller]")]
[ApiController]
public class RoomsController(IRepository<Room> roomRepository) : ControllerBase
{
    private readonly IRepository<Room> _roomRepository = roomRepository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
    {
        var rooms = await _roomRepository.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(int id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        if (room == null) return NotFound();
        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> CreateRoom(Room room)
    {
        await _roomRepository.AddAsync(room);
        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoom(int id, Room room)
    {
        if (id != room.Id) return BadRequest();
        await _roomRepository.UpdateAsync(room);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        await _roomRepository.DeleteAsync(id);
        return NoContent();
    }
}
```

### ProducesResponseType Usage
`RolesController` uses `[ProducesResponseType]` attributes for Swagger documentation:
```csharp
[ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
```

---

## HTTP Response Conventions

| Operation | Return |
|---|---|
| GET list | `Ok(list)` |
| GET by id (found) | `Ok(entity)` |
| GET by id (not found) | `NotFound()` |
| POST | `CreatedAtAction(nameof(GetX), new { id }, entity)` |
| PUT (id mismatch) | `BadRequest()` |
| PUT (success) | `NoContent()` |
| DELETE | `NoContent()` |

---

## ApiClient (Blazor)

Both Blazor projects share an identical `ApiClient` class that wraps `HttpClient` with generic typed methods. The base URL comes from `appsettings.json` key `ApiBaseUrl`:

```csharp
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"];
    }

    public async Task<T> GetAsync<T>(string url) { ... }
    public async Task<T> PostAsync<T>(string url, T data) { ... }
    public async Task<T> PutAsync<T>(string url, T data) { ... }
    public async Task DeleteAsync(string url) { ... }
}
```

Registered as `AddSingleton<ApiClient>()` in `Client/Program.cs`.

---

## Swagger / OpenAPI

API exposes Swagger UI with JWT Bearer security support. Security definition added in `Program.cs`:

```csharp
c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    In = ParameterLocation.Header,
    Description = "Please enter a valid token",
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    BearerFormat = "JWT",
    Scheme = "Bearer"
});
```
