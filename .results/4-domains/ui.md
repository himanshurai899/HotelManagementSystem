# Domain: UI

## Overview
The UI domain spans three separate client applications: an **Angular 19** customer-facing app, a **Blazor Server** customer client, and a **Blazor Server** admin panel. Each targets a different audience but all consume the same ASP.NET Core Web API.

---

## Angular UI (`HotelManagement.UI`)

### Component Structure
Angular components are **standalone** — no NgModules. Each component imports its Angular Material dependencies directly.

```typescript
// nav-bar.component.ts
@Component({
  selector: 'app-nav-bar',
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent {
  menus: Menu[] = menus
}
```

### Angular Material Usage
Angular Material is the UI component library. Modules are imported per component:
- `MatToolbarModule` — top navigation bar
- `MatButtonModule` — buttons
- `MatIconModule` — icons

### App Bootstrap
```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync()
  ]
};
```

### Navigation Model
Navigation menu items are typed and defined in `model/menus.ts`:
```typescript
export interface Menu {
    route: string;
    title: string;
    description: string;
}

export const menus: Menu[] = [
    { route: '', title: 'Dashboard', description: 'Dashboard Panel' }
];
```

---

## Blazor Server Client (`HotelManagementSystem.Client`)

### Page Pattern
Pages use the `@page` directive, inject `ApiClient`, and load data in `OnInitializedAsync`:

```razor
@page "/rooms"
@using HotelManagementSystem.Shared.Models
@inject ApiClient ApiClient

@if (rooms == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <ul>
        @foreach (var room in rooms)
        {
            <li>@room.RoomNumber</li>
        }
    </ul>
}

@code {
    private Room[]? rooms;

    protected override async Task OnInitializedAsync()
    {
        rooms = await ApiClient.GetAsync<Room[]>("rooms");
    }
}
```

### Layout
- `MainLayout.razor` wraps all pages
- `NavMenu.razor` provides the sidebar navigation
- Colocated CSS: `MainLayout.razor.css`, `NavMenu.razor.css`

---

## Blazor Server Admin (`HotelManagementSystem.Admin`)

Older Blazor Server scaffold using `_Host.cshtml` / `_Layout.cshtml` pattern (pre-.NET 8 style). Layout is in the `Shared/` folder, pages in `Pages/`.

```razor
@* Index.razor *@
@page "/"
<PageTitle>Index</PageTitle>
<h1>Hello, world!</h1>
Welcome to your new app.
<SurveyPrompt Title="How is Blazor working for you?" />
```
