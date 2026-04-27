# Domain: Auth

## Overview
Authentication and authorization use **ASP.NET Core Identity** for user/role management combined with **JWT Bearer tokens** for stateless API access. Authorization uses role-based and claim-based policies.

---

## Identity Configuration

Configured in `Program.cs`:

```csharp
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<HotelDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
});
```

---

## JWT Configuration

JWT Bearer authentication added in `Program.cs` using settings from `appsettings.json` (`JwtSettings` section with `SecretKey`, `Issuer`, `Audience`). Tokens expire in 30 minutes and use `HmacSha256`.

---

## Registration Flow

New users always receive the `Customer` role and a `Department:Sales` claim:

```csharp
var result = await _userManager.CreateAsync(user, model.Password);
await _userManager.AddToRoleAsync(user, "Customer");
await _userManager.AddClaimAsync(user, new Claim("Department", "Sales"));
```

---

## Login & Token Generation

Login validates credentials with `SignInManager`, then generates a JWT token:

```csharp
var token = GenerateJwtToken(user);
return Ok(new { Token = token });
```

Token claims include:
- `sub` (username)
- `jti` (new GUID)
- `NameIdentifier` (user ID)
- All user-level claims (from `UserManager.GetClaimsAsync`)
- All role-level claims (fetched from each role via `RoleManager.GetClaimsAsync`)
- `ClaimTypes.Role` for each role the user belongs to

---

## Authorization on Controllers

```csharp
[Authorize(Roles = "Administrator")]
[Authorize(Policy = "ManageRooms")]
public class RoomsController : ControllerBase { ... }

[Authorize(Roles = "Administrator")]
public class RolesController : ControllerBase { ... }

[AllowAnonymous]
public class AccountController : ControllerBase { ... }
```

---

## Seeded Roles

Three roles are seeded in `HotelDbContext`:
| Id | Name | NormalizedName |
|---|---|---|
| 1 | Administrator | ADMINISTRATOR |
| 2 | Guest | GUEST |
| 3 | Customer | CUSTOMER |

---

## Password Hashing Utility

A separate BCrypt-based utility exists in `Shared/Utilities/PasswordHasher.cs` (independent of Identity's hasher):

```csharp
public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
public static bool VerifyPassword(string password, string hashedPassword) => BCrypt.Net.BCrypt.Verify(password, hashedPassword);
```
