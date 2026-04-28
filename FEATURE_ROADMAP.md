# Hotel Management System — Feature Roadmap

> This file tracks planned features beyond the initial 7-phase implementation.  
> For coding conventions, architecture rules, and scaffold patterns see [.github/copilot-instructions.md](.github/copilot-instructions.md).

---

## Implementation Status

| Phase | Feature Area | Status |
|---|---|---|
| 8 | Billing & Payments | ✅ Done |
| 9 | Multi-Tenant SaaS | ✅ Done |
| 10 | Import / Export | ⬜ Planned |
| 11 | UX Enhancements | ⬜ Planned |
| 12 | Advanced Booking | 🔄 In Progress |
| 13 | Customer Experience & Security | ⬜ Planned |
| 14 | Multi-Hotel Ownership | ⬜ Planned |
| 15 | KPI Dashboard & Analytics | ⬜ Planned |
| 16 | Room Content & Media | ⬜ Planned |
| 17 | Pricing Engine | ⬜ Planned |
| 18 | Reviews & Bulk Operations | ⬜ Planned |
| 19 | Cancellation Rules | ⬜ Planned |
| 20 | Email Notifications | ⬜ Planned |

> **Status key:** ⬜ Planned · 🔄 In Progress · ✅ Done

---

## Execution Strategy — Sequential vs Parallel

### 🔒 Essential Sequential Foundation
These phases are hard dependencies for most downstream work. They **must** complete in order before the parallel tracks begin.

```
Phase 8 ✅  →  Phase 9 ✅  →  Phase 12 🔄  ← complete this first
(Billing)     (Multi-Tenant)   (Adv. Booking)
```

| Phase | Blocks | Why sequential |
|---|---|---|
| 8 — Billing & Payments | 13, 19, 20 | Invoice + Payment entities are referenced by cancellation fees, email triggers, and key generation |
| 9 — Multi-Tenant SaaS | 14, 15 | `TenantId` FK must exist on all entities before hotel ownership and per-tenant KPIs make sense |
| 12 — Advanced Booking | 13, 16, 17, 18, 19 | `BookingRoom`, `BookingType`, room-block, approve/reject — all downstream features query or extend this shape |

---

### 🔀 Parallel Tracks (after Phase 12 completes)

Once the sequential foundation is complete, work can fan out across **four independent tracks** simultaneously.

#### Track A — Customer Experience
```
Phase 12 ──► Phase 13 (Customer UX & Security)
         └──► Phase 16 (Room Content & Media)
         └──► Phase 18 (Reviews & Bulk Ops)
```
These share no write dependencies on each other. Teams can work on digital keys (13), room photos/descriptions (16), and reviews/bulk updates (18) at the same time.

#### Track B — Business Rules
```
Phase 12 ──► Phase 17 (Pricing Engine)
Phase 17 ──► Phase 19 (Cancellation Rules)   ← 19 depends on 17
```
Pricing engine must land before cancellation tiers (which reuse `PricingEngine` for fee calculation). **17 → 19 are sequential within this track.**

#### Track C — Ops & Admin Tooling
```
Phase 9 ──► Phase 14 (Multi-Hotel Ownership)
Phase 12 ──► Phase 10 (Import / Export)       ← entity shapes must be stable
Phase 11 (UX Enhancements) ── mostly independent; 11c (Reports) needs Phase 14 data
```
Phase 10 and Phase 11 (11a Notifications, 11b Maintenance Requests) are broadly independent and can run alongside Track A/B once Phase 12 is done. Phase 11c (Reports) needs Phase 14 to include per-hotel occupancy.

#### Track D — Analytics
```
Phase 11c ──► Phase 15 (KPI Dashboard)
Phase 14  ──► Phase 15
```
Phase 15 is a pure consumer of data produced by 11c (report endpoints) and 14 (hotel entities). Start only after both are complete.

#### Track E — Notifications (capping layer)
```
Phase 8 + Phase 13 + Phase 19 ──► Phase 20 (Email Notifications)
```
Phase 20 wraps all lifecycle events from earlier phases. It is the last phase to implement.

---

### Recommended Sprint Ordering

```
Sprint 1  │ Complete Phase 12 (sub-phases 12a → 12d)
          │
Sprint 2  │ Track A: Phase 16          Track B: Phase 17
          │ Track C: Phase 10 + 11a/b  Track D: Phase 14
          │
Sprint 3  │ Track A: Phase 13 + 18     Track B: Phase 19
          │ Track C: Phase 11c         (unblocks Track D)
          │
Sprint 4  │ Track D: Phase 15
          │ Track E: Phase 20
```

> **Parallel rule of thumb:** Any two phases in different tracks with no shared arrow in the diagrams above can be worked on simultaneously without merge conflicts on the data model.

---

## Phase 8 — Billing & Payments

### Overview
Allow customers to view invoices generated from bookings, and allow admins to manage and track payment status.

### New Files

#### Backend

| File | Purpose |
|---|---|
| `Shared/Models/Invoice.cs` | Invoice entity linked to a `Booking` |
| `Shared/Models/InvoiceItem.cs` | Line items on an invoice (e.g. room charge, extras) |
| `Shared/Enums/InvoiceStatus.cs` | `Unpaid`, `Paid`, `PartiallyPaid`, `Overdue`, `Cancelled` |
| `Shared/DTOs/InvoiceDTO.cs` | Flat DTO — `int BookingId`, `string GuestName` (denormalized) |
| `Shared/DTOs/InvoiceItemDTO.cs` | Flat DTO |
| `API/Controllers/InvoicesController.cs` | CRUD; Admin = all; Customer = own only (userId from JWT) |

#### Angular

| File | Route | Role |
|---|---|---|
| `component/invoices/invoices.component.ts` + `.html` + `.scss` | `/invoices` | Customer (own), Administrator |

### Entity Design

```csharp
// Invoice.cs
public class Invoice
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }

    public DateTime IssuedDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }

    public ICollection<InvoiceItem> Items { get; set; }
}

// InvoiceItem.cs
public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; }

    public string Description { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}
```

### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface Invoice {
  id: number;
  bookingId: number;
  guestName: string;        // denormalized — avoid complex object graph
  issuedDate: string;
  dueDate: string;
  totalAmount: number;
  status: string;
  items: InvoiceItem[];
}

