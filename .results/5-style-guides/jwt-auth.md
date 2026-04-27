# Style Guide: JWT Auth

## Unique Conventions in This Project

### 1. JWT Generation Inside AccountController (Not a Service)
JWT token generation is a private method on `AccountController` — it is not extracted into a dedicated service class despite `JwtTokenProvider.cs` existing as a separate class (which implements `IUserTwoFactorTokenProvider<TUser>` for 2FA purposes, not general login flow):

```csharp
// AccountController.cs
private async Task<string> GenerateJwtToken(User user) { ... }
```

### 2. Claims Built by Aggregating User Claims + Role Claims + Role Names
The JWT payload combines three sources:
1. User-level claims from `_userManager.GetClaimsAsync(user)`
2. All claims from each role the user belongs to (fetched via `_roleManager.GetClaimsAsync(roleObject)`)
3. `ClaimTypes.Role` for each role name

```csharp
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.UserName),
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new(ClaimTypes.NameIdentifier, user.Id.ToString())
}
.Union(userClaims)
.Union(roleClaims)
.Union(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));
```

### 3. Settings from IConfiguration Section "JwtSettings"
The JWT settings are read from `_configuration.GetSection("JwtSettings")` — not bound to a typed options class in the controller:

```csharp
var jwtSettings = _configuration.GetSection("JwtSettings");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
```

### 4. 30-Minute Expiry
Tokens always expire 30 minutes from generation:
```csharp
expires: DateTime.Now.AddMinutes(30)
```

### 5. HmacSha256 Signing
```csharp
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
```
