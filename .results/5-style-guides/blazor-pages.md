# Style Guide: Blazor Pages

## Unique Conventions in This Project

### 1. ApiClient Injected via @inject
Blazor pages consume data exclusively by injecting `ApiClient` with `@inject`:

```razor
@inject ApiClient ApiClient
```

No direct repository or DbContext injection in pages.

### 2. Null-Check Loading Pattern
All data-bound arrays are nullable and the template shows a loading placeholder until data arrives:

```razor
@if (rooms == null)
{
    <p><em>Loading...</em></p>
}
else
{
    @foreach (var room in rooms) { ... }
}
```

### 3. Data Loaded in OnInitializedAsync
All data fetching happens in the `OnInitializedAsync` lifecycle override in `@code { }`:

```csharp
@code {
    private Room[]? rooms;

    protected override async Task OnInitializedAsync()
    {
        rooms = await ApiClient.GetAsync<Room[]>("rooms");
    }
}
```

### 4. PageTitle Component Used
Every page sets a browser tab title via `<PageTitle>`:

```razor
<PageTitle>Rooms</PageTitle>
```

### 5. Using Directives at the Top of the Page
Namespace imports are declared with `@using` at the top of the page file:

```razor
@using HotelManagementSystem.Shared.Models
```
