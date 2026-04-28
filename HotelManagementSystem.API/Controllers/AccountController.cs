using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelManagementSystem.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, RoleManager<Role> roleManager, IRepository<UserTenant> userTenantRepo, IRepository<Tenant> tenantRepo) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly RoleManager<Role> _roleManager = roleManager;
        private readonly IRepository<UserTenant> _userTenantRepo = userTenantRepo;
        private readonly IRepository<Tenant> _tenantRepo = tenantRepo;

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "Customer");
            await _userManager.AddClaimAsync(user, new Claim("Department", "Sales"));
            return Ok(new { Message = "User registered successfully!" });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized();

            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            var roleClaims = new List<Claim>();
            foreach (var role in userRoles)
            {
                var roleObject = await _roleManager.FindByNameAsync(role);
                if (roleObject != null)
                {
                    var roleClaimsList = await _roleManager.GetClaimsAsync(roleObject);
                    roleClaims.AddRange(roleClaimsList);
                }
            }

            // Resolve user's primary tenant (first active membership) for TenantId claim
            var allUserTenants = await _userTenantRepo.GetAllAsync();
            var primaryTenant = allUserTenants.FirstOrDefault(ut => ut.UserId == user.Id);

            var baseClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            if (primaryTenant != null)
            {
                baseClaims.Add(new Claim("TenantId", primaryTenant.TenantId.ToString()));
                var tenantEntity = await _tenantRepo.GetByIdAsync(primaryTenant.TenantId);
                if (tenantEntity != null)
                {
                    baseClaims.Add(new Claim("CurrencyCode", tenantEntity.CurrencyCode ?? "INR"));
                    baseClaims.Add(new Claim("Locale",       tenantEntity.Locale       ?? "en-IN"));
                }
            }

            var claims = baseClaims
            .Union(userClaims)
            .Union(roleClaims)
            .Union(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
