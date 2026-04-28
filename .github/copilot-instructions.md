# GitHub Copilot Instructions — Hotel Management System

## Overview

This file enables AI coding assistants to generate features that are consistent with the architecture, conventions, and patterns of this codebase. All guidance here is based **exclusively on observed patterns** in the actual source code — no external best practices are imposed.

This is a **.NET 8 + Angular 19** hotel management platform with four projects:

| Project | Type | Purpose |
|---|---|---|
| `HotelManagementSystem.API` | ASP.NET Core Web API | Central REST API, JWT auth, Swagger |
| `HotelManagementSystem.Shared` | Class Library | Models, DTOs, EF Core, Migrations, Repositories |
| `HotelManagementSystem.Client` | Blazor Server | Customer-facing web app (ignored — UI work in Angular) |
| `HotelManagementSystem.Admin` | Blazor Server | Admin panel (ignored — UI work in Angular) |
| `HotelManagement.UI` | Angular 19 SPA | **Primary UI** — Angular + Material, full RBAC, JWT auth |

---

## Implementation Status

### ✅ Phase 1 — Core Infrastructure
| File | Purpose |
|---|---|
| `src/environments/environment.ts` | Production API base URL |
| `src/environments/environment.development.ts` | Dev API base URL → `https://localhost:7204/api` |
| `src/app/services/auth.service.ts` | JWT decode, localStorage, `isLoggedIn()`, `hasRole()`, `hasClaim()`, 5-min expiry signal |
| `src/app/services/api.service.ts` | Generic `get/post/put/delete` HTTP wrapper using `environment.apiBaseUrl` |
| `src/app/interceptors/auth.interceptor.ts` | Functional interceptor — attaches `Authorization: Bearer <token>` |
| `src/app/guards/auth.guard.ts` | Redirects unauthenticated users to `/login` |
| `src/app/guards/role.guard.ts` | Reads `route.data.roles[]`; redirects to `/unauthorized` if denied |
| `src/app/app.config.ts` | `provideHttpClient(withInterceptors([authInterceptor]))` wired |
| `src/app/model/menus.ts` | `Menu` interface with `requiredRoles: string[]` and `icon?`; full role-aware nav list |

### ✅ Phase 2 — New API Controllers
| Controller | Route | Auth |
|---|---|---|
| `BookingsController.cs` | `GET /api/bookings`, `GET /api/bookings/my`, CRUD | Admin all; Customer own only (userId from JWT `NameIdentifier`) |
| `StaffController.cs` | `CRUD /api/staff` | Administrator only |
| `AmenitiesController.cs` | `CRUD /api/amenities` | GET = `[AllowAnonymous]`; write = Administrator |
| `RoomTypesController.cs` | `CRUD /api/roomtypes` | GET = `[AllowAnonymous]`; write = Administrator |
| `UsersController.cs` | GET users, assign/remove roles | Administrator + `ManageUsers` policy |
| `RoomsController.cs` | `CRUD /api/rooms` *(refactored)* | GET = `[AllowAnonymous]`; write = Administrator + `ManageRooms` policy |

**Backend extras:**
- `ManageUsers` policy added to `Program.cs` `AddAuthorizationBuilder()`
- CORS policy `AngularClient` added — allows `http://localhost:4200`
- Duplicate `app.Run()` removed from `Program.cs`

### ✅ Phase 3 — Auth UI
| Component | Path | Purpose |
|---|---|---|
| `LoginComponent` | `/login` | Reactive form → `api/account/login` → role-based redirect |
| `RegisterComponent` | `/register` | Reactive form → `api/account/register` → redirects to `/login` |
| `NavBarComponent` | *(layout)* | Role-filtered links via `computed()`, Login/Logout, session expiry banner |

### ✅ Phase 4 — Role Dashboards
| Component | Path | Visible To |
|---|---|---|
| `DashboardComponent` | `/` | Hub — redirects to correct dashboard by role |
| `AdminDashboardComponent` | `/admin-dashboard` | Administrator |
| `CustomerDashboardComponent` | `/customer-dashboard` | Customer, Administrator |
| `GuestDashboardComponent` | `/guest-dashboard` | Guest (authenticated) |

