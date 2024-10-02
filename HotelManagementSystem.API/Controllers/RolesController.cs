using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController(IRepository<Role> roleRepository) : ControllerBase
    {
        private readonly IRepository<Role> _roleRepository = roleRepository;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            var roles = await _roleRepository.GetAllAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }
    }
}