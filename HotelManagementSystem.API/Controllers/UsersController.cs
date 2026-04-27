using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,SuperAdmin", Policy = "ManageUsers")]
    public class UsersController(UserManager<User> userManager, RoleManager<Role> roleManager) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly RoleManager<Role> _roleManager = roleManager;

        // ── GET /api/users ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserDTO>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var claims = await _userManager.GetClaimsAsync(u);
                result.Add(new UserDTO
                {
                    Id = u.Id,
                    Username = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    RoleId = u.RoleId,
                    RoleName = roles.FirstOrDefault() ?? string.Empty,
                    Roles = roles.ToList(),
                    Claims = claims
                        .Select(c => new ClaimDTO { Type = c.Type, Value = c.Value })
                        .ToList()
                });
            }
            return Ok(result);
        }

        // ── GET /api/users/{id} ────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            return Ok(new UserDTO
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                RoleId = user.RoleId,
                RoleName = roles.FirstOrDefault() ?? string.Empty,
                Roles = roles.ToList(),
                Claims = claims
                    .Select(c => new ClaimDTO { Type = c.Type, Value = c.Value })
                    .ToList()
            });
        }

        // ── POST /api/users ────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            foreach (var role in model.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                    await _userManager.AddToRoleAsync(user, role);
            }
            if (!model.Roles.Any())
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _userManager.AddClaimAsync(user, new Claim("Department", "Sales"));
            }
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.UserName, user.Email });
        }

        // ── PUT /api/users/{id} ────────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDTO model)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── DELETE /api/users/{id} ─────────────────────────────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── GET /api/users/{id}/roles ──────────────────────────────────────────
        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetRoles(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        // ── POST /api/users/{id}/roles ─────────────────────────────────────────
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] string roleName)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest($"Role '{roleName}' does not exist.");
            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── DELETE /api/users/{id}/roles/{roleName} ────────────────────────────
        [HttpDelete("{id}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRole(int id, string roleName)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── GET /api/users/{id}/claims ─────────────────────────────────────────
        [HttpGet("{id}/claims")]
        public async Task<IActionResult> GetClaims(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var claims = await _userManager.GetClaimsAsync(user);
            return Ok(claims.Select(c => new ClaimDTO { Type = c.Type, Value = c.Value }));
        }

        // ── POST /api/users/{id}/claims ────────────────────────────────────────
        [HttpPost("{id}/claims")]
        [Authorize(Policy = "ManagePermissions")]
        public async Task<IActionResult> AddClaim(int id, [FromBody] ClaimDTO dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var result = await _userManager.AddClaimAsync(user, new Claim(dto.Type, dto.Value));
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── DELETE /api/users/{id}/claims/{type}/{value} ───────────────────────
        [HttpDelete("{id}/claims/{type}/{value}")]
        [Authorize(Policy = "ManagePermissions")]
        public async Task<IActionResult> RemoveClaim(int id, string type, string value)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();
            var claims = await _userManager.GetClaimsAsync(user);
            var claim = claims.FirstOrDefault(c => c.Type == type && c.Value == value);
            if (claim is null) return NotFound();
            var result = await _userManager.RemoveClaimAsync(user, claim);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }
    }
}

