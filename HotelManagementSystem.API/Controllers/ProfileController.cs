using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using HotelManagementSystem.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController(UserManager<User> userManager, IFileStorageService fileStorage) : ControllerBase
    {
        private readonly UserManager<User>   _userManager = userManager;
        private readonly IFileStorageService _fileStorage = fileStorage;

        /// <summary>Resolves the authenticated user's integer ID from the NameIdentifier claim.</summary>
        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // ── GET /api/profile ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
            if (user is null) return NotFound();

            var roles  = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            return Ok(ToDto(user, roles, claims));
        }

        // ── PUT /api/profile ──────────────────────────────────────────────────
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
            if (user is null) return NotFound();

            user.UserName    = model.UserName;
            user.FirstName   = model.FirstName;
            user.LastName    = model.LastName;
            user.Email       = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            var roles  = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            return Ok(ToDto(user, roles, claims));
        }

        // ── POST /api/profile/photo ───────────────────────────────────────────
        [HttpPost("photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto([FromForm] PhotoUploadRequest request)
        {
            var photo = request?.Photo;
            if (photo is null || photo.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (photo.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "File exceeds the 2 MB limit." });

            var allowed = new[] { "image/jpeg", "image/png" };
            if (!allowed.Contains(photo.ContentType.ToLowerInvariant()))
                return BadRequest(new { message = "Only JPG and PNG files are allowed." });

            var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
            if (user is null) return NotFound();

            // Delete the previous photo before saving the new one
            if (!string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
                await _fileStorage.DeleteAsync(user.ProfilePhotoUrl);

            await using var stream = photo.OpenReadStream();
            var url = await _fileStorage.SaveAsync(stream, photo.FileName, photo.ContentType);

            user.ProfilePhotoUrl = url;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { profilePhotoUrl = url });
        }

        // ── PUT /api/profile/change-password ─────────────────────────────────
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.NewPassword != model.ConfirmNewPassword)
                return BadRequest(new { message = "New password and confirmation do not match." });

            var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
            if (user is null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Password changed successfully." });
        }

        // ── Private helper ────────────────────────────────────────────────────
        private static UserDTO ToDto(User user, IList<string> roles, IList<Claim> claims) => new()
        {
            Id             = user.Id,
            Username       = user.UserName      ?? string.Empty,
            FirstName      = user.FirstName     ?? string.Empty,
            LastName       = user.LastName      ?? string.Empty,
            Email          = user.Email         ?? string.Empty,
            PhoneNumber    = user.PhoneNumber   ?? string.Empty,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            RoleId         = user.RoleId,
            RoleName       = roles.FirstOrDefault() ?? string.Empty,
            Roles          = roles.ToList(),
            Claims         = claims.Select(c => new ClaimDTO { Type = c.Type, Value = c.Value }).ToList()
        };
    }
}
