using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomTypesController(IRepository<RoomType> repo, IMapper mapper) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var types = await repo.GetAllAsync();
            return Ok(mapper.Map<IEnumerable<RoomTypeDTO>>(types));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var type = await repo.GetByIdAsync(id);
            return type is null ? NotFound() : Ok(mapper.Map<RoomTypeDTO>(type));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] RoomTypeDTO dto)
        {
            var roomType = mapper.Map<RoomType>(dto);
            await repo.AddAsync(roomType);
            return CreatedAtAction(nameof(GetById), new { id = roomType.Id }, mapper.Map<RoomTypeDTO>(roomType));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomTypeDTO dto)
        {
            var roomType = await repo.GetByIdAsync(id);
            if (roomType is null) return NotFound();
            mapper.Map(dto, roomType);
            await repo.UpdateAsync(roomType);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await repo.GetByIdAsync(id) is null) return NotFound();
            await repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