### ✅ Phase 5 — Admin Feature Pages
| Component | Path | Role |
|---|---|---|
| `RoomsComponent` | `/rooms` | Administrator |
| `RoomTypesComponent` | `/room-types` | Administrator |
| `AmenitiesComponent` | `/amenities` | Administrator |
| `StaffComponent` | `/staff` | Administrator |
| `UsersComponent` | `/users` | Administrator |

All admin pages: `MatTable` list + inline form panel using `MatFormField`, `MatSelect`, `MatSnackBar`.

### ✅ Phase 6 — Customer Feature Pages
| Component | Path | Role |
|---|---|---|
| `BookingsComponent` | `/bookings` | Customer (own), Administrator (all) |
| `BrowseRoomsComponent` | `/browse-rooms` | Public (no auth required) |

**Booking form date-picker rules (implemented):**
- Check-in uses `MatDatepicker` with `[min]="today"` — no past dates allowed
- Check-in is pre-filled to the **next full hour** from now on form open
- Check-out uses `MatDatepicker` with `[min]="minCheckOutDate()"` (a `signal<Date>`)
- For **nightly** rooms (`allowHourlyStay = false`): checkout min = day after check-in
- For **hourly** rooms (`allowHourlyStay = true`): checkout min = same day as check-in
- `Room.allowHourlyStay` boolean is on `RoomDTO` and `api.models.ts` `RoomDTO` interface
- When room selection changes, `minCheckOutDate` signal is recalculated reactively
- `MatNativeDateModule` is imported in `BookingsComponent` alongside `MatDatepickerModule`

### ✅ Phase 7 — Routing + Unauthorized
- All routes in `app.routes.ts` — flat, no lazy loading
- Protected routes use `canActivate: [authGuard, roleGuard]` + `data: { roles: [...] }`
- `UnauthorizedComponent` at `/unauthorized` — shown when `roleGuard` denies access
- Fallback `**` → `''` (hub redirect)

### ✅ Phase 9 — Multi-Tenant SaaS
| File | Purpose |
|---|---|
| `Shared/Models/Tenant.cs` | Tenant entity — name, subdomain (unique), plan, active flag, createdAt |
| `Shared/Models/UserTenant.cs` | Join entity — many-to-many user ↔ tenant; `TenantRole?` for tenant-scoped roles |
| `Shared/Enums/TenantPlan.cs` | `Free`, `Pro`, `Enterprise` |
| `Shared/DTOs/TenantDTO.cs` | Flat DTO |
| `Shared/DTOs/UserTenantDTO.cs` | Flat DTO — `string UserName`, `string TenantName` (denormalized) |
| `API/Middleware/TenantResolverMiddleware.cs` | Reads `TenantId` from JWT claim → falls back to `X-Tenant-Id` header; sets `HttpContext.Items["TenantId"]` |
| `API/Controllers/TenantsController.cs` | CRUD + user-assignment endpoints; `[Authorize(Roles = "SuperAdmin")]` |
| `component/tenants/tenants.component.ts` | Angular CRUD page + expand-to-see-users panel; route `/tenants`, `data: { roles: ['SuperAdmin'] }` |

**Multi-Tenant extras in other files:**
- `TenantId` FK added to: `Room`, `Booking`, `Staff`, `RoomType`, `Amenity`, `Invoice`
- `AccountController.Login` includes `TenantId` claim (primary tenant from `UserTenant` table)
- `ManageTenants` policy registered in `Program.cs`
- `SuperAdmin` role (Id = 4) seeded with all permission claims

**⚠️ Known gap:** `GetAll()` in tenanted controllers does not yet filter by `TenantId` — `Administrator` sees cross-tenant data. Apply TenantId filter before returning lists in: `RoomsController`, `BookingsController`, `StaffController`, `RoomTypesController`, `AmenitiesController`, `InvoicesController`.

---

## File Category Reference

