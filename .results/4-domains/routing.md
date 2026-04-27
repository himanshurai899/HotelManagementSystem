# Domain: Routing

## Overview
Two routing mechanisms are used: **Angular Router** for the Angular UI, and **Blazor file-based routing** (via `@page` directives) for both Blazor projects.

---

## Angular Routing

Routes are defined in `app.routes.ts` as a flat `Routes` array. Components are imported directly — no lazy loading is present.

```typescript
// app.routes.ts
import { Routes } from '@angular/router';
import { DashboardComponent } from './component/dashboard/dashboard.component';
import { ErrorComponent } from './component/error/error.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'error', component: ErrorComponent }
];
```

Routes are registered via `provideRouter(routes)` in `app.config.ts`.

Navigation links use the `RouterLink` directive (imported per standalone component).

---

## Blazor Routing

Each Blazor page declares its own route using the `@page` directive:

```razor
@page "/rooms"
```

The router is bootstrapped in `Components/Routes.razor` (Client) and `App.razor` (Admin). Routes map 1:1 to `.razor` files under `Components/Pages/` (Client) or `Pages/` (Admin).

### Current Blazor Client Routes
| Route | Component |
|---|---|
| `/` | `Home.razor` |
| `/rooms` | `Rooms.razor` |
| `/counter` | `Counter.razor` |
| `/weather` | `Weather.razor` |
