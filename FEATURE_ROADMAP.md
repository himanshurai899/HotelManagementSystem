# Hotel Management System — Feature Roadmap

> This file tracks planned features beyond the initial 7-phase implementation.  
> For coding conventions, architecture rules, and scaffold patterns see [.github/copilot-instructions.md](.github/copilot-instructions.md).

---

## Implementation Status

| Phase | Feature Area | Status |
|---|---|---|
| 8 | Billing & Payments | ⬜ Planned |
| 9 | Multi-Tenant SaaS | ⬜ Planned |
| 10 | Import / Export | ⬜ Planned |
| 11 | UX Enhancements | ⬜ Planned |

> **Status key:** ⬜ Planned · 🔄 In Progress · ✅ Done

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
| Email notifications | `IEmailService` in `Shared/Interfaces/`; SMTP via `MailKit` NuGet |
| Audit log | `AuditLog` entity — auto-populated by a custom `SaveChanges` override in `HotelDbContext` |
| Room calendar view | Angular Material CDK virtual scroll; no additional charting library needed |
| Mobile PWA | `@angular/service-worker`; `manifest.webmanifest` already supported by Angular CLI |
| Payment gateway | Stripe SDK — server-side only; never store card data; use `PaymentIntent` API |