### `api-controllers`
**What it is:** ASP.NET Core Web API controllers that expose RESTful endpoints.  
**Examples:** `HotelManagementSystem.API/Controllers/RoomsController.cs`, `AccountController.cs`  
**Key conventions:**
- Use **primary constructor injection** with explicit `private readonly` field assignment:
  ```csharp
  public class RoomsController(IRepository<Room> repo, IMapper mapper) : ControllerBase
  {
      private readonly IRepository<Room> _repo = repo;
      private readonly IMapper _mapper = mapper;
  ```
- Always inject `IRepository<EntityType>` and `IMapper` — no service layer
- Protect with `[Authorize(Roles = "...")]` and/or `[Authorize(Policy = "...")]` as stacked attributes
- Public read endpoints use `[AllowAnonymous]`
- Return `CreatedAtAction(nameof(GetX), new { id }, entity)` from POST
- Return `NoContent()` from PUT and DELETE

---

### `entity-models`
**What it is:** Plain C# domain entity classes in `HotelManagementSystem.Shared/Models/`.  
**Examples:** `Room.cs`, `Booking.cs`  
**Key conventions:**
- Always use `int Id` as the primary key
- Declare both `int {Entity}Id` FK and `{Entity} {Entity}` navigation property side-by-side
- Use `ICollection<T>` for one-to-many collections
- Use enums from `Shared/Enums/` for status fields
- Include a comment at the top of the class body: `// EntityName.cs`

---

### `dtos`
**What it is:** Flat data transfer objects in `HotelManagementSystem.Shared/DTOs/`.  
**Examples:** `RoomDTO.cs`, `BookingDTO.cs`  
**Key conventions:**
- Never include navigation property objects — replace with `int {Entity}Id` + `string {Entity}KeyField`
- Annotate denormalized string fields with `// To avoid complex object graph`
- Preserve enum types (e.g., `BookingStatus Status`) as-is in the DTO
- Every entity must have a corresponding DTO registered in `MappingProfile.cs`

---

### `repositories`
**What it is:** The generic EF Core repository (`IRepository<T>` / `Repository<T>`) in `Shared/Repositories/`.  
**Examples:** `Repository.cs`, `IRepository.cs`  
**Key conventions:**
- One generic repository for all entities — no entity-specific repositories
- `SaveChangesAsync()` is called inside each mutating method
- `_context` and `_dbSet` are `protected readonly`
- `DeleteAsync` calls `GetByIdAsync` first, then `_dbSet.Remove`

---

### `db-context`
**What it is:** `HotelDbContext` in `Shared/Data/`, the single EF Core DbContext.  
**Examples:** `HotelDbContext.cs`  
**Key conventions:**
- Extends `IdentityDbContext<User, Role, int>`
- DbSets initialized in constructor body via `Set<T>()`
- Unique indexes added for all business-key strings in `OnModelCreating`
- Role seed data via `HasData()` with explicit integer IDs
- Many-to-many uses explicit join entities (e.g., `RoomAmenity`)

**Seeded Roles & Claims:**
| Id | Role | Permission Claims |
|---|---|---|
| 1 | Administrator | ManageUsers, ManageRoles, ManageRooms, ManageRoomTypes, ManageAmenities, ManageStaff, ManageBookings, ViewReports |
| 2 | Guest | ViewDashboard |
| 3 | Customer | MakeBooking |
| 4 | SuperAdmin | All permissions including ManageTenants |

**Seeded Users:**
| Username | Email | Role |
|---|---|---|
| `admin` | admin@example.com | Administrator |
| `guest` | guest@example.com | Guest |
| `superadmin` | superadmin@example.com | SuperAdmin |

New registrations always get: role `Customer` + claim `Department:Sales`.

---

### `angular-services`
**What it is:** Injectable services in `HotelManagement.UI/src/app/services/`.  
**Examples:** `auth.service.ts`, `api.service.ts`  
**Key conventions:**
- `@Injectable({ providedIn: 'root' })` — always root-scoped
- Use Angular `signal()` for reactive state (not `BehaviorSubject`)
- `AuthService` is the single source of truth for token, roles, claims
- `ApiService` wraps all HTTP calls — components never inject `HttpClient` directly
- Use `inject()` function in components; constructor injection in services

---