export interface InvoiceItem {
  id: number;
  invoiceId: number;
  description: string;
  amount: number;
  quantity: number;
}
```

### Scaffold Steps

1. `Shared/Models/Invoice.cs` + `InvoiceItem.cs` — entities with FK + navigation properties
2. `Shared/Enums/InvoiceStatus.cs` — `Unpaid`, `Paid`, `PartiallyPaid`, `Overdue`, `Cancelled`
3. `Shared/DTOs/InvoiceDTO.cs` + `InvoiceItemDTO.cs` — flat; denormalize `GuestName`
4. `Shared/Data/HotelDbContext.cs` — add `DbSet<Invoice>`, `DbSet<InvoiceItem>`, configure cascade
5. Add EF Core migration
6. `API/Profiles/MappingProfile.cs` — `CreateMap<Invoice, InvoiceDTO>().ReverseMap()`
7. `API/Controllers/InvoicesController.cs` — `[Authorize(Roles = "Administrator,Customer")]`; customer sees own invoices via `userId` from `NameIdentifier` claim
8. Angular `InvoicesComponent` — `signal<Invoice[]>([])`, load via `ApiService`, route `/invoices` with `data: { roles: ['Customer', 'Administrator'] }`
9. Add `Payment` TypeScript interface to `api.models.ts` (the `PaymentDTO` C# DTO exists but has no TS mirror yet)

### Notes
- PDF invoice generation: add `InvoicePdfUtility` static class to `Shared/Utilities/` using **QuestPDF** NuGet package
- `GET /api/invoices/{id}/pdf` returns `FileContentResult` with `application/pdf`
- Do NOT use client-side PDF generation

### Extra — Beyond Original Roadmap

| Addition | Detail |
|---|---|
| `Shared/Models/Payment.cs` | Payment entity — `BookingId`, `Amount`, `PaymentDate`, `PaymentStatus` |
| `Shared/DTOs/PaymentDTO.cs` | Flat DTO |
| `API/Controllers/PaymentsController.cs` | CRUD; Customer = own; Administrator = all |
| Angular `PaymentsComponent` | `/payments` — `data: { roles: ['Customer', 'Administrator', 'SuperAdmin'] }` |
| `Shared/Models/CompanyProfile.cs` | Single-row branding/settings table — `CompanyName`, `LogoUrl`, `Address`, `GstinNumber`, `PrimaryColor`, `AccentColor`, `FontFamily` |
| `Shared/DTOs/CompanyProfileDTO.cs` | Flat DTO |
| `API/Controllers/CompanyProfileController.cs` | GET (AllowAnonymous); PUT/upload-logo (Administrator + ManageCompanyProfile policy) |
| Angular `CompanyProfileComponent` | `/company-profile` — `data: { roles: ['Administrator', 'SuperAdmin'] }` |
| `CompanyProfileDTO` TypeScript interface | In `api.models.ts` |
| `Shared/Interfaces/IFileStorageService.cs` | Interface — `Task<string> SaveAsync(Stream, string, string)`; `Task DeleteAsync(string)` |
| `API/Services/LocalFileStorageService.cs` | Saves to `wwwroot/uploads/`; returns `/uploads/{guid}{ext}` |
| `API/Services/AzureBlobStorageService.cs` | Azure Blob Storage implementation of `IFileStorageService` |
| `Shared/Utilities/InvoicePdfUtility.cs` | ✅ Implemented using QuestPDF |
| User profile fields | `User.cs` gains `FirstName`, `LastName`, `ProfilePhotoUrl`, `IdProofType`, `IdProofNumber` |
| `API/Controllers/ProfileController.cs` | `GET/PUT /api/profile`; `PUT /api/profile/password`; `PUT /api/profile/photo` |
| Angular `ProfileComponent` | `/profile` — public (auth only, no role guard) |
| Angular `AccessControlComponent` | `/access-control` — unified Users + Roles + Permissions page; `data: { roles: ['Administrator', 'SuperAdmin'] }` |
| `Shared/DTOs/UpdateProfileDTO.cs`, `ChangePasswordDTO.cs`, `CreateUserDTO.cs` | Flat request DTOs |
| `UpdateProfileRequest`, `ChangePasswordRequest`, `ProfilePhotoResponse`, `CreateUserRequest` | TypeScript interfaces in `api.models.ts` |

---

## Phase 9 — Multi-Tenant SaaS

### Overview
Allow the system to serve multiple hotel organizations (tenants) from a single deployment. Uses **row-level isolation** — every tenanted entity carries a `TenantId` FK. `SuperAdmin` role manages tenants.

### Tenant Isolation Strategy: Row-Level

Every entity that is tenant-scoped gets:
```csharp
public int TenantId { get; set; }
public Tenant Tenant { get; set; }
```

**Entities to add `TenantId` to:**
- `Room`
- `Booking`
- `Staff`
- `RoomType`
- `Amenity`
- `Invoice`

The `HotelDbContext` stays as a single context — no schema splitting.

### New Files

#### Backend

| File | Purpose |
|---|---|
| `Shared/Models/Tenant.cs` | Tenant entity — name, subdomain, plan, active flag |
| `Shared/Enums/TenantPlan.cs` | `Free`, `Pro`, `Enterprise` |
| `Shared/DTOs/TenantDTO.cs` | Flat DTO |
| `API/Controllers/TenantsController.cs` | CRUD; `[Authorize(Roles = "SuperAdmin")]` |
| `API/Middleware/TenantResolverMiddleware.cs` | Resolves `TenantId` from JWT claim or subdomain header; sets `HttpContext.Items["TenantId"]` |

#### Angular

| File | Route | Role |
|---|---|---|
| `component/tenants/tenants.component.ts` + `.html` + `.scss` | `/tenants` | SuperAdmin |

### Entity Design

```csharp
// Tenant.cs
public class Tenant
{
    // Tenant.cs
    public int Id { get; set; }
    public string Name { get; set; }
    public string Subdomain { get; set; }   // unique index
    public TenantPlan Plan { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface Tenant {
  id: number;
  name: string;
  subdomain: string;
  plan: string;
  isActive: boolean;
  createdAt: string;
}
```

### Scaffold Steps

1. `Shared/Models/Tenant.cs` — `int Id`, `string Name`, `string Subdomain` (unique), `TenantPlan Plan`, `bool IsActive`
2. `Shared/Enums/TenantPlan.cs` — `Free`, `Pro`, `Enterprise`
3. Add `int TenantId` + `Tenant Tenant` FK to `Room`, `Booking`, `Staff`, `RoomType`, `Amenity`
4. `Shared/Data/HotelDbContext.cs` — add `DbSet<Tenant>`, unique index on `Subdomain`, update `OnModelCreating` for new FKs
5. `API/Middleware/TenantResolverMiddleware.cs` — read `TenantId` claim from JWT; fall back to `X-Tenant-Id` header; store in `HttpContext.Items`
6. Register middleware in `Program.cs` after `UseAuthentication`
7. Add EF Core migration
8. `API/Controllers/TenantsController.cs` — `[Authorize(Roles = "SuperAdmin")]`
9. Angular `TenantsComponent` at `/tenants` — `canActivate: [authGuard, roleGuard]`, `data: { roles: ['SuperAdmin'] }`
10. Add `Tenant` interface + nav entry in `menus.ts`

### Notes
- Controllers that return tenant-scoped data must filter by `TenantId` extracted from `HttpContext.Items["TenantId"]`
- Do NOT return cross-tenant data from any endpoint unless role is `SuperAdmin`
- JWT generation in `AccountController` must include a `TenantId` claim on login

### Implementation Record (✅ Done)

#### Extra — Beyond Original Roadmap

| Addition | Detail |
|---|---|
| `Shared/Models/UserTenant.cs` | Join entity for many-to-many user ↔ tenant relationship; fields: `UserId`, `TenantId`, `TenantRole?`, `JoinedAt` |
| `Shared/DTOs/UserTenantDTO.cs` | Flat DTO — `string UserName`, `string TenantName` (denormalized) |
| `TenantsController` user sub-endpoints | `GET /api/tenants/{id}/users`, `POST /api/tenants/{id}/users`, `DELETE /api/tenants/{id}/users/{userId}` |
| `ManageTenants` policy | `Permission = "ManageTenants"` added to `Program.cs` `AddAuthorizationBuilder()` |
| `SuperAdmin` role seeded | `Id = 4`, `Name = "SuperAdmin"` with all permission claims; `superadmin` user seeded (email: `superadmin@example.com`, password: `SuperAdmin@123`) |
| Angular `TenantsComponent` | Full CRUD + expand-to-see-users panel; route `/tenants`, `data: { roles: ['SuperAdmin'] }` |

#### Verified Implemented Items

| Item | Status |
|---|---|
| `Shared/Models/Tenant.cs` | ✅ |
| `Shared/Enums/TenantPlan.cs` (`Free`, `Pro`, `Enterprise`) | ✅ |
| `TenantId` FK on `Room`, `Booking`, `Staff`, `RoomType`, `Amenity`, `Invoice` | ✅ |
| `HotelDbContext` — `DbSet<Tenant>`, unique index on `Subdomain`, `UserTenant` composite PK | ✅ |
| EF Core migration generated | ✅ |
| `TenantResolverMiddleware` — JWT claim → `X-Tenant-Id` header fallback | ✅ |
| Registered in `Program.cs` after `UseAuthentication` | ✅ |
| `TenantsController` CRUD + `[Authorize(Roles = "SuperAdmin")]` | ✅ |
| JWT login response includes `TenantId` claim (primary tenant from `UserTenant`) | ✅ |
| Angular `TenantsComponent` + route + `menus.ts` nav entry | ✅ |
| `TenantDTO` + `UserTenantDTO` TypeScript interfaces in `api.models.ts` | ✅ |
| `TenantDTO.CurrencyCode` + `TenantDTO.Locale` fields (ISO 4217 / IETF locale) | ✅ |
| `DefaultCurrencyDTO` C# DTO + TypeScript interface in `api.models.ts` | ✅ |

#### ⚠️ Remaining Gap — Tenant-Filtered GET Endpoints

The CREATE endpoints correctly set `TenantId` from `HttpContext.Items["TenantId"]`. However, **GET list endpoints** (`GetAll`) for tenanted controllers currently return data from **all tenants** when called by an `Administrator` — they do not filter by `TenantId`. This means cross-tenant data can leak.

**Affected controllers:**
- `RoomsController.GetAll()`
- `BookingsController.GetAll()`
- `StaffController.GetAll()`
- `RoomTypesController.GetAll()`
- `AmenitiesController.GetAll()`
- `InvoicesController.GetAll()`

**Fix pattern** (to apply to each `GetAll` action):
```csharp
public async Task<IActionResult> GetAll()
{
    var all = await _repo.GetAllAsync();
    if (!User.IsInRole("SuperAdmin") &&
        HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
    {
        all = all.Where(e => e.TenantId == tenantId);
    }
    return Ok(_mapper.Map<IEnumerable<TDto>>(all));
}
```

This fix should be applied before Phase 14 (Multi-Hotel Ownership) which depends on correct tenant isolation.

---

## Phase 10 — Import / Export

### Overview
Allow admins to bulk-import rooms/bookings from CSV/Excel and export data for reporting.

### Supported Operations

| Endpoint | Method | Format | Role |
|---|---|---|---|
| `/api/rooms/import` | POST | CSV / Excel (.xlsx) | Administrator |
| `/api/bookings/import` | POST | CSV / Excel (.xlsx) | Administrator |
| `/api/rooms/export` | GET | CSV | AllowAnonymous |
| `/api/bookings/export` | GET | CSV | Administrator |
| `/api/staff/export` | GET | CSV | Administrator |

### New Files

#### Backend

| File | Purpose |
|---|---|
| `Shared/Utilities/DataImportExportUtility.cs` | Static utility — CSV read/write via `CsvHelper`; Excel read via `ExcelDataReader` |
| `Shared/DTOs/ImportResultDTO.cs` | `int Imported`, `int Skipped`, `List<string> Errors` |

#### Angular

| File | Route | Role |
|---|---|---|
| `component/import-export/import-export.component.ts` + `.html` + `.scss` | `/import-export` | Administrator |

### Utility Design

```csharp
// DataImportExportUtility.cs
public static class DataImportExportUtility
{
    public static IEnumerable<T> ReadCsv<T>(Stream stream) { ... }
    public static byte[] WriteCsv<T>(IEnumerable<T> records) { ... }
    public static IEnumerable<T> ReadExcel<T>(Stream stream) { ... }
}
```

### NuGet Packages Required

| Package | Purpose |
|---|---|
| `CsvHelper` | CSV read/write |
| `ExcelDataReader` + `ExcelDataReader.DataSet` | Excel (.xlsx) read |

### Scaffold Steps

1. Add NuGet packages to `HotelManagementSystem.Shared.csproj`
2. `Shared/Utilities/DataImportExportUtility.cs` — static class with `ReadCsv<T>`, `WriteCsv<T>`, `ReadExcel<T>`
3. `Shared/DTOs/ImportResultDTO.cs` — `int Imported`, `int Skipped`, `List<string> Errors`
4. Add `POST /import` and `GET /export` actions to existing `RoomsController`, `BookingsController`, `StaffController`
5. Import actions accept `IFormFile`; export actions return `FileContentResult` (`text/csv`)
6. Angular `ImportExportComponent` — file input via `<input type="file">`, progress via `signal<string>`, calls `ApiService.post<ImportResultDTO>('rooms/import', formData)`
7. Add route `/import-export` to `app.routes.ts` with `data: { roles: ['Administrator'] }`

### Notes
- Use `multipart/form-data` for upload endpoints; Angular must set correct content type automatically (do not manually set `Content-Type` header when using `FormData`)
- Export responses must include `Content-Disposition: attachment; filename="..."` header
- Validate CSV headers before bulk insert; return errors in `ImportResultDTO.Errors`

---

## Phase 11 — UX Enhancements

### 11a — In-App Notifications

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/Notification.cs` | Notification entity — user-targeted, typed, read/unread |
| `Shared/Enums/NotificationType.cs` | `BookingConfirmed`, `BookingCancelled`, `InvoiceDue`, `SystemAlert` |
| `Shared/DTOs/NotificationDTO.cs` | Flat DTO |
| `API/Controllers/NotificationsController.cs` | `GET /api/notifications/my` (current user); `PUT /api/notifications/{id}/read` |
| `component/notifications/notifications.component.ts` | Bell icon in `NavBarComponent` — unread count badge |

#### Entity Design

```csharp
// Notification.cs
public class Notification
{
    // Notification.cs
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }

    public string Title { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### TypeScript Interface

```typescript
export interface Notification {
  id: number;
  userId: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}
```

#### Scaffold Steps

1. `Shared/Models/Notification.cs` + `Shared/Enums/NotificationType.cs`
2. `Shared/DTOs/NotificationDTO.cs`
3. `HotelDbContext.cs` — add `DbSet<Notification>`, FK to `User`
4. Add EF Core migration
5. `API/Controllers/NotificationsController.cs` — `GET my` + `PUT {id}/read` (both `[Authorize]`)
6. Angular `NotificationsComponent` — embedded in `NavBarComponent`; badge count via `signal<number>(0)`; polls `GET /api/notifications/my` on interval or on nav

---

### 11b — Maintenance Requests

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/MaintenanceRequest.cs` | Room maintenance ticket — room, reporter, status, priority |
| `Shared/Enums/MaintenancePriority.cs` | `Low`, `Medium`, `High`, `Critical` |
| `Shared/Enums/MaintenanceStatus.cs` | `Open`, `InProgress`, `Resolved`, `Closed` |
| `Shared/DTOs/MaintenanceRequestDTO.cs` | Flat DTO — `string RoomNumber` (denormalized) |
| `API/Controllers/MaintenanceRequestsController.cs` | CRUD; Staff/Admin write; Customer read own |
| `component/maintenance/maintenance.component.ts` + `.html` + `.scss` | `/maintenance` | Staff, Administrator |

#### Scaffold Steps

1–7: Same pattern as other entities (Model → Enum → DTO → DbContext → Migration → AutoMapper → Controller)
8. Angular `MaintenanceComponent` at `/maintenance`, `data: { roles: ['Administrator'] }`

---

### 11c — Reports & Analytics Dashboard

#### New Files

| File | Purpose |
|---|---|
| `API/Controllers/ReportsController.cs` | Read-only analytics endpoints |
| `component/reports/reports.component.ts` + `.html` + `.scss` | `/reports` — charts + summary cards |

#### Endpoints

| Endpoint | Returns | Role |
|---|---|---|
| `GET /api/reports/occupancy` | `{ date, occupancyRate }[]` (last 30 days) | Administrator |
| `GET /api/reports/revenue` | `{ month, totalRevenue }[]` (last 12 months) | Administrator |
| `GET /api/reports/bookings-summary` | `{ total, confirmed, cancelled, pending }` | Administrator |
| `GET /api/reports/top-room-types` | `{ roomTypeName, count }[]` | Administrator |

#### Controller Design

```csharp
// ReportsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,SuperAdmin")]
public class ReportsController(HotelDbContext context) : ControllerBase
{
    private readonly HotelDbContext _context = context;
    // NOTE: ReportsController injects HotelDbContext directly (read-only analytics queries
    // are complex aggregations not suited to the generic IRepository<T> pattern)
}
```

#### Angular Charts — `ng2-charts` (Approved Exception)

> ⚠️ **Approved library exception:** `ng2-charts` (Chart.js wrapper) is the **only** approved third-party UI library beyond Angular Material. It is permitted exclusively in `ReportsComponent`. Do NOT introduce it in any other component.

**Install:**
```bash
npm install ng2-charts chart.js
```

**Usage in `ReportsComponent`:**
```typescript
import { BaseChartDirective } from 'ng2-charts';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, MatCardModule, BaseChartDirective],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
```

#### TypeScript Interfaces (add to `api.models.ts`)

```typescript
export interface OccupancyReport {
  date: string;
  occupancyRate: number;
}

export interface RevenueReport {
  month: string;
  totalRevenue: number;
}

export interface BookingsSummary {
  total: number;
  confirmed: number;
  cancelled: number;
  pending: number;
}

export interface TopRoomType {
  roomTypeName: string;
  count: number;
}
```

#### Scaffold Steps

1. `API/Controllers/ReportsController.cs` — inject `HotelDbContext` directly (exception to IRepository pattern — raw LINQ aggregations only)
2. Implement `GET occupancy`, `GET revenue`, `GET bookings-summary`, `GET top-room-types`
3. `npm install ng2-charts chart.js` in `HotelManagement.UI/`
4. Angular `ReportsComponent` — `signal` per dataset; `BaseChartDirective` for line/bar charts; `MatCard` for summary KPI cards
5. Route `/reports` with `data: { roles: ['Administrator', 'SuperAdmin'] }`
6. Add nav entry in `menus.ts` with `requiredRoles: ['Administrator', 'SuperAdmin']`

---

## Phase 12 — Advanced Booking

### Overview
Extends the core booking system to support multi-room reservations per booking, history-based rebooking for returning customers, hourly stays, and room blocking for maintenance or holds.

### ✅ Implemented Items (as of Phase 12 in-progress)

| Item | Detail |
|---|---|
| `Room.AllowHourlyStay` (`bool`) | Added to `Room.cs`, `RoomDTO.cs`, `api.models.ts`; migration applied |
| `Room.PricePerNight` (`decimal`) | Added to `Room.cs`, `RoomDTO.cs`, `api.models.ts`; migration applied |
| `GET /api/bookings/calendar` | Returns `BookingCalendarEntry[]` for a given year+month; `[Authorize(Roles = "Administrator,SuperAdmin")]` |
| `BookingCalendarEntry` TypeScript interface | In `api.models.ts` — `roomId`, `roomNumber`, `customerName`, `checkInDate`, `checkOutDate`, `status` |
| Angular `BookingCalendarComponent` | `/booking-calendar` — `data: { roles: ['Administrator', 'SuperAdmin'] }`; month/year navigation |
| Booking approve/reject workflow | `PUT /api/bookings/{id}/approve` and `PUT /api/bookings/{id}/reject` — `[Authorize(Policy = "ManageBookings")]` |
| Booking overlap conflict guard | `POST /api/bookings` rejects requests where room is already booked for overlapping dates |

### 12a — Multi-Room Booking

Allow a single booking to span multiple rooms (e.g., a family booking two adjacent rooms under one reservation).

#### New / Modified Files

| File | Change |
|---|---|
| `Shared/Models/BookingRoom.cs` | New join entity — `BookingId`, `RoomId`, `PriceAtBooking` |
| `Shared/DTOs/BookingRoomDTO.cs` | Flat DTO — `int RoomId`, `string RoomNumber`, `decimal PriceAtBooking` |
| `Shared/Models/Booking.cs` | Replace single `RoomId` FK with `ICollection<BookingRoom> Rooms` |
| `Shared/DTOs/BookingDTO.cs` | Replace `RoomId` with `List<BookingRoomDTO> Rooms` |
| `API/Controllers/BookingsController.cs` | Accept `List<int> RoomIds` on POST; validate each room is available |
| Angular `BookingsComponent` | Multi-select room picker; show booked rooms as chips |

#### Entity Design

```csharp
// BookingRoom.cs
public class BookingRoom
{
    // BookingRoom.cs
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public decimal PriceAtBooking { get; set; }  // snapshot at time of booking
}
```

#### Scaffold Steps

1. `Shared/Models/BookingRoom.cs` — join entity with `PriceAtBooking` snapshot
2. `Shared/DTOs/BookingRoomDTO.cs` — flat; include `string RoomNumber` (denormalized)
3. Modify `Booking.cs` — remove `int RoomId`; add `ICollection<BookingRoom> Rooms`
4. Update `BookingDTO.cs` — replace `RoomId` with `List<BookingRoomDTO> Rooms`
5. `HotelDbContext.cs` — configure `BookingRoom` join table; add `DbSet<BookingRoom>`
6. Add EF Core migration
7. `MappingProfile.cs` — add `CreateMap<BookingRoom, BookingRoomDTO>().ReverseMap()`
8. `BookingsController.cs` — validate room availability across all requested rooms atomically; create `BookingRoom` records in a single transaction
9. Angular `BookingsComponent` — `MatSelect` with `multiple` for room selection; display rooms as `MatChip` list

#### Notes
- Room availability check must be atomic — no partial bookings
- `PriceAtBooking` must snapshot the room price at booking time, not reference live room price
- Display total cost as sum of `PriceAtBooking × nights` across all rooms

---

### 12b — History-Based Rebooking

Allow returning customers to quickly rebook from their past stays with pre-filled form data.

#### New / Modified Files

| File | Change |
|---|---|
| `API/Controllers/BookingsController.cs` | New `GET /api/bookings/history` — last 5 distinct room configurations per user |
| `Shared/DTOs/RebookSuggestionDTO.cs` | `int OriginalBookingId`, `List<int> RoomIds`, `string RoomsSummary`, `DateTime LastStayDate` |
| Angular `BrowseRoomsComponent` / `BookingsComponent` | "Rebook" button on history rows; pre-fills new booking form |

#### Scaffold Steps

1. `Shared/DTOs/RebookSuggestionDTO.cs` — flat DTO summarising a past booking for rebooking
2. `BookingsController.cs` — add `GET /api/bookings/history` returning `IEnumerable<RebookSuggestionDTO>` filtered by current user's `NameIdentifier` claim
3. Angular — add "Rebook" action to booking history table; on click, navigate to `/bookings/new` with pre-filled `RoomIds` and date range from last stay

---

### 12c — Hourly Stay

Support sub-day bookings priced by the hour (day-use rooms).

> **✅ Pre-work done:** `bool AllowHourlyStay` added to `Room.cs`, `RoomDTO.cs`, and `api.models.ts` (`RoomDTO.allowHourlyStay`).  
> The `BookingsComponent` datepicker already reads this flag: when the selected room has `allowHourlyStay = true`, same-day checkout is permitted; otherwise checkout must be at least the next day.  
> Remaining work: `HourlyRate`, `BookingType` enum, time pickers, price calculation.

#### New / Modified Files

| File | Change |
|---|---|
| `Shared/Enums/BookingType.cs` | `Nightly` (default), `Hourly` |
| `Shared/Models/Booking.cs` | Add `BookingType Type`; add `TimeSpan? CheckInTime`, `TimeSpan? CheckOutTime` for hourly stays |
| `Shared/DTOs/BookingDTO.cs` | Add `string BookingType`, `string? CheckInTime`, `string? CheckOutTime` |
| `API/Controllers/BookingsController.cs` | Compute price as `HourlyRate × hours` when `Type = Hourly` |
| `Shared/Models/Room.cs` | ~~`bool AllowHourlyStay`~~ ✅ Done · Add `decimal? HourlyRate` (nullable — not all rooms support hourly) |
| `Shared/DTOs/RoomDTO.cs` | ~~`allowHourlyStay`~~ ✅ Done · Add `decimal? HourlyRate` |
| Angular `BookingsComponent` | ~~Date picker respects `allowHourlyStay`~~ ✅ Done · Add `MatButtonToggle` for booking type; show time pickers for hourly |

#### Scaffold Steps

1. `Shared/Enums/BookingType.cs` — `Nightly`, `Hourly`
2. Add `decimal? HourlyRate` to `Room.cs` and `RoomDTO.cs`
3. Add `BookingType Type`, `TimeSpan? CheckInTime`, `TimeSpan? CheckOutTime` to `Booking.cs` and `BookingDTO.cs`
4. Add EF Core migration
5. Update `BookingsController.cs` — price calculation branch on `BookingType`
6. Angular `BookingsComponent` — `MatButtonToggle` for booking type; `MatTimepicker` (or `MatInput type=time`) for hourly slots

---

### 12d — Room Blocking

Allow administrators to block rooms from being booked (maintenance, renovation, VIP hold, etc.).

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/RoomBlock.cs` | Block record — `RoomId`, `StartDate`, `EndDate`, `Reason`, `BlockType` |
| `Shared/Enums/RoomBlockType.cs` | `Maintenance`, `Renovation`, `VIPHold`, `Administrative` |
| `Shared/DTOs/RoomBlockDTO.cs` | Flat DTO — `string RoomNumber` (denormalized) |
| `API/Controllers/RoomBlocksController.cs` | CRUD; Administrator only |
| Angular `component/room-blocks/room-blocks.component.ts` | `/room-blocks` — calendar-style list + form |

#### Entity Design

```csharp
// RoomBlock.cs
public class RoomBlock
{
    // RoomBlock.cs
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; }
    public RoomBlockType BlockType { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Scaffold Steps

1. `Shared/Models/RoomBlock.cs` + `Shared/Enums/RoomBlockType.cs`
2. `Shared/DTOs/RoomBlockDTO.cs` — include `string RoomNumber` (denormalized)
3. `HotelDbContext.cs` — add `DbSet<RoomBlock>`
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<RoomBlock, RoomBlockDTO>().ReverseMap()`
6. `API/Controllers/RoomBlocksController.cs` — `[Authorize(Roles = "Administrator")]`
7. `BookingsController.cs` — availability check must query `RoomBlocks` table; reject if overlap exists
8. Angular `RoomBlocksComponent` at `/room-blocks` — `canActivate: [authGuard, roleGuard]`, `data: { roles: ['Administrator'] }`
9. Add nav entry in `menus.ts` — `requiredRoles: ['Administrator']`

---

## Phase 13 — Customer Experience & Security

### Overview
Enhances the customer-facing experience with a digital room key generation system, automated checkout reminders surfaced on the admin dashboard, and secure storage of customer identity proof documents.

### 13a — Customer Digital Key (Key Gen System)

Generate a time-limited digital access key (token) for a customer upon booking confirmation. The key can be shown as a QR code or alphanumeric code to simulate room access.

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/RoomKey.cs` | Key record — `BookingId`, `KeyCode` (GUID), `IssuedAt`, `ExpiresAt`, `IsRevoked` |
| `Shared/DTOs/RoomKeyDTO.cs` | Flat DTO — `string KeyCode`, `string QrPayload`, `DateTime ExpiresAt` |
| `API/Controllers/RoomKeysController.cs` | `POST /api/roomkeys/generate/{bookingId}` — Administrator/Customer; `GET /api/roomkeys/my` — Customer own |
| `Shared/Utilities/KeyGenUtility.cs` | Static — generates `Guid`-based key codes and QR payload strings |
| Angular `component/room-keys/room-keys.component.ts` | `/room-keys` — Customer sees active keys with QR display |

#### Entity Design

```csharp
// RoomKey.cs
public class RoomKey
{
    // RoomKey.cs
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public string KeyCode { get; set; }    // GUID-based unique token
    public string QrPayload { get; set; } // encoded string for QR rendering
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface RoomKey {
  id: number;
  bookingId: number;
  keyCode: string;
  qrPayload: string;
  issuedAt: string;
  expiresAt: string;
  isRevoked: boolean;
}
```

#### Scaffold Steps

1. `Shared/Models/RoomKey.cs` — entity; unique index on `KeyCode`
2. `Shared/DTOs/RoomKeyDTO.cs`
3. `Shared/Utilities/KeyGenUtility.cs` — `static string GenerateKeyCode()` (`Guid.NewGuid().ToString("N").ToUpper()`); `static string GenerateQrPayload(string keyCode, int bookingId)`
4. `HotelDbContext.cs` — add `DbSet<RoomKey>`; unique index on `KeyCode`
5. Add EF Core migration
6. `MappingProfile.cs` — `CreateMap<RoomKey, RoomKeyDTO>().ReverseMap()`
7. `API/Controllers/RoomKeysController.cs` — key generation auto-triggers on booking confirmation; customer can view own keys; admin can revoke
8. Angular `RoomKeysComponent` at `/room-keys` — display `KeyCode` in a styled card; render QR via `qrcode` npm package (approved exception for this component only)
9. Route `/room-keys` with `data: { roles: ['Customer', 'Administrator'] }`

#### Notes
- Key `ExpiresAt` = `Booking.CheckOutDate + 2 hours` (grace period)
- Revocation endpoint: `PUT /api/roomkeys/{id}/revoke` — sets `IsRevoked = true`
- QR package: `npm install qrcode` — import `QRCodeComponent` only in `RoomKeysComponent`

---

### 13b — Auto-Checkout & Pending Checkout Dashboard Widget

Flag bookings where `CheckOutDate` has passed but status is still `CheckedIn`. Surface these on the Admin Dashboard as an actionable widget.

#### New / Modified Files

| File | Change |
|---|---|
| `Shared/Enums/BookingStatus.cs` | Ensure `PendingCheckout` value exists (add if missing) |
| `API/Controllers/BookingsController.cs` | Add `GET /api/bookings/pending-checkout` — returns bookings past due date with `CheckedIn` status; `[Authorize(Roles = "Administrator")]` |
| `API/Services/AutoCheckoutService.cs` | `IHostedService` — runs every 15 min; queries overdue bookings; sets status to `PendingCheckout`; creates a `Notification` per affected booking |
| `Program.cs` | Register `AutoCheckoutService` via `builder.Services.AddHostedService<AutoCheckoutService>()` |
| Angular `AdminDashboardComponent` | Add "Pending Checkouts" card — polls `GET /api/bookings/pending-checkout`; shows count badge + table |

#### Scaffold Steps

1. Add `PendingCheckout` to `BookingStatus` enum (if absent)
2. `API/Services/AutoCheckoutService.cs` — implement `IHostedService`; use `IServiceScopeFactory` to resolve `IRepository<Booking>` inside the background scope
3. Register in `Program.cs`
4. `BookingsController.cs` — add `GET pending-checkout` action filtered to overdue `CheckedIn` bookings
5. Angular `AdminDashboardComponent` — new card section; signal-based polling with `setInterval` (60-second refresh); `MatBadge` on nav icon when count > 0

#### Notes
- `AutoCheckoutService` must use `IServiceScopeFactory` — never inject scoped services directly into a singleton `IHostedService`
- Auto-status change only sets `PendingCheckout`; actual `CheckedOut` transition requires admin confirmation

---

### 13c — Customer ID Proof Storage

Allow customers to upload government-issued ID proof documents during booking. Files are stored under `wwwroot/{company-name}/assets/{Customer-Name}/`.

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/CustomerIdProof.cs` | Proof record — `UserId`, `BookingId`, `FileName`, `FilePath`, `UploadedAt`, `VerifiedByAdminId` |
| `Shared/DTOs/CustomerIdProofDTO.cs` | Flat DTO — `string CustomerName`, `string FileName`, `string FileUrl` |
| `API/Controllers/CustomerIdProofsController.cs` | `POST /api/idproofs/upload` (Customer); `GET /api/idproofs/{bookingId}` (Administrator); `PUT /api/idproofs/{id}/verify` (Administrator) |
| `Shared/Utilities/FileStorageUtility.cs` | Static — resolves and creates the path `wwwroot/{companyName}/assets/{customerName}/`; returns relative URL |

#### Entity Design

```csharp
// CustomerIdProof.cs
public class CustomerIdProof
{
    // CustomerIdProof.cs
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }          // relative path under wwwroot
    public DateTime UploadedAt { get; set; }
    public int? VerifiedByAdminId { get; set; }   // nullable — null = not yet verified
    public User VerifiedByAdmin { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface CustomerIdProof {
  id: number;
  userId: number;
  customerName: string;   // denormalized — avoid complex object graph
  bookingId: number;
  fileName: string;
  fileUrl: string;
  uploadedAt: string;
  verifiedByAdminId: number | null;
}
```

#### Scaffold Steps

1. `Shared/Models/CustomerIdProof.cs`
2. `Shared/DTOs/CustomerIdProofDTO.cs` — include `string CustomerName`, `string FileUrl` (denormalized)
3. `Shared/Utilities/FileStorageUtility.cs`:
   ```csharp
   public static class FileStorageUtility
   {
       public static string BuildPath(string wwwrootPath, string companyName, string customerName)
           => Path.Combine(wwwrootPath, SanitizeName(companyName), "assets", SanitizeName(customerName));

       public static string SanitizeName(string name)
           => string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "-").ToLower();
   }
   ```
4. `HotelDbContext.cs` — add `DbSet<CustomerIdProof>`
5. Add EF Core migration
6. `MappingProfile.cs` — `CreateMap<CustomerIdProof, CustomerIdProofDTO>().ReverseMap()`
7. `API/Controllers/CustomerIdProofsController.cs` — accept `IFormFile`; call `FileStorageUtility.BuildPath`; use `IWebHostEnvironment.WebRootPath` for absolute base; save file; persist record
8. Angular `BookingsComponent` — add file upload input on booking detail view; `POST` with `FormData` to `api/idproofs/upload`
9. Admin view — show verification badge on booking detail; `PUT /api/idproofs/{id}/verify` button

#### Notes
- Only accept: `.jpg`, `.jpeg`, `.png`, `.pdf` — validate `ContentType` server-side
- Maximum file size: 5 MB — enforce via `[RequestSizeLimit]` attribute
- Never serve raw file paths in DTOs — always return a relative URL like `/company-name/assets/customer-name/file.jpg`
- Path traversal protection: always call `FileStorageUtility.SanitizeName` before constructing any path

---

## Phase 14 — Multi-Hotel Ownership

### Overview
Extends Phase 9 (Multi-Tenant SaaS) to model the concept of an **Owner** who can manage multiple hotel properties under a single account. Each `Hotel` is a property entity owned by a `User` with the `Owner` role. Tenants from Phase 9 map to organizations; hotels are the physical properties within a tenant.

### New Roles & Policies

| Addition | Detail |
|---|---|
| Role: `Owner` | Can create/manage their own hotels; cannot access other owners' hotels |
| Policy: `ManageOwnHotels` | `Permission = "ManageOwnHotels"` claim; assigned to `Owner` role |

### New Files

| File | Purpose |
|---|---|
| `Shared/Models/Hotel.cs` | Hotel property — `Name`, `Address`, `OwnerId` (FK to `User`), `TenantId` |
| `Shared/Enums/HotelStatus.cs` | `Active`, `Inactive`, `UnderRenovation` |
| `Shared/DTOs/HotelDTO.cs` | Flat DTO — `string OwnerName` (denormalized) |
| `API/Controllers/HotelsController.cs` | CRUD; `Owner` sees own; `Administrator`/`SuperAdmin` sees all |
| Angular `component/hotels/hotels.component.ts` | `/hotels` — Owner-facing property list |

### Entity Design

```csharp
// Hotel.cs
public class Hotel
{
    // Hotel.cs
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public int StarRating { get; set; }     // 1–5
    public HotelStatus Status { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; }
    public ICollection<Room> Rooms { get; set; }
}
```

### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface Hotel {
  id: number;
  name: string;
  address: string;
  city: string;
  country: string;
  starRating: number;
  status: string;
  ownerId: number;
  ownerName: string;  // denormalized — avoid complex object graph
}
```

### Impact on Existing Entities

| Entity | Change |
|---|---|
| `Room.cs` | Add `int HotelId` FK + `Hotel Hotel` navigation |
| `RoomDTO.cs` | Add `int HotelId` + `string HotelName` (denormalized) |
| `BookingsController.cs` | Scope available rooms to `HotelId` when querying for a booking |

### Scaffold Steps

1. `Shared/Models/Hotel.cs` + `Shared/Enums/HotelStatus.cs`
2. `Shared/DTOs/HotelDTO.cs` — include `string OwnerName` (denormalized)
3. Add `int HotelId` + `Hotel Hotel` to `Room.cs`; add `int HotelId` + `string HotelName` to `RoomDTO.cs`
4. `HotelDbContext.cs` — add `DbSet<Hotel>`; unique index on `Name + OwnerId`; configure cascade for `Hotel → Rooms`
5. Add `Owner` role seed (Id = 4) + `ManageOwnHotels` policy in `Program.cs`
6. Add EF Core migration
7. `MappingProfile.cs` — `CreateMap<Hotel, HotelDTO>().ReverseMap()`
8. `API/Controllers/HotelsController.cs` — Owner sees only `OwnerId == currentUserId`; Administrator/SuperAdmin sees all
9. Angular `HotelsComponent` at `/hotels` — `canActivate: [authGuard, roleGuard]`, `data: { roles: ['Owner', 'Administrator', 'SuperAdmin'] }`
10. Add nav entry in `menus.ts` — `requiredRoles: ['Owner', 'Administrator']`

### Notes
- `Room.cs` previously linked to `RoomType` and `Amenity` only — `HotelId` is a new non-nullable FK; migration must supply a default value or be applied carefully in production
- `Owner` role is distinct from `Administrator` — an Owner cannot manage users, staff, or other owners' properties

---

## Phase 15 — KPI Dashboard & Analytics

### Overview
Extends Phase 11c (Reports) with a rich KPI dashboard visible to Administrators and Owners. Surfaces booking type distribution, customer type segmentation, revenue trends, occupancy heatmaps, and room-type popularity — all using `ng2-charts` (already approved).

### New Endpoints (add to `ReportsController.cs`)

| Endpoint | Returns | Role |
|---|---|---|
| `GET /api/reports/booking-types` | `{ bookingType, count }[]` (`Nightly` vs `Hourly` split) | Administrator |
| `GET /api/reports/customer-types` | `{ customerType, count }[]` (New vs Returning) | Administrator |
| `GET /api/reports/room-type-demand` | `{ roomTypeName, bookingCount, revenue }[]` | Administrator |
| `GET /api/reports/checkin-heatmap` | `{ dayOfWeek, hour, count }[]` (peak check-in times) | Administrator |
| `GET /api/reports/occupancy-by-hotel` | `{ hotelName, occupancyRate }[]` | Administrator, Owner |

### TypeScript Interfaces (add to `api.models.ts`)

```typescript
export interface BookingTypeReport {
  bookingType: string;
  count: number;
}

export interface CustomerTypeReport {
  customerType: string;   // 'New' | 'Returning'
  count: number;
}

export interface RoomTypeDemandReport {
  roomTypeName: string;
  bookingCount: number;
  revenue: number;
}

export interface CheckinHeatmapEntry {
  dayOfWeek: string;
  hour: number;
  count: number;
}

export interface OccupancyByHotel {
  hotelName: string;
  occupancyRate: number;
}
```

### Angular KPI Dashboard

| Component | Path | Role |
|---|---|---|
| `component/kpi-dashboard/kpi-dashboard.component.ts` | `/kpi-dashboard` | Administrator, Owner |

#### KPI Cards & Charts

| Widget | Chart Type | Data Source |
|---|---|---|
| Total Bookings (MTD) | `MatCard` stat | `bookings-summary` |
| Revenue (MTD) | `MatCard` stat | `revenue` |
| Avg Occupancy Rate | `MatCard` stat | `occupancy` |
| Booking Type Split | Doughnut chart | `booking-types` |
| Customer Type Split | Doughnut chart | `customer-types` |
| Room Type Demand | Horizontal bar chart | `room-type-demand` |
| Check-in Heatmap | Stacked bar chart | `checkin-heatmap` |
| Occupancy by Hotel | Bar chart | `occupancy-by-hotel` |

### Scaffold Steps

1. Add new endpoints to `API/Controllers/ReportsController.cs` (direct LINQ on `HotelDbContext` — existing exception applies)
2. Add TypeScript interfaces to `HotelManagement.UI/src/app/model/api.models.ts`
3. Angular `KpiDashboardComponent` at `/kpi-dashboard`:
   - `standalone: true`
   - `imports: [CommonModule, MatCardModule, MatGridListModule, BaseChartDirective]`
   - One `signal<T[]>([])` per dataset; load all in `ngOnInit` via parallel `ApiService.get()` calls
   - Use `ng2-charts` `BaseChartDirective` for all charts (already installed)
4. Route `/kpi-dashboard` with `data: { roles: ['Administrator', 'Owner', 'SuperAdmin'] }`
5. Add nav entry in `menus.ts` — `requiredRoles: ['Administrator', 'Owner']`

### Notes
- "Returning customer" = user who has more than one completed booking (`BookingStatus.CheckedOut`)
- `occupancy-by-hotel` endpoint only — Owner role should see data filtered to their own `HotelId`s; Administrator/SuperAdmin sees all
- All chart data signals must be typed — no `any[]`
- Do NOT duplicate `ng2-charts` install; it is already added in Phase 11c

---

## Phase 16 — Room Content & Media

### Overview
Enrich room listings with photos and descriptive content so customers can make informed booking decisions from `BrowseRoomsComponent`.

### 16a — Room Photos

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/RoomPhoto.cs` | Photo record — `RoomId`, `FileName`, `FilePath`, `AltText`, `DisplayOrder`, `IsPrimary` |
| `Shared/DTOs/RoomPhotoDTO.cs` | Flat DTO — `int RoomId`, `string RoomNumber`, `string FileUrl` |
| `API/Controllers/RoomPhotosController.cs` | `POST /api/roomphotos/upload/{roomId}` (Administrator); `GET /api/roomphotos/{roomId}` (AllowAnonymous); `DELETE /api/roomphotos/{id}` (Administrator); `PUT /api/roomphotos/{id}/primary` (Administrator) |

#### Entity Design

```csharp
// RoomPhoto.cs
public class RoomPhoto
{
    // RoomPhoto.cs
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }     // relative path under wwwroot
    public string AltText { get; set; }
    public int DisplayOrder { get; set; }    // lower = shown first
    public bool IsPrimary { get; set; }      // one primary photo per room
    public DateTime UploadedAt { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface RoomPhoto {
  id: number;
  roomId: number;
  roomNumber: string;   // denormalized — avoid complex object graph
  fileUrl: string;
  altText: string;
  displayOrder: number;
  isPrimary: boolean;
  uploadedAt: string;
}
```

#### Scaffold Steps

1. `Shared/Models/RoomPhoto.cs`
2. `Shared/DTOs/RoomPhotoDTO.cs` — include `string RoomNumber` (denormalized), `string FileUrl`
3. `HotelDbContext.cs` — add `DbSet<RoomPhoto>`; configure cascade delete on Room → Photos
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<RoomPhoto, RoomPhotoDTO>().ReverseMap()`
6. `API/Controllers/RoomPhotosController.cs` — use `FileStorageUtility` (Phase 13c) for path construction under `wwwroot/rooms/{roomId}/`; accept `IFormFile`; enforce 5 MB max via `[RequestSizeLimit]`; accept only `.jpg`, `.jpeg`, `.png`, `.webp`
7. Angular `RoomsComponent` (admin) — photo upload panel per room; drag-and-drop order via `MatList`
8. Angular `BrowseRoomsComponent` — display primary photo as card image; image gallery carousel on room detail

#### Notes
- Only one `IsPrimary = true` per room — enforce in controller: set all others to `false` on primary change
- `DisplayOrder` resequencing endpoint: `PUT /api/roomphotos/reorder` accepts `[{ id, displayOrder }]` array
- Do NOT use a third-party carousel library — use Angular CDK or plain CSS for the gallery

---

### 16b — Room Description

#### Modified Files

| File | Change |
|---|---|
| `Shared/Models/Room.cs` | Add `string? ShortDescription`, `string? FullDescription`, `string? Highlights` |
| `Shared/DTOs/RoomDTO.cs` | Add `string? ShortDescription`, `string? FullDescription`, `string? Highlights` |

#### Scaffold Steps

1. Add `ShortDescription`, `FullDescription`, `Highlights` to `Room.cs` (all nullable strings)
2. Update `RoomDTO.cs` to mirror the new fields
3. Add EF Core migration
4. Angular `RoomsComponent` (admin) — textarea inputs for each description field in the room form
5. Angular `BrowseRoomsComponent` — display `ShortDescription` on cards; expand to `FullDescription` + `Highlights` on room detail view

#### Notes
- `FullDescription` may contain markdown; render client-side with a simple markdown pipe (no extra library — use Angular `DomSanitizer` + `innerHTML` with a stripped markdown-to-html transform)
- `Highlights` is a short bullet-point list stored as a pipe-delimited string (`"Free WiFi|King Bed|Sea View"`); split on `|` in Angular for display

---

## Phase 17 — Pricing Engine

### Overview
Introduces three pricing mechanisms on top of base room rates: date-specific rate segments (e.g. holiday surcharges), demand-based dynamic pricing (rate increases as occupancy rises), and customer/promotional discounts.

### 17a — Date-Specific Rate Segments

Allow admins to define custom price overrides for a room during specific date ranges (e.g. peak season, public holidays).

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/RoomRateSegment.cs` | Rate override — `RoomId`, `StartDate`, `EndDate`, `OverridePrice`, `Label` |
| `Shared/DTOs/RoomRateSegmentDTO.cs` | Flat DTO — `string RoomNumber`, `decimal OverridePrice` |
| `API/Controllers/RoomRateSegmentsController.cs` | CRUD; Administrator only |

#### Entity Design

```csharp
// RoomRateSegment.cs
public class RoomRateSegment
{
    // RoomRateSegment.cs
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OverridePrice { get; set; }   // per-night price during this period
    public string Label { get; set; }            // e.g. "Christmas", "Peak Season"
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface RoomRateSegment {
  id: number;
  roomId: number;
  roomNumber: string;       // denormalized
  startDate: string;
  endDate: string;
  overridePrice: number;
  label: string;
}
```

#### Scaffold Steps

1. `Shared/Models/RoomRateSegment.cs`
2. `Shared/DTOs/RoomRateSegmentDTO.cs`
3. `HotelDbContext.cs` — add `DbSet<RoomRateSegment>`
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<RoomRateSegment, RoomRateSegmentDTO>().ReverseMap()`
6. `API/Controllers/RoomRateSegmentsController.cs` — `[Authorize(Roles = "Administrator")]`; validate no overlapping segments for same room
7. `BookingsController.cs` — price calculation must check `RoomRateSegments` table; use `OverridePrice` for any night that falls within a segment, else fall back to `Room.PricePerNight`
8. Angular `RoomsComponent` (admin) — nested rate segment table per room; date-range form for adding segments
9. Angular `BrowseRoomsComponent` — show "From {min price}" when segments exist; highlight special rate periods

#### Notes
- Overlapping segments for the same room are rejected server-side with `400 Bad Request`
- Per-night cost = sum of each night's effective rate (iterate check-in to check-out, resolve segment or base price per night)
- `BookingsController` must expose an `GET /api/bookings/price-preview` endpoint accepting `roomIds[]`, `checkIn`, `checkOut` that returns the breakdown before confirming

---

### 17b — Dynamic Pricing (Demand-Based)

Automatically inflate room prices when occupancy for that room type exceeds configurable thresholds.

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/DynamicPricingRule.cs` | Rule — `RoomTypeId`, `OccupancyThresholdPercent`, `PriceMultiplier` |
| `Shared/DTOs/DynamicPricingRuleDTO.cs` | Flat DTO — `string RoomTypeName` (denormalized) |
| `API/Controllers/DynamicPricingController.cs` | CRUD; Administrator only |
| `API/Services/PricingEngine.cs` | Scoped service — `decimal ResolvePrice(Room room, DateTime date)` |

#### Entity Design

```csharp
// DynamicPricingRule.cs
public class DynamicPricingRule
{
    // DynamicPricingRule.cs
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; }
    public decimal OccupancyThresholdPercent { get; set; }  // e.g. 70 = 70%
    public decimal PriceMultiplier { get; set; }            // e.g. 1.25 = +25%
    public bool IsActive { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface DynamicPricingRule {
  id: number;
  roomTypeId: number;
  roomTypeName: string;    // denormalized
  occupancyThresholdPercent: number;
  priceMultiplier: number;
  isActive: boolean;
}
```

#### Pricing Resolution Order (highest priority first)

1. `RoomRateSegment` override (Phase 17a) — exact date match
2. `DynamicPricingRule` multiplier — applied to the base/segment price when occupancy ≥ threshold
3. `Room.PricePerNight` — base fallback

#### Scaffold Steps

1. `Shared/Models/DynamicPricingRule.cs`
2. `Shared/DTOs/DynamicPricingRuleDTO.cs`
3. `HotelDbContext.cs` — add `DbSet<DynamicPricingRule>`
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<DynamicPricingRule, DynamicPricingRuleDTO>().ReverseMap()`
6. `API/Services/PricingEngine.cs` — scoped service; inject `HotelDbContext`; implement resolution order above; compute current occupancy as `booked rooms / total rooms` of same type for given date
7. Register `PricingEngine` as `AddScoped<PricingEngine>()` in `Program.cs`
8. `BookingsController.cs` — inject `PricingEngine`; use it for all price calculations (replaces direct `Room.PricePerNight` references)
9. `API/Controllers/DynamicPricingController.cs` — `[Authorize(Roles = "Administrator")]`
10. Angular `component/dynamic-pricing/dynamic-pricing.component.ts` at `/dynamic-pricing` — rule list + form; `data: { roles: ['Administrator'] }`

#### Notes
- `PricingEngine` is a **scoped service** (not singleton) — it may access `HotelDbContext`
- Multiple rules may match; apply the rule with the **highest** threshold that is still met (most specific)
- Price multiplier is always applied **after** segment override, not on base price independently

---

### 17c — Discounts

Allow admins to create promotional discount codes or automatic discounts (e.g., early-bird, loyalty) applied at booking time.

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/Discount.cs` | Discount record — `Code`, `DiscountType`, `Value`, `ValidFrom`, `ValidTo`, `MaxUses`, `UsedCount` |
| `Shared/Enums/DiscountType.cs` | `Percentage`, `FixedAmount` |
| `Shared/DTOs/DiscountDTO.cs` | Flat DTO |
| `Shared/DTOs/ApplyDiscountDTO.cs` | Request DTO — `string Code`, `int[] RoomIds`, `DateTime CheckIn`, `DateTime CheckOut` |
| `API/Controllers/DiscountsController.cs` | CRUD (Administrator); `POST /api/discounts/apply` (Customer, Administrator) |

#### Entity Design

```csharp
// Discount.cs
public class Discount
{
    // Discount.cs
    public int Id { get; set; }
    public string Code { get; set; }           // unique; case-insensitive
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }         // % or fixed amount
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? MaxUses { get; set; }          // null = unlimited
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface Discount {
  id: number;
  code: string;
  discountType: string;   // 'Percentage' | 'FixedAmount'
  value: number;
  validFrom: string;
  validTo: string;
  maxUses: number | null;
  usedCount: number;
  isActive: boolean;
  description: string | null;
}

export interface DiscountPreview {
  originalTotal: number;
  discountAmount: number;
  finalTotal: number;
  discountCode: string;
}
```

#### Scaffold Steps

1. `Shared/Models/Discount.cs` + `Shared/Enums/DiscountType.cs`
2. `Shared/DTOs/DiscountDTO.cs` + `Shared/DTOs/ApplyDiscountDTO.cs`
3. `HotelDbContext.cs` — add `DbSet<Discount>`; unique index on `Code`
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<Discount, DiscountDTO>().ReverseMap()`
6. `API/Controllers/DiscountsController.cs` — CRUD for admin; `POST /api/discounts/apply` validates code (active, within date range, under max uses) and returns `DiscountPreview`; does NOT yet increment `UsedCount` — that happens on booking confirmation
7. `BookingsController.cs` — accept optional `string? DiscountCode` on POST; validate via `DiscountsController` logic; apply discount to final price; increment `UsedCount` inside the booking transaction
8. Angular `BookingsComponent` — discount code input field; live preview of savings via `POST /api/discounts/apply`
9. Angular `component/discounts/discounts.component.ts` at `/discounts` — admin CRUD page; `data: { roles: ['Administrator'] }`

#### Notes
- Discount codes are case-insensitive — normalise to uppercase before storing and comparing
- `Percentage` discounts are capped at 100%; `FixedAmount` discounts are capped at the order total
- Concurrent use protection: increment `UsedCount` inside the booking transaction with optimistic concurrency check

---

## Phase 18 — Reviews & Bulk Operations

### Overview
Adds customer review capabilities for completed stays and administrator bulk-update tools for rooms and bookings.

### 18a — Customer Reviews

Allow customers to leave a rating and review after a completed stay. Reviews are visible on the `BrowseRoomsComponent` public page.

#### New Files

| File | Purpose |
|---|---|
| `Shared/Models/Review.cs` | Review record — `BookingId`, `UserId`, `RoomId`, `Rating`, `Comment`, `CreatedAt` |
| `Shared/DTOs/ReviewDTO.cs` | Flat DTO — `string GuestName`, `string RoomNumber` (denormalized) |
| `API/Controllers/ReviewsController.cs` | `POST /api/reviews` (Customer — one per booking); `GET /api/reviews/room/{roomId}` (AllowAnonymous); `DELETE /api/reviews/{id}` (Administrator) |

#### Entity Design

```csharp
// Review.cs
public class Review
{
    // Review.cs
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public int Rating { get; set; }       // 1–5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### TypeScript Interface (add to `api.models.ts`)

```typescript
export interface Review {
  id: number;
  bookingId: number;
  userId: number;
  guestName: string;    // denormalized
  roomId: number;
  roomNumber: string;   // denormalized
  rating: number;       // 1–5
  comment: string | null;
  createdAt: string;
}
```

#### Scaffold Steps

1. `Shared/Models/Review.cs` — unique index on `BookingId` (one review per booking)
2. `Shared/DTOs/ReviewDTO.cs` — include `string GuestName`, `string RoomNumber` (denormalized)
3. `HotelDbContext.cs` — add `DbSet<Review>`; unique index on `BookingId`
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<Review, ReviewDTO>().ReverseMap()`
6. `API/Controllers/ReviewsController.cs` — validate booking belongs to the current user and has `CheckedOut` status before allowing POST
7. Add `GET /api/rooms/{id}/average-rating` to `RoomsController` — returns `decimal` average from `Reviews` table
8. Angular `BrowseRoomsComponent` — show star rating badge on room cards; review list on room detail; review submission form for logged-in customers with completed bookings
9. Angular `RoomsComponent` (admin) — show average rating column in rooms table; delete review button

#### Notes
- Rating must be 1–5; validate server-side with `[Range(1, 5)]`
- Customers may only submit one review per booking (enforced by unique index on `BookingId`)
- `Comment` is optional; `Rating` is required
- Do NOT allow editing reviews after submission — delete and re-submit only (admin delete only)

---

### 18b — Bulk Update Operations

Allow administrators to apply changes to multiple rooms or bookings simultaneously.

#### New Files

| File | Purpose |
|---|---|
| `Shared/DTOs/BulkUpdateRoomsDTO.cs` | `int[] RoomIds`, `RoomStatus? Status`, `decimal? PricePerNight`, `int? RoomTypeId` |
| `Shared/DTOs/BulkUpdateBookingsDTO.cs` | `int[] BookingIds`, `BookingStatus? Status` |
| `Shared/DTOs/BulkOperationResultDTO.cs` | `int Updated`, `int Skipped`, `List<string> Errors` |

#### New Endpoints (add to existing controllers)

| Endpoint | Controller | Role |
|---|---|---|
| `PUT /api/rooms/bulk-update` | `RoomsController` | Administrator |
| `PUT /api/bookings/bulk-update` | `BookingsController` | Administrator |

#### Scaffold Steps

1. `Shared/DTOs/BulkUpdateRoomsDTO.cs` + `Shared/DTOs/BulkUpdateBookingsDTO.cs` + `Shared/DTOs/BulkOperationResultDTO.cs`
2. `RoomsController.cs` — add `PUT bulk-update`; only update fields that are non-null in the DTO; return `BulkOperationResultDTO`
3. `BookingsController.cs` — add `PUT bulk-update`; validate each booking transition is legal before applying; return `BulkOperationResultDTO`
4. Angular `RoomsComponent` — row checkboxes + "Bulk Actions" toolbar (change status, set price, reassign room type); `MatCheckbox` for selection; confirm dialog via `MatDialog` before sending
5. Angular `BookingsComponent` (admin view) — row checkboxes + "Bulk Status Update" dropdown; confirm dialog before sending

#### Notes
- Bulk operations are **non-transactional per row** — each record is attempted independently; failures accumulate in `BulkOperationResultDTO.Errors` and do not roll back successful updates
- Maximum 100 records per bulk request — enforce with `[MaxLength(100)]` on the `int[]` fields
- Audit each bulk change with a log entry if the Audit Log feature (backlog) is implemented
- Do NOT use EF Core `ExecuteUpdateAsync` — iterate and update via `IRepository<T>` to maintain repository pattern

---

## Phase 19 — Cancellation Rules

### Overview
Allow administrators to define tiered cancellation policies where the cancellation fee percentage increases as the booking's check-in date approaches. Each policy is attached to a room or room type and is enforced automatically when a customer cancels.

### New Files

| File | Purpose |
|---|---|
| `Shared/Models/CancellationPolicy.cs` | Policy header — `Name`, `Description`, applies to `RoomTypeId` (nullable = global default) |
| `Shared/Models/CancellationTier.cs` | Tier record — `PolicyId`, `DaysBeforeCheckIn`, `FeePercent` |
| `Shared/Enums/CancellationFeeType.cs` | `Percentage`, `FixedAmount`, `FirstNight` |
| `Shared/DTOs/CancellationPolicyDTO.cs` | Flat DTO — `string RoomTypeName` (denormalized), `List<CancellationTierDTO> Tiers` |
| `Shared/DTOs/CancellationTierDTO.cs` | Flat DTO — `int DaysBeforeCheckIn`, `decimal FeePercent` |
| `Shared/DTOs/CancellationPreviewDTO.cs` | Response DTO — `decimal RefundAmount`, `decimal FeeAmount`, `string TierApplied` |
| `API/Controllers/CancellationPoliciesController.cs` | CRUD; Administrator only |
| `API/Controllers/BookingsController.cs` | New `GET /api/bookings/{id}/cancellation-preview`; modify cancel endpoint to apply fee |

### Entity Design

```csharp
// CancellationPolicy.cs
public class CancellationPolicy
{
    // CancellationPolicy.cs
    public int Id { get; set; }
    public string Name { get; set; }             // e.g. "Standard", "Non-Refundable"
    public string? Description { get; set; }
    public int? RoomTypeId { get; set; }         // null = global default policy
    public RoomType? RoomType { get; set; }
    public bool IsDefault { get; set; }
    public ICollection<CancellationTier> Tiers { get; set; }
}

// CancellationTier.cs
public class CancellationTier
{
    // CancellationTier.cs
    public int Id { get; set; }
    public int PolicyId { get; set; }
    public CancellationPolicy Policy { get; set; }
    public int DaysBeforeCheckIn { get; set; }   // applies when cancelling <= this many days before
    public decimal FeePercent { get; set; }      // e.g. 25 = 25% of TotalAmount charged as fee
    public CancellationFeeType FeeType { get; set; }
}
```

### Example Tier Configuration

| Days Before Check-In | Fee |
|---|---|
| > 14 days | 0% (free cancellation) |
| 8–14 days | 25% |
| 3–7 days | 50% |
| 1–2 days | 75% |
| Same day / no-show | 100% |

### TypeScript Interfaces (add to `api.models.ts`)

```typescript
export interface CancellationPolicy {
  id: number;
  name: string;
  description: string | null;
  roomTypeId: number | null;
  roomTypeName: string | null;  // denormalized
  isDefault: boolean;
  tiers: CancellationTier[];
}

export interface CancellationTier {
  id: number;
  policyId: number;
  daysBeforeCheckIn: number;
  feePercent: number;
  feeType: string;
}

export interface CancellationPreview {
  bookingId: number;
  totalAmount: number;
  feeAmount: number;
  refundAmount: number;
  tierApplied: string;   // e.g. "75% fee — within 2 days of check-in"
}
```

### Scaffold Steps

1. `Shared/Models/CancellationPolicy.cs` + `Shared/Models/CancellationTier.cs` + `Shared/Enums/CancellationFeeType.cs`
2. `Shared/DTOs/CancellationPolicyDTO.cs` + `Shared/DTOs/CancellationTierDTO.cs` + `Shared/DTOs/CancellationPreviewDTO.cs`
3. `HotelDbContext.cs` — add `DbSet<CancellationPolicy>`, `DbSet<CancellationTier>`; seed one default `IsDefault = true` policy (free cancellation > 7 days; 100% fee ≤ 1 day)
4. Add EF Core migration
5. `MappingProfile.cs` — `CreateMap<CancellationPolicy, CancellationPolicyDTO>().ReverseMap()`; same for `CancellationTier`
6. `API/Controllers/CancellationPoliciesController.cs` — `[Authorize(Roles = "Administrator")]`; enforce only one `IsDefault = true` at a time
7. `BookingsController.cs`:
   - Add `GET /api/bookings/{id}/cancellation-preview` — resolve applicable policy (room-type specific, else default); find the matching tier by days remaining; return `CancellationPreviewDTO`
   - Modify the cancel action (`PUT /api/bookings/{id}/cancel`) — call the same resolution logic; deduct `FeeAmount` from any refund; record `CancellationFee` on the booking
8. Add `decimal CancellationFee` field to `Booking.cs` and `BookingDTO.cs`
9. Angular `BookingsComponent` — show "Cancel Booking" button; on click, call `GET cancellation-preview` and display fee breakdown in a `MatDialog` before confirming
10. Angular `component/cancellation-policies/cancellation-policies.component.ts` at `/cancellation-policies` — admin CRUD for policies + tier table per policy
11. Add nav entry in `menus.ts` — `requiredRoles: ['Administrator']`

### Notes
- Policy resolution order: room-type-specific policy > global default (`IsDefault = true`)
- Tier matching: select the tier where `DaysBeforeCheckIn` is the **lowest value that is still ≥ actual days remaining** (most punitive tier that applies)
- If no tier matches (cancellation far in advance), fee = 0
- `CancellationFee` stored on `Booking` at time of cancellation for audit purposes — do NOT recompute after the fact
- `FeeType = FirstNight` charges the cost of the first night regardless of percentage

---

## Phase 20 — Email Notifications

### Overview
Send transactional emails to customers for key lifecycle events: invoice generation, booking confirmation, booking cancellation (with fee breakdown), and digital key issuance. Uses `MailKit` for SMTP delivery via a shared `IEmailService`.

### New Files

| File | Purpose |
|---|---|
| `Shared/Interfaces/IEmailService.cs` | Interface — `Task SendAsync(EmailMessage message)` |
| `Shared/Models/EmailMessage.cs` | Value object — `To`, `Subject`, `HtmlBody`, `PlainTextBody` |
| `API/Services/SmtpEmailService.cs` | `IEmailService` implementation using `MailKit`; reads config from `appsettings.json` |
| `Shared/Utilities/EmailTemplateUtility.cs` | Static — builds HTML email bodies from inline templates for each email type |

### Triggered Email Events

| Trigger | Recipient | Template |
|---|---|---|
| Invoice created (`POST /api/invoices`) | Customer | Invoice summary with line items + total + due date + PDF link |
| Booking confirmed (`POST /api/bookings`) | Customer | Booking reference, room(s), dates, total price |
| Booking cancelled | Customer | Cancellation confirmation + fee breakdown (Phase 19 data) |
| Room key generated (`POST /api/roomkeys/generate`) | Customer | Key code + QR payload + expiry time |

### Configuration (`appsettings.json`)

```json
"EmailSettings": {
  "SmtpHost": "smtp.example.com",
  "SmtpPort": 587,
  "UseSsl": true,
  "Username": "no-reply@hotel.com",
  "Password": "<secret>",
  "FromAddress": "no-reply@hotel.com",
  "FromName": "Hotel Management System"
}
```

### Interface & Model Design

```csharp
// IEmailService.cs
public interface IEmailService
{
    Task SendAsync(EmailMessage message);
}

// EmailMessage.cs
public class EmailMessage
{
    // EmailMessage.cs
    public string To { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string? PlainTextBody { get; set; }
}
```

### Scaffold Steps

1. Add `MailKit` NuGet package to `HotelManagementSystem.API.csproj`
2. `Shared/Interfaces/IEmailService.cs` — `Task SendAsync(EmailMessage message)`
3. `Shared/Models/EmailMessage.cs` — value object with `To`, `Subject`, `HtmlBody`, `PlainTextBody?`
4. `Shared/Utilities/EmailTemplateUtility.cs` — static methods:
   - `static string InvoiceEmail(InvoiceDTO invoice)` — returns HTML with line items table + due date
   - `static string BookingConfirmationEmail(BookingDTO booking)` — room list, dates, total
   - `static string BookingCancellationEmail(BookingDTO booking, CancellationPreviewDTO fee)` — includes fee breakdown
   - `static string RoomKeyEmail(RoomKeyDTO key, BookingDTO booking)` — key code + expiry
5. `API/Services/SmtpEmailService.cs` — implement `IEmailService`; use `MimeKit.MimeMessage` + `MailKit.Net.Smtp.SmtpClient`; read config via `IOptions<EmailSettings>`
6. Register in `Program.cs`:
   ```csharp
   builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
   builder.Services.AddScoped<IEmailService, SmtpEmailService>();
   ```
7. Inject `IEmailService` into:
   - `InvoicesController` — send invoice email after successful POST
   - `BookingsController` — send confirmation after POST; send cancellation email after cancel action
   - `RoomKeysController` — send key email after successful generate action
8. Email sending must be **fire-and-forget** (`_ = emailService.SendAsync(...)`) — do NOT await in the HTTP request path; email failure must never fail the API response

### Notes
- Store `EmailSettings` as a POCO bound via `IOptions<EmailSettings>` — never read `IConfiguration` directly in `SmtpEmailService`
- HTML templates are inline C# string interpolation — do NOT introduce a Razor or Handlebars template engine
- Do NOT store sent email records in the database in this phase — use application logs only (`ILogger<SmtpEmailService>`)
- For local development, set `SmtpHost` to `localhost` and use a tool like **MailHog** or **Papercut SMTP** to intercept emails
- `PlainTextBody` is optional but recommended as a fallback for email clients that block HTML

---

## Cross-Cutting Concerns

### Row-Level Tenant Isolation Rules

- Every controller action that reads tenanted data **must** filter by `TenantId` from `HttpContext.Items["TenantId"]`
- `SuperAdmin` role bypasses tenant filtering (sees all tenants)
- DTOs for tenanted entities must include `int TenantId` + `string TenantName` (denormalized)
- Do NOT use global query filters in `HotelDbContext` for tenant filtering — apply filters explicitly in each controller to maintain clarity

### ng2-charts Usage Rules

- Import `BaseChartDirective` only in `ReportsComponent`
- Chart data must use `signal()` for reactivity
- Do NOT use ng2-charts in any other component — use Angular Material primitives instead

### Import/Export Rules

- Always validate headers before bulk operations
- Return `ImportResultDTO` (imported count + errors) — never silently swallow row errors
- Export CSV must stream via `FileStreamResult` for large datasets
- Do NOT store uploaded files on disk — process from `IFormFile.OpenReadStream()` directly

---

## Future Consideration (Backlog)

| Feature | Notes |
|---|---|
| Real-time notifications | SignalR hub in API; Angular `HubConnection` in `NotificationsComponent` |
| Audit log | `AuditLog` entity — auto-populated by a custom `SaveChanges` override in `HotelDbContext` |
| Room calendar view | Angular Material CDK virtual scroll; no additional charting library needed |
| Mobile PWA | `@angular/service-worker`; `manifest.webmanifest` already supported by Angular CLI |
| Payment gateway | Stripe SDK — server-side only; never store card data; use `PaymentIntent` API |
| Digital key NFC/BLE handoff | Extend Phase 13a key gen to emit NFC/BLE payload for physical lock integration |
| Loyalty programme | Points accrual on `CheckedOut` bookings; `LoyaltyAccount` entity with `Points` balance |
| Smart room assignment | Auto-assign best available room based on customer preference history (Phase 12b data) |
| Multi-currency billing | `CurrencyCode` field on `Invoice`; exchange rate snapshot at invoice generation time |
