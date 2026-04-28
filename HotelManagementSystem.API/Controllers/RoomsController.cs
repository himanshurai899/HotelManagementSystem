using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController(IRepository<Room> roomRepository, IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Room> _roomRepository = roomRepository;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetRooms()
        {
            var rooms = await _roomRepository.GetAllWithIncludesAsync(r => r.RoomType);
            return Ok(_mapper.Map<IEnumerable<RoomDTO>>(rooms));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<RoomDTO>> GetRoom(int id)
        {
            var room = await _roomRepository.GetByIdWithIncludesAsync(id, r => r.RoomType);
            if (room == null) return NotFound();
            return Ok(_mapper.Map<RoomDTO>(room));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<ActionResult<RoomDTO>> CreateRoom([FromBody] RoomDTO dto)
        {
            var room = _mapper.Map<Room>(dto);
            room.RoomType = null!;
            room.Tenant = null!;
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                room.TenantId = tenantId;
            }
            await _roomRepository.AddAsync(room);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, _mapper.Map<RoomDTO>(room));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomDTO dto)
        {
            if (id != dto.Id) return BadRequest();

            // Fetch the existing tracked entity so EF does not attempt to INSERT
            // any navigation objects (Tenant, RoomType) that AutoMapper would produce
            // on a fresh mapping from a flat DTO.
            var room = await _roomRepository.GetByIdAsync(id);
            if (room is null) return NotFound();

            // Map scalar fields from the DTO onto the already-tracked entity.
            // Navigation properties are left as-is on the tracked object.
            _mapper.Map(dto, room);

            // Never let the DTO overwrite TenantId — it is set on creation and is immutable.
            // (dto.TenantId may be 0 or stale; leave what is already in the database.)

            await _roomRepository.UpdateAsync(room);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            await _roomRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