### `angular-guards`
**What it is:** Functional route guards in `HotelManagement.UI/src/app/guards/`.  
**Examples:** `auth.guard.ts`, `role.guard.ts`  
**Key conventions:**
- Always functional (`CanActivateFn`) — no class-based guards
- `authGuard` — checks `AuthService.isLoggedIn()`; redirects to `/login`
- `roleGuard` — reads `route.data.roles: string[]`; redirects to `/unauthorized`
- Always combine both: `canActivate: [authGuard, roleGuard]`

---

### `angular-interceptors`
**What it is:** Functional HTTP interceptors in `HotelManagement.UI/src/app/interceptors/`.  
**Examples:** `auth.interceptor.ts`  
**Key conventions:**
- Always `HttpInterceptorFn` (functional, not class-based)
- Registered via `provideHttpClient(withInterceptors([...]))` in `app.config.ts`
- Use `inject()` inside the interceptor function body

---

### `angular-components`
**What it is:** Standalone Angular components in `HotelManagement.UI/src/app/component/`.  
**Examples:** `nav-bar.component.ts`, `dashboard.component.ts`  
**Key conventions:**
- Always standalone (`imports: [...]` in `@Component`)
- Use `styleUrl` (singular) with `.scss` extension
- Selector prefix is `app-`
- Import Angular Material modules per-component in the `imports` array
- No NgModules
- Use Angular `signal()` for local state, `computed()` for derived state
- Use `inject()` function for dependency injection (not constructor params)
- HTTP calls via `ApiService` — never inject `HttpClient` directly

---

### `angular-models`
**What it is:** TypeScript interface + constant files in `src/app/model/`.  
**Examples:** `menus.ts`, `api.models.ts`  
**Key conventions:**
- Export both the interface and a typed constant array from the same file
- Directory is `model/` (singular, not `models/`)
- File named after the concept in plural form (e.g., `menus.ts`)
- All backend DTOs are mirrored in `api.models.ts` (camelCase field names)

---

### `angular-routing`
**What it is:** Route definitions in `app.routes.ts`.  
**Examples:** `app.routes.ts`  
**Key conventions:**
- Single flat `Routes` array — no lazy loading
- Components imported directly at the top of the file
- Registered via `provideRouter(routes)` in `app.config.ts`
- Protected routes: `canActivate: [authGuard, roleGuard]`, `data: { roles: ['Administrator'] }`
- Public routes (no guards): login, register, browse-rooms, unauthorized

---

### `automapper-profiles`
**What it is:** AutoMapper profile in `HotelManagementSystem.API/Profiles/`.  
**Examples:** `MappingProfile.cs`  
**Key conventions:**
- All maps in a single `MappingProfile` class
- Always use `.ReverseMap()` for bidirectional mapping
- Registered with `builder.Services.AddAutoMapper(typeof(MappingProfile))`

---

### `jwt-auth`
**What it is:** JWT token generation logic in `AccountController` and `JwtTokenProvider`.  
**Examples:** `AccountController.cs`, `JwtTokenProvider.cs`  
**Key conventions:**
- Token generated in `AccountController.GenerateJwtToken` private method
- Claims: `sub`, `jti`, `NameIdentifier` + user claims + role claims + role names
- Settings from `IConfiguration.GetSection("JwtSettings")` — keys: `SecretKey`, `Issuer`, `Audience`
- 30-minute expiry, `HmacSha256` signing
- Roles serialized as `ClaimTypes.Role` (long URI) — Angular `AuthService.getRoles()` handles this

---

### `enums`
**What it is:** Domain status enums in `Shared/Enums/`.  
**Examples:** `BookingStatus.cs`, `PaymentStatus.cs`  
**Key conventions:**
- One enum per file
- Comment `// Enums` at the top of the class body

---

