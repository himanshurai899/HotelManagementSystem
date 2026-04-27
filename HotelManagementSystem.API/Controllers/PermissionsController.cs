using HotelManagementSystem.Shared.Constants;
using HotelManagementSystem.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public class PermissionsController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var result = Permissions.All
                .Select(p => new PermissionDTO { Name = p.Name, Description = p.Description })
                .ToList();
            return Ok(result);
        }
    }
}
