# Style Guide: API Clients

## Unique Conventions in This Project

### 1. Identical ApiClient Class in Both Blazor Projects
`HotelManagementSystem.Client/ApiClient.cs` and `HotelManagementSystem.Admin/Data/ApiClient.cs` are **structurally identical** — same methods, same constructor signature. Placement differs (root vs `Data/` folder).

### 2. Base URL from IConfiguration
The API base URL is always read from `IConfiguration["ApiBaseUrl"]` in the constructor — not hardcoded, not from environment variables directly:

```csharp
_apiBaseUrl = configuration["ApiBaseUrl"];
```

### 3. Generic Typed Methods with Full URL Construction
Every method prepends the base URL string with simple concatenation:

```csharp
var response = await _httpClient.GetAsync($"{_apiBaseUrl}{url}");
```

### 4. `response.EnsureSuccessStatusCode()` on Every Call
Every HTTP method calls `EnsureSuccessStatusCode()` immediately — no manual status code checking:

```csharp
response.EnsureSuccessStatusCode();
var responseBody = await response.Content.ReadAsStringAsync();
return JsonSerializer.Deserialize<T>(responseBody);
```

### 5. `JsonSerializer.Deserialize<T>` for GET Responses
GET responses are deserialized using `System.Text.Json.JsonSerializer.Deserialize<T>` — not `response.Content.ReadFromJsonAsync<T>()`.

### 6. Singleton Registration
`ApiClient` is registered as `AddSingleton<ApiClient>()` in the Blazor Client `Program.cs`.
