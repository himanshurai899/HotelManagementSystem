# Tech Stack Analysis

## Core Technology Analysis

### Programming Languages
- **C# (.NET 8)** — Backend API, Blazor Server client, Blazor Server admin panel, shared class library
- **TypeScript (Angular 19)** — Angular-based customer-facing UI
- **HTML/SCSS** — Angular templates and component styles
- **Razor (.razor / .cshtml)** — Blazor component markup

### Primary Framework
- **ASP.NET Core Web API** (`HotelManagementSystem.API`) — RESTful HTTP API serving all clients

### Secondary / Tertiary Frameworks
- **Blazor Server** (`HotelManagementSystem.Client`) — Interactive server-side Blazor for customer-facing web app
- **Blazor Server** (`HotelManagementSystem.Admin`) — Blazor Server admin panel (older Blazor Server scaffold with `_Host.cshtml`)
- **Angular 19** (`HotelManagement.UI`) — Standalone Angular UI using Angular Material

### Key Libraries & NuGet Packages
- **Entity Framework Core** with **SQL Server** provider — ORM and database access
- **ASP.NET Core Identity** (`IdentityDbContext<User, Role, int>`) — User/role management, password hashing, lockout
- **JWT Bearer Authentication** (`Microsoft.AspNetCore.Authentication.JwtBearer`) — Stateless API auth
- **AutoMapper** — Model ↔ DTO mapping in the API
- **Swagger / Swashbuckle** — API documentation with JWT security definition
- **BCrypt.Net** — Password hashing utility in the shared library
- **Angular Material** — UI component library for the Angular app

### State Management Approach
- **No centralised frontend state management** (no NgRx, no Blazor Fluxor); state is component-local or page-local
- Blazor pages fetch data directly via injected `ApiClient` on `OnInitializedAsync`
- Angular components are early-stage (mostly scaffold); no services layer yet

---

## Domain Specificity Analysis

### Problem Domain
**Hotel Management System** — A multi-client platform for managing hotel operations including room inventory, guest bookings, payments, and staff records.

### Core Business Concepts
- **Room & Room Types** — Rooms belong to a typed category (e.g., Single, Suite) with a base price and capacity; rooms have a unique room number and availability flag
- **Amenities** — Rooms can have many amenities via a many-to-many join (`RoomAmenity`)
- **Bookings** — Users book rooms for check-in/check-out date ranges; bookings have a lifecycle (`Pending → Confirmed → Completed / Cancelled`)
- **Payments** — Payments are linked to bookings with an amount and status (`Pending / Paid / Refunded`)
- **Users & Roles** — Identity-based users with seeded roles: `Administrator`, `Guest`, `Customer`; role-based and claim-based authorization
- **Staff** — Internal hotel staff records (not Identity users) with position and hire date

### Supported User Interactions
- Guest/customer registration and login (JWT-based)
- Room browsing (Blazor client `/rooms` page)
- Admin management of rooms and roles (via Administrator-only API endpoints)
- Dashboard view (Angular UI scaffold)

### Primary Data Types & Structures
- Entity models: `Room`, `RoomType`, `Booking`, `Payment`, `Amenity`, `RoomAmenity`, `Staff`, `User` (IdentityUser), `Role` (IdentityRole)
- DTOs: flat representations of each entity used for API communication (avoids complex navigation property serialization)
- Enums: `BookingStatus` (`Pending`, `Confirmed`, `Cancelled`, `Completed`), `PaymentStatus`

---

## Application Boundaries

### Features Clearly In Scope
- CRUD operations for rooms, room types, amenities, bookings, payments, staff via the API
- JWT authentication with role claims and policy-based authorization
- Admin-only management operations (`[Authorize(Roles = "Administrator")]`)
- Customer self-registration and login
- Blazor client room listing
- Angular dashboard (early stage)

### Architecturally Inconsistent Features
- Direct database access from Angular or Blazor pages (all data access must go through the API via `ApiClient`)
- Adding non-hotel business domains (e.g., restaurant management, retail) without extending the existing entity model
- Custom authentication flows that bypass ASP.NET Core Identity and JWT
- Storing navigation property graphs in DTOs (pattern is explicitly flat DTOs with denormalized names like `RoomNumber`, `UserName`)

### Domain Constraints Implied by Libraries
- All persistence must go through `IRepository<T>` / `Repository<T>` (generic EF Core repository pattern)
- All model → DTO conversions must use AutoMapper `MappingProfile`
- Password operations (outside Identity) must use `PasswordHasher` (BCrypt)
- API consumers must include a `Bearer` JWT token for protected endpoints
