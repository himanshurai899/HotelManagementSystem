# HotelManagementSystem

A **.NET 8 + Angular 19** hotel management platform consisting of a REST API, Blazor Server client/admin apps, and an Angular Material SPA.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js & npm](https://nodejs.org/) (v18+ recommended)
- [Angular CLI](https://angular.io/cli) — `npm install -g @angular/cli`
- SQL Server / LocalDB

---

## 1. Database — Run Migrations

Migrations are managed from the `HotelManagementSystem.Shared` project using the **Package Manager Console** (Visual Studio) or the **.NET EF CLI**.

### Package Manager Console (Visual Studio)

> Set **Default project** to `HotelManagementSystem.Shared` and **Startup project** to `HotelManagementSystem.API`.

```powershell
# Create a new migration
Add-Migration InitialCreate

# Apply migrations to the database
Update-Database
```

### .NET EF CLI (terminal)

```bash
# From the solution root
dotnet ef migrations add InitialCreate \
  --project HotelManagementSystem.Shared \
  --startup-project HotelManagementSystem.API

dotnet ef database update \
  --project HotelManagementSystem.Shared \
  --startup-project HotelManagementSystem.API
```

> **Connection string** is configured in `HotelManagementSystem.API/appsettings.json` under `ConnectionStrings:DefaultConnection`.  
> Default: `Server=(localdb)\mssqllocaldb;Database=HotelManagementSystem;Trusted_Connection=True;`

---

## 2. Run the API

```bash
cd HotelManagementSystem.API
dotnet run
```

| Profile | URL |
|---------|-----|
| HTTP    | http://localhost:5180 |
| HTTPS   | https://localhost:7204 |
| Swagger | http://localhost:5180/swagger |

---

## 3. Run the Blazor Client (Customer-facing)

```bash
cd HotelManagementSystem.Client
dotnet run
```

---

## 4. Run the Blazor Admin Panel

```bash
cd HotelManagementSystem.Admin
dotnet run
```

---

## 5. Run the Angular Frontend

```bash
cd HotelManagement.UI

# Install dependencies (first time only)
npm install

# Start the development server
ng serve --port 5863
# or
npm start
```

The Angular app will be available at **http://localhost:5863**.

| Script | Command | Description |
|--------|---------|-------------|
| Start dev server | `ng serve --port 5863` | Serves the app at `localhost:5863` with live reload |
| Build | `npm run build` | Production build output to `dist/` |
| Build (watch) | `npm run watch` | Development build with file watching |
| Run tests | `npm test` | Runs unit tests via Karma |

---

## Project Structure

| Project | Type | Default URL |
|---------|------|-------------|
| `HotelManagementSystem.API` | ASP.NET Core Web API | https://localhost:7204 |
| `HotelManagementSystem.Shared` | Class Library (EF Core, Models, Repos) | — |
| `HotelManagementSystem.Client` | Blazor Server — Customer UI | — |
| `HotelManagementSystem.Admin` | Blazor Server — Admin Panel | — |
| `HotelManagement.UI` | Angular 19 SPA | http://localhost:5863 |