### `utilities`
**What it is:** Static utility classes in `Shared/Utilities/`.  
**Examples:** `PasswordHasher.cs`  
**Key conventions:**
- Static classes with static methods
- BCrypt used for password operations (not ASP.NET Core Identity's hasher)

---

## Authorization Policies (Program.cs)

| Policy | Claim Required |
|---|---|
| `ManageRooms` | `Permission = "ManageRooms"` |
| `ManageRoles` | `Permission = "ManageRoles"` |
| `ManageUsers` | `Permission = "ManageUsers"` |
| `ManagePermissions` | `Permission = "ManagePermissions"` |
| `ManageRoomTypes` | `Permission = "ManageRoomTypes"` |
| `ManageAmenities` | `Permission = "ManageAmenities"` |
| `ManageStaff` | `Permission = "ManageStaff"` |
| `ManageBookings` | `Permission = "ManageBookings"` |
| `ManageTenants` | `Permission = "ManageTenants"` |
| `ViewReports` | `Permission = "ViewReports"` |

---

## Feature Scaffold Guide

### Adding a New Domain Entity (e.g., "ServiceRequest")

1. **`Shared/Models/ServiceRequest.cs`** — Plain C# class, `int Id` PK, navigation properties with FK ints, `ICollection<T>` for collections, enum for status
2. **`Shared/Enums/ServiceRequestStatus.cs`** — New enum if a status field is needed
3. **`Shared/DTOs/ServiceRequestDTO.cs`** — Flat DTO; denormalize navigation names; annotate with `// To avoid complex object graph`
4. **`Shared/Data/HotelDbContext.cs`** — Add `DbSet<ServiceRequest>` and initialize in constructor; add unique indexes in `OnModelCreating`; add seed if needed
5. **`Shared/Migrations/`** — Generate migration with EF Core CLI (see `MigrationCommands.txt`)
6. **`API/Profiles/MappingProfile.cs`** — Add `CreateMap<ServiceRequest, ServiceRequestDTO>().ReverseMap()`
7. **`API/Controllers/ServiceRequestsController.cs`** — New controller with private readonly fields from primary constructor; inject `IRepository<ServiceRequest>` and `IMapper`; add `[Authorize]` attributes

### Adding a New Angular Page

1. Create `HotelManagement.UI/src/app/component/{name}/{name}.component.ts` + `.html` + `.scss`
2. Use `@Component({ selector: 'app-{name}', standalone: true, imports: [...], templateUrl: '...', styleUrl: '...' })`
3. Inject `ApiService` and `AuthService` via `inject()` — never `HttpClient` directly
4. Use `signal<T[]>([])` for list state; load in `ngOnInit` via `this.api.get<T[]>('endpoint').subscribe(...)`
5. Protect with `canActivate: [authGuard, roleGuard]` + `data: { roles: [...] }` in `app.routes.ts`
6. Add nav entry to `src/app/model/menus.ts` with appropriate `requiredRoles`

---

## Integration Rules

These rules are derived from `3-architectural-domains.json` and must be respected:

### Data Layer
- ✅ All entity CRUD **must** go through `IRepository<T>` — no direct DbContext in controllers or pages
- ✅ All entity ↔ DTO mapping **must** use AutoMapper; add new maps to `MappingProfile.cs`
- ✅ DTOs must be **flat** — no nested navigation objects; denormalize with string fields
- ✅ Migrations live in `Shared/Migrations/`

### Auth
- ✅ New controllers requiring authentication **must** use `[Authorize(Roles = "...")]`
- ✅ New public endpoints **must** explicitly mark `[AllowAnonymous]`
- ✅ New registered users always get `Customer` role and `Department:Sales` claim
- ✅ JWT config comes from `appsettings.json` `JwtSettings` section
- ✅ Angular route protection uses `canActivate: [authGuard, roleGuard]` — never check roles in component constructors

### API Layer
- ✅ All HTTP calls from Angular **must** go through `ApiService` — no raw `HttpClient` in components
- ✅ API responses follow the standard conventions (200/201/204/400/404)
- ✅ Swagger security definition must be preserved — do not remove `AddSecurityDefinition`/`AddSecurityRequirement`
- ✅ CORS policy `AngularClient` allows `http://localhost:4200` — do not remove

### Angular UI
- ✅ All Angular components **must** be standalone
- ✅ Angular Material is used for UI primitives — do not introduce other component libraries
- ✅ Routes are added to `app.routes.ts` — no separate routing modules
- ✅ State managed with `signal()` — do not introduce NgRx or other state libraries
- ✅ `AuthService` is the single source of truth for auth state — do not duplicate token logic
- ✅ Date fields in forms use `MatDatepicker` + `MatNativeDateModule` — never plain `<input type="date">`
- ✅ Booking check-in defaults to next full hour; check-out min is driven by `Room.allowHourlyStay` signal

### Shared Library
- ✅ Models, DTOs, enums, interfaces, repositories, and DbContext all live in `HotelManagementSystem.Shared`
- ✅ Never duplicate model or DTO classes in API, Client, or Admin projects


---

## File Category Reference

### `api-controllers`
**What it is:** ASP.NET Core Web API controllers that expose RESTful endpoints.  
**Examples:** `HotelManagementSystem.API/Controllers/RoomsController.cs`, `AccountController.cs`  
**Key conventions:**
- Use **primary constructor injection** (`public class RoomsController(IRepository<Room> repo)`)
- Always inject `IRepository<EntityType>` — no service layer
- Protect with `[Authorize(Roles = "...")]` and/or `[Authorize(Policy = "...")]` as stacked attributes
- Return `CreatedAtAction(nameof(GetX), new { id }, entity)` from POST
- Return `NoContent()` from PUT and DELETE

---

### `entity-models`
**What it is:** Plain C# domain entity classes in `HotelManagementSystem.Shared/Models/`.  
**Examples:** `Room.cs`, `Booking.cs`  
**Key conventions:**
- Always use `int Id` as the primary key
- Declare both `int {Entity}Id` FK and `{Entity} {Entity}` navigation property side-by-side
- Use `ICollection<T>` for one-to-many collections
- Use enums from `Shared/Enums/` for status fields
- Include a comment at the top of the class body: `// EntityName.cs`

---

### `dtos`
**What it is:** Flat data transfer objects in `HotelManagementSystem.Shared/DTOs/`.  
**Examples:** `RoomDTO.cs`, `BookingDTO.cs`  
**Key conventions:**
- Never include navigation property objects — replace with `int {Entity}Id` + `string {Entity}KeyField`
- Annotate denormalized string fields with `// To avoid complex object graph`
- Preserve enum types (e.g., `BookingStatus Status`) as-is in the DTO
- Every entity must have a corresponding DTO registered in `MappingProfile.cs`

---

### `repositories`
**What it is:** The generic EF Core repository (`IRepository<T>` / `Repository<T>`) in `Shared/Repositories/`.  
**Examples:** `Repository.cs`, `IRepository.cs`  
**Key conventions:**
- One generic repository for all entities — no entity-specific repositories
- `SaveChangesAsync()` is called inside each mutating method
- `_context` and `_dbSet` are `protected readonly`
- `DeleteAsync` calls `GetByIdAsync` first, then `_dbSet.Remove`

---

### `db-context`
**What it is:** `HotelDbContext` in `Shared/Data/`, the single EF Core DbContext.  
**Examples:** `HotelDbContext.cs`  
**Key conventions:**
- Extends `IdentityDbContext<User, Role, int>`
- DbSets initialized in constructor body via `Set<T>()`
- Unique indexes added for all business-key strings in `OnModelCreating`
- Role seed data via `HasData()` with explicit integer IDs
- Many-to-many uses explicit join entities (e.g., `RoomAmenity`)

---

### `api-clients`
**What it is:** `ApiClient` HTTP wrapper used by Blazor pages to call the API.  
**Examples:** `HotelManagementSystem.Client/ApiClient.cs`, `HotelManagementSystem.Admin/Data/ApiClient.cs`  
**Key conventions:**
- Base URL from `IConfiguration["ApiBaseUrl"]`
- Generic typed methods: `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `DeleteAsync`
- Always call `response.EnsureSuccessStatusCode()` before reading the body
- Use `JsonSerializer.Deserialize<T>` for GET responses
- Register as `AddSingleton<ApiClient>()`

---

### `blazor-pages`
**What it is:** Razor component pages in `Client/Components/Pages/` and `Admin/Pages/`.  
**Examples:** `Rooms.razor`, `Home.razor`  
**Key conventions:**
- `@page "/route"` at the top
- `@inject ApiClient ApiClient` to fetch data — no direct repository injection
- Null-check with `<em>Loading...</em>` placeholder before data arrives
- All data loaded in `OnInitializedAsync`
- Use `<PageTitle>` for browser tab title
- `@using HotelManagementSystem.Shared.Models` for entity types

---

### `blazor-layout`
**What it is:** Layout and navigation razor components.  
**Examples:** `MainLayout.razor`, `NavMenu.razor`  
**Key conventions:**
- CSS is colocated in `.razor.css` files (same name as the `.razor` file)
- `MainLayout.razor` is the root wrapper for all pages

---

### `angular-components`
**What it is:** Standalone Angular components in `HotelManagement.UI/src/app/component/`.  
**Examples:** `nav-bar.component.ts`, `dashboard.component.ts`  
**Key conventions:**
- Always standalone (`imports: [...]` in `@Component`)
- Use `styleUrl` (singular) with `.scss` extension
- Selector prefix is `app-`
- Import Angular Material modules per-component in the `imports` array
- No NgModules

---

### `angular-models`
**What it is:** TypeScript interface + constant files in `src/app/model/`.  
**Examples:** `menus.ts`  
**Key conventions:**
- Export both the interface and a typed constant array from the same file
- Directory is `model/` (singular, not `models/`)
- File named after the concept in plural form (e.g., `menus.ts`)

---

### `angular-routing`
**What it is:** Route definitions in `app.routes.ts`.  
**Examples:** `app.routes.ts`  
**Key conventions:**
- Single flat `Routes` array — no lazy loading
- Components imported directly at the top of the file
- Registered via `provideRouter(routes)` in `app.config.ts`

---

### `automapper-profiles`
**What it is:** AutoMapper profile in `HotelManagementSystem.API/Profiles/`.  
**Examples:** `MappingProfile.cs`  
**Key conventions:**
- All maps in a single `MappingProfile` class
- Always use `.ReverseMap()` for bidirectional mapping
- Registered with `builder.Services.AddAutoMapper(typeof(MappingProfile))`

---

### `jwt-auth`
**What it is:** JWT token generation logic in `AccountController` and `JwtTokenProvider`.  
**Examples:** `AccountController.cs`, `JwtTokenProvider.cs`  
**Key conventions:**
- Token generated in `AccountController.GenerateJwtToken` private method
- Claims: `sub`, `jti`, `NameIdentifier` + user claims + role claims + role names
- Settings from `IConfiguration.GetSection("JwtSettings")` — keys: `SecretKey`, `Issuer`, `Audience`
- 30-minute expiry, `HmacSha256` signing

---

### `enums`
**What it is:** Domain status enums in `Shared/Enums/`.  
**Examples:** `BookingStatus.cs`, `PaymentStatus.cs`  
**Key conventions:**
- One enum per file
- Comment `// Enums` at the top of the class body

---

### `utilities`
**What it is:** Static utility classes in `Shared/Utilities/`.  
**Examples:** `PasswordHasher.cs`  
**Key conventions:**
- Static classes with static methods
- BCrypt used for password operations (not ASP.NET Core Identity's hasher)

---

## Feature Scaffold Guide

### Adding a New Domain Entity (e.g., "ServiceRequest")

1. **`Shared/Models/ServiceRequest.cs`** — Plain C# class, `int Id` PK, navigation properties with FK ints, `ICollection<T>` for collections, enum for status
2. **`Shared/Enums/ServiceRequestStatus.cs`** — New enum if a status field is needed
3. **`Shared/DTOs/ServiceRequestDTO.cs`** — Flat DTO; denormalize navigation names; annotate with `// To avoid complex object graph`
4. **`Shared/Data/HotelDbContext.cs`** — Add `DbSet<ServiceRequest>` and initialize in constructor; add unique indexes in `OnModelCreating`; add seed if needed
5. **`Shared/Migrations/`** — Generate migration with EF Core CLI (see `MigrationCommands.txt`)
6. **`API/Profiles/MappingProfile.cs`** — Add `CreateMap<ServiceRequest, ServiceRequestDTO>().ReverseMap()`
7. **`API/Controllers/ServiceRequestsController.cs`** — New controller using primary constructor injection of `IRepository<ServiceRequest>`; add `[Authorize]` attributes

### Adding a New Blazor Page (Client)

1. Create `HotelManagementSystem.Client/Components/Pages/ServiceRequests.razor`
2. Add `@page "/service-requests"`, `@inject ApiClient ApiClient`, `@using HotelManagementSystem.Shared.Models`
3. Use null-check loading pattern; load data in `OnInitializedAsync` via `ApiClient.GetAsync<ServiceRequest[]>("servicerequests")`
4. Add `<PageTitle>` element

### Adding a New Angular Component

1. Create `HotelManagement.UI/src/app/component/{name}/{name}.component.ts` + `.html` + `.scss` + `.spec.ts`
2. Use `@Component({ selector: 'app-{name}', imports: [...], templateUrl: '...', styleUrl: '...' })`
3. Import Angular Material modules needed directly in the `imports` array
4. Add a route to `app.routes.ts` if it is a page

---

## Integration Rules

These rules are derived from `3-architectural-domains.json` and must be respected:

### Data Layer
- ✅ All entity CRUD **must** go through `IRepository<T>` — no direct DbContext in controllers or pages
- ✅ All entity ↔ DTO mapping **must** use AutoMapper; add new maps to `MappingProfile.cs`
- ✅ DTOs must be **flat** — no nested navigation objects; denormalize with string fields
- ✅ Migrations live in `Shared/Migrations/`

### Auth
- ✅ New controllers requiring authentication **must** use `[Authorize(Roles = "...")]`
- ✅ New public endpoints **must** explicitly mark `[AllowAnonymous]`
- ✅ New registered users always get `Customer` role and `Department:Sales` claim
- ✅ JWT config comes from `appsettings.json` `JwtSettings` section

### API Layer
- ✅ All HTTP calls from Blazor/Angular **must** go through `ApiClient` — no raw `HttpClient` in pages
- ✅ API responses follow the standard conventions (200/201/204/400/404)
- ✅ Swagger security definition must be preserved — do not remove `AddSecurityDefinition`/`AddSecurityRequirement`

### Angular UI
- ✅ All Angular components **must** be standalone
- ✅ Angular Material is used for UI primitives — do not introduce other component libraries
- ✅ Routes are added to `app.routes.ts` — no separate routing modules

### Shared Library
- ✅ Models, DTOs, enums, interfaces, repositories, and DbContext all live in `HotelManagementSystem.Shared`
- ✅ Never duplicate model or DTO classes in API, Client, or Admin projects

---

## Example Prompt Usage

**User prompt:**
> "Add a Staff management page to the Blazor client that lists all staff members."

**Expected files to create/modify:**

1. **`HotelManagementSystem.API/Controllers/StaffController.cs`** — New controller with `IRepository<Staff>`; `[Authorize(Roles = "Administrator")]`
2. **`HotelManagementSystem.Client/Components/Pages/Staff.razor`** — `@page "/staff"`, null-check pattern, `ApiClient.GetAsync<Staff[]>("staff")`, `<PageTitle>Staff</PageTitle>`
3. *(Optional)* **`HotelManagement.UI/src/app/component/staff/staff.component.ts`** + `.html` + `.scss` — Standalone Angular component if UI parity is needed
4. *(Optional)* **`HotelManagement.UI/src/app/app.routes.ts`** — Add `{ path: 'staff', component: StaffComponent }` entry

**Do NOT:**
- Inject `IRepository<Staff>` or `HotelDbContext` directly into the Blazor page
- Create a separate `StaffService` class (no service layer exists)
- Use a non-flat DTO with nested navigation objects
- Use `styleUrls` (plural) in the Angular component

---

## Planned Features & Roadmap

For planned phases (Billing, Multi-Tenant SaaS, Import/Export, UX Enhancements, Reports), see:

👉 **[FEATURE_ROADMAP.md](../FEATURE_ROADMAP.md)**

Key decisions recorded there:
- **Tenant isolation:** row-level (`TenantId` FK on every tenanted entity)
- **Charts:** `ng2-charts` (Chart.js) approved exclusively for `ReportsComponent`
- **PDF invoices:** server-side via `QuestPDF` in `Shared/Utilities/InvoicePdfUtility.cs`
