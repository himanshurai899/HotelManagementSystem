using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.API.Controllers
{
    [Authorize(Roles = "Administrator,SuperAdmin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController(RoleManager<Role> roleManager) : ControllerBase
    {
        private readonly RoleManager<Role> _roleManager = roleManager;

        // ── GET /api/roles ─────────────────────────────────────────────────────
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoleDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var roles = _roleManager.Roles.ToList();
            var result = new List<RoleDTO>();
            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                result.Add(new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Permissions = claims
                        .Where(c => c.Type == "Permission")
                        .Select(c => c.Value)
                        .ToList()
                });
            }
            return Ok(result);
        }

        // ── GET /api/roles/{id} ────────────────────────────────────────────────
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoleDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            var claims = await _roleManager.GetClaimsAsync(role);
            return Ok(new RoleDTO
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Permissions = claims
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .ToList()
            });
        }

        // ── POST /api/roles ────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(RoleDTO), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name is required.");
            if (await _roleManager.RoleExistsAsync(roleName))
                return Conflict($"Role '{roleName}' already exists.");

            var role = new Role { Name = roleName };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetById), new { id = role.Id },
                new RoleDTO { Id = role.Id, Name = role.Name! });
        }

        // ── PUT /api/roles/{id} ────────────────────────────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("Role name is required.");
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            role.Name = newName;
            var result = await _roleManager.UpdateAsync(role);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── DELETE /api/roles/{id} ─────────────────────────────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── GET /api/roles/{id}/permissions ───────────────────────────────────
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetPermissions(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToList();
            return Ok(permissions);
        }

        // ── POST /api/roles/{id}/permissions ──────────────────────────────────
        [HttpPost("{id}/permissions")]
        [Authorize(Policy = "ManagePermissions")]
        public async Task<IActionResult> AddPermission(int id, [FromBody] string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return BadRequest("Permission value is required.");
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            var result = await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }

        // ── DELETE /api/roles/{id}/permissions/{permission} ───────────────────
        [HttpDelete("{id}/permissions/{permission}")]
        [Authorize(Policy = "ManagePermissions")]
        public async Task<IActionResult> RemovePermission(int id, string permission)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null) return NotFound();
            var claims = await _roleManager.GetClaimsAsync(role);
            var claim = claims.FirstOrDefault(c => c.Type == "Permission" && c.Value == permission);
            if (claim is null) return NotFound();
            var result = await _roleManager.RemoveClaimAsync(role, claim);
            return result.Succeeded ? NoContent() : BadRequest(result.Errors);
        }
    }
}
